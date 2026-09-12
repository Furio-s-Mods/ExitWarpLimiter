using System.Runtime.CompilerServices;
using HarmonyLib;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace ExitWarpLimiter;

class TranslocatorPatch
{
    public static readonly ConditionalWeakTable<BlockEntityStaticTranslocator, TranslocatorSearchState> SearchCache = new();
    
    public static readonly Func<BlockEntityStaticTranslocator, object> GetChunkGenParams =
        AccessTools.MethodDelegate<Func<BlockEntityStaticTranslocator, object>>(
            AccessTools.Method(typeof(BlockEntityStaticTranslocator), "chunkGenParams")
        );

    public static readonly Action<BlockEntityStaticTranslocator, Dictionary<Vec2i, IServerChunk[]>, int, int> CallTestForExitPoint =
        AccessTools.MethodDelegate<Action<BlockEntityStaticTranslocator, Dictionary<Vec2i, IServerChunk[]>, int, int>>(
            AccessTools.Method(typeof(BlockEntityStaticTranslocator), "TestForExitPoint")
        );

    // ==========================================
    // PASS 1: Zero-Allocation Batched Selection
    // ==========================================
    public static void Pass1(BlockEntityStaticTranslocator instance, ICoreServerAPI sapi)
    {
        if (!SearchCache.TryGetValue(instance, out var searchState))
        {
            searchState = new TranslocatorSearchState(instance, GetChunkGenParams);
            SearchCache.Add(instance, searchState);
        }
        
        if (searchState.IsBatchActive) return;

        // Prevent vanilla OnServerGameTick from triggering its own search while async queries run
        instance.findNextChunk = false;

        int addrange = instance.MaxTeleporterRangeInBlocks - instance.MinTeleporterRangeInBlocks;
        BlockPos scratch = searchState.ScratchPadPos;
        int validCount = 0;

        for (int i = 0; i < TranslocatorSearchState.BATCH_SIZE; i++)
        {
            int dx = (int)(instance.MinTeleporterRangeInBlocks + sapi.World.Rand.NextDouble() * addrange) * (2 * sapi.World.Rand.Next(2) - 1);
            int dz = (int)(instance.MinTeleporterRangeInBlocks + sapi.World.Rand.NextDouble() * addrange) * (2 * sapi.World.Rand.Next(2) - 1);

            int chunkX = (instance.Pos.X + dx) / GlobalConstants.ChunkSize;
            int chunkZ = (instance.Pos.Z + dz) / GlobalConstants.ChunkSize;

            scratch.Set(instance.Pos.X + dx, 1, instance.Pos.Z + dz);
            scratch.dimension = instance.Pos.dimension;

            if (!sapi.World.BlockAccessor.IsValidPos(scratch)) continue;

            searchState.TargetSlotsX[validCount] = chunkX;
            searchState.TargetSlotsZ[validCount] = chunkZ;
            validCount++;
        }

        if (validCount == 0)
        {
            searchState.IsBatchActive = false;
            instance.findNextChunk = true;
            return;
        }

        searchState.PendingQueries = validCount;
        searchState.IsBatchActive = true;

        for (int i = 0; i < validCount; i++)
        {
            sapi.WorldManager.TestChunkExists(searchState.TargetSlotsX[i], 1, searchState.TargetSlotsZ[i], searchState.CallbackSlots[i]);
        }
    }

    // ==========================================
    // PASS 2: Native Hook Execution
    // ==========================================
    public static void Pass2(BlockEntityStaticTranslocator instance, ICoreServerAPI sapi)
    {
        if (SearchCache.TryGetValue(instance, out var searchState))
        {
            searchState.HasCandidateChunk = false;
            instance.findNextChunk = false;

            sapi.WorldManager.PeekChunkColumn(searchState.CandidateChunkX, searchState.CandidateChunkZ, searchState.PeekOptions);
        }
    }
}