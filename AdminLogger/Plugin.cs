using HarmonyLib;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;

namespace Mistaken.AdminLogger;

internal sealed class Plugin
{
    public static Plugin Instance { get; private set; }

    [PluginConfig]
    public Config Config;

    private static readonly Harmony Harmony = new("mistaken.adminlogger.patch");

    [PluginPriority(LoadPriority.Lowest)]
    [PluginEntryPoint("Admin Logger", "1.0.0", "Admin Logger", "Mistaken Devs")]
    private void Load()
    {
        Instance = this;
        new LoggingHandler();
        Harmony.PatchAll();
    }

    [PluginUnload]
    private void Unload()
    {
        Harmony.UnpatchAll();
    }
}
