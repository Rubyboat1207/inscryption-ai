using System.Linq;
using DiskCardGame;

namespace Inscryption_ai.Extensions
{
    public static class CardInfoExtensions
    {
        public static string DescribeToAI(this CardInfo card, bool includeCost=true)
        {
            var info =
                $"{card.name}. It deals {card.Attack} damage, has {card.Health} health.";

            if (includeCost)
            {
                if (card.BloodCost > 0)
                {
                    info += $"This card costs {card.BloodCost} blood this is the amount of cards you must sacrifice to play this card. ";
                }

                if (card.BonesCost > 0)
                {
                    info += $"This card costs {card.BonesCost} bones. You currently have {Singleton<ResourcesManager>.Instance.PlayerBones} bones. ";
                }
            }

            if (card.Abilities.Count > 0)
            {
                info += $"Abilities: ({string.Join(", ", card.Abilities.Select(a => a.ToString()))})";
            }
            

            return info;
        }
    }
}