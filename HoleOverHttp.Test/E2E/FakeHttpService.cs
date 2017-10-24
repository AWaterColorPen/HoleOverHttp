using System.Net;
using System.Threading.Tasks;
using HoleOverHttp.Core;

namespace HoleOverHttp.Test.E2E
{
    internal class FakeHttpService
    {
        private readonly HttpListener _listener = new HttpListener();

        private ICallConnectionPool _callConnectionPool;

        public FakeHttpService(ICallConnectionPool callConnectionPool)
        {
            _callConnectionPool = callConnectionPool;
        }

        public void Start(string[] prefixes)
        {
            _listener.Prefixes.Clear();
            foreach (var prefix in prefixes)
            {
                _listener.Prefixes.Add(prefix);
            }

            Task.Run(() =>
            {
                while (_listener.IsListening)
                {
                    //try
                    //{
                    //    var context = _listener.GetContext();
                    //    if (context.Request.IsWebSocketRequest)
                    //    {
                    //        Task.Run(async () =>
                    //        {
                    //            var socket = await context.AcceptWebSocketAsync(null)
                    //            var ns = Request.Query["ns"].FirstOrDefault();

                    //            if (string.IsNullOrWhiteSpace(ns))
                    //            {
                    //                // random
                    //                ns = Guid.NewGuid().ToString();
                    //            }

                    //            using (var connection = new WebsocketCallConnection(ns, socket))
                    //            {
                    //                _callConnectionPool.Register(connection);

                    //                try
                    //                {
                    //                    await connection.WorkUntilDisconnect();
                    //                }
                    //                catch (Exception e)
                    //                {
                    //                    // TODO ?? cannot close

                    //                    await socket.CloseOutputAsync(WebSocketCloseStatus.InternalServerError,
                    //                        e.ToString(), CancellationToken.None);
                    //                    await socket.CloseAsync(WebSocketCloseStatus.InternalServerError, e.ToString(),
                    //                        CancellationToken.None);
                    //                    socket.Abort();
                    //                    socket.Dispose();
                    //                }
                    //                finally
                    //                {
                    //                    _callConnectionPool.UnRegister(connection);
                    //                }
                    //            }
                    //        });
                    //    }

                    //}
                    //catch
                    //{
                    //    // Ignored
                    //}
                }
            });
        }

        public void Stop()
        {
            _listener?.Stop();
            _listener?.Close();
        }
    }
}