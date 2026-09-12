using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace ExitWarpLimiter;

[HarmonyPatch(typeof(BlockEntityTeleporterBase), nameof(BlockEntityTeleporterBase.OnEntityCollide))]
public static class DisableTeleporterPatch
{
    [HarmonyPrefix]
    public static bool Prefix(BlockEntityTeleporterBase __instance, Entity entity)
    {
        if (!ModConfig.Current.DisableTranslocatorUsage)
        {
            return true;
        }

        if ((entity as EntityPlayer)?.Player is IServerPlayer serverPlayer)
        {
            long now = entity.World.ElapsedMilliseconds;
            long lastWarnMs = entity.Attributes.GetLong("lastTranslocatorWarnMs", 0);

            if (now - lastWarnMs > 2000)
            {
                entity.Attributes.SetLong("lastTranslocatorWarnMs", now);
                serverPlayer.SendIngameError("translocatordisabled", "Translocators are disabled on this server!");
            }
        }

        return false;
    }
}