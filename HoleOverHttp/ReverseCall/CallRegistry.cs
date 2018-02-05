using System.Threading;
using System.Threading.Tasks;
using HoleOverHttp.Core;

namespace HoleOverHttp.ReverseCall
{
    public abstract class CallRegistry
    {
        protected readonly ICallConnectionPool CallConnectionPool;

        protected CallRegistry(ICallConnectionPool callConnectionPool)
        {
            CallConnectionPool = callConnectionPool;
        }

        public abstract Task RegisterAsync(CancellationToken cancellationToken);
    }
}