using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HoleOverHttp.Core;

namespace HoleOverHttp.WsProvider
{
    public abstract class CallProvider
    {
        private readonly IList<IProviderConnection> _providerConnectionList = new List<IProviderConnection>();

        public async Task ServeAsync(CancellationToken token)
        {
            var task = _providerConnectionList.Select(providerConnection => providerConnection.ServeAsync(token));
            await Task.WhenAll(task);
        }

        public void RegisterConnection(IProviderConnection providerConnection)
        {
            providerConnection.CallFunc = ProcessCall;
            _providerConnectionList.Add(providerConnection);
        }

        public abstract void RegisterService(object service);

        public abstract Task<object> ProcessCall(object input);
    }
}
