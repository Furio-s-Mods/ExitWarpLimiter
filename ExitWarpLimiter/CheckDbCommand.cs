// using System;
// using Vintagestory.API.Common;
// using Vintagestory.API.Config;
// using Vintagestory.API.Server;

// namespace ExitWarpLimiter
// {
//     public static class CheckDbCommand
//     {
//         public static void Register()
//         {
//             var sapi = ExitWarpLimiterSystem.Sapi;
//             var parsers = sapi.ChatCommands.Parsers;

//             sapi.ChatCommands.Create("checkchunk")
//                 .WithDescription("Checks if a 3D chunk exists in the save file using HUD coordinates (X Y Z)")
//                 .WithArgs(parsers.Int("x"), parsers.Int("y"), parsers.Int("z"))
//                 .RequiresPrivilege(Privilege.controlserver)
//                 .HandleWith(OnCommandCheckChunk);

//             // sapi.ChatCommands.Create("checkregion")
//             //     .WithDescription("Checks if a 2D map region exists in the save file using HUD coordinates (X Z)")
//             //     .WithArgs(parsers.Int("x"), parsers.Int("z"))
//             //     .RequiresPrivilege(Privilege.controlserver)
//             //     .HandleWith(OnCommandCheckRegion);
//         }
//         private static TextCommandResult OnCommandCheckChunk(TextCommandCallingArgs args)
//         {
//             ICoreServerAPI sapi = ExitWarpLimiterSystem.Sapi;
//             if (sapi == null) return TextCommandResult.Error("Server API context unavailable.");

//             int hudX = (int)args.Parsers[0].GetValue();
//             int hudY = (int)args.Parsers[1].GetValue();
//             int hudZ = (int)args.Parsers[2].GetValue();

//             // Traduzione nativa usando l'ancora di spawn del mondo corrente
//             double absX = (double)hudX + sapi.World.DefaultSpawnPosition.X;
//             double absZ = (double)hudZ + sapi.World.DefaultSpawnPosition.Z;
//             double absY = (double)hudY;

//             int chunkSize = GlobalConstants.ChunkSize;
//             int chunkX = (int)Math.Floor(absX / (double)chunkSize);
//             int chunkY = (int)Math.Floor(absY / (double)chunkSize);
//             int chunkZ = (int)Math.Floor(absZ / (double)chunkSize);

//             #region Telemetria (Rimuovere quando pronti)
//             IPlayer caller = args.Caller.Player;
//             sapi.SendMessage(caller, 0, $"[DEBUG CHUNK] HUD: [{hudX}, {hudY}, {hudZ}] | Spawn Anchor: [{sapi.World.DefaultSpawnPosition.X:F1}, {sapi.World.DefaultSpawnPosition.Z:F1}]", EnumChatType.Notification);
//             sapi.SendMessage(caller, 0, $"[DEBUG CHUNK] Calcolato Abs: [{absX:F1}, {absY:F1}, {absZ:F1}] -> Chunk Index: [{chunkX}, {chunkY}, {chunkZ}]", EnumChatType.Notification);
//             #endregion

//             sapi.WorldManager.TestChunkExists(chunkX, chunkY, chunkZ, (bool exists) => 
//             {
//                 string message = exists 
//                     ? $"[Success] Chunk [{chunkX}, {chunkY}, {chunkZ}] exists in save."
//                     : $"[Notice] Chunk [{chunkX}, {chunkY}, {chunkZ}] does NOT exist in save.";
                
//                 sapi.SendMessage(args.Caller.Player, 0, message, exists ? EnumChatType.CommandSuccess : EnumChatType.CommandError);
//             });

//             return TextCommandResult.Success("Chunk database query dispatched...");
//         }
//     }
// }
