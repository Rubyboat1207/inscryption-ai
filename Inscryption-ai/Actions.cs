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
using UnityEngine;

namespace Inscryption_ai
{
    public static class Actions
    {
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

        public static void GetCardFromDeck()
        {
            // Entrypoint.Instance.StartCoroutine(Singleton<CardDrawPiles3D>.Instance.DrawCardFromDeck());
            Singleton<CardDrawPiles3D>.Instance.Pile.CursorSelectEnded.Invoke(Singleton<CardDrawPiles3D>.Instance.Pile);
        }
        
        public static void GetCardFromSquirrelDeck()
        {
            // Entrypoint.Instance.StartCoroutine(Singleton<CardDrawPiles3D>.Instance.DrawFromSidePile());
            Singleton<CardDrawPiles3D>.Instance.SidePile.CursorSelectEnded.Invoke(Singleton<CardDrawPiles3D>.Instance.SidePile);
        }

        public static string GetAllCardsInDeck()
        {
            return string.Join(", ", Singleton<CardDrawPiles>.Instance.Deck.Cards.Randomize().Select(c => c.DescribeToAI()));
        }
    }
}