using System;
using System.Collections.Generic;

namespace HoleOverHttp.Core
{
    public interface ICallConnectionPool
    {
        IEnumerable<Tuple<string, int>> AllNamespaces { get; }

        void Register(ICallConnection connection);

        void UnRegister(ICallConnection connection);

        ICallConnection FindByNamespace(string ns);
    }
}