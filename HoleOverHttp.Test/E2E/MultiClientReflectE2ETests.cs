using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using HoleOverHttp.Core;
using HoleOverHttp.ReverseCall;
using HoleOverHttp.Test.WsProvider;
using HoleOverHttp.WsProvider;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HoleOverHttp.Test.E2E
{

    [TestClass]
    public class MultiClientTests
    {
        private static IContainer _container;

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            ThreadPool.SetMinThreads(1000, 1000);

            var builder = new ContainerBuilder();
            builder.RegisterType<ReusableCallConnectionPool>().As<ICallConnectionPool>().SingleInstance();
            builder.RegisterType<WebListenerCallRegistry>().AsSelf();
            builder.RegisterType<FakeHttpService>().AsSelf();

            builder.RegisterType<DummyAuthorizationProvider>().As<IAuthorizationProvider>().SingleInstance();
            builder.RegisterType<ReflectCallProvider>().AsSelf().SingleInstance();
            builder.RegisterType<WebSocketProviderConnection>().As<IProviderConnection>();
            _container = builder.Build();

            using (var scope = _container.BeginLifetimeScope())
            {
                var webListenerCallRegistry1 = scope.Resolve<WebListenerCallRegistry>(new NamedParameter("prefixes", new[] { "http://localhost:23331/ws/" }));
                var webListenerCallRegistry2 = scope.Resolve<WebListenerCallRegistry>(new NamedParameter("prefixes", new[] { "http://localhost:23332/ws/" }));
                var fakeHttpService1 = scope.Resolve<FakeHttpService>(new NamedParameter("webListenerCallRegistry", webListenerCallRegistry1));
                var fakeHttpService2 = scope.Resolve<FakeHttpService>(new NamedParameter("webListenerCallRegistry", webListenerCallRegistry2));
                fakeHttpService1.Start();
                fakeHttpService2.Start();
                
                var providerConnection1 = scope.Resolve<IProviderConnection>(new NamedParameter("host", "localhost:23331"), new NamedParameter("namespace", "ns"));
                var providerConnection2 = scope.Resolve<IProviderConnection>(new NamedParameter("host", "localhost:23332"), new NamedParameter("namespace", "ns"));
                providerConnection1.Secure = false;
                providerConnection2.Secure = false;

                var callProvider = scope.Resolve<ReflectCallProvider>();
                callProvider.RegisterConnection(providerConnection1);
                callProvider.RegisterConnection(providerConnection2);
                callProvider.RegisterService(new ReflectCallProviderObject());
            }

            Thread.Sleep(TimeSpan.FromSeconds(2));
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
        }

        [TestInitialize]
        public void TestInitialize()
        {
        }

        [TestCleanup]
        public void TestCleanup()
        {
        }

        [TestMethod]
        public void TestMultiClient_RemoteRegister()
        {
            var tokenSource = new CancellationTokenSource();
            using var scope = _container.BeginLifetimeScope();
            var callProvider = scope.Resolve<ReflectCallProvider>();
            Task.Run(async () =>
            {
                await callProvider.ServeAsync(tokenSource.Token);
            }, CancellationToken.None);

            Thread.Sleep(TimeSpan.FromSeconds(2));

            var callConnectionPool = scope.Resolve<ICallConnectionPool>();
            var namespaces1 = callConnectionPool.AllNamespaces.ToList();
            Assert.AreEqual(1, namespaces1.Count);
            Assert.AreEqual("ns", namespaces1[0].Item1);
            Assert.AreEqual(2, namespaces1[0].Item2);

            tokenSource.Cancel();

            Thread.Sleep(TimeSpan.FromSeconds(1));

            var namespaces2 = callConnectionPool.AllNamespaces.ToList();
            Assert.AreEqual(0, namespaces2.Count);
        }
        
        [TestMethod]
        public void TestMultiClient_MultiCall()
        {
            var tokenSource = new CancellationTokenSource();
            using var scope = _container.BeginLifetimeScope();
            var callProvider = scope.Resolve<ReflectCallProvider>();
            Task.Run(async () =>
            {
                await callProvider.ServeAsync(tokenSource.Token);
            }, CancellationToken.None);

            Thread.Sleep(TimeSpan.FromSeconds(1));

            var callConnectionPool = scope.Resolve<ICallConnectionPool>();

            // case 1:
            {
                Parallel.ForEach(Enumerable.Range(0, 10), i =>
                {
                    var result = callConnectionPool.CallAsync("ns", "NullableParameterMethod",
                        Encoding.UTF8.GetBytes("{p1:0,p2:0}")).Result;

                    var jobject = JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(result));

                    Assert.AreEqual(2, jobject.Count);
                    Assert.IsTrue((bool)jobject["result"]);
                    Assert.IsTrue((int)jobject["latency"] >= 0);
                });
            }

            // case 2:
            {
                var stopwatch = Stopwatch.StartNew();
                Parallel.ForEach(Enumerable.Range(0, 10), i =>
                {
                    var result = callConnectionPool.CallAsync("ns", "TimeOutMethod",
                        Encoding.UTF8.GetBytes($"{{sleepTime:1000,uid:{i}}}")).Result;

                    var jobject = JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(result));

                    Assert.AreEqual(2, jobject.Count);
                    Assert.AreEqual(i, (int)jobject["result"]);
                    Assert.IsTrue((int)jobject["latency"] >= 1000);
                });
                stopwatch.Stop();
                Assert.IsTrue(stopwatch.ElapsedMilliseconds < 3000);
            }

            tokenSource.Cancel();
        }

        [TestMethod]
        public void TestMultiClient_ProvideAvailableMethods()
        {
            var tokenSource = new CancellationTokenSource();
            using var scope = _container.BeginLifetimeScope();
            var callProvider = scope.Resolve<ReflectCallProvider>();
            Task.Run(async () =>
            {
                await callProvider.ServeAsync(tokenSource.Token);
            }, CancellationToken.None);

            Thread.Sleep(TimeSpan.FromSeconds(1));

            var callConnectionPool = scope.Resolve<ICallConnectionPool>();
            var result = callConnectionPool.ProvideAvailableMethodsAsync("ns").Result;

            var jobject = JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(result));
            Assert.AreEqual(2, jobject.Count);
            Assert.IsTrue((int)jobject["latency"] >= 0);

            // ReflectCallProviderObject class has 7 methods.
            Assert.IsTrue(jobject["result"].Count() >= 7);

            // ReflectCallProvider.MethodDefinition class has 4 properties.
            Assert.AreEqual(4, jobject["result"][0].Count());

            // check type sample for custom class argument.
            var mixedParameterMethod = jobject["result"].First(v => v["MethodName"].Value<string>() == "MixedParameterMethod");
            Assert.IsTrue(mixedParameterMethod.Count() == 4);
            // mixedParameterMethod.
            Assert.AreEqual(3, mixedParameterMethod["Arguments"].Count());
            Assert.AreEqual(2, mixedParameterMethod["Arguments"]["p3"].Count());
            Assert.AreEqual("HoleOverHttp.Test.WsProvider.DummyClass", mixedParameterMethod["Arguments"]["p3"]["Type"].Value<string>());
            Assert.AreEqual(2, mixedParameterMethod["Arguments"]["p3"]["Sample"].Count());
            Assert.AreEqual(false, (bool)mixedParameterMethod["Arguments"]["p3"]["Sample"]["P1"]);
            Assert.AreEqual(false, (bool)mixedParameterMethod["Arguments"]["p3"]["Sample"]["P2"]);
            tokenSource.Cancel();
        }
    }
}