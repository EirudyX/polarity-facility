using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PolarityFacility
{
    public sealed class LevelNavigation : MonoBehaviour
    {
        private const string MenuScene = "LevelMenu";
        private const float Width = 1120, Height = 630;

        private readonly struct Demo
        {
            public readonly string Scene, Name, EnglishName, Description, EnglishDescription;
            public Demo(string scene, string name, string englishName, string description, string englishDescription)
            {
                Scene = scene;
                Name = name;
                EnglishName = englishName;
                Description = description;
                EnglishDescription = englishDescription;
            }
        }

        // Add new mechanics here as they become playable; formal levels have their own slots.
        private static readonly Demo[] Demos =
        {
            new Demo("PolarityFacility", "双球磁力", "Dual-sphere magnetism",
                "切换极性，体验吸引、锁定和排斥弹射。", "Switch polarity to attract, latch and launch."),
            new Demo("MassTransfer", "配重转移", "Mass transfer",
                "转移两球间的配重，比较弹射距离。", "Transfer weight between spheres to change their launch range."),
            new Demo("RailRelay", "可翻转磁轨", "Reversible rail",
                "踩住开关翻转磁轨，让另一颗球改变路线。", "Reverse a magnetic rail to reroute the other sphere."),
            new Demo("PumpSurge", "泵涌", "Magnetic pumping",
                "控制 B 球跨过斜坡后的断口，启动接收台。", "Get B up the ramp and across the gap to the receiver."),
            new Demo("MagneticClimb", "磁吸攀壁", "Magnetic climb",
                "沿墙与顶棚移动，切极落到对岸，为另一颗球开路。",
                "Climb the wall and ceiling, then flip to land and open the other sphere's route.")
        };

        private static readonly Color Background = new Color32(12, 22, 35, 255);
        private static readonly Color Panel = new Color32(24, 39, 55, 255);
        private static readonly Color Border = new Color32(52, 77, 98, 255);
        private static readonly Color Text = new Color32(233, 240, 247, 255);
        private static readonly Color Muted = new Color32(153, 177, 198, 255);
        private static readonly Color Cyan = new Color32(91, 207, 249, 255);
        private static readonly Color Red = new Color32(255, 102, 105, 255);
        private static bool reopenDemos;
        private static int proofIndex;
        private bool showingDemos;
        private Vector2 demoScroll;
        private Font menuFont;
        private GUIStyle title, heading, body, small, centered;

        internal static bool MenuAvailable => Application.CanStreamedLevelBeLoaded(MenuScene);
        internal static bool DemoTourProof { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetNavigation()
        {
            reopenDemos = DemoTourProof = false;
            proofIndex = 0;
        }

        internal static void ReturnToDemoList()
        {
            if (!MenuAvailable) return;
            reopenDemos = true;
            SceneManager.LoadScene(MenuScene);
        }

        private void Awake()
        {
            showingDemos = reopenDemos;
            reopenDemos = false;
            menuFont = FindChineseFont();
        }

        internal static Font FindChineseFont()
        {
            string[] installedFonts = Font.GetOSInstalledFontNames();
            foreach (string family in new[] { "Microsoft YaHei UI", "Microsoft YaHei", "Noto Sans CJK SC", "PingFang SC" })
            {
                if (!Array.Exists(installedFonts,
                        installed => string.Equals(installed, family, StringComparison.OrdinalIgnoreCase))) continue;
                Font font = Font.CreateDynamicFontFromOSFont(family, 22);
                if (font != null) return font;
            }
            return null;
        }

        private void OnDestroy()
        {
            if (menuFont != null) Destroy(menuFont);
        }

        private bool OpenDemo(int index)
        {
            if (!showingDemos || index < 0 || index >= Demos.Length) return false;
            string scene = Demos[index].Scene;
            if (!Application.CanStreamedLevelBeLoaded(scene)) return false;
            SceneManager.LoadScene(scene);
            return true;
        }

        private IEnumerator Start()
        {
            string[] args = Environment.GetCommandLineArgs();
            int proof = Array.IndexOf(args, "-facilityProof");
            if (Array.IndexOf(args, "-facilityCampaign") < 0 || proof < 0 || proof + 1 >= args.Length) yield break;

            if (!DemoTourProof)
            {
                if (showingDemos || OpenDemo(0))
                {
                    Debug.LogError("FAIL demo navigation: launch must begin at formal level select");
                    Application.Quit(1);
                    yield break;
                }
                DemoTourProof = true;
                Debug.Log("PASS launch: formal level select");
                yield return CaptureFrame(args[proof + 1] + "-level-select.png");
                ShowDemoList();
                yield return CaptureFrame(args[proof + 1] + "-demo-list.png");
            }
            else if (!showingDemos)
            {
                Debug.LogError("FAIL demo navigation: expected return to demo list");
                Application.Quit(1);
                yield break;
            }
            else
            {
                Debug.Log("PASS returned to demo list after " + Demos[proofIndex - 1].Scene);
            }

            if (proofIndex == Demos.Length)
            {
                Debug.Log("PASS demo navigation: all demos opened independently and returned to demo list");
                Application.Quit(0);
                yield break;
            }

            if (!OpenDemo(proofIndex++))
            {
                Debug.LogError("FAIL demo navigation: cannot open demo");
                Application.Quit(1);
            }
        }

        internal static IEnumerator CaptureFrame(string path)
        {
            yield return new WaitForEndOfFrame();
            var image = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            System.IO.File.WriteAllBytes(path, image.EncodeToPNG());
            Destroy(image);
            yield return new WaitForEndOfFrame();
        }

        private void ShowDemoList()
        {
            demoScroll = Vector2.zero;
            showingDemos = true;
        }

        private void Update()
        {
            if (DemoTourProof) return;
            if (!showingDemos)
            {
                if (Input.GetKeyDown(KeyCode.D)) ShowDemoList();
                return;
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                showingDemos = false;
                return;
            }
            for (int i = 0; i < Demos.Length && i < 9; i++)
            {
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i)))
                {
                    OpenDemo(i);
                    return;
                }
            }
        }

        private string Local(string chinese, string english) => menuFont != null ? chinese : english;

        private void OnGUI()
        {
            if (title == null)
            {
                title = Style(63, Text, FontStyle.Bold);
                heading = Style(27, Text, FontStyle.Bold);
                body = Style(17, Muted);
                small = Style(14, Cyan, FontStyle.Bold);
                centered = Style(16, Text, FontStyle.Bold, TextAnchor.MiddleCenter);
            }

            Fill(new Rect(0, 0, Screen.width, Screen.height), Background);
            float scale = Mathf.Min(Screen.width / Width, Screen.height / Height);
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(
                new Vector3((Screen.width - Width * scale) / 2, (Screen.height - Height * scale) / 2, 0),
                Quaternion.identity, new Vector3(scale, scale, 1));

            for (int i = 1; i < 28; i++)
                Fill(new Rect(40 * i, 0, 1, Height), new Color(0.35f, 0.55f, 0.72f, 0.06f));
            Fill(new Rect(72, 81, 976, 1), Border);
            Fill(new Rect(72, 81, 43, 2), Cyan);
            GUI.Label(new Rect(72, 42, 410, 26), "POLARITY FACILITY", small);

            if (showingDemos) DrawDemoList();
            else DrawLevelSelect();
            GUI.matrix = previous;
        }

        private void DrawLevelSelect()
        {
            GUI.Label(new Rect(72, 117, 740, 92), Local("极性失控", "Polarity Facility"), title);
            GUI.Label(new Rect(72, 224, 620, 46), Local("选择关卡", "Select a level"), heading);
            Fill(new Rect(907, 158, 85, 2), Border);
            DrawPole(new Rect(861, 136, 46, 46), Red, "N");
            DrawPole(new Rect(992, 136, 46, 46), Cyan, "S");

            for (int i = 0; i < 2; i++)
            {
                var rect = new Rect(72 + 500 * i, 290, 476, 164);
                Card(rect, false);
                GUI.Label(new Rect(rect.x + 25, rect.y + 31, 416, 41),
                    Local($"第 {i + 1} 关", $"Level {i + 1}"), heading);
                GUI.Label(new Rect(rect.x + 25, rect.y + 95, 416, 29),
                    Local("待制作", "In development"), body);
            }

            if (Action(new Rect(867, 543, 181, 47), Local("功能演示  [D]", "Feature demos  [D]")))
                ShowDemoList();
        }

        private void DrawPole(Rect rect, Color accent, string pole)
        {
            Card(rect, false);
            Fill(new Rect(rect.x, rect.y, rect.width, 2), accent);
            GUI.Label(rect, pole, centered);
        }

        private void DrawDemoList()
        {
            GUI.Label(new Rect(72, 115, 680, 92), Local("功能演示", "Feature demos"), title);
            if (Action(new Rect(867, 139, 181, 47), Local("返回选关  [Esc]", "Level select  [Esc]")))
            {
                showingDemos = false;
                return;
            }

            const float rowHeight = 91, cardHeight = 77, viewHeight = 356;
            var viewport = new Rect(72, 234, 976, viewHeight);
            float contentHeight = (Demos.Length - 1) * rowHeight + cardHeight;
            float rowWidth = contentHeight > viewHeight ? 950 : viewport.width;
            demoScroll = GUI.BeginScrollView(viewport, demoScroll,
                new Rect(0, 0, rowWidth, contentHeight));
            int selected = -1;
            for (int i = 0; i < Demos.Length; i++)
            {
                Demo demo = Demos[i];
                var row = new Rect(0, i * rowHeight, rowWidth, cardHeight);
                bool available = Application.CanStreamedLevelBeLoaded(demo.Scene);
                Card(row, available && row.Contains(Event.current.mousePosition));
                GUI.Label(new Rect(22, row.y + 10, 540, 34), Local(demo.Name, demo.EnglishName), heading);
                GUI.Label(new Rect(24, row.y + 45, 690, 23), Local(demo.Description, demo.EnglishDescription), body);
                string suffix = i < 9 ? $"  [{i + 1}]" : "";
                GUI.Label(new Rect(rowWidth - 137, row.y, 115, row.height),
                    available ? Local("打开", "Open") + suffix : Local("暂不可用", "Unavailable"), centered);
                if (available && GUI.Button(row, GUIContent.none, GUIStyle.none)) selected = i;
            }
            GUI.EndScrollView();
            if (selected >= 0) OpenDemo(selected);
        }

        private GUIStyle Style(int size, Color color, FontStyle weight = FontStyle.Normal,
            TextAnchor alignment = TextAnchor.MiddleLeft) => new GUIStyle(GUI.skin.label)
        {
            font = menuFont != null ? menuFont : GUI.skin.font,
            fontSize = size,
            fontStyle = weight,
            alignment = alignment,
            normal = { textColor = color }
        };

        private bool Action(Rect rect, string label)
        {
            Card(rect, rect.Contains(Event.current.mousePosition));
            GUI.Label(rect, label, centered);
            return GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }

        private static void Card(Rect rect, bool hover)
        {
            Fill(rect, hover ? Cyan : Border);
            Fill(new Rect(rect.x + 1, rect.y + 1, rect.width - 2, rect.height - 2),
                hover ? new Color32(31, 58, 77, 255) : Panel);
        }

        internal static void Fill(Rect rect, Color color)
        {
            if (Event.current.type != EventType.Repaint) return;
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
