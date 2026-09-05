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
    internal sealed class PermissionPanel : MonoBehaviour
    {
        private const float Padding = 12f;
        private const float RowHeight = 46f;
        private const float RowGap = 10f;
        private const float RowTextInset = 18f;
        private const float StatusWidth = 110f;
        private const float OutlineBleed = 4f;
        private const float MinWidth = 260f;
        private const float LineHeightFactor = 1.3f;

        private static bool _hierarchyDumped;

        private readonly List<GameObject> _content = new List<GameObject>();
        private BackpackWheel _wheel;
        private RectTransform _rect;

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

            // No background of its own: the wheel already dims the whole screen.
            GameObject go = new GameObject("BackpackPermissionPanel", typeof(RectTransform), typeof(PermissionPanel));
            go.transform.SetParent(wheel.transform, false);
            go.AddComponent<LayoutElement>().ignoreLayout = true;

            PermissionPanel panel = go.GetComponent<PermissionPanel>();
            panel._wheel = wheel;
            panel._rect = go.GetComponent<RectTransform>();
            panel._rect.anchorMin = new Vector2(0.5f, 0.5f);
            panel._rect.anchorMax = new Vector2(0.5f, 0.5f);
            panel._rect.pivot = new Vector2(0f, 0.5f);

            Instance = panel;
            return panel;
        }

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

            LocalPermissions permissions = Plugin.Permissions;
            float width = Mathf.Max(MinWidth, Plugin.Settings.PanelWidth);
            float inner = width - 2f * Padding;
            float y = 0f;

            y -= AddLabel(Strings.Title, 32f, y, inner, 1f) + 2f;
            y -= AddLabel(Strings.Hint, 16f, y, inner, 0.75f) + 14f;

            y -= AddSwitchRow(Strings.AllowEveryone, permissions.AllowEveryone, permissions.ToggleAllowEveryone, y, inner) + RowGap;
            y -= AddSwitchRow(Strings.UnlockWhilePassedOut, permissions.UnlockWhilePassedOut, permissions.ToggleUnlockWhilePassedOut, y, inner) + 18f;

            y -= AddLabel(Strings.Players, 22f, y, inner, 1f) + 8f;

            List<Photon.Realtime.Player> others = PhotonNetwork.InRoom
                ? PhotonNetwork.PlayerListOthers.OrderBy(p => p.ActorNumber).ToList()
                : new List<Photon.Realtime.Player>();

            if (others.Count == 0)
            {
                y -= AddLabel(Strings.NoOtherPlayers, 17f, y, inner, 0.75f);
            }
            else
            {
                foreach (Photon.Realtime.Player player in others)
                {
                    y -= AddPlayerRow(player, permissions, y, inner) + RowGap;
                }
                y -= AddLabel(Strings.Summary(permissions.CountGranted(others), others.Count), 16f, y, inner, 0.75f);
            }

            _rect.sizeDelta = new Vector2(width, -y);
            _rect.anchoredPosition = new Vector2(Plugin.Settings.PanelOffsetX, 0f);
            transform.localScale = Vector3.one * Mathf.Max(0.3f, Plugin.Settings.PanelScale);
        }

        private float AddSwitchRow(string label, bool isOn, Action toggle, float y, float inner)
        {
            return AddRow(label, isOn ? Strings.On : Strings.Off, isOn ? HudStyle.Green : HudStyle.Muted,
                Strings.Toggle(label, !isOn), toggle, y, inner);
        }

        private float AddPlayerRow(Photon.Realtime.Player player, LocalPermissions permissions, float y, float inner)
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
            return AddRow(name, granted ? Strings.Allowed : Strings.Locked, statusColor, caption, () => permissions.Toggle(player), y, inner);
        }

        /// <summary>Adds a hotbar-styled row (shadow, hover fill, outline, label, status) and returns its height.</summary>
        private float AddRow(string label, string status, Color statusColor, string caption, Action onActivate, float y, float inner)
        {
            GameObject row = CreateRect("Row", transform, Padding, y, inner, RowHeight, typeof(CanvasRenderer), typeof(Image));
            _content.Add(row);

            // Shadow and hover fill share the outline's corner radius so nothing pokes out of the frame.
            Image shadow = row.GetComponent<Image>();
            HudStyle.ApplyRoundedFill(shadow, HudStyle.Shadow, RowHeight);
            shadow.raycastTarget = true;

            Image fill = CreateRect("Fill", row.transform, 3f, -3f, inner - 6f, RowHeight - 6f, typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            HudStyle.ApplyRoundedFill(fill, HudStyle.WithAlpha(HudStyle.Cream, 0f), RowHeight - 6f);
            fill.raycastTarget = false;

            float outlineHeight = RowHeight + 2f * OutlineBleed;
            Image outline = CreateRect("Outline", row.transform, -OutlineBleed, OutlineBleed, inner + 2f * OutlineBleed, outlineHeight, typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            HudStyle.ApplyOutline(outline, HudStyle.Cream, outlineHeight);
            outline.raycastTarget = false;

            float labelWidth = inner - StatusWidth - 3f * Padding;
            TextMeshProUGUI labelText = HudStyle.CreateText(row.transform, label, 21f, TextAlignmentOptions.MidlineLeft);
            labelText.overflowMode = TextOverflowModes.Ellipsis;
            Place(labelText.rectTransform, RowTextInset, 0f, labelWidth, RowHeight);

            TextMeshProUGUI statusText = HudStyle.CreateText(row.transform, status, 20f, TextAlignmentOptions.MidlineRight);
            statusText.color = statusColor;
            Place(statusText.rectTransform, inner - RowTextInset - 2f - StatusWidth, 0f, StatusWidth, RowHeight);

            row.AddComponent<PermissionRow>().Initialize(this, fill, caption, onActivate);
            return RowHeight;
        }

        /// <summary>Adds a free-standing caption-styled text line and returns its height.</summary>
        private float AddLabel(string text, float fontSize, float y, float inner, float alpha)
        {
            float height = fontSize * LineHeightFactor;
            TextMeshProUGUI label = HudStyle.CreateText(transform, text, fontSize, TextAlignmentOptions.MidlineLeft);
            label.color = HudStyle.WithAlpha(label.color, label.color.a * alpha);
            Place(label.rectTransform, Padding, y, inner, height);
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
            try
            {
                Character character = PlayerHandler.GetPlayerCharacter(player);
                if (character != null && !string.IsNullOrEmpty(character.characterName))
                {
                    return character.characterName;
                }
            }
            catch
            {
                // PlayerHandler is unavailable outside a run; fall back to the network name.
            }
            return string.IsNullOrEmpty(player.NickName) ? Strings.FallbackPlayerName(player.ActorNumber) : player.NickName;
        }
    }
}
