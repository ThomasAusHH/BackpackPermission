using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BackpackPermission.UI
{
    /// <summary>
    /// Opens the access panel without a backpack wheel, toggled by a configurable key. Hosts
    /// without a pack manage teams this way, and everyone can edit their list at any time.
    /// While open, the game is told a wheel is active so the cursor is free and player input pauses.
    /// </summary>
    internal static class StandalonePanel
    {
        private static PermissionPanel _panel;

        /// <summary>True while the standalone panel is visible. Read by the wheelActive patch.</summary>
        public static bool IsOpen => _panel != null && _panel.gameObject.activeSelf;

        /// <summary>Call once per frame: handles the hotkey and closes the panel when its context disappears.</summary>
        public static void Tick()
        {
            if (IsOpen && (!PhotonNetwork.InRoom || Character.localCharacter == null))
            {
                Close();
                return;
            }

            Key key = Plugin.Settings.PanelHotkey;
            if (key == Key.None || Keyboard.current == null || !Keyboard.current[key].wasPressedThisFrame)
            {
                return;
            }

            if (IsOpen)
            {
                Close();
            }
            else
            {
                TryOpen();
            }
        }

        public static void Close()
        {
            if (_panel != null)
            {
                _panel.Hide();
            }
        }

        private static void TryOpen()
        {
            GUIManager gui = GUIManager.instance;
            if (gui == null || gui.backpackWheel == null || Character.localCharacter == null)
            {
                return;
            }
            // Never on top of a real wheel or a menu, and not while typing is impossible anyway.
            if (gui.wheelActive || gui.windowBlockingInput || GUIManager.InPauseMenu || Character.localCharacter.data.dead)
            {
                return;
            }

            BackpackWheel wheel = gui.backpackWheel;
            HudStyle.EnsureCaptured(wheel);

            // The wheel's parent is the HUD layer: same canvas, same scaling, same raycaster.
            Transform hudLayer = wheel.transform.parent != null ? wheel.transform.parent : wheel.transform;
            if (_panel == null)
            {
                _panel = PermissionPanel.Create(hudLayer, null, "BackpackPermissionStandalonePanel");
            }
            _panel.ShowCentered();
        }
    }
}
