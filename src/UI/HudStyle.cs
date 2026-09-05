using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackPermission.UI
{
    /// <summary>
    /// Visual vocabulary borrowed from the game's HUD so the panel looks native: the wheel's
    /// caption font and colour, and the soft outline sprite the hotbar slots use.
    /// </summary>
    internal static class HudStyle
    {
        private const string OutlineSpriteName = "UI_Blur_Outlne";

        /// <summary>Fraction of the outline sprite reserved for each 9-slice border.</summary>
        private const float OutlineBorderFraction = 0.45f;

        /// <summary>Size and corner radius of the generated rounded fill sprite, in texture pixels.</summary>
        private const int FillSpriteSize = 64;
        private const float FillCornerRadius = 25f;

        /// <summary>Drop shadow behind hotbar slots.</summary>
        public static readonly Color Shadow = new Color(0f, 0f, 0f, 0.345f);
        public static readonly Color Green = new Color(0.55f, 0.82f, 0.40f, 1f);
        /// <summary>The HUD's orange-red, e.g. the fuel gauge needle.</summary>
        public static readonly Color Red = new Color(0.972f, 0.375f, 0.270f, 1f);

        private static TMP_FontAsset _font;
        private static Material _fontMaterial;
        private static Sprite _outlineSprite;
        private static Sprite _roundedFillSprite;
        private static bool _outlineSearched;

        /// <summary>Cream tone of the HUD (hotbar outline, wheel caption). Refreshed from the wheel on capture.</summary>
        public static Color Cream { get; private set; } = new Color(0.874f, 0.857f, 0.762f, 1f);

        /// <summary>Cream at reduced opacity for secondary text.</summary>
        public static Color Muted => WithAlpha(Cream, 0.8f);

        /// <summary>Status colour for a team (1..4); unassigned players use <see cref="Muted"/>.</summary>
        public static Color TeamColor(int team)
        {
            switch (team)
            {
                case 1: return Green;
                case 2: return new Color(0.45f, 0.72f, 0.95f, 1f);
                case 3: return new Color(0.98f, 0.72f, 0.30f, 1f);
                case 4: return new Color(0.80f, 0.55f, 0.95f, 1f);
                default: return Muted;
            }
        }

        /// <summary>Reads font and colours from the wheel and locates the outline sprite once.</summary>
        public static void EnsureCaptured(BackpackWheel wheel)
        {
            TextMeshProUGUI caption = wheel != null ? wheel.chosenItemText : null;
            if (caption != null && caption.font != null)
            {
                _font = caption.font;
                _fontMaterial = caption.fontSharedMaterial;
                Cream = WithAlpha(caption.color, 1f);
            }
            else if (_font == null)
            {
                _font = TMP_Settings.defaultFontAsset;
                _fontMaterial = null;
            }

            if (_outlineSearched)
            {
                return;
            }
            _outlineSearched = true;
            Sprite source = FindSprite(OutlineSpriteName);
            _outlineSprite = source != null ? Reslice(source) : null;
            Plugin.LogVerbose(_outlineSprite != null
                ? $"Outline sprite: {source.name} (resliced)"
                : "Outline sprite not found, falling back to a plain frame.");
        }

        public static TextMeshProUGUI CreateText(Transform parent, string text, float fontSize, TextAlignmentOptions alignment)
        {
            GameObject go = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(parent, false);

            TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
            label.font = _font;
            if (_fontMaterial != null)
            {
                label.fontSharedMaterial = _fontMaterial;
            }
            label.text = text;
            label.fontSize = fontSize;
            label.color = Cream;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
            return label;
        }

        /// <summary>
        /// Turns <paramref name="image"/> into a soft outline of the given height. The sprite is
        /// scaled so that its full height maps onto <paramref name="targetHeight"/>; otherwise Unity
        /// would shrink only the vertical borders and the corners would degrade into flat ellipses.
        /// </summary>
        public static void ApplyOutline(Image image, Color color, float targetHeight)
        {
            if (_outlineSprite == null)
            {
                image.sprite = null;
                image.color = WithAlpha(color, 0f);
                Outline frame = image.gameObject.AddComponent<Outline>();
                frame.effectColor = color;
                frame.effectDistance = new Vector2(2f, -2f);
                return;
            }

            image.sprite = _outlineSprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.pixelsPerUnitMultiplier = MultiplierForHeight(image, _outlineSprite, targetHeight);
        }

        /// <summary>
        /// Pixels-per-unit multiplier that renders a sliced sprite at exactly <paramref name="targetHeight"/>:
        /// rendered height = rect.height * referencePpu / (spritePpu * multiplier).
        /// </summary>
        private static float MultiplierForHeight(Image image, Sprite sprite, float targetHeight)
        {
            float referencePpu = image.canvas != null ? image.canvas.referencePixelsPerUnit : 100f;
            float spritePpu = Mathf.Max(1f, sprite.pixelsPerUnit);
            float multiplier = sprite.rect.height * referencePpu / (spritePpu * Mathf.Max(1f, targetHeight));
            return Mathf.Max(0.01f, multiplier);
        }

        /// <summary>White rounded rectangle with anti-aliased corners, 9-sliced at the corner radius.</summary>
        private static Sprite CreateRoundedFillSprite()
        {
            int size = FillSpriteSize;
            float radius = FillCornerRadius;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = "BackpackPermission_RoundedFill"
            };

            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    float dx = Mathf.Max(radius - px, px - (size - radius), 0f);
                    float dy = Mathf.Max(radius - py, py - (size - radius), 0f);
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius - distance + 0.5f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            float border = radius + 1f;
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0u,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            sprite.name = texture.name;
            return sprite;
        }

        /// <summary>
        /// Gives <paramref name="image"/> a filled rounded-rectangle shape whose corner radius matches
        /// the outline at the same <paramref name="targetHeight"/>, so shadows and fills stay inside the frame.
        /// </summary>
        public static void ApplyRoundedFill(Image image, Color color, float targetHeight)
        {
            if (_roundedFillSprite == null)
            {
                _roundedFillSprite = CreateRoundedFillSprite();
            }
            image.sprite = _roundedFillSprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.pixelsPerUnitMultiplier = MultiplierForHeight(image, _roundedFillSprite, targetHeight);
        }

        public static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        /// <summary>Looks for a sprite on the hotbar first (cheap), then among all loaded sprites.</summary>
        private static Sprite FindSprite(string name)
        {
            try
            {
                GUIManager gui = GUIManager.instance;
                if (gui != null && gui.items != null)
                {
                    foreach (InventoryItemUI slot in gui.items)
                    {
                        if (slot == null)
                        {
                            continue;
                        }
                        foreach (Image image in slot.GetComponentsInChildren<Image>(true))
                        {
                            if (image.sprite != null && image.sprite.name == name)
                            {
                                return image.sprite;
                            }
                        }
                    }
                }

                foreach (Sprite sprite in Resources.FindObjectsOfTypeAll<Sprite>())
                {
                    if (sprite != null && sprite.name == name)
                    {
                        return sprite;
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.LogVerbose($"Sprite lookup for '{name}' failed: {e.Message}");
            }
            return null;
        }

        /// <summary>
        /// The game's outline sprite has no 9-slice borders and stretches into an ellipse on wide
        /// rects. A copy with generous borders keeps the corners round and stretches only the edges.
        /// </summary>
        private static Sprite Reslice(Sprite source)
        {
            try
            {
                Rect rect = source.rect;
                Vector4 border = new Vector4(rect.width, rect.height, rect.width, rect.height) * OutlineBorderFraction;
                Sprite copy = Sprite.Create(source.texture, rect, new Vector2(0.5f, 0.5f), source.pixelsPerUnit, 0u, SpriteMeshType.FullRect, border);
                copy.name = source.name + "_Sliced";
                return copy;
            }
            catch (Exception e)
            {
                Plugin.LogVerbose($"Reslicing '{source.name}' failed: {e.Message}");
                return source;
            }
        }
    }
}
