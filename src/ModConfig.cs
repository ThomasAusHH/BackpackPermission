using BackpackPermission.Localization;
using BackpackPermission.Permissions;
using BepInEx.Configuration;
using UnityEngine.InputSystem;

namespace BackpackPermission
{
    /// <summary>
    /// Typed access to the BepInEx config file. All user-facing keys and descriptions live here.
    /// </summary>
    internal sealed class ModConfig
    {
        private readonly ConfigEntry<bool> _unlockWhilePassedOut;
        private readonly ConfigEntry<bool> _protectDroppedPack;
        private readonly ConfigEntry<bool> _protectDeathDrop;
        private readonly ConfigEntry<bool> _rememberAllowedPlayers;
        private readonly ConfigEntry<Language> _language;
        private readonly ConfigEntry<float> _panelOffsetX;
        private readonly ConfigEntry<float> _panelWidth;
        private readonly ConfigEntry<float> _panelScale;
        private readonly ConfigEntry<Key> _panelHotkey;
        private readonly ConfigEntry<string> _allowedPlayers;
        private readonly ConfigEntry<bool> _allowEveryone;
        private readonly ConfigEntry<LobbyMode> _lobbyMode;
        private readonly ConfigEntry<string> _hostTeams;
        private readonly ConfigEntry<bool> _hostUnlockWhilePassedOut;
        private readonly ConfigEntry<bool> _hostAllowEveryone;
        private readonly ConfigEntry<bool> _hostDroppedPacksTeamOnly;
        private readonly ConfigEntry<bool> _hostDeathDropsTeamOnly;
        private readonly ConfigEntry<bool> _verbose;

        public ModConfig(ConfigFile file)
        {
            _unlockWhilePassedOut = file.Bind("General", "UnlockWhilePassedOut", true,
                "While you are passed out on the ground, everyone may access your backpack (for example to help you). " +
                "Can also be toggled inside the backpack wheel.");
            _protectDroppedPack = file.Bind("General", "ProtectDroppedPack", true,
                "Your permission list keeps applying to your pack after you put it down. Off = anyone may open a pack you dropped.");
            _protectDeathDrop = file.Bind("General", "ProtectPackAfterDeath", false,
                "Your permission list keeps applying to your pack after you died. Off = anyone may loot it, as in the base game.");
            _rememberAllowedPlayers = file.Bind("General", "RememberAllowedPlayers", true,
                "Remember allowed players and host team assignments by Steam ID so they apply again in the next session.");
            _language = file.Bind("General", "Language", Language.English,
                "Language of the in-game texts: English (default), Deutsch, or Auto (follows the game language).");

            _panelOffsetX = file.Bind("UI", "PanelOffsetX", 340f,
                "Horizontal distance of the access panel from the wheel center (UI pixels).");
            _panelWidth = file.Bind("UI", "PanelWidth", 380f, "Width of the access panel.");
            _panelScale = file.Bind("UI", "PanelScale", 1f, "Scale of the access panel.");
            _panelHotkey = file.Bind("UI", "PanelHotkey", Key.F7,
                "Key that opens the Backpack Access panel without a backpack, for example as host without a pack. " +
                "Press again to close. None disables the hotkey.");

            _allowedPlayers = file.Bind("Saved", "AllowedPlayers", "",
                "Managed by the mod: comma separated keys (u:<UserId>) of allowed players.");
            _allowEveryone = file.Bind("Saved", "AllowEveryone", false,
                "Managed by the mod: true = everyone may access your backpack.");

            _lobbyMode = file.Bind("Host", "LobbyMode", LobbyMode.Individual,
                "Applies when you are the host. Individual: every wearer manages their own list. " +
                "HostControlled: you assign teams, team mates may access each other's packs and individual lists are ignored. " +
                "Can also be toggled inside the backpack wheel.");
            _hostTeams = file.Bind("Host", "Teams", "",
                "Managed by the mod: comma separated team assignments (u:<UserId>=1..4).");
            _hostUnlockWhilePassedOut = file.Bind("Host", "UnlockWhilePassedOut", true,
                "Host controlled lobbies: everyone may access the pack of a passed out player.");
            _hostAllowEveryone = file.Bind("Host", "AllowEveryone", false,
                "Host controlled lobbies: everyone may access every pack.");
            _hostDroppedPacksTeamOnly = file.Bind("Host", "DroppedPacksTeamOnly", true,
                "Host controlled lobbies: a pack a player put down stays restricted to that player's team.");
            _hostDeathDropsTeamOnly = file.Bind("Host", "DeathDropsTeamOnly", false,
                "Host controlled lobbies: a pack dropped on death stays restricted to that player's team. Off = anyone may loot it.");

            _verbose = file.Bind("Debug", "Verbose", false, "Verbose logging, including a UI hierarchy dump.");
        }

        public bool UnlockWhilePassedOut
        {
            get => _unlockWhilePassedOut.Value;
            set => _unlockWhilePassedOut.Value = value;
        }

        public bool ProtectDroppedPack
        {
            get => _protectDroppedPack.Value;
            set => _protectDroppedPack.Value = value;
        }

        public bool ProtectDeathDrop
        {
            get => _protectDeathDrop.Value;
            set => _protectDeathDrop.Value = value;
        }

        public bool RememberAllowedPlayers => _rememberAllowedPlayers.Value;

        public Language Language => _language.Value;

        public float PanelOffsetX => _panelOffsetX.Value;

        public float PanelWidth => _panelWidth.Value;

        public float PanelScale => _panelScale.Value;

        public Key PanelHotkey => _panelHotkey.Value;

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

        public LobbyMode LobbyMode
        {
            get => _lobbyMode.Value;
            set => _lobbyMode.Value = value;
        }

        public string HostTeams
        {
            get => _hostTeams.Value;
            set => _hostTeams.Value = value;
        }

        public bool HostUnlockWhilePassedOut
        {
            get => _hostUnlockWhilePassedOut.Value;
            set => _hostUnlockWhilePassedOut.Value = value;
        }

        public bool HostAllowEveryone
        {
            get => _hostAllowEveryone.Value;
            set => _hostAllowEveryone.Value = value;
        }

        public bool HostDroppedPacksTeamOnly
        {
            get => _hostDroppedPacksTeamOnly.Value;
            set => _hostDroppedPacksTeamOnly.Value = value;
        }

        public bool HostDeathDropsTeamOnly
        {
            get => _hostDeathDropsTeamOnly.Value;
            set => _hostDeathDropsTeamOnly.Value = value;
        }

        public bool Verbose => _verbose.Value;
    }
}
