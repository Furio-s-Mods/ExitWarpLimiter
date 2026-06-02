using System;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace ExitWarpLimiter
{
    public class ExitWarpLimiterSystem : ModSystem
    {
        public static ICoreServerAPI Sapi { get; private set; }
        private Harmony harmonyInstance;
        private const string HarmonyId = "com.yourname.twarplimiter";

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            Sapi = api;

            try
            {
                // api.Logger.Notification($"[tWarpLimiter] Concrete WorldManager Type: {api.WorldManager.GetType().FullName}");
                // api.Logger.Notification("[tWarpLimiter] Initializing Harmony hooks...");
                harmonyInstance = new Harmony(HarmonyId);
                harmonyInstance.PatchAll(System.Reflection.Assembly.GetExecutingAssembly());
                api.Logger.Notification("[tWarpLimiter] Harmony patches applied successfully!");
            }
            catch (Exception ex)
            {
                api.Logger.Error($"[tWarpLimiter] Failed to apply Harmony patches! (e: {ex}");
            }
        }

        public override void Dispose()
        {
            harmonyInstance?.UnpatchAll(HarmonyId);
            Sapi = null;
            base.Dispose();
        }
        
        
        [HarmonyPatch(typeof(BlockEntityStaticTranslocator), "OnServerGameTick")]
        public static class TranslocatorTickPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(BlockEntityStaticTranslocator __instance, float dt)
            {
                if (__instance.findNextChunk)
                {
                    // Sapi?.Logger.Notification("*** Prefix disables translocator");
                    __instance.findNextChunk = false;

                    if (__instance.tpLocation == null)
                    {
                        TranslocatorPatch.Pass1(__instance, Sapi);
                    } else
                    {
                        TranslocatorPatch.Pass2(__instance, Sapi);
                    }
                }

                return true;
            }
        }
    }
    
}