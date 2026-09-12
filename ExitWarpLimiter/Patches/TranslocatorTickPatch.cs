using HarmonyLib;
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
        if (!ModConfig.Current.EnableChunkSearchLimiter)
        {
            return true;
        }

        if (__instance?.Api is ICoreServerAPI serverApi)
        {
            if (__instance.findNextChunk)
            {
                __instance.findNextChunk = false;

                if (!TranslocatorPatch.SearchCache.TryGetValue(__instance, out var searchState) || !searchState.HasCandidateChunk)
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
    
    public const int BATCH_SIZE = 5;
    
    public readonly int[] TargetSlotsX = new int[BATCH_SIZE];
    public readonly int[] TargetSlotsZ = new int[BATCH_SIZE];
    public readonly Action<bool>[] CallbackSlots;
    public readonly BlockPos ScratchPadPos = new(0, 1, 0);

    public int PendingQueries;
    public bool IsBatchActive;
    public int CandidateChunkX;
    public int CandidateChunkZ;
    public bool HasCandidateChunk;

    public TranslocatorSearchState(BlockEntityStaticTranslocator instance, Func<BlockEntityStaticTranslocator, object> getParams)
    {
        _instance = instance;
        
        PeekOptions = new ChunkPeekOptions
        {
            UntilPass = EnumWorldGenPass.TerrainFeatures,
            ChunkGenParams = (ITreeAttribute)getParams(instance),
            OnGenerated = HandleChunkGenerated 
        };

        // Bound exactly once at construction. Costs 0 bytes on the hot path.
        CallbackSlots = new Action<bool>[BATCH_SIZE]
        {
            Slot0, Slot1, Slot2, Slot3, Slot4
        };
    }

    private void Slot0(bool e) => ProcessSlot(0, e);
    private void Slot1(bool e) => ProcessSlot(1, e);
    private void Slot2(bool e) => ProcessSlot(2, e);
    private void Slot3(bool e) => ProcessSlot(3, e);
    private void Slot4(bool e) => ProcessSlot(4, e);

    private void ProcessSlot(int slotIndex, bool exists)
    {
        if (_instance.Api?.World?.BlockAccessor?.GetBlockEntity(_instance.Pos) != _instance) return;
        
        lock (this)
        {
            if (!IsBatchActive) return;

            if (exists)
            {
                IsBatchActive = false;
                CandidateChunkX = TargetSlotsX[slotIndex];
                CandidateChunkZ = TargetSlotsZ[slotIndex];
                HasCandidateChunk = true;
                _instance.findNextChunk = true;
                return;
            }

            PendingQueries--;
            
            if (PendingQueries <= 0)
            {
                IsBatchActive = false;
                _instance.findNextChunk = true;
            }
        }
    }

    private void HandleChunkGenerated(Dictionary<Vec2i, IServerChunk[]> chunks)
    {
        if (_instance.Api?.World?.BlockAccessor?.GetBlockEntity(_instance.Pos) != _instance) return;

        TranslocatorPatch.CallTestForExitPoint(_instance, chunks, CandidateChunkX, CandidateChunkZ);

        if (!_instance.findNextChunk)
        {
            TranslocatorPatch.SearchCache.Remove(_instance);
        }
    }
}