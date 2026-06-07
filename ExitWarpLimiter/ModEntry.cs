using System;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace ExitWarpLimiter;
public class ExitWarpLimiterSystem : ModSystem
{
    public ICoreServerAPI ServerApi { get; private set; }
    private Harmony harmony;
    private const string HarmonyId = $"com.furio.twarplimiter";
    public static ExitWarpLimiterSystem Instance { get; private set; }
    private bool disposed;

    public override bool ShouldLoad(EnumAppSide side) => side == EnumAppSide.Server;

    public override void StartServerSide(ICoreServerAPI api)
    {
        base.StartServerSide(api);
        Instance = this;
        ServerApi = api;

        try
        {
            // api.Logger.Notification($"[tWarpLimiter] Concrete WorldManager Type: {api.WorldManager.GetType().FullName}");
            // api.Logger.Notification("[tWarpLimiter] Initializing Harmony hooks...");
            harmony = new Harmony(HarmonyId);
            harmony.PatchAll(System.Reflection.Assembly.GetExecutingAssembly());
            api.Logger.Notification("[tWarpLimiter] Harmony patches applied successfully!");
        }
        catch (Exception ex)
        {
            api.Logger.Error($"[tWarpLimiter] Failed to apply Harmony patches! (e: {ex}");
        }
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
                    TranslocatorPatch.Pass1(__instance, Instance.ServerApi);
                } else
                {
                    TranslocatorPatch.Pass2(__instance, Instance.ServerApi);
                }
            }

            return true;
        }
    }

    public override void Dispose()
    {
        if (disposed) return;
        disposed = true;

        harmony?.UnpatchAll(HarmonyId);
        harmony = null;

        ServerApi = null;

        Instance = null;
        base.Dispose();
    }
}
