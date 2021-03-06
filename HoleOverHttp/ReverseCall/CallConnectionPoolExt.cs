﻿using System;
using System.Threading.Tasks;
using HoleOverHttp.Core;

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

        public static async Task<byte[]> ProvideAvailableMethodsAsync(this ICallConnectionPool connectionPool, string ns)
        {
            return await CallAsync(connectionPool, ns, string.Empty, new byte[0]);
        }

        public static Task Activated(this ICallConnectionPool connectionPool, Func<ICallConnection> connectionFactory)
        {
            return Task.Run(() =>
            {
                using var connection = connectionFactory();
                connection.WorkUntilDisconnect(connectionPool);
            });
        }
    }
}
