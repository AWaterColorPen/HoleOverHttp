using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HoleOverHttp.Core
{
    public interface IProviderConnection
    {
        bool Secure { get; set; }

        string UriPattern { get; set; }

        Func<object, Task<object>> CallFunc { get; set; }

        Task ServeAsync(CancellationToken token);
    }
}