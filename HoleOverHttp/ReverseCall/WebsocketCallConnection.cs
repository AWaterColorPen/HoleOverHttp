using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using HoleOverHttp.Core;

namespace HoleOverHttp.ReverseCall
{
    internal class WebsocketCallConnection : ICallConnection, IDisposable
    {
        private const int SizeOfGuid = 16;
        private static readonly TimeSpan CallHandleTimeout = TimeSpan.FromMinutes(1);

        private readonly ConcurrentDictionary<Guid, CallTaskHandle> _callHandles =
            new ConcurrentDictionary<Guid, CallTaskHandle>();

        private readonly SemaphoreSlim _sem = new SemaphoreSlim(100, 100);

        private readonly WebSocket _socket;

        public WebsocketCallConnection(string ns, WebSocket socket)
        {
            Namespace = ns;
            _socket = socket;
        }

        public string Namespace { get; }

        public bool IsAlive => _socket.State == WebSocketState.Open;

        public async Task<byte[]> CallAsync(string method, byte[] param)
        {
            TryReleaseTimeoutHandles();
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

                await _socket.SendAsync(new ArraySegment<byte>(ms.ToArray()), WebSocketMessageType.Binary, true,
                    CancellationToken.None);

                return await handle.Source.Task;
            }
            finally
            {
                CleanupHandle(callid);
            }
        }

        public void Dispose()
        {
            _sem?.Dispose();
            //            _socket?.Dispose();
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
                    for (; ; )
                    {
                        var result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                        if (result.MessageType == WebSocketMessageType.Binary)
                        {
                            ms.Write(buffer, 0, result.Count);
                        }

                        if (result.EndOfMessage)
                        {
                            ms.Position = 0;

                            try
                            {
                                ConsumeOneMessage(ms);
                            }
                            catch
                            {
                                // TODO log
                            }
                            break;
                        }
                    }
                }

                TryReleaseTimeoutHandles();
            }
        }

        private void TryReleaseTimeoutHandles()
        {
            foreach (var callHandle in _callHandles)
            {
                if (callHandle.Value.StartTime + CallHandleTimeout > DateTime.Now)
                {
                    if (callHandle.Value.Source.TrySetCanceled())
                    {
                        CleanupHandle(callHandle.Key);
                    }
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
                handle.Source.SetResult(br.ReadBytes((int)message.Length));
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