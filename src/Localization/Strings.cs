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
        public static string AllowEveryone => Pick("Allow everyone", "Alle erlauben");
        public static string UnlockWhilePassedOut => Pick("Unlock while passed out", "Offen bei Ohnmacht");
        public static string On => Pick("On", "An");
        public static string Off => Pick("Off", "Aus");
        public static string Allowed => Pick("Allowed", "Erlaubt");
        public static string Locked => Pick("Locked", "Gesperrt");
        public static string LockedPrompt => Pick("Locked", "Gesperrt");
        public static string Players => Pick("Players", "Mitspieler");
        public static string NoOtherPlayers => Pick("No other players in the lobby", "Keine anderen Spieler in der Runde");

        public static string Summary(int allowed, int total)
        {
            return Pick($"{allowed} of {total} players allowed", $"{allowed} von {total} Spielern erlaubt");
        }

        public static string AllowPlayer(string name) => Pick($"Allow {name}", $"{name} erlauben");

        public static string LockPlayer(string name) => Pick($"Lock {name}", $"{name} sperren");

        public static string Toggle(string label, bool turnOn)
        {
            return Pick($"{label}: turn {(turnOn ? "on" : "off")}", $"{label}: {(turnOn ? "einschalten" : "ausschalten")}");
        }

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
