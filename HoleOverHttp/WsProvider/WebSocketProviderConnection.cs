using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HoleOverHttp.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Serilog;

namespace HoleOverHttp.WsProvider
{
    public class WebSocketProviderConnection : IProviderConnection
    {
        private static readonly int SizeOfGuid = Guid.Empty.ToByteArray().Length;

        private readonly string _host;

        private readonly string _namespace;

        private readonly IAuthorizationProvider _tokenProvider;

        public WebSocketProviderConnection(string host, string @namespace, IAuthorizationProvider tokenProvider)
        {
            _namespace = @namespace;
            _tokenProvider = tokenProvider;
            _host = host;
        }

        public string UriPattern { get; set; } = "{0}://{1}/ws/register?ns={2}";

        public Func<object, Task<object>> CallFunc { get; set; }

        public bool Secure { get; set; } = true;

        private Uri Uri => new Uri(string.Format(UriPattern, Secure ? "wss" : "ws", _host, _namespace));

        private async Task<WebSocket> ReconnectAsync()
        {
            var socket = new ClientWebSocket();
            socket.Options.SetRequestHeader(_tokenProvider.Key, _tokenProvider.Value);
            await socket.ConnectAsync(Uri, CancellationToken.None);
            return socket;
        }

        public async Task ServeAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var socket = await ReconnectAsync();
                    var buffer = new byte[4096];
                    var locksend = new object();
                    while (socket.State == WebSocketState.Open)
                    {
                        using (var ms = new MemoryStream())
                        {
                            while (true)
                            {
                                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), token);

                                if (result.MessageType == WebSocketMessageType.Close)
                                {
                                    await socket.CloseAsync(WebSocketCloseStatus.Empty, "", token);
                                    return;
                                }

                                if (result.CloseStatus.HasValue)
                                {
                                    return;
                                }

                                if (result.MessageType == WebSocketMessageType.Binary)
                                {
                                    ms.Write(buffer, 0, result.Count);
                                }

                                if (!result.EndOfMessage) continue;

                                ms.Position = 0;
                                var br = new BinaryReader(ms);

                                var id = br.ReadBytes(SizeOfGuid);
                                var method = br.ReadString();
                                var param = br.ReadBytes((int) ms.Length);
                                
                                Log.Verbose($"Receive and start task. id:{new Guid(id)} method:{method}");
                                var unused = Task.Run(async () =>
                                {
                                    byte[] rt;
                                    var stopwatch = Stopwatch.StartNew();
                                    try
                                    {
                                        var resultObject = await CallFunc(new {method, param});
                                        stopwatch.Stop();
                                        rt = Encoding.UTF8.GetBytes(
                                            JsonConvert.SerializeObject(
                                                new
                                                {
                                                    result = resultObject,
                                                    latency = stopwatch.ElapsedMilliseconds
                                                }, new JsonSerializerSettings
                                                {
                                                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                                                    Converters = new List<JsonConverter> { new StringEnumConverter() }
                                                }
                                            )
                                        );
                                    }
                                    catch (Exception e)
                                    {
                                        stopwatch.Stop();
                                        rt = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
                                        {
                                            error = e.ToString(),
                                            latency = stopwatch.ElapsedMilliseconds
                                        }));
                                    }

                                    var buf = new MemoryStream();
                                    buf.Write(id, 0, id.Length);
                                    buf.Write(rt, 0, rt.Length);

                                    lock (locksend)
                                    {
                                        socket.SendAsync(new ArraySegment<byte>(buf.ToArray()),
                                            WebSocketMessageType.Binary, true, token).Wait(token);                                        
                                    }

                                    Log.Verbose($"Send and finish task. id:{new Guid(id)} method:{method}");
                                }, token);

                                break;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e, "");
                    await Task.Delay(TimeSpan.FromSeconds(5), token);
                }
            }
        }
    }
}
