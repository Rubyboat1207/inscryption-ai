using System.Linq;
using DiskCardGame;

namespace Inscryption_ai.Extensions
{
    public static class PlayableCardExtensions
    {
        public static string DescribeToAI(this PlayableCard card, bool includeCost=true)
        {
            string info =
                $"Card is {card.name}. It deals {card.Attack} damage, has {card.Health} health.";

            if (includeCost)
            {
                if (card.Info.BloodCost > 0)
                {
                    info += $"This card costs {card.Info.BloodCost} blood.";
                }

                if (card.Info.BonesCost > 0)
                {
                    info += $"This card costs {card.Info.BonesCost} bones. You currently have {Singleton<ResourcesManager>.Instance.PlayerBones} bones.";
                }
            }

            if (card.Info.Abilities.Count > 0)
            {
                info += $"Abilities: ({string.Join(", ", card.Info.Abilities.Select(a => a.ToString()))})";
            }
            

            return info;
        }
    }
}