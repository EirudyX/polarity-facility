using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PolarityFacility
{
    public sealed class LevelNavigation : MonoBehaviour
    {
        private const string MenuScene = "LevelMenu";
        public const string FoundrySceneFolder = "Assets/Art/FacilityPreviewGenerated";
        internal const float LayoutWidth = 1120;
        private const float LayoutHeight = 630;

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

        private static bool reopenDemos;
        private static int proofIndex, lastDemoIndex;
        private bool showingDemos;
        private int selected, previousSelection, pendingDemo = -1;
        private float pageOpened, previewChanged, transitionAt;
        private readonly Texture2D[] previews = new Texture2D[Demos.Length];
        private Font menuFont;
        private GUIStyle title, heading, detail, small, accent, centered, button, rowTitle;

        internal static bool MenuAvailable => Application.CanStreamedLevelBeLoaded(MenuScene);
        internal static bool DemoTourProof { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetNavigation()
        {
            reopenDemos = DemoTourProof = false;
            proofIndex = lastDemoIndex = 0;
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
            selected = previousSelection = lastDemoIndex;
            pageOpened = previewChanged = Time.unscaledTime;
            menuFont = FindChineseFont();
            for (int i = 0; i < Demos.Length; i++)
                previews[i] = Resources.Load<Texture2D>("FoundryInterface/" + Demos[i].Scene);
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
            lastDemoIndex = index;
#if UNITY_EDITOR
            string foundryPath = $"{FoundrySceneFolder}/{scene}.unity";
            if (UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(foundryPath) != null)
            {
                UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(foundryPath,
                    new LoadSceneParameters(LoadSceneMode.Single));
                return true;
            }
#endif
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
            Camera view = Camera.main;
            if (view != null && view.GetComponent<FacilityFoundryAtmosphere>() != null)
                CaptureSceneCamera(view, path);
            else
            {
                var image = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
                System.IO.File.WriteAllBytes(path, image.EncodeToPNG());
                Destroy(image);
            }
            yield return new WaitForEndOfFrame();
        }

        internal static void CaptureSceneCamera(Camera view, string path)
        {
            var target = RenderTexture.GetTemporary(1600, 900, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 4);
            var previous = RenderTexture.active;
            var previousTarget = view.targetTexture;
            var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            try
            {
                view.targetTexture = target;
                view.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                System.IO.File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                view.targetTexture = previousTarget;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                Destroy(image);
            }
        }

        private void ShowDemoList()
        {
            showingDemos = true;
            pageOpened = previewChanged = Time.unscaledTime;
            selected = previousSelection = lastDemoIndex;
        }

        private void Update()
        {
            if (DemoTourProof) return;
            if (pendingDemo >= 0)
            {
                if (Time.unscaledTime - transitionAt >= .20f)
                {
                    int index = pendingDemo;
                    pendingDemo = -1;
                    OpenDemo(index);
                }
                return;
            }
            if (!showingDemos)
            {
                if (Input.GetKeyDown(KeyCode.D)) ShowDemoList();
                return;
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ShowLevelSelect();
                return;
            }
            if (Input.GetKeyDown(KeyCode.UpArrow)) SelectDemo((selected + Demos.Length - 1) % Demos.Length);
            if (Input.GetKeyDown(KeyCode.DownArrow)) SelectDemo((selected + 1) % Demos.Length);
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                QueueDemo(selected);
                return;
            }
            for (int i = 0; i < Demos.Length && i < 9; i++)
            {
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i)))
                {
                    QueueDemo(i);
                    return;
                }
            }
        }

        private string Local(string chinese, string english) => menuFont != null ? chinese : english;

        private void ShowLevelSelect()
        {
            showingDemos = false;
            pageOpened = Time.unscaledTime;
        }

        private void SelectDemo(int index)
        {
            if (index == selected) return;
            previousSelection = selected;
            selected = lastDemoIndex = index;
            previewChanged = Time.unscaledTime;
        }

        private void QueueDemo(int index)
        {
            if (pendingDemo >= 0 || !Application.CanStreamedLevelBeLoaded(Demos[index].Scene)) return;
            SelectDemo(index);
            pendingDemo = index;
            transitionAt = Time.unscaledTime;
        }

        private void OnGUI()
        {
            if (title == null)
            {
                title = FacilityConsole.TextStyle(menuFont, menuFont != null ? 76 : 55, FacilityConsole.Text, FontStyle.Bold);
                heading = FacilityConsole.TextStyle(menuFont, 40, FacilityConsole.Text, FontStyle.Bold);
                detail = FacilityConsole.TextStyle(menuFont, 16, FacilityConsole.Muted);
                small = FacilityConsole.TextStyle(menuFont, 13, FacilityConsole.Muted, FontStyle.Bold);
                accent = FacilityConsole.TextStyle(menuFont, 14, FacilityConsole.Cyan, FontStyle.Bold);
                centered = FacilityConsole.TextStyle(menuFont, 13, FacilityConsole.Text, FontStyle.Bold, TextAnchor.MiddleCenter);
                button = FacilityConsole.TextStyle(menuFont, 20, FacilityConsole.Text, FontStyle.Bold);
                rowTitle = FacilityConsole.TextStyle(menuFont, menuFont != null ? 23 : 19, FacilityConsole.Text, FontStyle.Bold);
            }

            FacilityConsole.Fill(new Rect(0, 0, Screen.width, Screen.height), FacilityConsole.Background);
            Matrix4x4 previous = BeginScaledGUI();
            Color previousColor = GUI.color;
            float entrance = DemoTourProof ? 1 : FacilityConsole.Ease(Time.unscaledTime - pageOpened, .24f);
            GUI.color = previousColor * new Color(1, 1, 1, Mathf.Lerp(.64f, 1, entrance));
            GUI.matrix *= Matrix4x4.Translate(Vector3.up * ((1 - entrance) * 9));
            if (showingDemos) DrawDemoList();
            else DrawLevelSelect();

            GUI.color = previousColor;
            GUI.matrix = previous;
            if (pendingDemo >= 0)
                FacilityConsole.Fill(new Rect(0, 0, Screen.width, Screen.height),
                    new Color(.025f, .05f, .085f, FacilityConsole.Ease(Time.unscaledTime - transitionAt, .22f)));
        }

        private void DrawLevelSelect()
        {
            var hero = new Rect(379, 29, 704, 570);
            FacilityConsole.Panel(hero);
            var picture = new Rect(hero.x + 5, hero.y + 5, hero.width - 10, hero.height - 10);
            DrawPreview(picture, previews[2], .96f);
            FacilityConsole.LeftShade(new Rect(379, 29, 296, 570), FacilityConsole.Background);
            FacilityConsole.BottomShade(new Rect(422, 455, 657, 140), new Color32(6, 13, 23, 236));
            FacilityConsole.FrameCorners(new Rect(659, 34, 419, 560), new Color32(95, 156, 184, 175), 16);
            FacilityConsole.Trace(968, 48, 94, FacilityConsole.Cyan);
            FacilityConsole.Light(50, 56, FacilityConsole.Cyan);
            GUI.Label(new Rect(68, 46, 293, 27), "POLARITY FACILITY", accent);
            FacilityConsole.Fill(new Rect(50, 84, 311, 1), FacilityConsole.Line);

            GUI.Label(new Rect(47, 140, 484, 102), Local("极性失控", "Polarity Facility"), title);
            GUI.Label(new Rect(52, 242, 329, 34), Local("双球磁力解谜", "Dual-sphere magnetic puzzles"), detail);
            FacilityConsole.Trace(52, 299, 48, FacilityConsole.Cyan);
            if (FacilityConsole.Action(new Rect(50, 322, 320, 64), Local("进入功能演示", "Feature demos"),
                    "D", button, centered, true))
                ShowDemoList();

            GUI.Label(new Rect(52, 436, 302, 26), Local("正式关卡", "Campaign"), small);
            for (int i = 0; i < 2; i++)
            {
                var row = new Rect(50, 470 + i * 49, 320, 40);
                FacilityConsole.Panel(row, true);
                GUI.Label(new Rect(row.x + 13, row.y, 121, row.height),
                    Local($"第 {i + 1} 关", $"Level {i + 1}"), detail);
                GUI.Label(new Rect(row.x + 199, row.y, 112, row.height),
                    Local("待制作", "In development"), small);
            }
            GUI.Label(new Rect(903, 554, 155, 27), Local("可翻转磁轨", "Reversible rail"), centered);
        }

        private void DrawDemoList()
        {
            FacilityConsole.Light(50, 44, FacilityConsole.Cyan);
            GUI.Label(new Rect(68, 32, 410, 30), "POLARITY FACILITY", accent);
            FacilityConsole.Fill(new Rect(48, 72, 1025, 1), FacilityConsole.Line);
            GUI.Label(new Rect(47, 87, 673, 53), Local("功能演示", "Feature demos"), heading);
            if (FacilityConsole.Action(new Rect(882, 26, 192, 40), Local("返回选关", "Level select"),
                    "Esc", detail, centered, false))
            {
                ShowLevelSelect();
                return;
            }

            for (int i = 0; i < Demos.Length; i++)
            {
                Demo item = Demos[i];
                var row = new Rect(48, 157 + i * 76, 317, 62);
                FacilityConsole.Row(row, i == selected, row.Contains(Event.current.mousePosition));
                FacilityConsole.Key(new Rect(row.x + 13, row.y + 17, 31, 28), (i + 1).ToString(), centered);
                GUI.Label(new Rect(row.x + 55, row.y, 207, row.height), Local(item.Name, item.EnglishName), rowTitle);
                if (GUI.Button(new Rect(row.x, row.y, row.width - 43, row.height), GUIContent.none, GUIStyle.none))
                    SelectDemo(i);
                if (FacilityConsole.Action(new Rect(row.xMax - 39, row.y + 13, 29, 36), "›", null, centered, centered, false))
                    QueueDemo(i);
            }
            GUI.Label(new Rect(48, 558, 320, 27),
                Local("↑ / ↓  切换预览       1–5  直接进入", "↑ / ↓  Browse       1–5  Open"), small);

            var frame = new Rect(395, 157, 679, 339);
            FacilityConsole.Panel(frame);
            var area = new Rect(frame.x + 5, frame.y + 5, frame.width - 10, frame.height - 10);
            float fade = FacilityConsole.Ease(Time.unscaledTime - previewChanged, .26f);
            if (fade < 1) DrawPreview(area, previews[previousSelection], 1);
            DrawPreview(area, previews[selected], fade);
            FacilityConsole.BottomShade(new Rect(area.x, area.yMax - 68, area.width, 68), new Color32(5, 12, 22, 162));
            FacilityConsole.FrameCorners(area, new Color32(102, 195, 224, 208), 18);
            FacilityConsole.Trace(frame.x + 19, frame.y + 5, 57, FacilityConsole.Cyan);

            Demo demo = Demos[selected];
            GUI.Label(new Rect(399, 513, 459, 32), Local(demo.Name, demo.EnglishName), rowTitle);
            GUI.Label(new Rect(399, 548, 444, 26), Local(demo.Description, demo.EnglishDescription), detail);
            if (FacilityConsole.Action(new Rect(858, 514, 216, 60), Local("进入演示", "Open demo"),
                    "Enter", button, centered, true))
                QueueDemo(selected);
        }

        private static void DrawPreview(Rect rect, Texture2D texture, float opacity)
        {
            if (texture == null || Event.current.type != EventType.Repaint) return;
            Color previous = GUI.color;
            GUI.color = previous * new Color(1, 1, 1, opacity);
            GUI.DrawTexture(rect, texture, ScaleMode.ScaleAndCrop);
            GUI.color = previous;
        }

        internal static Matrix4x4 BeginScaledGUI()
        {
            float scale = Mathf.Min(Screen.width / LayoutWidth, Screen.height / LayoutHeight);
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(
                new Vector3((Screen.width - LayoutWidth * scale) / 2, (Screen.height - LayoutHeight * scale) / 2, 0),
                Quaternion.identity, new Vector3(scale, scale, 1));
            return previous;
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
