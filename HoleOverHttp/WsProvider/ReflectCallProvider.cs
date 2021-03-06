﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace HoleOverHttp.WsProvider
{
    public class ReflectCallProvider : CallProvider
    {
        private readonly ConcurrentDictionary<string, Tuple<MethodInfo, object>> _methods =
            new ConcurrentDictionary<string, Tuple<MethodInfo, object>>();

        public override void RegisterService(object service)
        {
            foreach (var method in service.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                if (string.IsNullOrEmpty(method.Name))
                {
                    throw new Exception(
                        $"service:{service} method:{method.Name} was invalid method name to register.");
                }

                if (_methods.ContainsKey(method.Name))
                {
                    throw new Exception(
                        $"service:{service} method:{method.Name} was already registed by other serivce:{_methods[method.Name]}.");
                }

                _methods.TryAdd(method.Name, new Tuple<MethodInfo, object>(method, service));
            }
        }

        private Task<object> ProcessCallInternal(string method, byte[] param)
        {
            if (string.IsNullOrEmpty(method))
            {
                return ProvideAvailableMethods();
            }

            var methodInfo = _methods[method].Item1;
            var paramObjects = MethodParameterParser(methodInfo, param);
            return Task.Run(() => methodInfo.Invoke(_methods[method].Item2, paramObjects));
        }

        private static void InputParser(object input, out string method, out byte[] param)
        {
            method = (string) input.GetType().GetProperty("method")?.GetValue(input);
            param = (byte[]) input.GetType().GetProperty("param")?.GetValue(input);
        }

        private Task<object> ProvideAvailableMethods()
        {
            IDictionary<string, object> BuildParameterTypeDescription(Type type)
            {
                var description = new Dictionary<string, object>
                {
                    ["Type"] = $"{type}"
                };

                if (type.IsValueType ||
                    type.GetConstructor(Type.EmptyTypes) != null)
                {
                    description["Sample"] = Activator.CreateInstance(type);
                }

                return description;
            }

            return
                Task.Run(() => (object)_methods.Select(
                    method => new
                    {
                        MethodName = method.Key,
                        Instance = $"{method.Value.Item2}",
                        ReturnType = $"{method.Value.Item1.ReturnType}",
                        Arguments = method.Value.Item1.GetParameters().ToDictionary(
                            v => v.Name,
                            v => BuildParameterTypeDescription(v.ParameterType))
                    }).ToList());
        }

        public override Task<object> ProcessCall(object input)
        {
            InputParser(input, out var method, out var param);
            return ProcessCallInternal(method, param);
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
                        : v.Value.IsEnum
                            ? Enum.Parse(v.Value, param.ToString())
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
