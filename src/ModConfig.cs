using BepInEx.Configuration;
using BackpackPermission.Localization;

namespace BackpackPermission
{
    /// <summary>
    /// Typed access to the BepInEx config file. All user-facing keys and descriptions live here.
    /// </summary>
    internal sealed class ModConfig
    {
        private readonly ConfigEntry<bool> _unlockWhilePassedOut;
        private readonly ConfigEntry<bool> _rememberAllowedPlayers;
        private readonly ConfigEntry<Language> _language;
        private readonly ConfigEntry<float> _panelOffsetX;
        private readonly ConfigEntry<float> _panelWidth;
        private readonly ConfigEntry<float> _panelScale;
        private readonly ConfigEntry<string> _allowedPlayers;
        private readonly ConfigEntry<bool> _allowEveryone;
        private readonly ConfigEntry<bool> _verbose;

        public ModConfig(ConfigFile file)
        {
            _unlockWhilePassedOut = file.Bind("General", "UnlockWhilePassedOut", true,
                "While you are passed out on the ground, everyone may access your backpack (for example to help you). " +
                "Can also be toggled inside the backpack wheel.");
            _rememberAllowedPlayers = file.Bind("General", "RememberAllowedPlayers", true,
                "Remember allowed players by their Steam ID so the permission applies again in the next session.");
            _language = file.Bind("General", "Language", Language.English,
                "Language of the in-game texts: English (default), Deutsch, or Auto (follows the game language).");

            _panelOffsetX = file.Bind("UI", "PanelOffsetX", 340f,
                "Horizontal distance of the access panel from the wheel center (UI pixels).");
            _panelWidth = file.Bind("UI", "PanelWidth", 380f, "Width of the access panel.");
            _panelScale = file.Bind("UI", "PanelScale", 1f, "Scale of the access panel.");

            _allowedPlayers = file.Bind("Saved", "AllowedPlayers", "",
                "Managed by the mod: comma separated keys (u:<UserId>) of allowed players.");
            _allowEveryone = file.Bind("Saved", "AllowEveryone", false,
                "Managed by the mod: true = everyone may access your backpack.");

            _verbose = file.Bind("Debug", "Verbose", false, "Verbose logging, including a UI hierarchy dump.");
        }

        public bool UnlockWhilePassedOut
        {
            get => _unlockWhilePassedOut.Value;
            set => _unlockWhilePassedOut.Value = value;
        }

        public bool RememberAllowedPlayers => _rememberAllowedPlayers.Value;

        public Language Language => _language.Value;

        public float PanelOffsetX => _panelOffsetX.Value;

        public float PanelWidth => _panelWidth.Value;

        public float PanelScale => _panelScale.Value;

        public string AllowedPlayers
        {
            get => _allowedPlayers.Value;
            set => _allowedPlayers.Value = value;
        }

        public bool AllowEveryone
        {
            get => _allowEveryone.Value;
            set => _allowEveryone.Value = value;
        }

        public bool Verbose => _verbose.Value;
    }
}
