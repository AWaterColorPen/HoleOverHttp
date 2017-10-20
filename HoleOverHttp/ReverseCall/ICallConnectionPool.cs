using System.Collections.Generic;

namespace HoleOverHttp.ReverseCall
{
    public interface ICallConnectionPool
    {
        IEnumerable<string> AllNamespaces { get; }

        void Register(ICallConnection connection);

        void UnRegister(ICallConnection connection);

        ICallConnection FindByNamespace(string ns);
    }
}