using System.Reflection;
using DiskCardGame;
using HarmonyLib;

namespace Inscryption_ai.Patches
{
    [HarmonyPatch(typeof(Scales3D), nameof(Scales3D.AddDamage))]
    public class OnScalesTipped
    {
        public static void Postfix(Scales3D __instance)
        {
            var playerWeight = (int) typeof(Scales3D).GetField(nameof(Scales3D.playerWeight), BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
            var opponentWeight = (int) typeof(Scales3D).GetField(nameof(Scales3D.opponentWeight), BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
            _ = WebsocketManager.Send(
                new AddEnvironmentContext($"The scales have tipped to {playerWeight - opponentWeight} hit 5 to win or -5 to lose"));
        }
    }
}