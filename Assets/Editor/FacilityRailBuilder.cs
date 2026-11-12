using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PolarityFacility
{
    public static partial class FacilityBuilder
    {
        private const string RailScenePath = "Assets/Scenes/RailRelay.unity";

        [MenuItem("Tools/Polarity Facility/Rebuild Rail Relay Scene")]
        public static void CreateRailRelayScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildMaterials();
            geometry = new GameObject("Suspended platforms").transform;
            detail = new GameObject("Rail relay hardware").transform;
            background = new GameObject("Lower facility").transform;

            Platform("Control platform", -7f, -1.85f, -3.28f, -.62f, 0, 4.0f);
            Platform("Return platform", .20f, 4.6f, -3.28f, -.62f, 0, 4.0f);
            Platform("Rail entry", -7f, -1.38f, .58f, 4.69f, 0, 4.5f);
            Platform("Turning walkway", -1.38f, 2.68f, 2.73f, 4.69f, 0, 3.1f, -1.42f, 2.72f);
            Platform("Receiver platform", 2.68f, 4.6f, .58f, 4.69f, 0, 4.1f);
            Platform("Shared exit", 4.6f, 7.06f, -3.28f, 4.69f, 0, 5.0f, 4.56f, 7.06f);

            Transform bridge = Box("Return lift bridge", new Vector3(-.825f, -.23f, -1.88f),
                new Vector3(2.02f, .46f, 2.32f), steel, geometry, true);
            bridge.GetComponent<BoxCollider>().size = new Vector3(2.16f / 2.02f, 1, 1);
            LocalBox(bridge, "Bridge guide line", new Vector3(0,.53f,0), new Vector3(.94f,.025f,.08f), ready);

            Transform pad = Mark("Rail control pad", new Vector3(-2.65f, 0, -1.88f));
            Renderer padLamp = Socket(pad.position, .75f, idle, "Rail control plate");
            Box("Control approach marking", new Vector3(-4.08f,.025f,-1.88f),
                new Vector3(1.28f,.022f,.075f), stripe, detail, false);

            Transform guideStart = Mark("Fixed rail start", new Vector3(-3.05f, 0, 4.5f));
            Transform guideEnd = Mark("Fixed rail end", new Vector3(2.48f, 0, 4.5f));
            BuildMagneticRail("Fixed north guide", guideStart.position, guideEnd.position);
            Transform railStart = Mark("Reversing rail start", new Vector3(-3.78f, 0, 1.12f));
            Transform railEnd = Mark("Reversing rail end", new Vector3(-1.48f, 0, 1.12f));
            Renderer switchRail = BuildMagneticRail("Reversible corner rail", railStart.position, railEnd.position);

            Box("Corner barrier", new Vector3(-1.40f,.39f,1.70f),
                new Vector3(.19f,.78f,2.10f), steel, geometry, true);
            Box("Walkway guard", new Vector3(.66f,.33f,2.79f),
                new Vector3(4.12f,.66f,.18f), dark, geometry, true);
            Box("Walkway guard stripe", new Vector3(.66f,.674f,2.79f),
                new Vector3(4.03f,.025f,.15f), stripe, detail, false);
            Transform shutter = Box("Corner shutter", new Vector3(-1.03f,1.06f,3.63f),
                new Vector3(.26f,2.12f,1.82f), steel, geometry, true);
            for (int i = 0; i < 5; i++)
                LocalBox(shutter, "Shutter stripe", new Vector3(-.55f,-.36f+i*.18f,0),
                    new Vector3(.13f,.047f,.89f), stripe);
            var actuator = shutter.gameObject.AddComponent<Rigidbody>();
            actuator.isKinematic = true;
            actuator.useGravity = false;
            actuator.interpolation = RigidbodyInterpolation.Interpolate;
            actuator.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            Transform receiver = Mark("B receiver", new Vector3(3.45f, 0, 3.56f));
            Renderer receiverLamp = Socket(receiver.position, .81f, idle, "Relay receiver plate");
            const float exitRadius = 1.24f;
            Transform exit = Mark("Pair exit", new Vector3(5.65f, 0, .35f));
            Socket(exit.position, exitRadius, ready, "Exit annulus");
            Box("Exit backstop", new Vector3(6.98f,.67f,.65f),
                new Vector3(.17f,1.34f,5.76f), dark, geometry, true);
            Frame(new Vector3(-5.48f,0,4.25f), 2.43f, 2.55f, false);
            Decorate(true);

            Rigidbody a = Ball("Body A", new Vector3(-5.55f,.50f,-1.88f), red, out var shellA, out var haloA, out var trailA);
            Rigidbody b = Ball("Body B", new Vector3(-5.24f,.50f,1.91f), blue, out var shellB, out var haloB, out var trailB);
            LineRenderer field = Line("Pair field", 37, .038f);
            Camera view = Lighting(false);
            view.orthographicSize = 6.15f;
            view.transform.position = new Vector3(.72f,16.5f,-21.8f);
            view.transform.LookAt(new Vector3(.02f,-.65f,.55f));

            var root = new GameObject("Facility simulation");
            var relay = root.AddComponent<RailRelay>();
            var so = new SerializedObject(relay);
            Put(so, "controlPad", pad); Put(so, "controlLamp", padLamp);
            Put(so, "railLamp", switchRail); Put(so, "shutter", actuator);
            Put(so, "idle", idle); Put(so, "live", ready); Put(so, "north", red); Put(so, "south", blue);
            so.FindProperty("exitRadius").floatValue = exitRadius;
            ConfigureRail(so, "guide", guideStart, guideEnd, 2.7f, 17f);
            ConfigureRail(so, "reversingRail", railStart, railEnd, 2.45f, 26f);
            so.ApplyModifiedPropertiesWithoutUndo();

            so = new SerializedObject(root.AddComponent<FacilityLevel>());
            PutArray(so, "bodies", a, b); PutArray(so, "shells", shellA, shellB);
            PutArray(so, "halos", haloA, haloB); PutArray(so, "trails", trailA, trailB);
            Put(so, "northShell", red); Put(so, "southShell", blue); Put(so, "field", field);
            Put(so, "farSwitch", receiver); Put(so, "exit", exit); Put(so, "bridge", bridge);
            Put(so, "switchLight", receiverLamp); Put(so, "standbyLight", idle);
            Put(so, "readyLight", ready); Put(so, "view", view); Put(so, "railRelay", relay);
            so.ApplyModifiedPropertiesWithoutUndo();
            SavePlayableScene(root.scene, RailScenePath);
        }

        private static Renderer BuildMagneticRail(string name, Vector3 start, Vector3 end)
        {
            Vector3 center = (start + end) * .5f;
            float length = end.x - start.x;
            Box(name + " chassis", center + Vector3.up * .43f,
                new Vector3(length,.86f,.22f), steel, geometry, true);
            return Box(name + " pole strip", center + Vector3.up * .896f,
                new Vector3(length-.10f,.07f,.22f), red, detail, false).GetComponent<Renderer>();
        }

        private static void ConfigureRail(SerializedObject so, string key, Transform start, Transform end, float range, float strength)
        {
            var rail = so.FindProperty(key);
            rail.FindPropertyRelative("start").objectReferenceValue = start;
            rail.FindPropertyRelative("end").objectReferenceValue = end;
            rail.FindPropertyRelative("range").floatValue = range;
            rail.FindPropertyRelative("strength").floatValue = strength;
        }

        [MenuItem("Tools/Polarity Facility/Build Rail Relay for Windows")]
        public static void BuildRailRelayWindows() => BuildChallengeWindows(RailScenePath, "Windows-RailRelay");
    }
}
