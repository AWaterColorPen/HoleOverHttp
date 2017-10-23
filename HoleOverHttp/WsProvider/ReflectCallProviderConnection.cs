using System;
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
        private readonly Dictionary<string, MethodInfo> _methods;
        private readonly object _target;

        public ReflectCallProviderConnection(object target, string host, string @namespace,
            IAuthorizationProvider tokenProvider) : base(host, @namespace, tokenProvider)
        {
            _target = target;

            _methods = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .ToDictionary(m => m.Name, m => m);
        }

        public override Task<object> ProcessCall(string method, byte[] bytes)
        {
            var methodInfo = _methods[method];
            var paramObjects = MethodParameterParser(methodInfo, bytes);
            return Task.Run(() => methodInfo.Invoke(_target, paramObjects));
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
