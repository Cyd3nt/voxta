using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using ChatMate.Server;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;

namespace WebSocketTests
{
    [TestFixture]
    public class WebSocketTest
    {
        private JsonSerializerOptions _serializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        
        private WebSocketClient _wsClient = null!;
        private WebSocket _wsConnection = null!;
        private TestServer _server = null!;
        private HttpClient _httpClient = null!;

        [SetUp]
        public async Task SetUp()
        {
            var webDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "ChatMate.Server");
            var builder = WebHost.CreateDefaultBuilder()
                .UseStartup<Startup>()
                .UseContentRoot(webDir);
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddJsonFile("appsettings.Local.json", optional: false, reloadOnChange: true);
            });
            _server = new TestServer(builder);
            _httpClient = _server.CreateClient();
            _wsClient = _server.CreateWebSocketClient();
            var wsUri = new UriBuilder(_server.BaseAddress)
            {
                Scheme = "ws",
                Path = "/ws"
            }.Uri;
            _wsConnection = await _wsClient.ConnectAsync(wsUri, CancellationToken.None);
        }

        [TearDown]
        public async Task TearDown()
        {
            try
            {
                await _wsConnection.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            }
            catch (Exception)
            {
                // ignored
            }
            _server.Dispose();
        }

        [Test]
        public async Task SendMessageAndGetReply()
        {
            
            var requestBytes = JsonSerializer.SerializeToUtf8Bytes<ClientMessage>(
                new ClientSendMessage { Text = "Hello, world!" },
                _serializerOptions
            );
            string tmp2 = Encoding.UTF8.GetString(requestBytes);
            Console.WriteLine(tmp2);
            await _wsConnection.SendAsync(
                requestBytes,
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );

            var buffer = new byte[1024];
            var result = await _wsConnection.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            Assert.That(result.CloseStatus, Is.Null, result.CloseStatusDescription);
            var reply = (ServerReplyMessage?)JsonSerializer.Deserialize<ServerMessage>(buffer.AsMemory(0, result.Count).Span, _serializerOptions);
            Assert.Multiple(() =>
            {
                Assert.That(reply, Is.Not.Null);
                Assert.That(reply?.Text, Is.Not.Null.Or.Empty);
                Assert.That(reply?.SpeechUrl, Does.Match(@"/chats/.+/messages/.+/speech/.+\.wav"));
            });

            result = await _wsConnection.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            Assert.That(result.CloseStatus, Is.Null, result.CloseStatusDescription);
            var animation = (ServerAnimationMessage?)JsonSerializer.Deserialize<ServerMessage>(buffer.AsMemory(0, result.Count).Span, _serializerOptions);
            Assert.Multiple(() =>
            {
                Assert.That(animation, Is.Not.Null);            
                Assert.That(animation?.Value, Is.Not.Null.Or.Empty);
            });

            if (reply?.SpeechUrl != null)
            {
                var response = await _httpClient.GetAsync(new Uri(_server.BaseAddress, reply.SpeechUrl));
                if (!response.IsSuccessStatusCode)
                    Assert.Fail($"GET {reply.SpeechUrl}{Environment.NewLine}{await response.Content.ReadAsStringAsync()}");

                Assert.Multiple(() =>
                {
                    Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), reply.SpeechUrl);
                    Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("audio/x-wav"), reply.SpeechUrl);
                    Assert.That(response.Content.Headers.ContentLength, Is.GreaterThan(1000), reply.SpeechUrl);
                });
            }
        }
    }
}