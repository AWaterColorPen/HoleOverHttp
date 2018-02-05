using System;
using System.Threading;

namespace HoleOverHttp.Test.E2E
{
    internal class FakeHttpService : IDisposable
    {
        private readonly WebListenerCallRegistry _webListenerCallRegistry;

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public FakeHttpService(WebListenerCallRegistry webListenerCallRegistry)
        {
            _webListenerCallRegistry = webListenerCallRegistry;
        }

        public void Start()
        {
            var unused = _webListenerCallRegistry.RegisterAsync(_cancellationTokenSource.Token);
        }
        
        public void Dispose()
        {
            _cancellationTokenSource?.Dispose();
        }
    }
}