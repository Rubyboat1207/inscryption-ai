using System.Collections.Generic;
using DiskCardGame;
using HarmonyLib;

namespace Inscryption_ai.Patches
{
    [HarmonyPatch(typeof(CardDrawPiles3D), nameof(CardDrawPiles3D.ChooseDraw))]
    public class OnDraw
    {
        public static void Prefix()
        {
            _ = Entrypoint.Instance.Send(new RegisterEphemeralActionGroup(new List<EphemeralAction>
            {
                new EphemeralAction(
                    "draw_from_deck", 
                    "at the end of your turn, you must draw from the deck. no other actions can be played until this has been",
                    new Dictionary<string, object>
                    {
                        { "type", "object" },
                        { "properties", new Dictionary<string, object>
                            {
                                { "card_type", new Dictionary<string, object>
                                    {
                                        { "type", "string" },
                                        { "enum", new[] { "squirrel", "main" } },
                                        { "description", "Specifies the type of card; must be either 'squirrel' or 'main'. Squirrels can be sacrificed for 1 blood or used as a shield blocking all incoming damage from an opposing space. main gets a card out of your deck randomly." }
                                    }
                                }
                            }
                        }
                    }
                )
            }));
            _ = Entrypoint.Instance.Send(new ForceAction("draw_from_deck"));
            _ = Entrypoint.Instance.Send(new RequestAction());
        }
    }
}