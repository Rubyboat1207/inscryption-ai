using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using DiskCardGame;
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

        class CheckAbilityRuleBook
        {
            [JsonPropertyName("ability")]
            public string Ability { get; set; }
        }
        
        public static string CheckRuleBook(string abilityNameJson)
        {
            string abilityName = JsonSerializer.Deserialize<CheckAbilityRuleBook>(abilityNameJson).Ability;
            
            Singleton<RuleBookController>.Instance.OpenToAbilityPage(abilityName, null, true);
            Entrypoint.Instance.StartCoroutine(CloseBook());
            
            Ability ability = Singleton<RuleBookController>.Instance.PageData.Find((RuleBookPageInfo x) =>
                x.abilityPage && x.pageId == abilityName).ability;

            AbilityInfo info = AbilitiesUtil.GetInfo(ability);

            return info.LocalizedRulebookDescription.Replace("[creature]", Localization.Translate("a card bearing this sigil"));;
        }

        private static IEnumerator CloseBook()
        {
            yield return new WaitForSeconds(1.5f);
            Singleton<RuleBookController>.Instance.SetShown(false);
        }
    }
}