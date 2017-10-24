using System.Net;
using Autofac;
using HoleOverHttp.Core;
using HoleOverHttp.ReverseCall;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HoleOverHttp.Test.E2E
{

    [TestClass]
    public class ReflectE2ETests
    {
        private IContainer _container;

        [TestInitialize]
        public void TestInitialize()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<ReusableCallConnectionPool>().As<ICallConnectionPool>().SingleInstance();
            var listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:23333/ws/register");
            listener.Start();
            _container = builder.Build();
        }

        [TestCleanup]
        public void TestCleanup()
        {
        }

        [TestMethod]
        public void TestReflectE2E_AddOne()
        {
            using (var scope = _container.BeginLifetimeScope())
            {
                var callConnectionPool = scope.Resolve<ICallConnectionPool>();
                var listener = new HttpListener();
                // listener
                // new HttpServer();

            }
        }
    }
}