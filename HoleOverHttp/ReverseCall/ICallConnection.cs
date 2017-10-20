using System.Threading.Tasks;

namespace HoleOverHttp.ReverseCall
{
    public interface ICallConnection
    {
        string Namespace { get; }

        bool IsAlive { get; }

        Task<byte[]> CallAsync(string method, byte[] param);
    }
}
