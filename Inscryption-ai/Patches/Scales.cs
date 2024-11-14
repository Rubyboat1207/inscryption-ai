using DiskCardGame;
using HarmonyLib;

namespace Inscryption_ai.Patches
{
    [HarmonyPatch(typeof(Scales), nameof(Scales.AddDamage))]
    public class OnScalesTipped
    {
        public static void Postfix(ref int ___opponentWeight, ref int ___playerWeight)
        {
            _ = Entrypoint.Instance.Send(
                new AddEnvironmentContext($"The scales have tipped to {___playerWeight - ___opponentWeight} hit 5 to win or -5 to lose"));
        }
    }
}