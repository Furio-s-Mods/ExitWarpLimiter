using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace ExitWarpLimiter;

[HarmonyPatch(typeof(BlockEntityStaticTranslocator), "OnServerGameTick")]
public static class TranslocatorTickPatch
{
    [HarmonyPrefix]
    public static bool Prefix(BlockEntityStaticTranslocator __instance, float dt)
    {
        if (__instance?.Api is ICoreServerAPI serverApi)
        {
            if (__instance.findNextChunk)
            {
                // Sapi?.Logger.Notification("*** Prefix disables translocator");
                __instance.findNextChunk = false;

                if (__instance.tpLocation == null)
                {
                    TranslocatorPatch.Pass1(__instance, serverApi);
                } else
                {
                    TranslocatorPatch.Pass2(__instance, serverApi);
                }
            }
        }

        return true;
    }
}

// State container for the translocator's search lifecycle
class TranslocatorSearchState
{
    private readonly BlockEntityStaticTranslocator _instance;
    public readonly ChunkPeekOptions PeekOptions;
    public readonly Action<bool> ChunkExistsCallback;
    public int CurrentTargetX;
    public int CurrentTargetZ;

    public TranslocatorSearchState(BlockEntityStaticTranslocator instance, Func<BlockEntityStaticTranslocator, object> getParams)
    {
        _instance = instance;
        
        PeekOptions = new ChunkPeekOptions
        {
            UntilPass = EnumWorldGenPass.TerrainFeatures,
            ChunkGenParams = (ITreeAttribute)getParams(instance),
            OnGenerated = HandleChunkGenerated 
        };
        ChunkExistsCallback = HandleChunkExists;
    }

    private void HandleChunkExists(bool exists)
    {
        if (exists)
        {
            _instance.tpLocation = new BlockPos(CurrentTargetX, 1, CurrentTargetZ);
        }
        _instance.findNextChunk = true;
    }

    private void HandleChunkGenerated(Dictionary<Vec2i, IServerChunk[]> chunks)
    {
        TranslocatorPatch.CallTestForExitPoint(_instance, chunks, CurrentTargetX, CurrentTargetZ);
        if (!_instance.findNextChunk)
        {
            TranslocatorPatch.SearchCache.Remove(_instance);
        }
    }
}

class TranslocatorPatch
{
    public static readonly ConditionalWeakTable<BlockEntityStaticTranslocator, TranslocatorSearchState> SearchCache = new();
    
    // Creates a high-performance direct delegate to the private methods
    public static readonly Func<BlockEntityStaticTranslocator, object> GetChunkGenParams =
        AccessTools.MethodDelegate<Func<BlockEntityStaticTranslocator, object>>(
            AccessTools.Method(typeof(BlockEntityStaticTranslocator), "chunkGenParams")
        );

    public static readonly Action<BlockEntityStaticTranslocator, Dictionary<Vec2i, IServerChunk[]>, int, int> CallTestForExitPoint =
        AccessTools.MethodDelegate<Action<BlockEntityStaticTranslocator, Dictionary<Vec2i, IServerChunk[]>, int, int>>(
            AccessTools.Method(typeof(BlockEntityStaticTranslocator), "TestForExitPoint")
        );
    
    private static readonly BlockPos ScratchPadPos = new(0, 1, 0);

    // ==========================================
    // PASS1
    // ==========================================
    public static void Pass1(BlockEntityStaticTranslocator instance, ICoreServerAPI Sapi)
    {
        ICoreServerAPI sapi = Sapi;
        var searchState = SearchCache.GetValue(instance, inst => new TranslocatorSearchState(inst, GetChunkGenParams));
        // Sapi?.Logger.Notification("*** Pass1");
        int addrange = instance.MaxTeleporterRangeInBlocks - instance.MinTeleporterRangeInBlocks;

        int dx = (int)(instance.MinTeleporterRangeInBlocks + sapi.World.Rand.NextDouble() * addrange) * (2 * sapi.World.Rand.Next(2) - 1);
        int dz = (int)(instance.MinTeleporterRangeInBlocks + sapi.World.Rand.NextDouble() * addrange) * (2 * sapi.World.Rand.Next(2) - 1);

        int chunkX = (instance.Pos.X + dx) / GlobalConstants.ChunkSize;
        int chunkZ = (instance.Pos.Z + dz) / GlobalConstants.ChunkSize;

        ScratchPadPos.Set(instance.Pos.X + dx, 1, instance.Pos.Z + dz);
        ScratchPadPos.dimension = instance.Pos.dimension;

        if (!sapi.World.BlockAccessor.IsValidPos(ScratchPadPos))
        {
            instance.findNextChunk = true;
            return;
        }
        
        sapi.WorldManager.TestChunkExists(chunkX, 1, chunkZ, searchState.ChunkExistsCallback);
    }

    // ==========================================
    // PASS2
    // ==========================================
    public static void Pass2(BlockEntityStaticTranslocator instance, ICoreServerAPI Sapi)
    {
        ICoreServerAPI sapi = Sapi;
        // sapi?.Logger.Notification("*** Pass2");
        int chunkX = instance.tpLocation.X;
        int chunkZ = instance.tpLocation.Z;
        instance.tpLocation = null;
        instance.findNextChunk = false;

        // var searchState = SearchCache.GetOrCreateValue(instance);
        var searchState = SearchCache.GetValue(instance, inst => new TranslocatorSearchState(inst, GetChunkGenParams));

        searchState.CurrentTargetX = chunkX;
        searchState.CurrentTargetZ = chunkZ;

        sapi.WorldManager.PeekChunkColumn(chunkX, chunkZ, searchState.PeekOptions);
    }
}
