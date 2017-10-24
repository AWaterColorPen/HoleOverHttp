using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HoleOverHttp.Core;
using Newtonsoft.Json;

namespace HoleOverHttp.WsProvider
{
    public class ReflectCallProviderConnection : CallProviderConnection
    {
        private readonly ConcurrentDictionary<string, Tuple<MethodInfo, object>> _methods =
            new ConcurrentDictionary<string, Tuple<MethodInfo, object>>();

        public ReflectCallProviderConnection(string host, string @namespace, IAuthorizationProvider tokenProvider) :
            base(host, @namespace, tokenProvider)
        {
        }

        public void RegisterService(object service)
        {
            foreach (var method in service.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                if (_methods.ContainsKey(method.Name))
                {
                    throw new Exception(
                        $"service:{service} rmethod:{method.Name} were already registed by other serivce:{_methods[method.Name]}.");
                }

                _methods.TryAdd(method.Name, new Tuple<MethodInfo, object>(method, service));
            }
        }

        public override Task<object> ProcessCall(string method, byte[] bytes)
        {
            var methodInfo = _methods[method].Item1;
            var paramObjects = MethodParameterParser(methodInfo, bytes);
            return Task.Run(() => methodInfo.Invoke(_methods[method].Item2, paramObjects));
        }

        public static object[] MethodParameterParser(MethodInfo methodInfo, byte[] bytes)
        {
            var parameterMap = methodInfo.GetParameters().ToDictionary(v => v.Name, v => v.ParameterType);
            var parameterInput =
                JsonConvert.DeserializeObject<IDictionary<string, object>>(Encoding.UTF8.GetString(bytes), new JsonSerializerSettings
                {
                    DateParseHandling = DateParseHandling.DateTimeOffset
                });

            return
                parameterMap.Select(v =>
                {
                    if (parameterInput == null || !parameterInput.ContainsKey(v.Key)) return null;
                    var param = parameterInput[v.Key];
                    return param == null
                        ? null
                        : (v.Value.IsSerializable
                            ? Convert.ChangeType(param,
                                (v.Value.IsGenericType && v.Value.GetGenericTypeDefinition() == typeof(Nullable<>)
                                    ? Nullable.GetUnderlyingType(v.Value)
                                    : v.Value) ?? throw new InvalidOperationException())
                            : JsonConvert.DeserializeObject(param.ToString(), v.Value));
                }).ToArray();
        }
    }
}
