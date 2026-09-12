using Vintagestory.API.Common;

namespace ExitWarpLimiter;

public class ModConfig
{
    public const string ModVersion = "1.0.0";

    public string Version { get; set; } = ModVersion;
    public bool DisableTranslocatorUsage { get; set; } = true;
    public bool DisableTranslocatorRepair { get; set; } = true;
    public bool EnableChunkSearchLimiter { get; set; } = false;

    public static ModConfig Current { get; private set; }

    private static string ConfigFileName => $"ExitWarpLimiter_v{ModVersion}.json";

    public static void Load(ICoreAPI api)
    {
        try
        {
            Current = api.LoadModConfig<ModConfig>(ConfigFileName);
        }
        catch
        {
            Current = null;
        }

        if (Current == null)
        {
            Current = new ModConfig();
            api.StoreModConfig(Current, ConfigFileName);
        }
    }
}