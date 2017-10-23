using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HoleOverHttp.Core;
using HoleOverHttp.WsProvider;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace HoleOverHttp.Test.WsProvider
{
    [TestClass]
    public class ReflectCallProviderConnectionTests
    {
        private ReflectCallProviderObject _target;

        private Dictionary<string, MethodInfo> _methods;

        private ReflectCallProviderConnection _reflectCallProviderConnection;

        [TestInitialize]
        public void TestInitialize()
        {
            _target = new ReflectCallProviderObject();
            _methods = _target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .ToDictionary(m => m.Name, m => m);

            var mockAuthorizationProvider = new Mock<IAuthorizationProvider>();
            _reflectCallProviderConnection =
                new ReflectCallProviderConnection("host", "namespace", mockAuthorizationProvider.Object);

            _reflectCallProviderConnection.RegisterService(_target);
        }

        [TestCleanup]
        public void TestCleanup()
        {
        }

        [TestMethod]
        public void TestReflectCallProvider_MethodParameterParser_General()
        {
            // case 1: no parameter case.
            {
                var methodInfo = _methods["NoParameterMethod"];

                var bytes1 = Encoding.UTF8.GetBytes("{}");
                var objects1 = ReflectCallProviderConnection.MethodParameterParser(methodInfo, bytes1);
                Assert.AreEqual(0, objects1.Length);

                var bytes2 = Encoding.UTF8.GetBytes("null");
                var objects2 = ReflectCallProviderConnection.MethodParameterParser(methodInfo, bytes2);
                Assert.AreEqual(0, objects2.Length);
            }

            // case 2: nullable parameter case.
            {
                var methodInfo = _methods["NullableParameterMethod"];

                var bytes1 = Encoding.UTF8.GetBytes("{p1:0,p2:0}");
                var objects1 = ReflectCallProviderConnection.MethodParameterParser(methodInfo, bytes1);
                Assert.AreEqual(2, objects1.Length);
                Assert.AreEqual(0, (int) objects1[0]);
                Assert.AreEqual(0, (int?) objects1[1]);

                var bytes2 = Encoding.UTF8.GetBytes("{p1:-1}");
                var objects2 = ReflectCallProviderConnection.MethodParameterParser(methodInfo, bytes2);
                Assert.AreEqual(2, objects2.Length);
                Assert.AreEqual(-1, (int) objects2[0]);
                Assert.AreEqual(null, objects2[1]);

                var bytes3 = Encoding.UTF8.GetBytes("{P1:0,p2:null}");
                var objects3 = ReflectCallProviderConnection.MethodParameterParser(methodInfo, bytes3);
                Assert.AreEqual(2, objects3.Length);
                Assert.AreEqual(null, objects3[0]);
                Assert.AreEqual(null, objects3[1]);

                var bytes4 = Encoding.UTF8.GetBytes("null");
                var objects4 = ReflectCallProviderConnection.MethodParameterParser(methodInfo, bytes4);
                Assert.AreEqual(2, objects4.Length);
                Assert.AreEqual(null, objects4[0]);
                Assert.AreEqual(null, objects4[1]);
            }

            // case 3: mixed parameter case.
            {
                var methodInfo = _methods["MixedParameterMethod"];

                var bytes1 = Encoding.UTF8.GetBytes("{p1:0,p2:{\"key\":0}}");
                var objects1 = ReflectCallProviderConnection.MethodParameterParser(methodInfo, bytes1);
                Assert.AreEqual(2, objects1.Length);
                Assert.AreEqual(0, (int)objects1[0]);
                Assert.AreEqual(1, ((IDictionary<string, int>)objects1[1]).Count);

                var bytes2 = Encoding.UTF8.GetBytes("{p1:0,p2:{\"key\":-1}}");
                var objects2 = ReflectCallProviderConnection.MethodParameterParser(methodInfo, bytes2);
                Assert.AreEqual(2, objects2.Length);
                Assert.AreEqual(0, (int)objects2[0]);
                Assert.AreEqual(1, ((IDictionary<string, int>)objects2[1]).Count);

                var bytes3 = Encoding.UTF8.GetBytes("{p1:0,p2:{}}");
                var objects3 = ReflectCallProviderConnection.MethodParameterParser(methodInfo, bytes3);
                Assert.AreEqual(2, objects3.Length);
                Assert.AreEqual(0, (int)objects3[0]);
                Assert.AreEqual(0, ((IDictionary<string, int>)objects3[1]).Count);

                var bytes4 = Encoding.UTF8.GetBytes("{p1:0,p2:{key:0}}");
                var objects4 = ReflectCallProviderConnection.MethodParameterParser(methodInfo, bytes4);
                Assert.AreEqual(2, objects4.Length);
                Assert.AreEqual(0, (int)objects4[0]);
                Assert.AreEqual(1, ((IDictionary<string, int>)objects4[1]).Count);

                var bytes5 = Encoding.UTF8.GetBytes("{p1:0}");
                var objects5 = ReflectCallProviderConnection.MethodParameterParser(methodInfo, bytes5);
                Assert.AreEqual(2, objects5.Length);
                Assert.AreEqual(0, (int)objects5[0]);
                Assert.AreEqual(null, objects5[1]);
            }
        }

        [TestMethod]
        public void TestReflectCallProvider_MethodParameterParser_SpecialType()
        {
            // case 1: string parameter case.
            {
                var methodInfo = _methods["StringMethod"];
                var bytes1 = Encoding.UTF8.GetBytes("{p1:\"right\"}");
                var objects1 = ReflectCallProviderConnection.MethodParameterParser(methodInfo, bytes1);
                Assert.AreEqual(1, objects1.Length);
                Assert.AreEqual("right", (string)objects1[0]);

                var bytes2 = Encoding.UTF8.GetBytes("{p1:null}");
                var objects2 = ReflectCallProviderConnection.MethodParameterParser(methodInfo, bytes2);
                Assert.AreEqual(1, objects2.Length);
                Assert.AreEqual(null, objects2[0]);

                var bytes3 = Encoding.UTF8.GetBytes("{p1:\"\"}");
                var objects3 = ReflectCallProviderConnection.MethodParameterParser(methodInfo, bytes3);
                Assert.AreEqual(1, objects3.Length);
                Assert.AreEqual("", (string)objects3[0]);

                var bytes4 = Encoding.UTF8.GetBytes("{p1:0}");
                var objects4 = ReflectCallProviderConnection.MethodParameterParser(methodInfo, bytes4);
                Assert.AreEqual(1, objects4.Length);
            }

            // case 2: bool parameter case.
            {
                var methodInfo = _methods["BoolMethod"];
                var bytes1 = Encoding.UTF8.GetBytes("{p1:true,p2:true}");
                var objects1 = ReflectCallProviderConnection.MethodParameterParser(methodInfo, bytes1);
                Assert.AreEqual(2, objects1.Length);
                Assert.AreEqual(true, (bool)objects1[0]);
                Assert.AreEqual(true, (bool?)objects1[1]);

                var bytes2 = Encoding.UTF8.GetBytes("{p1:\"true\",p2:\"true\"}");
                var objects2 = ReflectCallProviderConnection.MethodParameterParser(methodInfo, bytes2);
                Assert.AreEqual(2, objects2.Length);
                Assert.AreEqual(true, (bool)objects2[0]);
                Assert.AreEqual(true, (bool?)objects2[1]);

                var bytes3 = Encoding.UTF8.GetBytes("{p1:\"\"}");
                Assert.ThrowsException<FormatException>(() => ReflectCallProviderConnection.MethodParameterParser(methodInfo, bytes3));

                var bytes4 = Encoding.UTF8.GetBytes("{p2:null}");
                var objects4 = ReflectCallProviderConnection.MethodParameterParser(methodInfo, bytes4);
                Assert.AreEqual(2, objects4.Length);
                Assert.AreEqual(null, objects4[0]);
                Assert.AreEqual(null, (bool?)objects4[1]);
            }

            // case 3: DateTimeOffset parameter case.
            {
                var methodInfo = _methods["DateTimeOffsetMethod"];
                var bytes1 = Encoding.UTF8.GetBytes("{p1:\"2017-10-10T08:47:51.3834082+00:00\",p2:\"2017-10-10T08:47:51.3834082+00:00\"}");
                var objects1 = ReflectCallProviderConnection.MethodParameterParser(methodInfo, bytes1);
                Assert.AreEqual(2, objects1.Length);
                Assert.AreEqual(DateTimeOffset.Parse("2017-10-10T08:47:51.3834082+00:00"), (DateTimeOffset)objects1[0]);
                Assert.AreEqual(DateTimeOffset.Parse("2017-10-10T08:47:51.3834082+00:00"), (DateTimeOffset?)objects1[1]);
            }
        }

        [TestMethod]
        public void TestReflectCallProvider_ProcessCall_General()
        {
            // case 1: no parameter case.
            {
                var result1 = _reflectCallProviderConnection
                    .ProcessCall("NoParameterMethod", Encoding.UTF8.GetBytes("{}")).Result;
                Assert.AreEqual(true, (bool) result1);

                var result2 = _reflectCallProviderConnection
                    .ProcessCall("NoParameterMethod", Encoding.UTF8.GetBytes("null")).Result;
                Assert.AreEqual(true, (bool) result2);
            }

            // case 2: nullable parameter case.
            {
                var result1 = _reflectCallProviderConnection
                    .ProcessCall("NullableParameterMethod", Encoding.UTF8.GetBytes("{p1:0,p2:0}")).Result;
                Assert.AreEqual(true, (bool) result1);

                var result2 = _reflectCallProviderConnection
                    .ProcessCall("NullableParameterMethod", Encoding.UTF8.GetBytes("{p1:-1}")).Result;
                Assert.AreEqual(false, (bool) result2);

                var result3 = _reflectCallProviderConnection
                    .ProcessCall("NullableParameterMethod", Encoding.UTF8.GetBytes("{P1:0,p2:null}")).Result;
                Assert.AreEqual(false, (bool) result3);

                var result4 = _reflectCallProviderConnection
                    .ProcessCall("NullableParameterMethod", Encoding.UTF8.GetBytes("null")).Result;
                Assert.AreEqual(false, (bool) result4);
            }

            // case 3: mixed parameter case.
            {
                var result1 = _reflectCallProviderConnection.ProcessCall("MixedParameterMethod", Encoding.UTF8.GetBytes("{p1:0,p2:{\"key\":0}}")).Result;
                Assert.AreEqual(true, (bool)result1);

                var result2 = _reflectCallProviderConnection.ProcessCall("MixedParameterMethod", Encoding.UTF8.GetBytes("{p1:0,p2:{\"key\":-1}}")).Result;
                Assert.AreEqual(false, (bool)result2);

                var result3 = _reflectCallProviderConnection.ProcessCall("MixedParameterMethod", Encoding.UTF8.GetBytes("{p1:0,p2:{}}")).Result;
                Assert.AreEqual(false, (bool)result3);

                var result4 = _reflectCallProviderConnection.ProcessCall("MixedParameterMethod", Encoding.UTF8.GetBytes("{p1:0,p2:{key:0}}")).Result;
                Assert.AreEqual(true, (bool)result4);

                var result5 = _reflectCallProviderConnection.ProcessCall("MixedParameterMethod", Encoding.UTF8.GetBytes("{p1:0}")).Result;
                Assert.AreEqual(false, (bool)result5);
            }

            // case 4: invalid case.
            {
                Assert.ThrowsException<KeyNotFoundException>(() =>
                    _reflectCallProviderConnection.ProcessCall("InvalidMethod", Encoding.UTF8.GetBytes("{}"))
                        .Result);

                Assert.ThrowsException<FormatException>(() =>
                    _reflectCallProviderConnection
                        .ProcessCall("NullableParameterMethod", Encoding.UTF8.GetBytes("{p1:\"\"}")).Result);
            }
        }

        [TestMethod]
        public void TestReflectCallProvider_ProcessCall_SpecialType()
        {
            // case 1: string parameter case.
            {
                var result1 = _reflectCallProviderConnection.ProcessCall("StringMethod", Encoding.UTF8.GetBytes("{p1:\"right\"}")).Result;
                Assert.AreEqual(true, (bool)result1);

                var result2 = _reflectCallProviderConnection.ProcessCall("StringMethod", Encoding.UTF8.GetBytes("{}")).Result;
                Assert.AreEqual(false, (bool)result2);

                var result3 = _reflectCallProviderConnection.ProcessCall("StringMethod", Encoding.UTF8.GetBytes("{p1:null}")).Result;
                Assert.AreEqual(false, (bool)result3);

                var result4 = _reflectCallProviderConnection.ProcessCall("StringMethod", Encoding.UTF8.GetBytes("{\"p1\":\"right\"}")).Result;
                Assert.AreEqual(true, (bool)result4);
            }

            // case 2: bool parameter case.
            {
                var result1 = _reflectCallProviderConnection.ProcessCall("BoolMethod", Encoding.UTF8.GetBytes("{p1:true,p2:true}")).Result;
                Assert.AreEqual(true, (bool)result1);

                var result2 = _reflectCallProviderConnection.ProcessCall("BoolMethod", Encoding.UTF8.GetBytes("{p1:\"true\",p2:\"true\"}")).Result;
                Assert.AreEqual(true, (bool)result2);

                var result3 = _reflectCallProviderConnection.ProcessCall("BoolMethod", Encoding.UTF8.GetBytes("{p2:null}")).Result;
                Assert.AreEqual(false, (bool)result3);
            }

            // case 3: DateTimeOffset parameter case.
            {
                var result1 = _reflectCallProviderConnection.ProcessCall("DateTimeOffsetMethod",
                    Encoding.UTF8.GetBytes(
                        "{p1:\"2017-10-10T08:47:51.3834082+00:00\",p2:\"2017-10-10T08:47:51.3834082+00:00\"}")).Result;
                Assert.AreEqual(true, (bool)result1);

                var result2 = _reflectCallProviderConnection.ProcessCall("DateTimeOffsetMethod",
                    Encoding.UTF8.GetBytes(
                        "{p2:\"2017-10-10T08:47:51.3834082+00:00\"}")).Result;
                Assert.AreEqual(true, (bool)result2);
            }
        }
    }
}
