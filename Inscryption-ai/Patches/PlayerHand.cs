using DiskCardGame;
using HarmonyLib;

namespace Inscryption_ai.Patches
{
    [HarmonyPatch(typeof(PlayerHand), nameof(PlayerHand.AddCardToHand))]
    public class AddingCardToHand
    {
        public static void Prefix(PlayableCard card)
        {
            string info =
                $"You've gained a new {card.name} in your hand. It deals {card.Attack} damage, has {card.Health} health.";

            if (card.Info.BloodCost > 0)
            {
                info += $"This card costs {card.Info.BloodCost} blood.";
            }

            if (card.Info.BonesCost > 0)
            {
                info += $"This card costs {card.Info.BonesCost} bones. You currently have {Singleton<ResourcesManager>.Instance.PlayerBones} bones.";
            }

            if (card.Info.SpecialAbilities.Count > 0)
            {
                info += "Abilities:";
            }
            
            foreach (var ability in card.Info.Abilities)
            {
                info += "\n  " + ability;
            }



            _ = Entrypoint.Instance.Send(new AddEnvironmentContext(info));
        }
    }
}