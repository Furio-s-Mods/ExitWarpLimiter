using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace ExitWarpLimiter;

[HarmonyPatch(typeof(BlockStaticTranslocator), nameof(BlockStaticTranslocator.OnBlockInteractStart))]
public static class DisableTranslocatorRepairPatch
{
    [HarmonyPrefix]
    public static bool Prefix(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref bool __result)
    {
        if (world.Side != EnumAppSide.Server)
        {
            return true;
        }

        if (!ModConfig.Current.DisableTranslocatorRepair)
        {
            return true;
        }

        ItemSlot slot = byPlayer?.InventoryManager?.ActiveHotbarSlot;
        
        if (slot != null && !slot.Empty)
        {
            bool isMetalParts = slot.Itemstack.Collectible.Code.Path == "metal-parts";
            bool isTemporalGear = slot.Itemstack.Collectible is ItemTemporalGear;

            if (isMetalParts || isTemporalGear)
            {
                if (byPlayer is IServerPlayer serverPlayer)
                {
                    serverPlayer.SendIngameError(
                        "translocatorrepairdisabled",
                        "Translocator repairs are disabled on this server!"
                    );
                }

                // Force server inventory and block state resync to snap client back to reality
                slot.MarkDirty();
                if (blockSel?.Position != null)
                {
                    world.BlockAccessor.MarkBlockDirty(blockSel.Position);
                    world.BlockAccessor.MarkBlockEntityDirty(blockSel.Position);
                }

                __result = false;
                return false;
            }
        }

        return true;
    }
}