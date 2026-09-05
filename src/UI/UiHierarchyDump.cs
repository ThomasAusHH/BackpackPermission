using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackPermission.UI
{
    /// <summary>
    /// Diagnostic helper: writes the wheel and hotbar hierarchies (sizes, sprites, fonts, colours)
    /// to the log so the panel can be matched against the game's own UI. Verbose mode only.
    /// </summary>
    internal static class UiHierarchyDump
    {
        private const int MaxDepth = 4;

        public static void Log(BackpackWheel wheel)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("=== BackpackWheel hierarchy ===");
                Append(wheel.transform, 0, sb);

                GUIManager gui = GUIManager.instance;
                if (gui != null && gui.items != null && gui.items.Length > 0 && gui.items[0] != null)
                {
                    sb.AppendLine("=== Hotbar slot 0 ===");
                    Append(gui.items[0].transform, 0, sb);
                }
                Plugin.Log.LogInfo(sb.ToString());
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"UI hierarchy dump failed: {e.Message}");
            }
        }

        private static void Append(Transform node, int depth, StringBuilder sb)
        {
            if (depth > MaxDepth)
            {
                return;
            }

            sb.Append(' ', depth * 2).Append(node.name);
            if (!node.gameObject.activeSelf)
            {
                sb.Append(" (inactive)");
            }
            if (node is RectTransform rect)
            {
                sb.Append($" size={rect.sizeDelta} pos={rect.anchoredPosition} scale={rect.localScale}");
            }
            if (node.TryGetComponent(out Image image))
            {
                sb.Append($" | Image sprite={NameOf(image.sprite)} type={image.type} color={image.color}");
            }
            if (node.TryGetComponent(out RawImage rawImage))
            {
                sb.Append($" | RawImage tex={NameOf(rawImage.texture)}");
            }
            if (node.TryGetComponent(out TextMeshProUGUI text))
            {
                sb.Append($" | TMP font={NameOf(text.font)} mat={NameOf(text.fontSharedMaterial)} size={text.fontSize} color={text.color} text='{text.text}'");
            }
            sb.AppendLine();

            for (int i = 0; i < node.childCount; i++)
            {
                Append(node.GetChild(i), depth + 1, sb);
            }
        }

        private static string NameOf(UnityEngine.Object obj) => obj != null ? obj.name : "null";
    }
}
