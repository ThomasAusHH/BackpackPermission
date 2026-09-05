namespace BackpackPermission.Localization
{
    /// <summary>Language selection for the in-game texts.</summary>
    public enum Language
    {
        English,
        Deutsch,
        /// <summary>Follow the game's language setting (German or English).</summary>
        Auto
    }

    /// <summary>All user-visible texts of the mod. English is the default, German is optional.</summary>
    internal static class Strings
    {
        public static string Title => Pick("Backpack Access", "Rucksack-Zugriff");
        public static string Hint => Pick("Click a row to toggle", "Zeile anklicken zum Umschalten");
        public static string WornPack => Pick("Worn pack", "Getragener Rucksack");
        public static string WhilePassedOut => Pick("While passed out", "Bei Ohnmacht");
        public static string Allowed => Pick("Allowed", "Erlaubt");
        public static string Locked => Pick("Locked", "Gesperrt");
        public static string LockedPrompt => Pick("Locked", "Gesperrt");
        public static string Players => Pick("Players", "Mitspieler");
        public static string NoOtherPlayers => Pick("No other players in the lobby", "Keine anderen Spieler in der Runde");

        // Dropped packs
        public static string DroppedPack => Pick("Dropped pack", "Abgelegter Rucksack");
        public static string AfterDeath => Pick("After death", "Nach dem Tod");
        public static string Everyone => Pick("Everyone", "Alle");
        public static string MyList => Pick("My list", "Meine Liste");
        public static string TeamOnly => Pick("Team only", "Nur Team");

        public static string SetTo(string label, string value) => Pick($"{label}: {value}", $"{label}: {value}");

        // Lobby mode and teams
        public static string LobbyMode => Pick("Lobby mode", "Lobby-Modus");
        public static string ModeIndividual => Pick("Individual", "Jeder für sich");
        public static string ModeHost => Pick("Host decides", "Host entscheidet");
        public static string SwitchToHostMode => Pick("Lobby mode: host decides", "Lobby-Modus: Host entscheidet");
        public static string SwitchToIndividualMode => Pick("Lobby mode: everyone for themselves", "Lobby-Modus: jeder für sich");
        public static string HostHint => Pick("Click a player to change their team", "Spieler anklicken, um das Team zu wechseln");
        public static string HostManagesHint => Pick("The host manages access in this lobby", "Der Host verwaltet den Zugriff in dieser Runde");
        public static string Teams => Pick("Teams", "Teams");
        public static string NoTeam => Pick("No team", "Kein Team");
        public static string You => Pick("you", "du");

        public static string Team(string name) => Pick($"Team {name}", $"Team {name}");

        public static string TeamOrNone(string name) => name == null ? NoTeam : Team(name);

        public static string YourTeam(string name) => Pick($"Your team: {TeamOrNone(name)}", $"Dein Team: {TeamOrNone(name)}");

        public static string MoveToTeam(string player, string team)
        {
            return Pick($"Move {player} to {TeamOrNone(team)}", $"{player} nach {TeamOrNone(team)} verschieben");
        }

        public static string Summary(int allowed, int total)
        {
            return Pick($"{allowed} of {total} players allowed", $"{allowed} von {total} Spielern erlaubt");
        }

        public static string AllowPlayer(string name) => Pick($"Allow {name}", $"{name} erlauben");

        public static string LockPlayer(string name) => Pick($"Lock {name}", $"{name} sperren");

        public static string CloseHint(string key) => Pick($"Press {key} to close", $"{key} drücken zum Schließen");

        public static string FallbackPlayerName(int actorNumber) => Pick($"Player {actorNumber}", $"Spieler {actorNumber}");

        private static bool UseGerman
        {
            get
            {
                Language language = Plugin.Settings != null ? Plugin.Settings.Language : Language.English;
                switch (language)
                {
                    case Language.Deutsch:
                        return true;
                    case Language.Auto:
                        return LocalizedText.CURRENT_LANGUAGE == LocalizedText.Language.German;
                    default:
                        return false;
                }
            }
        }

        private static string Pick(string english, string german) => UseGerman ? german : english;
    }
}
