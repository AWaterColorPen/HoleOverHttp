﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace HoleOverHttp.ReverseCall
{
    public class ReusableCallConnectionPool : ICallConnectionPool
    {
        private uint _roundRobinCounter;

        private readonly ConcurrentDictionary<string, ICallConnection[]> _pool =
            new ConcurrentDictionary<string, ICallConnection[]>();

        public void Register(ICallConnection connection)
        {
            _pool.AddOrUpdate(connection.Namespace, new[] { connection },
                (s, connections) => connections.Concat(new[] { connection }).ToArray());
        }

        public void UnRegister(ICallConnection connection)
        {
            var toremove = new[] { connection };
            while (true)
            {
                if (!_pool.TryGetValue(connection.Namespace, out var connections))
                {
                    return;
                }

                var newconnections = connections.Except(toremove).ToArray();

                if (newconnections.Length == 0)
                {
                    if (_pool.TryRemove(connection.Namespace, out var removed))
                    {
                        foreach (var callConnection in removed.Except(toremove))
                        {
                            Register(callConnection);
                        }

                        return;
                    }

                    continue;
                }

                if (_pool.TryUpdate(connection.Namespace, newconnections, connections))
                {
                    return;
                }
            }
        }

        public ICallConnection FindByNamespace(string ns)
        {
            if (_pool.TryGetValue(ns, out var connections))
            {
                return RoundRobin(connections.Where(c => c.IsAlive).ToArray());
            }

            return null;
        }

        public IEnumerable<string> AllNamespaces => _pool.Keys;

        private ICallConnection RoundRobin(ICallConnection[] connections)
        {
            if (connections.Length == 0)
            {
                return null;
            }

            return connections[_roundRobinCounter++ % connections.Length];
        }
    }
}