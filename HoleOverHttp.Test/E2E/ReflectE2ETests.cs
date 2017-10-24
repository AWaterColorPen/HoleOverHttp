using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using HoleOverHttp.Core;
using HoleOverHttp.ReverseCall;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HoleOverHttp.Test.E2E
{

    [TestClass]
    public class ReflectE2ETests
    {
        private IContainer _container;

        [TestInitialize]
        public void TestInitialize()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<ReusableCallConnectionPool>().As<ICallConnectionPool>().SingleInstance();
            var listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:23333/ws/register");
            listener.Start();
            _container = builder.Build();
        }

        [TestCleanup]
        public void TestCleanup()
        {
        }

        [TestMethod]
        public void TestReflectE2E_AddOne()
        {
            using (var scope = _container.BeginLifetimeScope())
            {
                var callConnectionPool = scope.Resolve<ICallConnectionPool>();
                var listener = new HttpListener();
                listener
                // new HttpServer();

            }
        }
    }

    internal class FakeHttpService
    {
        private HttpListener _listener = new HttpListener();

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
                    try
                    {
                        var context = _listener.GetContext();
                        if (context.Request.IsWebSocketRequest)
                        {
                            Task.Run(async () =>
                            {
                                var socket = await context.AcceptWebSocketAsync(null)
                                var ns = Request.Query["ns"].FirstOrDefault();

                                if (string.IsNullOrWhiteSpace(ns))
                                {
                                    // random
                                    ns = Guid.NewGuid().ToString();
                                }

                                using (var connection = new WebsocketCallConnection(ns, socket))
                                {
                                    _callConnectionPool.Register(connection);

                                    try
                                    {
                                        await connection.WorkUntilDisconnect();
                                    }
                                    catch (Exception e)
                                    {
                                        // TODO ?? cannot close

                                        await socket.CloseOutputAsync(WebSocketCloseStatus.InternalServerError,
                                            e.ToString(), CancellationToken.None);
                                        await socket.CloseAsync(WebSocketCloseStatus.InternalServerError, e.ToString(),
                                            CancellationToken.None);
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
                    catch
                    {
                        // Ignored
                    }
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