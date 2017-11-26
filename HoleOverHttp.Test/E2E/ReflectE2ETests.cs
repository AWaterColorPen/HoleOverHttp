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
    public class ReflectE2ETests
    {
        private static IContainer _container;

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            ThreadPool.SetMaxThreads(1024, 1024);
            ThreadPool.SetMinThreads(1024, 1024);

            var builder = new ContainerBuilder();
            builder.RegisterType<ReusableCallConnectionPool>().As<ICallConnectionPool>().SingleInstance();
            builder.RegisterType<FakeHttpService>().AsSelf().SingleInstance();

            builder.RegisterType<DummyAuthorizationProvider>().As<IAuthorizationProvider>().SingleInstance();
            builder.RegisterType<ReflectCallProviderConnection>().AsSelf();
            _container = builder.Build();

            using (var scope = _container.BeginLifetimeScope())
            {
                var fakeHttpService = scope.Resolve<FakeHttpService>();
                fakeHttpService.Start(new[] { "http://localhost:23333/ws/" });
            }

            Thread.Sleep(TimeSpan.FromSeconds(1));
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
        public void TestReflectE2E_RemoteRegister()
        {
            var tokenSource = new CancellationTokenSource();
            using (var scope = _container.BeginLifetimeScope())
            {
                var callProvider =
                    scope.Resolve<ReflectCallProviderConnection>(
                        new NamedParameter("host", "localhost:23333"),
                        new NamedParameter("namespace", "ns"));

                callProvider.Secure = false;
                callProvider.RegisterService(new ReflectCallProviderObject());
                Task.Run(async () =>
                {
                    await callProvider.ServeAsync(tokenSource.Token);
                }, CancellationToken.None);

                Thread.Sleep(TimeSpan.FromSeconds(1));

                var callConnectionPool = scope.Resolve<ICallConnectionPool>();
                var namespaces1 = callConnectionPool.AllNamespaces.ToList();
                Assert.AreEqual(1, namespaces1.Count);
                Assert.AreEqual("ns", namespaces1[0].Item1);
                Assert.AreEqual(1, namespaces1[0].Item2);

                tokenSource.Cancel();

                Thread.Sleep(TimeSpan.FromSeconds(1));

                var namespaces2 = callConnectionPool.AllNamespaces.ToList();
                Assert.AreEqual(0, namespaces2.Count);
            }
        }

        [TestMethod]
        public void TestReflectE2E_OneCall()
        {
            var tokenSource = new CancellationTokenSource();
            using (var scope = _container.BeginLifetimeScope())
            {
                var callProvider =
                    scope.Resolve<ReflectCallProviderConnection>(
                        new NamedParameter("host", "localhost:23333"),
                        new NamedParameter("namespace", "ns"));

                callProvider.Secure = false;
                callProvider.RegisterService(new ReflectCallProviderObject());
                Task.Run(async () =>
                {
                    await callProvider.ServeAsync(tokenSource.Token);
                }, CancellationToken.None);

                Thread.Sleep(TimeSpan.FromSeconds(1));

                var callConnectionPool = scope.Resolve<ICallConnectionPool>();
                var result = callConnectionPool.CallAsync("ns", "NullableParameterMethod",
                    Encoding.UTF8.GetBytes("{p1:0,p2:0}")).Result;
                var jobject = JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(result));
                Assert.AreEqual(2, jobject.Count);
                Assert.IsTrue((bool) jobject["result"]);
                Assert.IsTrue((int) jobject["latency"] >= 0);

                tokenSource.Cancel();
            }
        }

        [TestMethod]
        public void TestReflectE2E_MultiCall()
        {
            var tokenSource = new CancellationTokenSource();
            using (var scope = _container.BeginLifetimeScope())
            {
                var callProvider =
                    scope.Resolve<ReflectCallProviderConnection>(
                        new NamedParameter("host", "localhost:23333"),
                        new NamedParameter("namespace", "ns"));

                callProvider.Secure = false;
                callProvider.RegisterService(new ReflectCallProviderObject());
                Task.Run(async () =>
                {
                    await callProvider.ServeAsync(tokenSource.Token);
                }, CancellationToken.None);

                Thread.Sleep(TimeSpan.FromSeconds(1));

                var callConnectionPool = scope.Resolve<ICallConnectionPool>();

                // case 1:
                {
                    Parallel.ForEach(Enumerable.Range(0, 10), new ParallelOptions { MaxDegreeOfParallelism = 12 }, i =>
                    {
                        var result = callConnectionPool.CallAsync("ns", "NullableParameterMethod",
                            Encoding.UTF8.GetBytes("{p1:0,p2:0}")).Result;

                        var jobject = JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(result));

                        Assert.AreEqual(2, jobject.Count);
                        Assert.IsTrue((bool) jobject["result"]);
                        Assert.IsTrue((int) jobject["latency"] >= 0);
                    });
                }

                // case 2:
                {
                    var stopwatch = Stopwatch.StartNew();
                    Parallel.ForEach(Enumerable.Range(0, 10), new ParallelOptions { MaxDegreeOfParallelism = 12 }, i =>
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
        }

        [TestMethod]
        public void TestReflectE2E_TimeOutCall()
        {
            var tokenSource = new CancellationTokenSource();
            using (var scope = _container.BeginLifetimeScope())
            {
                var callProvider =
                    scope.Resolve<ReflectCallProviderConnection>(
                        new NamedParameter("host", "localhost:23333"),
                        new NamedParameter("namespace", "ns"));

                callProvider.Secure = false;
                callProvider.RegisterService(new ReflectCallProviderObject());
                Task.Run(async () =>
                {
                    await callProvider.ServeAsync(tokenSource.Token);
                }, CancellationToken.None);

                Thread.Sleep(TimeSpan.FromSeconds(1));

                var callConnectionPool = scope.Resolve<ICallConnectionPool>();

                Assert.ThrowsException<AggregateException>(() =>
                    callConnectionPool.CallAsync("ns", "TimeOutMethod", Encoding.UTF8.GetBytes("{sleepTime:6000}"))
                        .Result);

                tokenSource.Cancel();
            }
        }

        [TestMethod]
        public void TestReflectE2E_ProvideAvailableMethods()
        {
            var tokenSource = new CancellationTokenSource();
            using (var scope = _container.BeginLifetimeScope())
            {
                var callProvider =
                    scope.Resolve<ReflectCallProviderConnection>(
                        new NamedParameter("host", "localhost:23333"),
                        new NamedParameter("namespace", "ns"));

                callProvider.Secure = false;
                callProvider.RegisterService(new ReflectCallProviderObject());
                Task.Run(async () =>
                {
                    await callProvider.ServeAsync(tokenSource.Token);
                }, CancellationToken.None);

                Thread.Sleep(TimeSpan.FromSeconds(1));

                var callConnectionPool = scope.Resolve<ICallConnectionPool>();
                var result = callConnectionPool.ProvideAvailableMethodsAsync("ns").Result;

                var jobject = JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(result));
                Assert.AreEqual(2, jobject.Count);
                Assert.IsTrue((int) jobject["latency"] >= 0);

                // ReflectCallProviderObject class has 7 methods.
                Assert.IsTrue(jobject["result"].Count() >= 7);

                // ReflectCallProviderConnection.MethodDefinition class has 4 properties.
                Assert.AreEqual(4, jobject["result"][0].Count());

                // check type sample for custom class argument.
                var mixedParameterMethod = jobject["result"].First(v => v["MethodName"].Value<string>() == "MixedParameterMethod");
                Assert.IsTrue(mixedParameterMethod.Count() == 4);
                // mixedParameterMethod.
                Assert.AreEqual(3, mixedParameterMethod["Arguments"].Count());
                Assert.AreEqual(2, mixedParameterMethod["Arguments"]["p3"].Count());
                Assert.AreEqual("HoleOverHttp.Test.WsProvider.DummyClass", mixedParameterMethod["Arguments"]["p3"]["Type"].Value<string>());
                Assert.AreEqual(2, mixedParameterMethod["Arguments"]["p3"]["Sample"].Count());
                Assert.AreEqual(false, (bool) mixedParameterMethod["Arguments"]["p3"]["Sample"]["P1"]);
                Assert.AreEqual(false, (bool )mixedParameterMethod["Arguments"]["p3"]["Sample"]["P2"]);
                tokenSource.Cancel();
            }
        }

        [TestMethod]
        public void TestReflectE2E_DummyAuthorizationProvider()
        {
            var dummyAuthorizationProvider = new DummyAuthorizationProvider();
            Assert.AreEqual("Key", dummyAuthorizationProvider.Key);
            Assert.AreEqual("Value", dummyAuthorizationProvider.Value);
        }

        private class DummyAuthorizationProvider : IAuthorizationProvider
        {
            public string Key { get; } = "Key";
            public string Value { get; } = "Value";
        }
    }
}