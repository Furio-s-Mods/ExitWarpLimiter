using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace ExitWarpLimiter;
public class ExitWarpLimiterSystem : ModSystem
{
    private int disposed = 0;
    private Harmony harmony;
    public const string ModName = "twarplimiter";
    private const string HarmonyId = $"com.furio.{ModName}";

    public override bool ShouldLoad(EnumAppSide side) => side == EnumAppSide.Server;

    public override void StartServerSide(ICoreServerAPI api)
    {
        base.StartServerSide(api);

        ModConfig.Load(api);

        try
        {
            harmony = new Harmony(HarmonyId);
            harmony.PatchAll(System.Reflection.Assembly.GetExecutingAssembly());
        }
        catch (Exception ex)
        {
            api.Logger.Error($"[{ModName}] Failed to apply Harmony patches! (e: {ex}");
        }
    }

    public override void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) == 1) return;

        harmony?.UnpatchAll(HarmonyId);
        harmony = null;

        base.Dispose();
    }
}
