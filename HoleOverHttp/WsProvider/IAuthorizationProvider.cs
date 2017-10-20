using System;
using System.Collections.Generic;
using System.Text;

namespace HoleOverHttp.WsProvider
{
    public interface IAuthorizationProvider
    {
        string Key { get; }
        string Value { get; }
    }
}
