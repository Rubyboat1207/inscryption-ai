using System;
using BepInEx;
using DiskCardGame;
using HarmonyLib;

namespace Inscryption_ai
{    
    [BepInPlugin("net.rubyboat.plugins.inscryption-ai", "Inscryption AI", "1.0.0")]
    public class Entrypoint : BaseUnityPlugin
    {
        public static Entrypoint Instance;
        public Entrypoint() : base()
        {
            var harmony = new Harmony(Info.Metadata.GUID);
            
            harmony.PatchAll();
        }
        
        private void Awake()
        {
            Console.WriteLine("Entrypoint: Awake called");
            if (Instance != null)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }
    }
}
