using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Logging;
// using HarmonyLib;

namespace practice_mod
{
    [BepInPlugin(GUID, MODNAME, VERSION)]
    public class Plugin : BasePlugin
    {
        public const string
            MODNAME = "practice_mod",
            AUTHOR = "daniel_reeder",
            GUID = AUTHOR + "_" + MODNAME,
            VERSION = "0.1.0";

        public Plugin()
        {
            log = Log;
        }

        public override void Load()
        {
            log.LogInfo($"Loading {MODNAME} v{VERSION} by {AUTHOR}");
        }

        public static ManualLogSource log;
    }
}