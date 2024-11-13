using DiskCardGame;
using HarmonyLib;

namespace Inscryption_ai.Patches
{
    [HarmonyPatch(typeof(TextDisplayer), nameof(TextDisplayer.ShowMessage))]
    public class ShowMessage
    {
        public static void Prefix(string message)
        {
            _ = Entrypoint.Instance.Send(new AddEnvironmentContext(message));
        } 
    }
}