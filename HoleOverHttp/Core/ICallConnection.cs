using System;
using System.Threading.Tasks;

namespace HoleOverHttp.Core
{
    public interface ICallConnection
    {
        string Namespace { get; }

        bool IsAlive { get; }

        TimeSpan TimeOutSetting { get; set; }

        Task<byte[]> CallAsync(string method, byte[] param);
    }
}
