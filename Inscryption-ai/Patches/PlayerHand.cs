using DiskCardGame;
using HarmonyLib;
using Inscryption_ai.Extensions;

namespace Inscryption_ai.Patches
{
    [HarmonyPatch(typeof(PlayerHand), nameof(PlayerHand.AddCardToHand))]
    public class AddCardToHandPatch
    {
        public static void Prefix(PlayableCard card)
        {
            _ = WebsocketManager.Send(new AddEnvironmentContext("[NEW CARD IN HAND]: " + card.DescribeToAI()));
        }
    }
}