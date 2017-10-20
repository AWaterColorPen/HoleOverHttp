using System;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Serilog;

namespace HoleOverHttp.WsProvider
{
    public abstract class CallProviderConnection
    {
        private readonly string _host;
        private readonly string _namespace;

        private readonly IAuthorizationProvider _tokenProvider;

        protected CallProviderConnection(string host, string @namespace, IAuthorizationProvider tokenProvider)
        {
            _namespace = @namespace;
            _tokenProvider = tokenProvider;
            _host = host;
        }

        public string UriPattern { get; set; } = "{0}://{1}/ws/register?ns={2}";

        public bool Secure { get; set; } = true;

        private Uri Uri => new Uri(string.Format(UriPattern, Secure ? "wss" : "ws", _host, _namespace));

        public async Task<WebSocket> ReconnectAsync()
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

                                var id = br.ReadBytes(16);
                                var method = br.ReadString();
                                var param = br.ReadBytes((int)ms.Length);

                                await Task.Run(async () =>
                                {
                                    byte[] rt;
                                    var stopwatch = Stopwatch.StartNew();
                                    try
                                    {
                                        var resultObject = await ProcessCall(method, param);
                                        stopwatch.Stop();
                                        rt = Encoding.UTF8.GetBytes(
                                            JsonConvert.SerializeObject(
                                                new
                                                {
                                                    result = resultObject,
                                                    latency = stopwatch.ElapsedMilliseconds
                                                }, Formatting.None, new JsonSerializerSettings
                                                {
                                                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
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

                                    await socket.SendAsync(new ArraySegment<byte>(buf.ToArray()),
                                        WebSocketMessageType.Binary, true, token);
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

        public abstract Task<object> ProcessCall(string method, byte[] bytes);
    }
}
