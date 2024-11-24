using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BepInEx;
using DiskCardGame;
using HarmonyLib;
using Inscryption_ai.Extensions;

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
        public Dictionary<string, Func<string, Task<string>>> AsyncActionRegistry { get; set; }
        private readonly object _responsesLock = new object();
        
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
                        if (result.MessageType == WebSocketMessageType.Close) continue;
                        lock (_responsesLock)
                        {
                            Responses.Add(WebSocketResponseFactory.ParseResponse(message.ToString()));
                        }
                        Console.WriteLine("Message received: " + message);
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
                ["get_ability_info"] = Actions.CheckRuleBook,
                ["get_cards_in_hand"] = _ => Actions.GetCardsInHand(),
                ["see_board_state"] = _ => Singleton<BoardManager>.Instance.DescribeStateToAI(),
                ["draw_from_deck"] = Actions.DrawCardFromDeck,
                ["see_cards_in_deck"] = _ => Actions.GetAllCardsInDeck(),
            };

            AsyncActionRegistry = new Dictionary<string, Func<string, Task<string>>>
            {
                ["play_card_in_hand"] = Actions.PlayCardInHand,
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
            await Send(new RegisterAction("ring_bell", "rings the combat bell, forcing the next turn to be played. Do not ring if you had an error, this is only after you are done.", new Dictionary<string, object>() {}));
            await Send(new RegisterAction("get_cards_in_hand", "Gets all of the cards currently in hand. Always run and wait for result before playing anything.", new Dictionary<string, object>() {}));
            await Send(new RegisterAction("get_ability_info", "Gives you information about the function of an ability", new Dictionary<string, object>()
            {
                {"type", "object"},
                {"properties", new Dictionary<string, object>
                {
                    {"ability", new Dictionary<string, object>
                    {
                        {"type", "string"},
                        {"description", "the ID of the ability to search. Case sensitive. (often no spaces and is PascalCase)"}
                    }}
                }}
            }));
            await Send(new RegisterAction("play_card_in_hand", "Gives you information about the function of an ability", new Dictionary<string, object>()
            {
                {"type", "object"},
                {"properties", new Dictionary<string, object>
                {
                    {"card_idx", new Dictionary<string, object>
                    {
                        {"type", "number"},
                        {"description", "The index of the card you wish to play. Remember to re-check your hand to get updated indices after playing."}
                    }},
                    {"sacrifice_indexes", new Dictionary<string, object>
                    {
                        {"type", "array"},
                        {"items", new Dictionary<string, object>
                        {
                            {"type", "number"}
                        }},
                        {"description", "If the card requires blood, the corresponding number of cards must be sacrificed, each giving 1 blood. Friendly board indices only. THIS IS NOT NULLABLE. THIS MUST BE PROVIDED."}
                    }},
                    {"placement_index", new Dictionary<string, object>
                    {
                        {"type", "number"},
                        {"description", "What index on the board to place in. Friendly board indices only."}
                    }}
                }}
            }));
            await Send(new RegisterAction("see_board_state", "Gives you information about the board", new Dictionary<string, object>() {}));
            await Send(new RegisterAction("see_cards_in_deck", "Gives you information about your deck", new Dictionary<string, object>() {}));
            await Send(new AddEnvironmentContext(
                "Always wait for the results of actions before proceeding to the next step. Check the board state and hand after each action. Play the best card available based on the current board state and hand, focusing on strategy. Prioritize sacrificing cards when necessary to summon more powerful cards. Communicate clearly about actions taken and decisions made during the game."));
            await Send(new AddEnvironmentContext(
                "When facing a strong enemy like a Grizzly, prioritize playing defensive cards and using sacrifices wisely. Always check your hand and board after each action. Utilize cards with defensive abilities like the River Snapper when possible. Squirrels are essential for quick sacrifices, enabling powerful plays with cards like Wolves. Remember the power of sigils like Evolve and DebuffEnemy to strengthen your board presence and weaken foes. Plan sacrifices carefully to maintain a balanced hand and responsive board."));
            
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

        private async Task RunActionAsync(WebSocketResponse res)
        {
            try
            {
                var actionResult = await AsyncActionRegistry[res.ActionName].Invoke(res.Params);
                await Send(new ActionResponse(res.ActionId, actionResult));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await Send(new ActionResponse(res.ActionId, "Action failed. Say this exactly along with whatever else: \"Tell Rubyboat there is a problem with the inscryption mod.\""));
            }

        } 
        
        private void Update()
        {
            if (Responses.Count <= 0) return;
            var playedThisTurn = false;
            lock (_responsesLock)
            {
                try
                {
                    Console.WriteLine($"Updating messages... ({Responses.Count})");
                    foreach (var res in Responses.ToList())
                    {
                        Console.WriteLine($"Responding to a {res.Type} action");
                        if (res.Type == "send_all_actions")
                        {
                            Console.WriteLine("Sending all actions");
                            _ = SendAllActions();
                        }
                        else if (res.Type == "execute_action")
                        {
                            Console.WriteLine("Executing action " + res.ActionName);
                            if (res.ActionName == "play_card_in_hand")
                            {
                                if (playedThisTurn)
                                {
                                    _ = Send(new ActionResponse(res.ActionId,
                                        "Only one play per action set. send it again, ONLY AFTER checking board state and your hand."));
                                    continue;
                                }

                                playedThisTurn = true;
                            }

                            if (ActionRegistry.ContainsKey(res.ActionName))
                            {
                                try
                                {
                                    var actionResult = ActionRegistry[res.ActionName].Invoke(res.Params);
                                    _ = Send(new ActionResponse(res.ActionId, actionResult));
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e);
                                    _ = Send(new ActionResponse(res.ActionId,
                                        "Action failed. Tell Rubyboat there is a problem with the inscryption mod."));
                                }
                            }
                            else if (AsyncActionRegistry.ContainsKey(res.ActionName))
                            {
                                try
                                {
                                    _ = RunActionAsync(res);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e);
                                    _ = Send(new ActionResponse(res.ActionId,
                                        "Action failed. Tell Rubyboat there is a problem with the inscryption mod."));
                                }
                            }
                        }
                        else
                        {
                            if (res.Ok) continue;
                            Console.WriteLine("Unknown request from AI");
                            Console.WriteLine(res.Type);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error occured during foreach");
                    Console.WriteLine(e);
                }
            }
            
            Console.WriteLine("Actions executed. clearing responses");
            try
            {
                Responses.Clear();
            }
            catch(Exception e)
            {
                Console.WriteLine("Error occured while clearing");
                Console.WriteLine(e);
            }
            
        }
    }
}
