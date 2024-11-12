using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BepInEx;
using HarmonyLib;

namespace Inscryption_ai
{    
    [BepInPlugin("net.rubyboat.plugins.inscryption-ai", "Inscryption AI", "1.0.0")]
    public class Entrypoint : BaseUnityPlugin
    {
        public static Entrypoint Instance;
        private ClientWebSocket _ws;
        private const int Port = 9302;
        private bool Connected { get; set; }
        public List<WebSocketResponse> Responses { get; set; } = new List<WebSocketResponse>();

        public Dictionary<string, Func<string, string>> ActionRegistry { get; set; }

        public Entrypoint() : base()
        {
            var harmony = new Harmony(Info.Metadata.GUID);
            
            harmony.PatchAll();
        }

        private async Task AttemptWebsocketConnection(Func<Task> callback)
        {
            Console.WriteLine("Attempting websocket connection...");
            var attempts = 0;
            while (!Connected)
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
                    Connected = true;
                    await callback();
                }
                else
                {
                    await Task.Delay(1000);
                }

                attempts++;
            }
        }

        private async Task PollMessages()
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
                            Responses.Add(WebSocketResponseFactory.ParseResponse(message.ToString()));
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
                Connected = false;
                await AttemptWebsocketConnection(async () =>
                {
                    Console.WriteLine("Connection re-established.");
                    await PollMessages();
                });
                break; // Exit the current PollMessages loop since a new one will be started upon reconnection
            }
        }

        private void Awake()
        {
            Console.WriteLine("Entrypoint: Awake called");
            if (Instance != null)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            ActionRegistry = new Dictionary<string, Func<string, string>>()
            {
                ["ring_bell"] = _ => Actions.RingBell(),
                ["get_ability_info"] = Actions.CheckRuleBook
            };
            
            Task.Run(async () =>
            {
                try
                {
                    await AttemptWebsocketConnection(async () =>
                    {
                        Console.WriteLine("Websocket connection established.");
                        await PollMessages();
                    });
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            });
        }

        private async Task SendAllActions()
        {
            await Send(new RegisterAction("ring_bell", "rings the combat bell, forcing the next turn to be played", new Dictionary<string, object>() {}));
            await Send(new RegisterAction("get_ability_info", "Gives you information about the function of an ability", new Dictionary<string, object>()
            {
                {"type", "object"},
                {"properties", new Dictionary<string, object>
                {
                    {"ability", new Dictionary<string, object>
                    {
                        {"type", "string"},
                        {"description", "the ID of the ability to search."}
                    }}
                }}
            }));
        }
        
        public async Task Send(object action)
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
        
        private void Update()
        {
            if (Responses.Count <= 0) return;
            
            Console.WriteLine("Updating messages...");
            foreach (var res in Responses)
            {
                switch (res.Type)
                {
                    case "send_all_actions":
                        Console.WriteLine("Sending all actions");
                        _ = SendAllActions();
                        break;
                    case "execute_action":
                        Console.WriteLine("Executing action " + res.ActionName);

                        try
                        {
                            string actionResult = ActionRegistry[res.ActionName].Invoke(res.Params);
                            _ = Send(new ActionResponse(res.ActionId, actionResult));
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            _ = Send(new ActionResponse(res.ActionId, "Action failed. Tell Rubyboat there is a problem with the inscryption mod."));
                        }

                        break;
                    default:
                        Console.WriteLine("Unknown request from AI");
                        Console.WriteLine(res.Type);
                        break;
                }
            }
            Console.WriteLine("Actions executed. clearing responses");
            Responses.Clear();
        }
    }
}
