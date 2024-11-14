using System.Linq;
using DiskCardGame;

namespace Inscryption_ai.Extensions
{
    public static class PlayableCardExtensions
    {
        public static string DescribeToAI(this PlayableCard card, bool includeCost=true)
        {
            var info =
                $"{card.name}. It deals {card.Attack} damage, has {card.Health - card.Status.damageTaken} health.";

            if (includeCost)
            {
                if (card.Info.BloodCost > 0)
                {
                    info += $"This card costs {card.Info.BloodCost} blood this is the amount of cards you must sacrifice to play this card. ";
                }

                if (card.Info.BonesCost > 0)
                {
                    info += $"This card costs {card.Info.BonesCost} bones. You currently have {Singleton<ResourcesManager>.Instance.PlayerBones} bones. ";
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