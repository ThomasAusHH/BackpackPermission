using BackpackPermission.Permissions;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace BackpackPermission
{
    /// <summary>
    /// Plugin entry point. Owns the long-lived services (configuration, the local permission
    /// list and its network synchronisation) and installs the Harmony patches.
    /// </summary>
    [BepInPlugin(Guid, Name, Version)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = "com.peakcode.backpackpermission";
        public const string Name = "BackpackPermission";
        public const string Version = "1.0.0";

        internal static ManualLogSource Log { get; private set; }
        internal static ModConfig Settings { get; private set; }
        internal static LocalPermissions Permissions { get; private set; }

        private RuleSync _ruleSync;
        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            Settings = new ModConfig(Config);
            Permissions = new LocalPermissions(Settings);
            _ruleSync = new RuleSync(Permissions);

            _harmony = new Harmony(Guid);
            _harmony.PatchAll(typeof(Plugin).Assembly);

            Log.LogInfo($"{Name} {Version} loaded. Default: nobody may access your backpack.");
        }

        private void Update()
        {
            _ruleSync.Tick();
        }

        private void OnDestroy()
        {
            _ruleSync?.Dispose();
            _harmony?.UnpatchSelf();
        }

        /// <summary>Logs at info level only when verbose logging is enabled in the config.</summary>
        internal static void LogVerbose(string message)
        {
            if (Settings != null && Settings.Verbose)
            {
                Log.LogInfo(message);
            }
        }
    }
}
