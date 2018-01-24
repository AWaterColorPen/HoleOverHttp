using System;
using System.Collections.Generic;
using System.Threading;
using HoleOverHttp.Core;
using HoleOverHttp.ReverseCall;
using Microsoft.Net.Http.Server;

namespace HoleOverHttp.Test.E2E
{
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