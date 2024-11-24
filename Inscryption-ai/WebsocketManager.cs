using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Inscryption_ai
{
    public static class WebsocketManager
    {
        private static ClientWebSocket _ws;
        private const int Port = 9302;
        public static List<WebSocketResponse> UnresolvedResponses { get; private set; }
        
        private static async Task PollMessages()
        {
            while (true)
            {
                Console.WriteLine("Polling messages...");
                var buffer = new byte[1024 * 4];

                try
                {
                    // Keep polling while the WebSocket state is open
                    while (_ws.State == WebSocketState.Open)
                    {
                        WebSocketReceiveResult result;
                        var message = new StringBuilder();

                        do
                        {
                            result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                            if (result.MessageType == WebSocketMessageType.Close)
                            {
                                Console.WriteLine("Connection closed by the server.");
                                break;
                            }

                            message.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

                        } while (!result.EndOfMessage);

                        // Process message if it's fully received
                        if (result.MessageType != WebSocketMessageType.Close)
                        {
                            UnresolvedResponses.Add(WebSocketResponseFactory.ParseResponse(message.ToString()));
                            Console.WriteLine("Message received: " + message);
                        }
                    }
                }
                catch (WebSocketException ex)
                {
                    Console.WriteLine($"WebSocket error: {ex.Message}");
                }

                Console.WriteLine("Connection Broken");

                // Attempt to reconnect if the connection is lost
                await AttemptWebsocketConnection(async () =>
                {
                    Console.WriteLine("Connection re-established.");
                    await PollMessages();
                });
                break; // Exit the current PollMessages loop since a new one will be started upon reconnection
            }
        }
        
        private static async Task AttemptWebsocketConnection(Func<Task> callback)
        {
            Console.WriteLine("Attempting websocket connection...");
            var attempts = 0;
            while (true)
            {
                if (attempts > 0)
                {
                    Console.WriteLine($"Connections have failed. Trying again. (attempts: {attempts})");
                }

                _ws = new ClientWebSocket();  // Create a new ClientWebSocket instance

                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                {
                    try
                    {
                        await _ws.ConnectAsync(new Uri("ws://localhost:" + Port), cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        Console.WriteLine("Connection attempt timed out.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Connection attempt failed: {ex.Message}");
                    }
                }

                if (_ws.State == WebSocketState.Open)
                {
                    await callback();
                    break;

                }
                await Task.Delay(1000);

                attempts++;
            }
        }
        
        public static async Task Send(object action)
        {
            var message = JsonSerializer.Serialize(action);
            Console.WriteLine("Sending: " + message);
            var bytes = Encoding.UTF8.GetBytes(message);
            await _ws.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );
        }
    }
}