using System;
using System.Threading.Tasks;
using HoleOverHttp.Core;
using HoleOverHttp.ReverseCall;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HoleOverHttp.Test.ReverseCall
{
    [TestClass]
    public class ReusableCallConnectionPoolTests
    {
        [TestMethod]
        public void TestReusableCallConnectionPool_AddOne()
        {
            var pool = new ReusableCallConnectionPool();

            var dummyConnection = new DummyConnection("1");
            pool.Register(dummyConnection);

            Assert.AreEqual(dummyConnection, pool.FindByNamespace("1"));
        }

        [TestMethod]
        public void TestReusableCallConnectionPool_AddTwo()
        {
            var pool = new ReusableCallConnectionPool();

            var dummyConnection1 = new DummyConnection("ns");
            var dummyConnection2 = new DummyConnection("ns");
            pool.Register(dummyConnection1);
            pool.Register(dummyConnection2);

            Assert.AreEqual(dummyConnection1, pool.FindByNamespace("ns"));
            Assert.AreEqual(dummyConnection2, pool.FindByNamespace("ns"));
        }

        [TestMethod]
        public void TestReusableCallConnectionPool_RemoveOne()
        {
            var pool = new ReusableCallConnectionPool();

            var dummyConnection1 = new DummyConnection("ns");
            var dummyConnection2 = new DummyConnection("ns");
            pool.Register(dummyConnection1);
            pool.Register(dummyConnection2);

            Assert.AreEqual(dummyConnection1, pool.FindByNamespace("ns"));
            Assert.AreEqual(dummyConnection2, pool.FindByNamespace("ns"));

            pool.UnRegister(dummyConnection1);
            Assert.AreEqual(dummyConnection2, pool.FindByNamespace("ns"));
        }

        [TestMethod]
        public void TestReusableCallConnectionPool_RemoveAll()
        {
            var pool = new ReusableCallConnectionPool();

            var dummyConnection1 = new DummyConnection("ns");
            var dummyConnection2 = new DummyConnection("ns");
            pool.Register(dummyConnection1);
            pool.Register(dummyConnection2);

            Assert.AreEqual(dummyConnection1, pool.FindByNamespace("ns"));
            Assert.AreEqual(dummyConnection2, pool.FindByNamespace("ns"));

            pool.UnRegister(dummyConnection1);
            pool.UnRegister(dummyConnection2);
            Assert.AreEqual(null, pool.FindByNamespace("ns"));
        }
        [TestMethod]
        public void TestReusableCallConnectionPool_IgnoreNotAlive()
        {
            var pool = new ReusableCallConnectionPool();

            var dummyConnection1 = new DummyConnection("ns");
            var dummyConnection2 = new DummyConnection("ns");
            pool.Register(dummyConnection1);
            pool.Register(dummyConnection2);

            dummyConnection1.IsAlive = false;

            Assert.AreEqual(dummyConnection2, pool.FindByNamespace("ns"));
        }

        private class DummyConnection : ICallConnection
        {
            public DummyConnection(string ns)
            {
                Namespace = ns;
            }

            public string Namespace { get; }
            public bool IsAlive { get; set; } = true;
            public TimeSpan TimeOutSetting { get; set; }

            public Task<byte[]> CallAsync(string method, byte[] param)
            {
                throw new NotImplementedException();
            }
        }
    }
}