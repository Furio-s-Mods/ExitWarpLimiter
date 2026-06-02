using System;
using System.Collections.Generic;
using HarmonyLib;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace ExitWarpLimiter
{
    class TranslocatorPatch
    {
        
        // Creates a high-performance direct delegate to the private methods
        private static readonly Func<BlockEntityStaticTranslocator, object> GetChunkGenParams =
            AccessTools.MethodDelegate<Func<BlockEntityStaticTranslocator, object>>(
                AccessTools.Method(typeof(BlockEntityStaticTranslocator), "chunkGenParams")
            );

        private static readonly Action<BlockEntityStaticTranslocator, Dictionary<Vec2i, IServerChunk[]>, int, int> CallTestForExitPoint =
            AccessTools.MethodDelegate<Action<BlockEntityStaticTranslocator, Dictionary<Vec2i, IServerChunk[]>, int, int>>(
                AccessTools.Method(typeof(BlockEntityStaticTranslocator), "TestForExitPoint")
            );

        public static ChunkPeekOptions PreparePeekOptionsFast(BlockEntityStaticTranslocator instance, int chunkX, int chunkZ)
        {
            return new ChunkPeekOptions
            {
                UntilPass = EnumWorldGenPass.TerrainFeatures,
                ChunkGenParams = (ITreeAttribute)GetChunkGenParams(instance),
                OnGenerated = (chunks) => CallTestForExitPoint(instance, chunks, chunkX, chunkZ)
            };
        }

        // ==========================================
        // PASS1
        // ==========================================
        public static void Pass1(BlockEntityStaticTranslocator instance, ICoreServerAPI Sapi)
        {
            ICoreServerAPI sapi = Sapi;
            // Sapi?.Logger.Notification("*** Pass1");
            int addrange = instance.MaxTeleporterRangeInBlocks - instance.MinTeleporterRangeInBlocks;

            int dx = (int)(instance.MinTeleporterRangeInBlocks + sapi.World.Rand.NextDouble() * addrange) * (2 * sapi.World.Rand.Next(2) - 1);
            int dz = (int)(instance.MinTeleporterRangeInBlocks + sapi.World.Rand.NextDouble() * addrange) * (2 * sapi.World.Rand.Next(2) - 1);

            int chunkX = (instance.Pos.X + dx) / GlobalConstants.ChunkSize;
            int chunkZ = (instance.Pos.Z + dz) / GlobalConstants.ChunkSize;

            BlockPos tpos = new(instance.Pos.X + dx, 1, instance.Pos.Z + dz);
            if (!sapi.World.BlockAccessor.IsValidPos(tpos))
            {
                instance.findNextChunk = true;
                return;
            }
            
            MyTestFunction([], chunkX, chunkZ, instance, sapi);
        }

        private static void MyTestFunction(Dictionary<Vec2i, IServerChunk[]> _, int centerCx, int centerCz, BlockEntityStaticTranslocator instance, ICoreServerAPI Sapi)
        {
            ICoreServerAPI sapi = Sapi;   
            sapi.WorldManager.TestChunkExists(centerCx, 1, centerCz, exists =>
            {
                string message = exists 
                    ? $"[Success] Chunk [{centerCx}, 1, {centerCz}] exists in save."
                    : $"[Notice] Chunk [{centerCx}, 1, {centerCz}] does NOT exist in save.";
                
                // sapi.Logger.Notification(message);
                
                if (exists)
                {
                    instance.tpLocation = new BlockPos(centerCx, 1, centerCz);
                }
                instance.findNextChunk = true;
            });
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

            ChunkPeekOptions opts = PreparePeekOptionsFast(instance, chunkX, chunkZ);

            sapi.WorldManager.PeekChunkColumn(chunkX, chunkZ, opts);
        }
    }
}