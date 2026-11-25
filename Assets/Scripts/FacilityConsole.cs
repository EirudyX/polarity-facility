using UnityEngine;

namespace PolarityFacility
{
    internal static class FacilityConsole
    {
        internal static readonly Color Text = new Color32(232, 244, 253, 255);
        internal static readonly Color Muted = new Color32(151, 178, 201, 255);
        internal static readonly Color Cyan = new Color32(100, 228, 255, 255);
        internal static readonly Color Amber = new Color32(239, 181, 94, 255);
        internal static readonly Color Red = new Color32(255, 91, 119, 255);
        internal static readonly Color Background = new Color32(6, 13, 23, 255);
        internal static readonly Color Line = new Color32(48, 77, 102, 255);
        private static GUIStyle plate, inset, raised, focused, live, liveHover;
        private static Texture2D leftFade, glowBand, groundFade, checkmark;

        internal static GUIStyle TextStyle(Font font, int size, Color color, FontStyle weight = FontStyle.Normal,
            TextAnchor alignment = TextAnchor.MiddleLeft) => new GUIStyle(GUI.skin.label)
        {
            font = font != null ? font : GUI.skin.font,
            fontSize = size,
            fontStyle = weight,
            alignment = alignment,
            clipping = TextClipping.Clip,
            padding = new RectOffset(),
            normal = { textColor = color }
        };

        internal static void Fill(Rect rect, Color color)
        {
            if (Event.current.type != EventType.Repaint) return;
            Color previous = GUI.color;
            GUI.color = previous * color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        internal static void Panel(Rect rect, bool recessed = false)
        {
            if (Event.current.type != EventType.Repaint) return;
            EnsureSkins();
            GUI.Box(rect, GUIContent.none, recessed ? inset : plate);
        }

        internal static void Row(Rect rect, bool selected, bool hover)
        {
            if (Event.current.type != EventType.Repaint) return;
            EnsureSkins();
            GUI.Box(rect, GUIContent.none, selected || hover ? focused : raised);
            if (selected) Fill(new Rect(rect.x + 2, rect.y + 9, 2, rect.height - 18), Cyan);
        }

        internal static bool Action(Rect rect, string label, string key, GUIStyle text, GUIStyle keyText, bool primary)
        {
            bool hot = rect.Contains(Event.current.mousePosition) && GUI.enabled;
            if (Event.current.type == EventType.Repaint)
            {
                EnsureSkins();
                GUI.Box(rect, GUIContent.none, primary ? hot ? liveHover : live : hot ? focused : raised);
                if (primary)
                {
                    Trace(rect.x + 13, rect.y + 2, rect.width - 26, hot ? Cyan : new Color32(64, 170, 222, 255));
                    if (hot)
                    {
                        float scan = Mathf.Repeat(Time.unscaledTime * .72f, 1f);
                        Trace(rect.x + 14 + (rect.width - 39) * scan, rect.yMax - 3, 12, Cyan);
                    }
                }
                float labelStart = rect.x + 16;
                if (string.IsNullOrEmpty(key))
                    GUI.Label(rect, label, text);
                else
                {
                    GUI.Label(new Rect(labelStart, rect.y, rect.width - 77, rect.height), label, text);
                    Key(new Rect(rect.xMax - 58, rect.center.y - 13, 43, 26), key, keyText);
                }
            }
            return GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }

        internal static void Key(Rect rect, string key, GUIStyle text)
        {
            Panel(rect, true);
            Fill(new Rect(rect.x + 7, rect.yMax - 3, rect.width - 14, 1), Line);
            GUI.Label(rect, key, text);
        }

        internal static void Trace(float x, float y, float width, Color accent)
        {
            if (Event.current.type != EventType.Repaint) return;
            EnsureSkins();
            Color previous = GUI.color;
            GUI.color = previous * new Color(accent.r, accent.g, accent.b, accent.a * .37f);
            GUI.DrawTexture(new Rect(x, y - 6, width, 13), glowBand);
            GUI.color = previous;
            Fill(new Rect(x, y, width, 1), accent);
        }

        internal static void Light(float x, float y, Color color, float intensity = 1)
        {
            Color glow = color;
            glow.a = .15f * intensity;
            Fill(new Rect(x - 2, y - 2, 9, 9), glow);
            color.a *= intensity;
            Fill(new Rect(x, y, 5, 5), color);
        }

        internal static void FrameCorners(Rect rect, Color color, float length = 13)
        {
            Fill(new Rect(rect.x, rect.y, length, 1), color);
            Fill(new Rect(rect.x, rect.y, 1, length), color);
            Fill(new Rect(rect.xMax - length, rect.yMax - 1, length, 1), color);
            Fill(new Rect(rect.xMax - 1, rect.yMax - length, 1, length), color);
        }

        internal static void LeftShade(Rect rect, Color color)
        {
            EnsureSkins();
            TintImage(rect, leftFade, color);
        }

        internal static void BottomShade(Rect rect, Color color)
        {
            EnsureSkins();
            TintImage(rect, groundFade, color);
        }

        internal static void Checkmark(Rect rect)
        {
            EnsureSkins();
            TintImage(rect, checkmark, Cyan);
        }

        private static void TintImage(Rect rect, Texture2D texture, Color color)
        {
            if (Event.current.type != EventType.Repaint) return;
            Color previous = GUI.color;
            GUI.color = previous * color;
            GUI.DrawTexture(rect, texture);
            GUI.color = previous;
        }

        internal static float Ease(float age, float duration)
        {
            float t = Mathf.Clamp01(age / duration);
            return 1 - (1 - t) * (1 - t) * (1 - t);
        }

        private static void EnsureSkins()
        {
            if (plate != null) return;
            plate = Surface(new Color32(28, 48, 67, 250), new Color32(13, 24, 37, 250), new Color32(67, 94, 116, 255));
            inset = Surface(new Color32(17, 31, 47, 254), new Color32(9, 18, 30, 254), new Color32(48, 76, 98, 255));
            raised = Surface(new Color32(33, 53, 73, 255), new Color32(19, 33, 49, 255), new Color32(57, 82, 105, 255));
            focused = Surface(new Color32(49, 84, 111, 255), new Color32(21, 47, 67, 255), new Color32(84, 145, 180, 255));
            live = Surface(new Color32(33, 97, 133, 255), new Color32(18, 56, 87, 255), new Color32(79, 176, 214, 255));
            liveHover = Surface(new Color32(48, 124, 162, 255), new Color32(21, 67, 98, 255), new Color32(108, 228, 251, 255));
            leftFade = AlphaTexture(96, 1, (x, _) => 1 - Mathf.SmoothStep(0, 1, x / 95f));
            groundFade = AlphaTexture(1, 96, (_, y) => 1 - Mathf.SmoothStep(0, 1, y / 95f));
            glowBand = AlphaTexture(1, 48, (_, y) => Mathf.Pow(Mathf.Max(0, 1 - Mathf.Abs(y / 23.5f - 1)), 2.2f));
            checkmark = AlphaTexture(32, 32, (x, y) =>
            {
                var point = new Vector2(x, y);
                var joint = new Vector2(13, 9);
                float distance = Mathf.Min(SegmentDistance(point, new Vector2(7, 15), joint),
                    SegmentDistance(point, joint, new Vector2(25, 23)));
                return Mathf.Max(1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.77f, 1.58f, distance)),
                    .31f * Mathf.Clamp01(1 - distance / 4.2f));
            });
        }

        private static float SegmentDistance(Vector2 point, Vector2 start, Vector2 end)
        {
            Vector2 direction = end - start;
            float progress = Mathf.Clamp01(Vector2.Dot(point - start, direction) / direction.sqrMagnitude);
            return Vector2.Distance(point, start + direction * progress);
        }

        private static GUIStyle Surface(Color cap, Color baseColor, Color rim)
        {
            const int size = 64;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float cut = Mathf.Min(Mathf.Min(x, size - 1 - x), Mathf.Min(y, size - 1 - y));
                cut = Mathf.Min(cut, x + size - 1 - y - 7);
                cut = Mathf.Min(cut, size - 1 - x + y - 7);
                if (cut < 0) continue;
                float t = y / (size - 1f);
                Color steel = Color.Lerp(baseColor, cap, t);
                float grain = (((x * 19 + y * 43) % 31) - 15) * .00063f;
                steel.r += grain;
                steel.g += grain;
                steel.b += grain;
                if (cut <= 1) steel = Color.Lerp(rim * .48f, rim, t * .85f + .15f);
                else if (cut == 2) steel = Color.Lerp(steel, rim, .34f);
                pixels[y * size + x] = steel;
            }
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return new GUIStyle { border = new RectOffset(11, 11, 11, 11), normal = { background = texture } };
        }

        private static Texture2D AlphaTexture(int width, int height, System.Func<int, int, float> opacity)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                pixels[y * width + x] = new Color(1, 1, 1, opacity(x, y));
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }
    }
}
