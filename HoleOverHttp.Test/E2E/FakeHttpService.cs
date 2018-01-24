using System;
using System.Collections.Generic;
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

        public void Start(IEnumerable<string> prefixes)
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
                                using (var connection =
                                    new WebsocketCallConnection("ns", socket)
                                    {
                                        TimeOutSetting = TimeSpan.FromSeconds(5)
                                    })
                                {
                                    connection.WorkUntilDisconnect(_callConnectionPool);
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

    public class WebListenerCallRegistry : CallRegistry
    {
        private readonly WebListenerSettings _settings = new WebListenerSettings();

        public WebListenerCallRegistry(ICallConnectionPool callConnectionPool, IEnumerable<string> prefixes)
            : base(callConnectionPool)
        {
            foreach (var prefix in prefixes)
            {
                _settings.UrlPrefixes.Add(prefix);
            }
        }

        public override void RegisterRemoteSocket(CancellationToken cancellationToken)
        {
            using (var listener = new WebListener(_settings))
            {
                listener.Start();
                while (!cancellationToken.IsCancellationRequested)
                {
                    var context = listener.AcceptAsync().Result;
                    if (context.IsWebSocketRequest)
                    {
                        CallConnectionPool.Activated(() =>
                        {
                            var socket = context.AcceptWebSocketAsync().Result;
                            return new WebsocketCallConnection("ns", socket)
                            {
                                TimeOutSetting = TimeSpan.FromSeconds(5)
                            };
                        });
                    }
                }
            }
        }
    }
}