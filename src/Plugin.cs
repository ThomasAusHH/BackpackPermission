using BackpackPermission.Permissions;
using BackpackPermission.UI;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace BackpackPermission
{
    /// <summary>
    /// Plugin entry point. Owns the long-lived services (configuration, the local permission
    /// list, the host settings and their network synchronisation) and installs the Harmony patches.
    /// </summary>
    [BepInPlugin(Guid, Name, Version)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = "com.peakcode.backpackpermission";
        public const string Name = "BackpackPermission";
        public const string Version = "1.1.0";

        internal static ManualLogSource Log { get; private set; }
        internal static ModConfig Settings { get; private set; }
        internal static LocalPermissions Permissions { get; private set; }
        internal static HostSettings Host { get; private set; }
        internal static DroppedPackRegistry DroppedPacks { get; private set; }

        private RuleSync _ruleSync;
        private LobbySync _lobbySync;
        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            Settings = new ModConfig(Config);
            Permissions = new LocalPermissions(Settings);
            Host = new HostSettings(Settings);
            DroppedPacks = new DroppedPackRegistry();
            _ruleSync = new RuleSync(Permissions);
            _lobbySync = new LobbySync(Host);

            _harmony = new Harmony(Guid);
            _harmony.PatchAll(typeof(Plugin).Assembly);

            Log.LogInfo($"{Name} {Version} loaded. Default: nobody may access your backpack.");
        }

        private void Update()
        {
            _ruleSync.Tick();
            _lobbySync.Tick();
            DroppedPacks.Tick();
            StandalonePanel.Tick();
        }

        private void OnDestroy()
        {
            _ruleSync?.Dispose();
            _lobbySync?.Dispose();
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
