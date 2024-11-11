using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BepInEx;

namespace Inscryption_ai
{    
    [BepInPlugin("net.rubyboat.plugins.inscryption-ai", "Inscryption AI", "1.0.0")]
    public class Entrypoint : BaseUnityPlugin
    {
        private readonly ClientWebSocket _ws = new ClientWebSocket();
        private const int Port = 9302;
        private bool Connected { get; set; }
        public List<WebSocketResponse> Responses { get; set; } = new List<WebSocketResponse>();

        private async Task AttemptWebsocketConnection(Action callback)
        {
            var attempts = 0;
            while (!Connected)
            {
                if (attempts > 0)
                {
                    Console.WriteLine($"Connections have failed. Trying again. (attempts: {attempts})");
                }

                await _ws.ConnectAsync(new Uri("ws://localhost:" + Port), CancellationToken.None);
                if (_ws.State == WebSocketState.Open)
                {
                    Connected = true;
                    callback();
                }
                else
                {
                    await Task.Delay(1000);
                }

                attempts++;

            }

            var buffer = new byte[1024 * 4];

            while (_ws.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result;
                var message = new StringBuilder();

                do
                {
                    result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    var receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Responses.Add(WebSocketResponseFactory.ParseResponse(receivedMessage));
                } while (!result.EndOfMessage);

                Console.WriteLine("Message received: " + message);
            }
        }

        private void Awake()
        {
            AttemptWebsocketConnection(() =>
            {
                Console.WriteLine("Websocket connection established!");
            }).Start();
        }

        private async Task SendAllActions()
        {
            await SendAction(new RegisterAction("hello", "says hello in console", new JsonElement()));
        }

        private async Task SendAction(RegisterAction action)
        {
            await _ws.SendAsync(
                new ArraySegment<byte>(Encoding.UTF8.GetBytes(
                    JsonSerializer.Serialize(action)
                )),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );
        }

        private void Update()
        {
            if (Responses.Count <= 0) return;
            foreach (var _ in Responses.Where(response => response.Type == "send_all_actions"))
            {
                Console.WriteLine("Sending all actions");
                SendAllActions().Start();
            }
        }
    }
}