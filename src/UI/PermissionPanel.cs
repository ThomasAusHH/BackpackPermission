using System;
using System.Collections.Generic;
using System.Linq;
using BackpackPermission.Localization;
using BackpackPermission.Permissions;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackPermission.UI
{
    /// <summary>
    /// The access panel shown next to the backpack wheel while the local player's own pack is
    /// open. Built from the HUD's hotbar vocabulary (shadow, soft outline, cream caption font).
    /// Rows are clicked while the interact key keeps the wheel open, so several permissions can be
    /// changed in one go; the panel rebuilds in place after every click.
    /// </summary>
    /// <remarks>
    /// Three views: the individual permission list (default), the host's team editor when the
    /// host controls the lobby, and a read-only team overview for everyone else in that mode.
    /// </remarks>
    internal sealed class PermissionPanel : MonoBehaviour
    {
        private const float Padding = 12f;
        private const float RowHeight = 46f;
        private const float RowGap = 10f;
        private const float SectionGap = 18f;
        private const float RowTextInset = 18f;
        private const float StatusWidth = 130f;
        private const float OutlineBleed = 4f;
        private const float MinWidth = 260f;
        private const float LineHeightFactor = 1.3f;

        private static bool _hierarchyDumped;

        private readonly List<GameObject> _content = new List<GameObject>();
        private BackpackWheel _wheel;
        private RectTransform _rect;
        private bool _centered;
        private float _width;
        private float _inner;

        /// <summary>The panel currently attached to the wheel, or null (also after Unity destroyed it).</summary>
        public static PermissionPanel Instance { get; private set; }

        /// <summary>The row under the pointer, or null.</summary>
        public PermissionRow HoveredRow { get; private set; }

        /// <summary>Attaches the panel to the wheel (creating it on first use), rebuilds its rows and shows it.</summary>
        public static void ShowFor(BackpackWheel wheel)
        {
            if (wheel == null)
            {
                return;
            }
            try
            {
                HudStyle.EnsureCaptured(wheel);
                if (Plugin.Settings.Verbose && !_hierarchyDumped)
                {
                    _hierarchyDumped = true;
                    UiHierarchyDump.Log(wheel);
                }

                PermissionPanel panel = GetOrCreate(wheel);
                panel.Rebuild();
                panel.gameObject.SetActive(true);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Failed to build the access panel: {e}");
            }
        }

        public static void HideIfOpen()
        {
            if (Instance != null)
            {
                Instance.Hide();
            }
        }

        public void Hide()
        {
            ClearWheelCaption();
            HoveredRow = null;
            gameObject.SetActive(false);
        }

        internal void OnRowClicked(PermissionRow row)
        {
            row.Activate();
            // The rows are recreated; the pointer-enter event of the new row under the cursor
            // restores the hover state on the next frame.
            Rebuild();
        }

        internal void OnRowEntered(PermissionRow row)
        {
            HoveredRow = row;
            SetWheelCaption(row.Caption);
        }

        internal void OnRowExited(PermissionRow row)
        {
            if (HoveredRow == row)
            {
                HoveredRow = null;
                ClearWheelCaption();
            }
        }

        /// <summary>Shows the panel centred on screen, without a wheel. Used by the hotkey.</summary>
        public void ShowCentered()
        {
            _centered = true;
            Rebuild();
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Creates a panel under <paramref name="parent"/>. <paramref name="wheel"/> may be null for a
        /// standalone panel; it is only used for the hover caption.
        /// </summary>
        public static PermissionPanel Create(Transform parent, BackpackWheel wheel, string name)
        {
            // No background of its own: the wheel (or the darkened HUD) already provides contrast.
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(PermissionPanel));
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().ignoreLayout = true;

            PermissionPanel panel = go.GetComponent<PermissionPanel>();
            panel._wheel = wheel;
            panel._rect = go.GetComponent<RectTransform>();
            panel._rect.anchorMin = new Vector2(0.5f, 0.5f);
            panel._rect.anchorMax = new Vector2(0.5f, 0.5f);
            panel._rect.pivot = new Vector2(0f, 0.5f);
            go.SetActive(false);
            return panel;
        }

        private static PermissionPanel GetOrCreate(BackpackWheel wheel)
        {
            if (Instance != null && Instance._wheel == wheel)
            {
                return Instance;
            }
            if (Instance != null)
            {
                Destroy(Instance.gameObject);
            }
            Instance = Create(wheel.transform, wheel, "BackpackPermissionPanel");
            return Instance;
        }

        // ------------------------------------------------------------------
        // Layout
        // ------------------------------------------------------------------

        private void Rebuild()
        {
            HoveredRow = null;
            foreach (GameObject go in _content)
            {
                if (go != null)
                {
                    Destroy(go);
                }
            }
            _content.Clear();

            _width = Mathf.Max(MinWidth, Plugin.Settings.PanelWidth);
            _inner = _width - 2f * Padding;
            float y = 0f;

            y -= AddLabel(Strings.Title, 32f, y, 1f) + 2f;

            bool isHost = PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient;
            bool hostRuleKnown = LobbySync.TryReadRule(out LobbyRule lobby);
            bool hostControlled = hostRuleKnown && lobby.Mode == LobbyMode.HostControlled;

            if (isHost)
            {
                y = BuildHostView(y, Plugin.Host.Mode == LobbyMode.HostControlled);
            }
            else if (hostControlled)
            {
                y = BuildTeamOverview(y, lobby);
            }
            else
            {
                y = BuildIndividualView(y, hostRuleKnown);
            }

            if (_wheel == null)
            {
                y -= SectionGap;
                y -= AddLabel(Strings.CloseHint(Plugin.Settings.PanelHotkey.ToString()), 15f, y, 0.7f);
            }

            _rect.sizeDelta = new Vector2(_width, -y);
            _rect.anchoredPosition = new Vector2(_centered ? -_width / 2f : Plugin.Settings.PanelOffsetX, 0f);
            transform.localScale = Vector3.one * Mathf.Max(0.3f, Plugin.Settings.PanelScale);
        }

        /// <summary>The wearer's own permission list. Shown while nobody controls the lobby centrally.</summary>
        private float BuildIndividualView(float y, bool showModeRow)
        {
            LocalPermissions permissions = Plugin.Permissions;

            y -= AddLabel(Strings.Hint, 16f, y, 0.75f) + 14f;
            if (showModeRow)
            {
                y -= AddRow(Strings.LobbyMode, Strings.ModeIndividual, HudStyle.Muted, null, null, y) + RowGap;
            }

            return BuildIndividualRows(y, permissions);
        }

        /// <summary>Switches, dropped-pack rules and the player list of the individual view.</summary>
        private float BuildIndividualRows(float y, LocalPermissions permissions)
        {
            y -= AddChoiceRow(Strings.WornPack, !permissions.AllowEveryone, Strings.MyList, Strings.Everyone, permissions.ToggleAllowEveryone, y) + RowGap;
            y -= AddChoiceRow(Strings.WhilePassedOut, !permissions.UnlockWhilePassedOut, Strings.MyList, Strings.Everyone, permissions.ToggleUnlockWhilePassedOut, y) + RowGap;
            y -= AddChoiceRow(Strings.DroppedPack, permissions.ProtectDroppedPack, Strings.MyList, Strings.Everyone, permissions.ToggleProtectDroppedPack, y) + RowGap;
            y -= AddChoiceRow(Strings.AfterDeath, permissions.ProtectDeathDrop, Strings.MyList, Strings.Everyone, permissions.ToggleProtectDeathDrop, y) + SectionGap;

            y -= AddLabel(Strings.Players, 22f, y, 1f) + 8f;

            List<Photon.Realtime.Player> others = OtherPlayers();
            if (others.Count == 0)
            {
                return y - AddLabel(Strings.NoOtherPlayers, 17f, y, 0.75f);
            }

            foreach (Photon.Realtime.Player player in others)
            {
                y -= AddPlayerPermissionRow(player, permissions, y) + RowGap;
            }
            return y - AddLabel(Strings.Summary(permissions.CountGranted(others), others.Count), 16f, y, 0.75f);
        }

        /// <summary>The host's view: lobby mode switch, and in host mode the team editor.</summary>
        private float BuildHostView(float y, bool hostControlled)
        {
            HostSettings host = Plugin.Host;

            y -= AddLabel(hostControlled ? Strings.HostHint : Strings.Hint, 16f, y, 0.75f) + 14f;
            y -= AddRow(Strings.LobbyMode, hostControlled ? Strings.ModeHost : Strings.ModeIndividual,
                hostControlled ? HudStyle.Green : HudStyle.Muted,
                hostControlled ? Strings.SwitchToIndividualMode : Strings.SwitchToHostMode, host.ToggleMode, y) + RowGap;

            if (!hostControlled)
            {
                // The host's own pack follows the individual rules like everyone else's.
                return BuildIndividualRows(y, Plugin.Permissions);
            }

            y -= AddChoiceRow(Strings.WornPack, !host.AllowEveryone, Strings.TeamOnly, Strings.Everyone, host.ToggleAllowEveryone, y) + RowGap;
            y -= AddChoiceRow(Strings.WhilePassedOut, !host.UnlockWhilePassedOut, Strings.TeamOnly, Strings.Everyone, host.ToggleUnlockWhilePassedOut, y) + RowGap;
            y -= AddChoiceRow(Strings.DroppedPack, host.DroppedPacksTeamOnly, Strings.TeamOnly, Strings.Everyone, host.ToggleDroppedPacksTeamOnly, y) + RowGap;
            y -= AddChoiceRow(Strings.AfterDeath, host.DeathDropsTeamOnly, Strings.TeamOnly, Strings.Everyone, host.ToggleDeathDropsTeamOnly, y) + SectionGap;
            y -= AddLabel(Strings.Teams, 22f, y, 1f) + 8f;

            foreach (Photon.Realtime.Player player in AllPlayers())
            {
                int team = host.TeamOf(player);
                int next = (team + 1) % (LobbyRule.TeamCount + 1);
                string name = DisplayName(player);
                y -= AddRow(name, Strings.TeamOrNone(LobbyRule.TeamName(team)), HudStyle.TeamColor(team),
                    Strings.MoveToTeam(name, LobbyRule.TeamName(next)), () => host.CycleTeam(player), y) + RowGap;
            }
            return y;
        }

        /// <summary>Read-only view for non-hosts while the host controls the lobby.</summary>
        private float BuildTeamOverview(float y, LobbyRule lobby)
        {
            y -= AddLabel(Strings.HostManagesHint, 16f, y, 0.75f) + 14f;
            y -= AddRow(Strings.LobbyMode, Strings.ModeHost, HudStyle.Green, null, null, y) + RowGap;
            y -= AddReadOnlyChoiceRow(Strings.WornPack, !lobby.AllowEveryone, y) + RowGap;
            y -= AddReadOnlyChoiceRow(Strings.WhilePassedOut, !lobby.UnlockWhilePassedOut, y) + RowGap;
            y -= AddReadOnlyChoiceRow(Strings.DroppedPack, lobby.DroppedPacksTeamOnly, y) + RowGap;
            y -= AddReadOnlyChoiceRow(Strings.AfterDeath, lobby.DeathDropsTeamOnly, y) + SectionGap;

            y -= AddLabel(Strings.YourTeam(LobbyRule.TeamName(lobby.TeamOf(PhotonNetwork.LocalPlayer))), 22f, y, 1f) + 8f;

            foreach (Photon.Realtime.Player player in AllPlayers())
            {
                int team = lobby.TeamOf(player);
                y -= AddRow(DisplayName(player), Strings.TeamOrNone(LobbyRule.TeamName(team)), HudStyle.TeamColor(team), null, null, y) + RowGap;
            }
            return y;
        }

        /// <summary>Read-only "Team only" / "Everyone" row for the team overview.</summary>
        private float AddReadOnlyChoiceRow(string label, bool teamOnly, float y)
        {
            return AddRow(label, teamOnly ? Strings.TeamOnly : Strings.Everyone, teamOnly ? HudStyle.Green : HudStyle.Muted, null, null, y);
        }

        /// <summary>A two-state row whose status reads as a choice ("Team only" / "Everyone") rather than On/Off.</summary>
        private float AddChoiceRow(string label, bool restricted, string restrictedText, string openText, Action toggle, float y)
        {
            string status = restricted ? restrictedText : openText;
            string next = restricted ? openText : restrictedText;
            return AddRow(label, status, restricted ? HudStyle.Green : HudStyle.Muted, Strings.SetTo(label, next), toggle, y);
        }

        private float AddPlayerPermissionRow(Photon.Realtime.Player player, LocalPermissions permissions, float y)
        {
            bool listed = permissions.IsListed(player);
            bool granted = permissions.AllowEveryone || listed;

            Color statusColor = granted ? HudStyle.Green : HudStyle.Red;
            if (granted && !listed)
            {
                // Granted only through "Allow everyone": show it, but dimmed.
                statusColor = HudStyle.WithAlpha(statusColor, 0.6f);
            }

            string name = DisplayName(player);
            string caption = listed ? Strings.LockPlayer(name) : Strings.AllowPlayer(name);
            return AddRow(name, granted ? Strings.Allowed : Strings.Locked, statusColor, caption, () => permissions.Toggle(player), y);
        }

        /// <summary>
        /// Adds a hotbar-styled row (shadow, hover fill, outline, label, status) and returns its
        /// height. Rows without an action are read-only.
        /// </summary>
        private float AddRow(string label, string status, Color statusColor, string caption, Action onActivate, float y)
        {
            GameObject row = CreateRect("Row", transform, Padding, y, _inner, RowHeight, typeof(CanvasRenderer), typeof(Image));
            _content.Add(row);

            // Shadow and hover fill share the outline's corner radius so nothing pokes out of the frame.
            Image shadow = row.GetComponent<Image>();
            HudStyle.ApplyRoundedFill(shadow, HudStyle.Shadow, RowHeight);
            shadow.raycastTarget = onActivate != null;

            Image fill = CreateRect("Fill", row.transform, 3f, -3f, _inner - 6f, RowHeight - 6f, typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            HudStyle.ApplyRoundedFill(fill, HudStyle.WithAlpha(HudStyle.Cream, 0f), RowHeight - 6f);
            fill.raycastTarget = false;

            float outlineHeight = RowHeight + 2f * OutlineBleed;
            Image outline = CreateRect("Outline", row.transform, -OutlineBleed, OutlineBleed, _inner + 2f * OutlineBleed, outlineHeight, typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            HudStyle.ApplyOutline(outline, onActivate != null ? HudStyle.Cream : HudStyle.WithAlpha(HudStyle.Cream, 0.6f), outlineHeight);
            outline.raycastTarget = false;

            float labelWidth = _inner - StatusWidth - 3f * Padding;
            TextMeshProUGUI labelText = HudStyle.CreateText(row.transform, label, 21f, TextAlignmentOptions.MidlineLeft);
            labelText.overflowMode = TextOverflowModes.Ellipsis;
            Place(labelText.rectTransform, RowTextInset, 0f, labelWidth, RowHeight);

            TextMeshProUGUI statusText = HudStyle.CreateText(row.transform, status, 20f, TextAlignmentOptions.MidlineRight);
            statusText.color = statusColor;
            Place(statusText.rectTransform, _inner - RowTextInset - 2f - StatusWidth, 0f, StatusWidth, RowHeight);

            row.AddComponent<PermissionRow>().Initialize(this, fill, caption, onActivate);
            return RowHeight;
        }

        /// <summary>Adds a free-standing caption-styled text line and returns its height.</summary>
        private float AddLabel(string text, float fontSize, float y, float alpha)
        {
            float height = fontSize * LineHeightFactor;
            TextMeshProUGUI label = HudStyle.CreateText(transform, text, fontSize, TextAlignmentOptions.MidlineLeft);
            label.color = HudStyle.WithAlpha(label.color, label.color.a * alpha);
            Place(label.rectTransform, Padding, y, _inner, height);
            _content.Add(label.gameObject);
            return height;
        }

        private void SetWheelCaption(string text)
        {
            if (_wheel != null && _wheel.chosenItemText != null)
            {
                _wheel.chosenItemText.text = text ?? string.Empty;
            }
        }

        /// <summary>Clears the wheel caption unless a wheel slice owns it.</summary>
        private void ClearWheelCaption()
        {
            if (_wheel != null && _wheel.chosenItemText != null && _wheel.chosenSlice.IsNone)
            {
                _wheel.chosenItemText.text = string.Empty;
            }
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private static List<Photon.Realtime.Player> OtherPlayers()
        {
            return PhotonNetwork.InRoom
                ? PhotonNetwork.PlayerListOthers.OrderBy(p => p.ActorNumber).ToList()
                : new List<Photon.Realtime.Player>();
        }

        private static List<Photon.Realtime.Player> AllPlayers()
        {
            return PhotonNetwork.InRoom
                ? PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).ToList()
                : new List<Photon.Realtime.Player>();
        }

        private static GameObject CreateRect(string name, Transform parent, float x, float y, float width, float height, params Type[] components)
        {
            Type[] all = new Type[components.Length + 1];
            all[0] = typeof(RectTransform);
            Array.Copy(components, 0, all, 1, components.Length);

            GameObject go = new GameObject(name, all);
            go.transform.SetParent(parent, false);
            Place(go.GetComponent<RectTransform>(), x, y, width, height);
            return go;
        }

        /// <summary>Positions a rect relative to its parent's top-left corner; y grows downwards as negative values.</summary>
        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static string DisplayName(Photon.Realtime.Player player)
        {
            string name = null;
            try
            {
                Character character = PlayerHandler.GetPlayerCharacter(player);
                if (character != null && !string.IsNullOrEmpty(character.characterName))
                {
                    name = character.characterName;
                }
            }
            catch
            {
                // PlayerHandler is unavailable outside a run; fall back to the network name.
            }
            if (string.IsNullOrEmpty(name))
            {
                name = string.IsNullOrEmpty(player.NickName) ? Strings.FallbackPlayerName(player.ActorNumber) : player.NickName;
            }
            return player.IsLocal ? $"{name} ({Strings.You})" : name;
        }
    }
}
