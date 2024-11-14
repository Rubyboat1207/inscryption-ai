using DiskCardGame;
using HarmonyLib;
using Inscryption_ai.Extensions;

namespace Inscryption_ai.Patches
{
    [HarmonyPatch(typeof(CombatPhaseManager), nameof(CombatPhaseManager.DoCombatPhase))]
    public class TurnEndedPatch
    {
        public static void Postfix()
        {
            // _ = Entrypoint.Instance.Send(new AddEnvironmentContext(Singleton<BoardManager>.Instance.DescribeStateToAI()));
            _ = Entrypoint.Instance.Send(new RequestAction());
        }
    }
}