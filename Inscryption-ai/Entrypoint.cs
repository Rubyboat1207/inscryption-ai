using System;
using System.Net.WebSockets;
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
        
        private async void AttemptWebsocketConnection(Action callback)
        {
            var attempts = 0;
            while (!Connected)
            {
                if (attempts > 0)
                {
                    Console.WriteLine($"Connections have failed. Trying again. (attempts: {attempts})");
                }
                
                await Task.Delay(500);
                await _ws.ConnectAsync(new Uri("ws://localhost:" + Port), CancellationToken.None);
                if (_ws.State == WebSocketState.Open)
                {
                    Connected = true;
                    callback();
                }
                attempts++;
            }
        }
        
        private void Awake()
        {
            AttemptWebsocketConnection(() =>
            {
                Console.WriteLine("Websocket connection established!");
            });
        }
    }
}