using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HoleOverHttp.Core;
using Serilog;

namespace HoleOverHttp.ReverseCall
{
    public class WebsocketCallConnection : ICallConnection, IDisposable
    {
        private static readonly int SizeOfGuid = Guid.Empty.ToByteArray().Length;

        private readonly ConcurrentDictionary<Guid, CallTaskHandle> _callHandles =
            new ConcurrentDictionary<Guid, CallTaskHandle>();

        private readonly SemaphoreSlim _sem;

        private readonly object _locksend = new object();

        private readonly WebSocket _socket;

        public WebsocketCallConnection(string ns, WebSocket socket)
            : this(ns, socket, 10)
        {
        }

        public WebsocketCallConnection(string ns, WebSocket socket, int maxDegreeOfParallelism)
        {
            Namespace = ns;
            _socket = socket;
            _sem = new SemaphoreSlim(maxDegreeOfParallelism, maxDegreeOfParallelism);
        }

        public string Namespace { get; }

        public bool IsAlive => _socket.State == WebSocketState.Open;

        public TimeSpan TimeOutSetting { get; set; } = TimeSpan.FromMinutes(1);

        public async Task<byte[]> CallAsync(string method, byte[] param)
        {
            await _sem.WaitAsync();
            var callid = Guid.NewGuid();
            var handle = _callHandles.GetOrAdd(callid, new CallTaskHandle());

            try
            {
                var ms = new MemoryStream();
                var writer = new BinaryWriter(ms);

                writer.Write(callid.ToByteArray());
                writer.Write(method);
                writer.Write(param);

                lock (_locksend)
                {
                    _socket.SendAsync(new ArraySegment<byte>(ms.ToArray()), WebSocketMessageType.Binary, true,
                        CancellationToken.None).Wait();
                }

                await Task.WhenAny(handle.Source.Task, Task.Delay(TimeOutSetting));
                if (handle.Source.Task.IsCompleted)
                {
                    return await handle.Source.Task;
                }

                TryReleaseTimeoutHandles();
                throw new TimeoutException(
                    $"callid:{callid} method:{method} param:{Encoding.UTF8.GetString(param)} " +
                    $"TimeOutSetting:{TimeOutSetting} " +
                    "Timeout hit.");
            }
            finally
            {
                CleanupHandle(callid);
            }
        }

        public void Dispose()
        {
            _sem?.Dispose();
            _socket?.Dispose();
        }

        private void CleanupHandle(Guid callid)
        {
            if (_callHandles.TryRemove(callid, out _))
            {
                _sem.Release();
            }
        }
        
        public async Task WorkUntilDisconnect()
        {
            var buffer = new byte[4096];
            while (IsAlive)
            {
                using (var ms = new MemoryStream())
                {
                    while (true)
                    {
                        var result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                        if (result.MessageType == WebSocketMessageType.Binary)
                        {
                            ms.Write(buffer, 0, result.Count);
                        }

                        if (!result.EndOfMessage) continue;
                        ms.Position = 0;

                        try
                        {
                            ConsumeOneMessage(ms);
                        }
                        catch (Exception e)
                        {
                            Log.Error(e, "");
                        }
                        break;
                    }
                }
            }
        }

        private void TryReleaseTimeoutHandles()
        {
            foreach (var callHandle in _callHandles)
            {
                if (callHandle.Value.StartTime + TimeOutSetting >= DateTime.Now) continue;
                if (callHandle.Value.Source.TrySetCanceled())
                {
                    CleanupHandle(callHandle.Key);
                }
            }
        }

        private void ConsumeOneMessage(Stream message)
        {
            if (message.Length < SizeOfGuid)
            {
                return;
            }

            var br = new BinaryReader(message);

            var callid = new Guid(br.ReadBytes(SizeOfGuid));

            if (_callHandles.TryGetValue(callid, out var handle))
            {
                handle.Source.SetResult(br.ReadBytes((int) message.Length));
            }

            CleanupHandle(callid);
        }

        private class CallTaskHandle
        {
            public DateTime StartTime { get; } = DateTime.Now;
            public TaskCompletionSource<byte[]> Source { get; } = new TaskCompletionSource<byte[]>();
        }
    }
}