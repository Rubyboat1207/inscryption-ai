using DiskCardGame;
using HarmonyLib;

namespace Inscryption_ai.Patches
{
    [HarmonyPatch(typeof(TurnManager), nameof(TurnManager.StartGame), typeof(EncounterData))]
    public class StartGamePatch
    {
        public static void Prefix(EncounterData encounterData)
        {
            string info = "A battle has started.";

            if (encounterData.opponentTotem != null)
            {
                info += "\nThe opponent has a totem which gives the " + encounterData.opponentTotem.bottom.effectParams.ability + " effect on any cards of tribe " + encounterData.opponentTotem.top.prerequisites.tribe + ".";
            }

            info += $"The battle is of type {encounterData.opponentType} at difficulty {encounterData.Difficulty}.";
            
            _ = Entrypoint.Instance.Send(new AddEnvironmentContext(info));
        }
    }
}