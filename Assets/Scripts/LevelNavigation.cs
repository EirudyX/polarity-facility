using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PolarityFacility
{
    public sealed class LevelNavigation : MonoBehaviour
    {
        private const string Menu = "LevelMenu";
        private static readonly string[] Scenes = { "PolarityFacility", "MassTransfer", "RailRelay" };
        private static readonly string[] Labels = { "Original Facility", "Mass Transfer", "Reversible Rail" };
        private static readonly KeyCode[] Shortcuts = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3 };

        internal static bool MenuAvailable => Application.CanStreamedLevelBeLoaded(Menu);
        internal static bool CampaignProof { get; private set; }

        private GUIStyle title, detail, button;

        internal static string NextLabel
        {
            get
            {
                int next = NextIndex();
                return next >= 0 ? Labels[next] : null;
            }
        }

        internal static void ReturnToMenu()
        {
            if (MenuAvailable) SceneManager.LoadScene(Menu);
        }

        internal static void Advance()
        {
            int next = NextIndex();
            if (next >= 0) Open(next);
            else ReturnToMenu();
        }

        private static int NextIndex()
        {
            int current = Array.IndexOf(Scenes, SceneManager.GetActiveScene().name);
            int next = current + 1;
            return current >= 0 && next < Scenes.Length && Application.CanStreamedLevelBeLoaded(Scenes[next])
                ? next : -1;
        }

        private static void Open(int index)
        {
            if (Application.CanStreamedLevelBeLoaded(Scenes[index]))
                SceneManager.LoadScene(Scenes[index]);
        }

        private void Start()
        {
            string[] args = Environment.GetCommandLineArgs();
            int proof = Array.IndexOf(args, "-facilityProof");
            if (Array.IndexOf(args, "-facilityCampaign") < 0 || proof < 0 || proof + 1 >= args.Length) return;
            if (CampaignProof)
            {
                Debug.Log("PASS campaign navigation: menu, original, mass transfer, rail relay, menu");
                Application.Quit(0);
                return;
            }

            CampaignProof = true;
            Debug.Log("CAMPAIGN START LevelMenu");
            Open(0);
        }

        private void Update()
        {
            if (CampaignProof) return;
            for (int i = 0; i < Shortcuts.Length; i++)
            {
                if (!Input.GetKeyDown(Shortcuts[i])) continue;
                Open(i);
                return;
            }
            if (Input.GetKeyDown(KeyCode.Return)) Open(0);
        }

        private void OnGUI()
        {
            if (CampaignProof) return;
            title ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter, fontSize = 55, fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            detail ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter, fontSize = 18,
                normal = { textColor = new Color(.76f, .84f, .93f) }
            };
            button ??= new GUIStyle(GUI.skin.button) { fontSize = 23, fontStyle = FontStyle.Bold };

            const float width = 660, height = 480;
            float scale = Mathf.Min(Screen.width / (width + 64), Screen.height / (height + 36));
            var previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(
                new Vector3((Screen.width - width * scale) / 2, (Screen.height - height * scale) / 2, 0),
                Quaternion.identity, new Vector3(scale, scale, 1));
            GUI.Label(new Rect(0, 11, width, 74), "POLARITY FACILITY", title);
            GUI.Label(new Rect(0, 94, width, 40), "SELECT A LEVEL", detail);
            for (int i = 0; i < Labels.Length; i++)
            {
                if (!GUI.Button(new Rect(52, 152 + i * 68, width - 104, 53), $"{i + 1}    {Labels[i]}", button)) continue;
                Open(i);
                break;
            }
            GUI.Label(new Rect(0, 375, width, 34), "A: WASD + Q     B: ARROWS + RIGHT SHIFT", detail);
            GUI.Label(new Rect(0, 412, width, 34), "1–3 SELECT     ENTER STARTS LEVEL 1", detail);
            GUI.matrix = previous;
        }
    }
}
