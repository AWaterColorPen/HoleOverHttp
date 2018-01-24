using System.Threading;
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

        public abstract void RegisterRemoteSocket(CancellationToken cancellationToken);
    }
}