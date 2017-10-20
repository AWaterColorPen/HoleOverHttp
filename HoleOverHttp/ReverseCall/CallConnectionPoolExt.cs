using System.Threading.Tasks;

namespace HoleOverHttp.ReverseCall
{
    public static class CallConnectionPoolExt
    {
        public static byte[] Call(this ICallConnectionPool connectionPool, string ns, string method, byte[] param)
        {
            return CallAsync(connectionPool, ns, method, param).Result;
        }

        public static async Task<byte[]> CallAsync(this ICallConnectionPool connectionPool, string ns, string method,
            byte[] param)
        {
            var connection = connectionPool.FindByNamespace(ns);

            if (connection == null)
            {
                return null;
            }

            return await connection.CallAsync(method, param);
        }
    }
}
