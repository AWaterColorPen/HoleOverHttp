using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using HoleOverHttp.Core;
using HoleOverHttp.ReverseCall;
using Microsoft.Net.Http.Server;

namespace HoleOverHttp.Test.E2E
{
    internal class FakeHttpService : IDisposable
    {
        private readonly ICallConnectionPool _callConnectionPool;

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public FakeHttpService(ICallConnectionPool callConnectionPool)
        {
            _callConnectionPool = callConnectionPool;
        }

        public void Start(string[] prefixes)
        {
            var settings = new WebListenerSettings();
            foreach (var prefix in prefixes)
            {
                settings.UrlPrefixes.Add(prefix);
            }

            Task.Run(() =>
            {
                using (var listener = new WebListener(settings))
                {
                    listener.Start();
                    while (!_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        var context = listener.AcceptAsync().Result;
                        if (context.IsWebSocketRequest)
                        {
                            Task.Run(() =>
                            {
                                var socket = context.AcceptWebSocketAsync().Result;
                                using (var connection = new WebsocketCallConnection("ns", socket))
                                {
                                    _callConnectionPool.Register(connection);

                                    try
                                    {
                                        connection.WorkUntilDisconnect().Wait();
                                    }
                                    catch (Exception e)
                                    {
                                        socket.CloseOutputAsync(WebSocketCloseStatus.InternalServerError, e.ToString(),
                                            CancellationToken.None).Wait();
                                        socket.CloseAsync(WebSocketCloseStatus.InternalServerError, e.ToString(),
                                            CancellationToken.None).Wait();
                                        socket.Abort();
                                        socket.Dispose();
                                    }
                                    finally
                                    {
                                        _callConnectionPool.UnRegister(connection);
                                    }
                                }
                            });
                        }
                    }
                }
            });
        }



        public void Dispose()
        {
            _cancellationTokenSource?.Dispose();
        }
    }
}