using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using DiskCardGame;
using Inscryption_ai.Extensions;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using UnityEngine;

namespace Inscryption_ai
{
    public static class Actions
    {
        public static Dictionary<string, Func<string, string>> ActionRegistry { get; set; }
        public static Dictionary<string, Func<string, Task<string>>> AsyncActionRegistry { get; set; }

        public static void LoadActions()
        {
            ActionRegistry = new Dictionary<string, Func<string, string>>()
            {
                ["ring_bell"] = _ => Actions.RingBell(),
                ["get_ability_info"] = Actions.CheckRuleBook,
                ["get_cards_in_hand"] = _ => Actions.GetCardsInHand(),
                ["see_board_state"] = _ => Singleton<BoardManager>.Instance.DescribeStateToAI(),
                ["draw_from_deck"] = Actions.DrawCardFromDeck,
            };

            AsyncActionRegistry = new Dictionary<string, Func<string, Task<string>>>
            {
                ["play_card_in_hand"] = Actions.PlayCardInHand,
            };
        }
        
        public static string RingBell()
        {
            var bell = GameObject.Find("CombatBell").GetComponent<CombatBell3D>();
            
            if ((bool) typeof(CombatBell3D).GetMethod(nameof(CombatBell3D.PressingAllowed), BindingFlags.Instance | BindingFlags.NonPublic).Invoke(bell, null))
            {
                typeof(CombatBell3D).GetMethod(nameof(CombatBell3D.OnBellPressed), BindingFlags.Instance | BindingFlags.NonPublic).Invoke(bell, null);
                Singleton<TurnManager>.Instance.OnCombatBellRang();
                return "Bell Pressed";
            }
            

            return "Bell was not pressable at this time.";
        }

        private class CheckAbilityRuleBook
        {
            [JsonPropertyName("ability")]
            public string Ability { get; set; }
        }
        
        public static string CheckRuleBook(string abilityNameJson)
        {
            var abilityName = JsonSerializer.Deserialize<CheckAbilityRuleBook>(abilityNameJson).Ability;
            
            Singleton<RuleBookController>.Instance.OpenToAbilityPage(abilityName, null, true);
            Singleton<BoardManager>.Instance.StartCoroutine(CloseBook());
            
            var ability = Singleton<RuleBookController>.Instance.PageData.Find((RuleBookPageInfo x) =>
                x.abilityPage && x.pageId == abilityName).ability;

            var info = AbilitiesUtil.GetInfo(ability);

            return info.LocalizedRulebookDescription.Replace("[creature]", Localization.Translate("a card bearing this sigil"));;
        }

        private static IEnumerator CloseBook()
        {
            yield return new WaitForSeconds(5f);
            Singleton<RuleBookController>.Instance.SetShown(false);
        }

        public static string GetCardsInHand()
        {
            var i = 0;
            return Singleton<PlayerHand>.Instance.CardsInHand.Aggregate("", (current, card) => current + $"[{i++} - {card.DescribeToAI()}]");
        }
        
        class PlayCardInHandStructure
        {
            [JsonPropertyName("card_idx")]
            public int CardIndex { get; set; }
            [JsonPropertyName("sacrifice_indexes")]

            public int[] SacrificeIndexes { get; set; }
            [JsonPropertyName("placement_index")]

            public int PlacementIndex { get; set; }
        }
        public static async Task<string> PlayCardInHand(string cardInHandJson)
        {
            var info = JsonSerializer.Deserialize<PlayCardInHandStructure>(cardInHandJson);
            var slot = Singleton<BoardManager>.Instance.PlayerSlotsCopy[info.PlacementIndex];
            if (slot.Card != null && !info.SacrificeIndexes.Contains(info.PlacementIndex))
            {
                return "There is a card in that slot already.";
            }

            var card = Singleton<PlayerHand>.Instance.CardsInHand[info.CardIndex];
            
            // select card before returning so leshy can send you a message about it.
            Singleton<PlayerHand>.Instance.OnCardSelected(card);
            if (!card.CanPlay())
            {
                return $"You were unable to play the {card.name}. The reason why may come soon.";
            }

            await Task.Delay(TimeSpan.FromSeconds(0.75));
            if (info.SacrificeIndexes.Length > 0)
            {
                await Task.Delay(2);
            }
            foreach (var idx in info.SacrificeIndexes)
            {
                var sac = Singleton<BoardManager>.Instance.PlayerSlotsCopy[idx];
                Singleton<BoardManager>.Instance.OnSlotSelected(sac);
                await Task.Delay(TimeSpan.FromSeconds(1.2));
            }
            
            // Singleton<BoardManager>.Instance.StartCoroutine(Singleton<BoardManager>.Instance.AssignCardToSlot(card, slot));
            
            Singleton<BoardManager>.Instance.OnSlotSelected(slot);
            
            return "Placement Successful, (remember to use the new updated indices) hand is now: " + GetCardsInHand();
        }

        public class DrawCardParams
        {
            [JsonPropertyName("card_type")]
            public string CardType { get; set; }
        }

        public static string DrawCardFromDeck(string drawCardJson)
        {
            var info = JsonSerializer.Deserialize<DrawCardParams>(drawCardJson);

            if (info.CardType == "squirrel")
            {
                GetCardFromSquirrelDeck();
            }
            else
            {
                GetCardFromDeck();
            }

            return "New card in hand! (remember to use the new updated indices) Hand is now:" + GetCardsInHand();

        }

        private static void GetCardFromDeck()
        {
            // Entrypoint.Instance.StartCoroutine(Singleton<CardDrawPiles3D>.Instance.DrawCardFromDeck());
            Singleton<CardDrawPiles3D>.Instance.Pile.CursorSelectEnded.Invoke(Singleton<CardDrawPiles3D>.Instance.Pile);
        }
        
        private static void GetCardFromSquirrelDeck()
        {
            // Entrypoint.Instance.StartCoroutine(Singleton<CardDrawPiles3D>.Instance.DrawFromSidePile());
            Singleton<CardDrawPiles3D>.Instance.SidePile.CursorSelectEnded.Invoke(Singleton<CardDrawPiles3D>.Instance.SidePile);
        }

        public static string GetAllCardsInDeck()
        {
            return string.Join(", ", Singleton<CardDrawPiles>.Instance.Deck.Cards.Randomize().Select(c => c.DescribeToAI()));
        }

        public static async Task RunCsCode(string code)
        {
            var opt = ScriptOptions.Default;
            opt = opt.AddReferences(
                AppDomain.CurrentDomain.GetAssemblies()
                    .Where(assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
            );
            opt = opt.AddImports("System", "System.Collections.Generic", "UnityEngine", "Inscryption_ai.Extensions", "DiskCardGame");
            try
            {
                var res = await CSharpScript.RunAsync(code, opt, null, null);
                
                Console.WriteLine(res);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Console.WriteLine("Code execution complete.");
        }
        
        public static async Task SendAllActions()
        {
            await WebsocketManager.Send(new RegisterAction("ring_bell", "rings the combat bell, forcing the next turn to be played", new Dictionary<string, object>() {}));
            await WebsocketManager.Send(new RegisterAction("get_cards_in_hand", "Gets all of the cards currently in hand. Always run and wait for result before playing anything.", new Dictionary<string, object>() {}));
            await WebsocketManager.Send(new RegisterAction("get_ability_info", "Gives you information about the function of an ability", new Dictionary<string, object>()
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
            await WebsocketManager.Send(new RegisterAction("play_card_in_hand", "Gives you information about the function of an ability", new Dictionary<string, object>()
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
            await WebsocketManager.Send(new RegisterAction("see_board_state", "Gives you information about the board", new Dictionary<string, object>() {}));
            await WebsocketManager.Send(new AddEnvironmentContext(
                "Always wait for the results of actions before proceeding to the next step. Check the board state and hand after each action. Play the best card available based on the current board state and hand, focusing on strategy. Prioritize sacrificing cards when necessary to summon more powerful cards. Communicate clearly about actions taken and decisions made during the game."));
            await WebsocketManager.Send(new AddEnvironmentContext(
                "When facing a strong enemy like a Grizzly, prioritize playing defensive cards and using sacrifices wisely. Always check your hand and board after each action. Utilize cards with defensive abilities like the River Snapper when possible. Squirrels are essential for quick sacrifices, enabling powerful plays with cards like Wolves. Remember the power of sigils like Evolve and DebuffEnemy to strengthen your board presence and weaken foes. Plan sacrifices carefully to maintain a balanced hand and responsive board."));
            
        }

        private static void ActionFailure(ActionRequest request, string details)
        {
            Console.WriteLine(details);

            _ = WebsocketManager.Send(new ActionResponse(request.UUID,
                "Say exactly: 'Someone tell rubyboat there is a problem with the inscryption mod' somewhere in your message"));
        }
        
        public static async Task RunActions(List<ActionRequest> actionRequests)
        {
            foreach(var action in actionRequests)
            {
                if (ActionRegistry.ContainsKey(action.Name))
                {
                    try
                    {
                        ActionRegistry[action.Name].Invoke(action.Params);
                    }
                    catch (Exception e)
                    {
                        ActionFailure(action, e.ToString());
                    }
                }else if (AsyncActionRegistry.ContainsKey(action.Name))
                {
                    try
                    {
                        await AsyncActionRegistry[action.Name].Invoke(action.Params);
                    }
                    catch (Exception e)
                    {
                        ActionFailure(action, e.ToString());
                    }
                }
                else
                {
                    ActionFailure(action, "Unknown action");
                }
            }
        }
    }
    
    public class ActionRequest
    {
        public string UUID { get; set; }
        public string Name { get; set; }
        public string Params { get; set; }

        public ActionRequest(string uuid, string name, string parameters)
        {
            UUID = uuid;
            Name = name;
            Params = parameters;
        }
    }
}