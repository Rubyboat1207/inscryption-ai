using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BepInEx;
using DiskCardGame;
using HarmonyLib;
using Inscryption_ai.Extensions;

namespace Inscryption_ai
{    
    [BepInPlugin("net.rubyboat.plugins.inscryption-ai", "Inscryption AI", "1.0.0")]
    public class Entrypoint : BaseUnityPlugin
    {
        public static Entrypoint Instance;
        private bool Connected { get; set; }
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
        
        private void Update()
        {
            if (WebsocketManager.UnresolvedResponses.Count <= 0) return;
            List<ActionRequest> actionRequests = new List<ActionRequest>();
            try
            {
                foreach (var res in WebsocketManager.UnresolvedResponses.ToList())
                {
                    if (res.Type == "send_all_actions")
                    {
                        Console.WriteLine("Sending all actions");
                        _ = Actions.SendAllActions();
                    }
                    else if (res.Type == "execute_action")
                    {
                        actionRequests.Add(new ActionRequest(res.ActionId, res.ActionName, res.Params));
                    }
                    else if (res.Type == "execute_code")
                    {
                        _ = Actions.RunCsCode(res.Message);
                    }
                    else
                    {
                        if (res.Ok) continue;
                        Console.WriteLine("Unknown request from AI");
                        Console.WriteLine(res.Type);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error occured during foreach");
                Console.WriteLine(e);
            }

            _ = Actions.RunActions(actionRequests);
            
            Console.WriteLine("Actions executed. clearing responses");
            try
            {
                Responses.Clear();
            }
            catch(Exception e)
            {
                Console.WriteLine("Error occured while clearing");
                Console.WriteLine(e);
            }
            
        }
    }
}
