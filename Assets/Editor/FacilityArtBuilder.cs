using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace PolarityFacility
{
    public static partial class FacilityBuilder
    {
        private const string PreviewFolder = LevelNavigation.FoundrySceneFolder;

        [MenuItem("Tools/Polarity Facility/Rebuild Art Preview")]
        public static void CreateArtPreview() => GenerateFoundryScene(MassScenePath);

        [MenuItem("Tools/Polarity Facility/Rebuild Foundry Demos")]
        public static void CreateFoundryDemos()
        {
            RebuildFoundryDemos();
            EditorSceneManager.OpenScene(MenuScenePath);
        }

        private static string[] RebuildFoundryDemos() => new[]
        {
            MenuScenePath, GenerateFoundryScene(ScenePath), GenerateFoundryScene(MassScenePath),
            GenerateFoundryScene(RailScenePath), GenerateFoundryScene(PumpScenePath), GenerateFoundryScene(ClimbScenePath)
        };

        private static string GenerateFoundryScene(string sourcePath)
        {
            if (!AssetDatabase.IsValidFolder(PreviewFolder))
                AssetDatabase.CreateFolder("Assets/Art", "FacilityPreviewGenerated");
            string name = Path.GetFileNameWithoutExtension(sourcePath);
            string foundryPath = $"{PreviewFolder}/{name}.unity";
            var source = EditorSceneManager.OpenScene(sourcePath);
            if (!EditorSceneManager.SaveScene(source, foundryPath, true))
                throw new IOException("Could not copy " + sourcePath);
            var scene = EditorSceneManager.OpenScene(foundryPath);
            var art = new FoundryArt(name);
            if (sourcePath == MassScenePath) art.DressBallastScene();
            else if (sourcePath == ScenePath) art.DressDualScene();
            else if (sourcePath == RailScenePath) art.DressRailScene();
            else if (sourcePath == PumpScenePath) art.DressPumpScene();
            else if (sourcePath == ClimbScenePath) art.DressClimbScene();
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save " + foundryPath);
            AssetDatabase.SaveAssets();
            return foundryPath;
        }

        [MenuItem("Tools/Polarity Facility/Build Art Preview for Windows")]
        public static void BuildArtPreviewWindows() =>
            BuildWindowsPlayer(new[] { GenerateFoundryScene(MassScenePath) }, "Windows-FoundryPreview");

        private sealed partial class FoundryArt
        {
            private readonly Material top = ArtMaterial("Ceramic deck", "Polarity/Facility Plate", 188, 187, 182, .07f, .25f);
            private readonly Material lightTop = ArtMaterial("Ceramic panel", "Polarity/Facility Plate", 208, 206, 197, .07f, .25f);
            private readonly Material edge = ArtMaterial("Cast aluminium", "Polarity/Weathered Steel", 119, 139, 161, .56f, .47f);
            private readonly Material shell = ArtMaterial("Midnight steel", "Polarity/Weathered Steel", 52, 67, 87, .51f, .39f);
            private readonly Material armor = ArtMaterial("Foundry cladding", "Polarity/Weathered Steel", 73, 87, 106, .55f, .45f);
            private readonly Material rib = ArtMaterial("Gunmetal", "Polarity/Weathered Steel", 41, 53, 72, .43f, .38f);
            private readonly Material patina = ArtMaterial("Oxidized conduit", "Polarity/Weathered Steel", 108, 88, 69, .60f, .35f);
            private readonly Material recess = ArtMaterial("Recess", "Standard", 26, 40, 58, .34f, .31f);
            private readonly Material safety = ArtMaterial("Safety ochre", "Polarity/Facility Plate", 218, 155, 48, .14f, .34f);
            private readonly Material brass = ArtMaterial("Machined brass", "Standard", 150, 97, 31, .68f, .51f);
            private readonly Material north = ArtMaterial("Vermilion core", "Polarity/Luminous Core", 255, 26, 48, 0, .76f);
            private readonly Material south = ArtMaterial("Cobalt core", "Polarity/Luminous Core", 25, 110, 255, 0, .78f);
            private readonly Material tracer = ArtMaterial("Residual arc power", "Standard", 24, 103, 197, .12f, .34f,
                new Color(.015f, .23f, .79f));
            private readonly Material pilot = ArtMaterial("Blue pilot light", "Standard", 77, 187, 238, .10f, .49f,
                new Color(.06f, .40f, 1.10f));
            private readonly Material idle = ArtMaterial("Standby amber", "Standard", 190, 118, 34, .12f, .49f, new Color(.35f, .11f, .01f));
            private readonly Material live = ArtMaterial("Live frost", "Standard", 83, 182, 228, .12f, .59f, new Color(.14f, .48f, .87f));
            private readonly Material signal = ArtMaterial("Signal ribbon", "Polarity/Facility Signal", 255, 255, 255);
            private readonly Material halo = ArtMaterial("Induction halo", "Polarity/Facility Signal", 255, 255, 255);
            private readonly Mesh collar = SaveMesh("Annular collar", MakeCollar(.38f));
            private readonly Mesh lampCollar = SaveMesh("Recessed light band", MakeCollar(.424f));
            private readonly string roomName;
            private int bevelIndex;

            internal FoundryArt(string roomName)
            {
                this.roomName = roomName;
                halo.SetFloat("_Ring", 1);
            }

            internal void DressBallastScene()
            {
                var level = Object.FindFirstObjectByType<FacilityLevel>();
                var settings = new SerializedObject(level);
                var root = new GameObject("05  Art preview").transform;
                Transform decks = GameObject.Find("01  Suspended platforms").transform;
                Transform fittings = GameObject.Find("02  Machine details").transform;
                Transform oldBackground = GameObject.Find("03  Lower facility").transform;
                oldBackground.gameObject.SetActive(false);
                var bodyA = Find<Rigidbody>(settings, "bodies", 0);
                var bodyB = Find<Rigidbody>(settings, "bodies", 1);
                var receiver = Find<Transform>(settings, "farSwitch");
                var bridge = Find<Transform>(settings, "bridge");
                var goal = Find<Transform>(settings, "exit");
                var receiverSensor = Find<Renderer>(settings, "switchLight");

                ShadeExistingGeometry(decks, fittings);
                BuildDeckTrim(root);
                LightScene(Find<Camera>(settings, "view"), root);
                BuildFoundry(root);
                DressCore(root, bodyA, north);
                DressCore(root, bodyB, south);
                Find<LineRenderer>(settings, "halos", 0).enabled = false;
                Find<LineRenderer>(settings, "halos", 1).enabled = false;
                Find<LineRenderer>(settings, "field").sharedMaterial = signal;
                Find<TrailRenderer>(settings, "trails", 0).sharedMaterial = signal;
                Find<TrailRenderer>(settings, "trails", 1).sharedMaterial = signal;

                Transform anchor = Find<Transform>(settings, "anchor");
                SocketSkin(root, anchor.position, .64f, safety, out _);
                var stationData = Find<MassTransferStation>(settings, "massTransfer");
                var station = new SerializedObject(stationData);
                SocketSkin(root, Find<Transform>(station, "nearPad").position, .70f, idle, out var nearLamp);
                SocketSkin(root, Find<Transform>(station, "farPad").position, .70f, idle, out var farLamp);
                Find<Renderer>(station, "nearLamp").enabled = false;
                Find<Renderer>(station, "farLamp").enabled = false;
                Put(station, "nearLamp", nearLamp);
                Put(station, "farLamp", farLamp);
                Put(station, "idle", idle);
                Put(station, "live", live);
                station.ApplyModifiedPropertiesWithoutUndo();

                Transform receiverHead = SocketSkin(root, receiver.position, .79f, idle, out var receiverLamp);
                SocketSkin(root, goal.position, 1.47f, live, out _);
                DressExit(root, goal.position);
                receiverSensor.enabled = false;
                Renderer bridgeLamp = DressBridge(bridge, root);
                Vector3 signalStart = receiver.position + new Vector3(-.62f, .036f, 0);
                Vector3 signalEnd = receiver.position + new Vector3(-1.93f, .036f, 0);
                Box("Receiver conduit", (signalStart + signalEnd) / 2 - Vector3.up * .018f,
                    new Vector3(1.33f, .014f, .12f), recess, root, false);

                var presentation = level.gameObject.AddComponent<FacilityPreviewPresentation>();
                var display = new SerializedObject(presentation);
                Put(display, "level", level);
                Put(display, "station", stationData);
                Put(display, "receiverSensor", receiverSensor);
                Put(display, "receiverHead", receiverHead);
                Put(display, "receiverLamp", receiverLamp);
                var pulse = MakeRingPlane(root, "Receiver shock ring");
                pulse.transform.position = receiver.position + Vector3.up * .11f;
                Put(display, "receiverPulse", pulse);
                Put(display, "bridgeLamp", bridgeLamp);
                Put(display, "bridgeSurface", bridge.GetComponent<Collider>());
                Put(display, "powered", live);
                Put(display, "unpowered", idle);
                Put(display, "signal", MakeSignal(root));
                display.FindProperty("signalStart").vector3Value = signalStart;
                display.FindProperty("signalEnd").vector3Value = signalEnd;
                display.ApplyModifiedPropertiesWithoutUndo();

                Put(settings, "northShell", north);
                Put(settings, "southShell", south);
                Put(settings, "standbyLight", idle);
                Put(settings, "readyLight", live);
                Put(settings, "artPreview", presentation);
                settings.ApplyModifiedPropertiesWithoutUndo();
            }

            private void ShadeExistingGeometry(Transform decks, Transform fittings)
            {
                foreach (Transform item in decks)
                {
                    if (item.name.EndsWith(" top")) Refinish(item, top, .045f);
                    else if (item.name.EndsWith(" chassis")) Refinish(item, shell, .11f);
                    else if (item.name == "Exit backstop") item.GetComponent<Renderer>().enabled = false;
                }
                foreach (Transform item in fittings)
                {
                    Renderer surface = item.GetComponent<Renderer>();
                    if (surface == null) continue;
                    if (item.name == "Floor panel")
                        surface.sharedMaterial = item.GetSiblingIndex() % 5 == 0 ? top : lightTop;
                    else if (item.name == "Painted edge" || item.name == "Face rib" || item.name == "Terminal lug"
                        || item.name.EndsWith(" outer") || item.name.EndsWith(" insert") || item.name.EndsWith(" hub")
                        || item.name == "Receiver cue" || item.name == "Break edge beam" || item.name == "Throw lane"
                        || item.name.StartsWith("Exit backstop ribs") || item.name == "Anchor recess emitting rim"
                        || item.name == "Exit annulus emitting rim" || item.name == "Upright paint")
                        surface.enabled = false;
                    else if (item.name == "Far sill" || item.name == "Lower front belt")
                        surface.sharedMaterial = edge;
                    else if (item.name == "Frame upright" || item.name == "Overhead beam" || item.name.StartsWith("Anchor "))
                        Refinish(item, shell, .045f);
                    else if (item.name == "Ballast station channel")
                        surface.sharedMaterial = recess;
                }
            }

            private void BuildDeckTrim(Transform root, float transferEnd = 4.85f, string intake = "02", string transfer = "03")
            {
                DeckFacade(root, -12.5f, -3.35f, -2.85f, 0, 4.5f, intake);
                DeckFacade(root, -1.3f, transferEnd, -2.65f, -.62f, 5.7f, transfer);
                Hazard(root, -3.38f, 0, -2.72f, 2.72f, -1);
                Hazard(root, -1.27f, -.62f, -2.53f, 3.23f, 1);
                for (int i = -1; i <= 1; i++)
                {
                    float x = -4.07f + i * .22f;
                    for (int side = -1; side <= 1; side += 2)
                    {
                        var arrow = Box("Launch chevron", new Vector3(x, .021f, side * .115f),
                            new Vector3(.036f, .009f, .27f), safety, root, false);
                        arrow.rotation = Quaternion.Euler(0, side * 38, 0);
                    }
                }
                for (int side = -1; side <= 1; side += 2)
                {
                    var column = Box("Lift guide", new Vector3(-2.30f, -3.18f, side * 1.31f),
                        new Vector3(.35f, 4.87f, .30f), shell, root, false);
                    Refinish(column, shell, .045f);
                    Box("Lift guide track", column.position + new Vector3(0, 0, -side * .16f),
                        new Vector3(.105f, 4.7f, .04f), edge, root, false);
                }
            }

            private void DeckFacade(Transform root, float from, float to, float front, float topY, float depth, string number)
            {
                float middle = (from + to) / 2;
                var fascia = Box("Segment fascia", new Vector3(middle, topY - .30f, front - .04f),
                    new Vector3(to - from - .12f, .26f, .17f), edge, root, false);
                Refinish(fascia, edge, .035f);
                int count = Mathf.CeilToInt((to - from) / 1.92f);
                float pitch = (to - from - .24f) / count;
                for (int i = 0; i < count; i++)
                {
                    float x = from + .12f + (i + .5f) * pitch;
                    bool open = i == 1;
                    Box("Welded box section", new Vector3(x - pitch * .50f, topY - depth * .53f, front - .006f),
                        new Vector3(.25f, depth * .90f, .36f), edge, root, false);
                    var plate = Box(open ? "Open service bay frame" : "Armored maintenance cover",
                        new Vector3(x, topY - depth * .54f, front - .025f),
                        new Vector3(pitch - .37f, depth * .61f, .19f), open ? recess : armor, root, false);
                    Refinish(plate, open ? recess : armor, .055f);
                    if (open)
                    {
                        for (int bar = -1; bar <= 1; bar++)
                        {
                            Pipe(root, new Vector3(x + bar * .26f, topY - depth * .25f, front - .17f),
                                new Vector3(x + bar * .26f, topY - depth * .79f, front - .17f), .068f, bar == 0 ? patina : rib);
                        }
                        Box("Service bay pilot slot", new Vector3(x, topY - depth * .66f, front - .205f),
                            new Vector3(pitch - .65f, .08f, .055f), tracer, root, false);
                    }
                    else
                    {
                        Box("Panel strengthening band", new Vector3(x, topY - depth * .62f, front - .127f),
                            new Vector3(pitch - .47f, .16f, .071f), rib, root, false);
                        Box("Upper vent recess", new Vector3(x, topY - depth * .34f, front - .126f),
                            new Vector3(pitch * .54f, .44f, .038f), recess, root, false);
                        for (int slat = -1; slat <= 1; slat++)
                            Box("Vent grille bar", new Vector3(x, topY - depth * .34f + slat * .104f, front - .155f),
                                new Vector3(pitch * .46f, .035f, .041f), edge, root, false);
                        for (int side = -1; side <= 1; side += 2)
                        for (int row = -1; row <= 1; row += 2)
                        {
                            var bolt = Primitive(PrimitiveType.Cylinder, "Captive flange bolt",
                                new Vector3(x + side * (pitch * .5f - .27f), topY - depth * .54f + row * depth * .25f, front - .139f),
                                new Vector3(.10f, .023f, .10f), edge, root, false);
                            bolt.rotation = Quaternion.Euler(90, 0, 0);
                        }
                    }
                }
                Box("Survey marking plate", new Vector3(to - .91f, topY - 1.19f, front - .219f),
                    new Vector3(1.00f, .73f, .065f), edge, root, false);
                Engrave(root, number, new Vector3(to - .91f, topY - 1.20f, front - .258f), .073f);
                Pipe(root, new Vector3(from + .38f, topY - depth * .87f, front - .20f),
                    new Vector3(to - .34f, topY - depth * .87f, front - .20f), .105f, patina);
                for (int i = 0; i < 3; i++)
                {
                    float x = Mathf.Lerp(from + .73f, to - .74f, i / 2f);
                    Box("Service run hanger", new Vector3(x, topY - depth * .87f, front - .20f),
                        new Vector3(.10f, .30f, .29f), edge, root, false);
                }
                Box("Foundation pedestal", new Vector3(middle, topY - depth - 7.4f, front + 1.94f),
                    new Vector3((to - from) * .68f, 15.1f, 3.15f), rib, root, false);
                Box("Vertical actuator housing", new Vector3(to + .06f, topY - depth * .52f, front + .78f),
                    new Vector3(.22f, depth * .81f, .92f), armor, root, false);
                for (int railIndex = 0; railIndex < 2; railIndex++)
                {
                    float z = front + 1.37f + railIndex * .38f;
                    Pipe(root, new Vector3(to + .15f, topY - .74f, z),
                        new Vector3(to + .15f, topY - depth - 3.0f, z), .117f, railIndex == 0 ? edge : patina);
                }
            }

            private void Hazard(Transform root, float x, float y, float fromZ, float toZ, int inside)
            {
                Box("Break edge paint", new Vector3(x + inside * .045f, y + .017f, (fromZ + toZ) / 2),
                    new Vector3(.062f, .013f, toZ - fromZ), safety, root, false);
                for (float z = fromZ + .27f; z < toZ - .22f; z += .48f)
                {
                    var stripeMark = Box("Diagonal edge marking", new Vector3(x + inside * .25f, y + .018f, z),
                        new Vector3(.34f, .011f, .12f), safety, root, false);
                    stripeMark.rotation = Quaternion.Euler(0, inside * 31, 0);
                }
            }

            private static void Engrave(Transform root, string title, Vector3 at, float size)
            {
                var mark = new GameObject("Stencilled " + title, typeof(TextMesh));
                mark.transform.SetParent(root);
                mark.transform.position = at;
                var text = mark.GetComponent<TextMesh>();
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                mark.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
                text.text = title;
                text.fontSize = 72;
                text.characterSize = size;
                text.fontStyle = FontStyle.Bold;
                text.anchor = TextAnchor.MiddleCenter;
                text.color = new Color32(27, 46, 68, 255);
            }

            private Renderer DressBridge(Transform bridge, Transform root)
            {
                Refinish(bridge, edge, .058f);
                foreach (Transform child in bridge) child.gameObject.SetActive(false);
                AddTreads(bridge, root, true);
                Vector3 size = bridge.localScale;
                float surface = bridge.position.y + size.y * .5f + .023f;
                var lamp = Box("Lift ready strip", new Vector3(bridge.position.x, surface, bridge.position.z - size.z * .47f),
                    new Vector3(size.x - .16f, .035f, .062f), idle, root, false);
                lamp.SetParent(bridge, true);
                return lamp.GetComponent<Renderer>();
            }

            private void DressExit(Transform root, Vector3 at, float distance = 1.62f, float width = 4.8f)
            {
                float x = at.x + distance;
                var bumper = Box("Exit safety bumper", new Vector3(x, at.y + .19f, at.z),
                    new Vector3(.31f, .38f, width), shell, root, false);
                Refinish(bumper, shell, .049f);
                Box("Exit bumper strip", bumper.position + Vector3.up * .19f,
                    new Vector3(.21f, .013f, width - .20f), safety, root, false);
                for (int side = -1; side <= 1; side += 2)
                {
                    var upright = Box("Exit indicator housing", new Vector3(x, at.y + .61f, at.z + side * 1.33f),
                        new Vector3(.39f, 1.20f, .23f), shell, root, false);
                    Refinish(upright, shell, .032f);
                    Box("Exit indicator", upright.position + new Vector3(-.206f, .15f, 0),
                        new Vector3(.032f, .73f, .080f), live, root, false);
                }
            }

            private Transform SocketSkin(Transform root, Vector3 at, float radius, Material lamp, out Renderer lightRing)
            {
                var head = new GameObject("Recessed receiver assembly").transform;
                head.SetParent(root);
                head.position = at;
                Disc(head, "Steel seat", at + Vector3.up * .042f, radius * 2, .09f, edge, collar);
                Disc(head, "Insulated plate", at + Vector3.up * .053f, radius * 1.72f, .055f, recess);
                lightRing = Disc(head, "Recessed status ring", at + Vector3.up * .080f, radius * 1.78f, .026f, lamp, lampCollar);
                Disc(head, "Contact cap", at + Vector3.up * .082f, radius * 1.41f, .040f, shell);
                Disc(head, "Brushed hub", at + Vector3.up * .108f, radius * .44f, .024f, edge);
                for (int i = 0; i < 4; i++)
                {
                    var tab = Box("Four fixture clamps", at + Quaternion.Euler(0, i * 90, 0) * new Vector3(radius * .91f, .079f, 0),
                        new Vector3(radius * .19f, .090f, radius * .24f), edge, head, false);
                    tab.rotation = Quaternion.Euler(0, i * 90, 0);
                }
                return head;
            }

            private void DressCore(Transform root, Rigidbody rigidbody, Material core)
            {
                Transform ball = rigidbody.transform;
                Renderer sphere = ball.GetComponent<Renderer>();
                sphere.sharedMaterial = core;
                foreach (LineRenderer seam in ball.GetComponentsInChildren<LineRenderer>(true)) seam.enabled = false;
                var meridian = Disc(ball, "Metal meridian", ball.position, 1.006f, .029f, recess, collar).transform;
                meridian.localPosition = Vector3.zero;
                meridian.localScale = new Vector3(1.005f, .038f, 1.005f);
                meridian.localRotation = Quaternion.Euler(0, 0, 90);
                for (int side = -1; side <= 1; side += 2)
                {
                    var cap = Disc(ball, "Axle pin", ball.position, .21f, .058f, edge).transform;
                    cap.localScale = new Vector3(.24f, .025f, .24f);
                    cap.localPosition = new Vector3(side * .48f, 0, 0);
                    cap.localRotation = Quaternion.Euler(0, 0, 90);
                }
                foreach (MeshRenderer fixture in ball.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (fixture.name != "Ballast band") continue;
                    fixture.GetComponent<MeshFilter>().sharedMesh = collar;
                    fixture.sharedMaterial = brass;
                    Transform strap = fixture.transform;
                    Vector3 bandScale = strap.localScale;
                    strap.localScale = new Vector3(bandScale.x, .108f, bandScale.z);
                    for (int clip = 0; clip < 4; clip++)
                    {
                        float angle = 45 + 90 * clip;
                        Vector3 pin = Quaternion.Euler(0, angle, 0) * new Vector3(.493f, 0, 0);
                        var clasp = Box("Ballast clasp", Vector3.zero, Vector3.one, safety, strap, false);
                        clasp.localPosition = pin;
                        clasp.localScale = new Vector3(.052f, .87f, .117f);
                        clasp.localRotation = Quaternion.Euler(0, angle, 0);
                    }
                }
                var glowSettings = new SerializedObject(ball.gameObject.AddComponent<FacilityCoreGlow>());
                Put(glowSettings, "shell", sphere);
                Put(glowSettings, "north", north);
                Put(glowSettings, "coreLight", MakeCoreLight(rigidbody));
                Put(glowSettings, "groundRing", MakeRingPlane(root, ball.name + " ground field"));
                glowSettings.ApplyModifiedPropertiesWithoutUndo();
            }

            private void LightScene(Camera view, Transform root, bool ballast = true)
            {
                view.backgroundColor = new Color32(8, 15, 30, 255);
                if (ballast)
                {
                    view.orthographicSize = 5.75f;
                    Vector3 target = new Vector3(-3.0f, -1.38f, .14f);
                    view.transform.position = target + new Vector3(7.5f, 14.1f, -20.5f);
                    view.transform.LookAt(target);
                }
                view.farClipPlane = 126;
                view.allowHDR = true;
                var atmosphere = new SerializedObject(view.gameObject.AddComponent<FacilityFoundryAtmosphere>());
                Put(atmosphere, "shader", Shader.Find("Polarity/Foundry Atmosphere"));
                atmosphere.ApplyModifiedPropertiesWithoutUndo();
                GameObject.Find("Key cold daylight").SetActive(false);
                GameObject.Find("Warm bounce").SetActive(false);
                Directional("Overhead service lighting", Quaternion.Euler(47, 37, 0), new Color(1, .94f, .84f), .91f, true);
                Directional("Residual blue bounce", Quaternion.Euler(-25, -121, 0), new Color(.44f, .60f, 1), .57f, false);
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = new Color(.47f, .57f, .72f);
                RenderSettings.ambientEquatorColor = new Color(.25f, .34f, .49f);
                RenderSettings.ambientGroundColor = new Color(.16f, .24f, .40f);
                RenderSettings.fog = true;
                RenderSettings.fogColor = view.backgroundColor;
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogStartDistance = 28;
                RenderSettings.fogEndDistance = 91;
                var probe = new GameObject("Chamber reflections", typeof(ReflectionProbe)).GetComponent<ReflectionProbe>();
                probe.transform.SetParent(root);
                probe.transform.position = ballast ? new Vector3(-4.8f, 3.7f, -1.5f) : new Vector3(1.6f, 3.7f, .2f);
                probe.size = new Vector3(37, 16, 20);
                probe.mode = ReflectionProbeMode.Realtime;
                probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
                probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
                probe.resolution = 128;
                probe.intensity = .72f;
                probe.clearFlags = ReflectionProbeClearFlags.SolidColor;
                probe.backgroundColor = view.backgroundColor;
            }

            private void BuildFoundry(Transform root, Vector3 offset = default)
            {
                var depths = new GameObject("Abandoned induction foundry").transform;
                depths.SetParent(root);
                Box("Foundry outer wall", new Vector3(-4, -8, 38), new Vector3(82, 57, 3), rib, depths, false);
                for (int bay = -4; bay <= 4; bay++)
                {
                    float lane = bay * 7.1f;
                    Box("Outer wall slab", new Vector3(lane, -7, 35.1f), new Vector3(6.83f, 45, .68f),
                        bay % 2 == 0 ? shell : armor, depths, false);
                    Box("Outer wall reinforcement", new Vector3(lane + 3.36f, -5, 34.5f),
                        new Vector3(.31f, 49, 1.05f), edge, depths, false);
                    Box("Dormant window recess", new Vector3(lane, 1.1f, 34.68f),
                        new Vector3(5.6f, 2.8f, .3f), recess, depths, false);
                    if (bay % 3 != 0)
                        Box("Distant service light", new Vector3(lane, 1.71f, 34.51f),
                            new Vector3(2.5f, .17f, .055f), tracer, depths, false);
                }
                float[] x = { -24, -15, -7, .5f, 8.6f, 17, 25 };
                float[] z = { 14, 17, 20, 28, 16, 23, 13 };
                float[] caps = { 8, 12, 4.1f, 8, 13, 2.5f, 11 };
                for (int i = 0; i < x.Length; i++)
                {
                    float width = 2.9f + (i * 7 % 5) * .56f;
                    float height = caps[i] + 29;
                    var center = new Vector3(x[i], caps[i] - height / 2, z[i]);
                    Box("Press foundation", center, new Vector3(width, height, 3.6f), shell, depths, false);
                    float face = z[i] - 1.87f;
                    for (int segment = 0; segment < 4; segment++)
                    {
                        float heightCenter = caps[i] - 4.4f - segment * 5.2f;
                        Box("Riveted armor course", new Vector3(x[i], heightCenter, face),
                            new Vector3(width * .85f, 4.91f, .28f), segment % 2 == 0 ? armor : shell, depths, false);
                        Box("Reinforcing plate joint", new Vector3(x[i], heightCenter - 2.46f, face - .19f),
                            new Vector3(width * .88f, .18f, .19f), edge, depths, false);
                        if (segment > 0 && (i + segment) % 3 == 0)
                        {
                            Box("Recessed induction cell", new Vector3(x[i], heightCenter + .6f, face - .19f),
                                new Vector3(width * .63f, 2.3f, .11f), recess, depths, false);
                            for (int fin = -1; fin <= 1; fin++)
                                Box("Induction cell electrode", new Vector3(x[i] + fin * width * .147f,
                                    heightCenter + .60f, face - .285f), new Vector3(.062f, 1.57f, .065f), tracer, depths, false);
                        }
                    }
                    float braceX = x[i] + width * .44f;
                    Box("Massive press guide", new Vector3(braceX, caps[i] - 10.1f, face - .24f),
                        new Vector3(.44f, 22.1f, .66f), rib, depths, false);
                    Pipe(depths, new Vector3(braceX - .25f, caps[i] + .31f, face - .62f),
                        new Vector3(braceX - .25f, caps[i] - 22.2f, face - .62f), .16f, patina);
                    if (i % 2 == 0)
                    {
                        Box("Service light bracket", new Vector3(x[i] - width * .31f, caps[i] - 5.1f, face - .31f),
                            new Vector3(.47f, 2.05f, .24f), rib, depths, false);
                        Box("Glowing inspection slot", new Vector3(x[i] - width * .31f, caps[i] - 5.1f, face - .45f),
                            new Vector3(.102f, 1.55f, .042f), pilot, depths, false);
                    }
                }

                for (int span = 0; span < 2; span++)
                {
                    float y = span == 0 ? .65f : -8.2f;
                    float far = span == 0 ? 9.8f : 9.3f;
                    Box("Foundry crosshead", new Vector3(-3.3f, y, far), new Vector3(33, 1.02f, 1.05f),
                        shell, depths, false);
                    Box("Crosshead face plate", new Vector3(-3.3f, y + .23f, far - .55f), new Vector3(32.7f, .33f, .13f),
                        edge, depths, false);
                    for (int i = 0; i < 8; i++)
                    {
                        float px = -17.5f + i * 4.05f;
                        Beam(depths, "Gantry brace", new Vector3(px, y - .42f, far - .28f),
                            new Vector3(px + 1.72f, y - 3.21f, far - .28f), .22f, .38f, armor);
                        if ((i + span) % 3 == 1)
                            Box("Maintenance bar light", new Vector3(px + .85f, y - .21f, far - .606f),
                                new Vector3(.83f, .10f, .052f), tracer, depths, false);
                    }
                }
                var lowerPress = Box("Lower induction press", new Vector3(-.89f, -11.1f, 6.95f),
                    new Vector3(3.45f, 8.8f, 4.02f), rib, depths, false);
                Refinish(lowerPress, rib, .15f);
                Box("Press inspection sill", new Vector3(-.89f, -6.7f, 6.95f),
                    new Vector3(3.68f, .23f, 4.17f), armor, depths, false);
                for (int fin = -2; fin <= 2; fin++)
                {
                    float px = -.89f + fin * .56f;
                    Box("Reactor shield rib", new Vector3(px, -10.3f, 4.85f), new Vector3(.27f, 4.25f, .24f), edge, depths, false);
                    Box("Residual arc inside press", new Vector3(px + .23f, -9.75f, 4.80f),
                        new Vector3(.092f, 2.65f, .10f), tracer, depths, false);
                }
                Box("Lower foundry floor", new Vector3(-5, -29.1f, 14), new Vector3(69, .82f, 55), rib, depths, false);
                foreach (Renderer silhouette in depths.GetComponentsInChildren<Renderer>())
                    silhouette.shadowCastingMode = ShadowCastingMode.Off;
                depths.position += offset;
            }

            private static Light MakeCoreLight(Rigidbody ball)
            {
                var light = new GameObject(ball.name + " local glow", typeof(Light)).GetComponent<Light>();
                light.transform.SetParent(ball.transform, false);
                light.type = LightType.Point;
                light.range = 2.02f;
                light.intensity = .74f;
                light.renderMode = LightRenderMode.ForcePixel;
                return light;
            }

            private static void Pipe(Transform root, Vector3 from, Vector3 to, float radius, Material material)
            {
                Vector3 axis = to - from;
                var pipe = Primitive(PrimitiveType.Cylinder, "Machined conduit", (from + to) * .5f,
                    new Vector3(radius * 2, axis.magnitude * .5f, radius * 2), material, root, false);
                pipe.rotation = Quaternion.FromToRotation(Vector3.up, axis);
            }

            private static void Beam(Transform root, string name, Vector3 from, Vector3 to, float width, float thickness, Material material)
            {
                Vector3 axis = to - from;
                var beam = Box(name, (from + to) * .5f, new Vector3(width, axis.magnitude, thickness),
                    material, root, false);
                beam.rotation = Quaternion.FromToRotation(Vector3.up, axis);
            }

            private Renderer MakeRingPlane(Transform root, string name)
            {
                var plane = Primitive(PrimitiveType.Quad, name, Vector3.zero, Vector3.one, halo, root, false);
                plane.rotation = Quaternion.Euler(90, 0, 0);
                var renderer = plane.GetComponent<Renderer>();
                renderer.enabled = false;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                return renderer;
            }

            private LineRenderer MakeSignal(Transform root)
            {
                var guide = new GameObject("Receiver to lift signal", typeof(LineRenderer)).GetComponent<LineRenderer>();
                guide.transform.SetParent(root);
                guide.sharedMaterial = signal;
                guide.useWorldSpace = true;
                guide.positionCount = 2;
                guide.widthMultiplier = .12f;
                guide.startColor = new Color(.36f, .81f, 1, .14f);
                guide.endColor = new Color(.57f, .94f, 1, 1);
                guide.shadowCastingMode = ShadowCastingMode.Off;
                guide.enabled = false;
                return guide;
            }

            private static T Find<T>(SerializedObject settings, string property, int? index = null) where T : Object
            {
                SerializedProperty value = settings.FindProperty(property);
                return (index.HasValue ? value.GetArrayElementAtIndex(index.Value) : value).objectReferenceValue as T;
            }

            private static Renderer Disc(Transform parent, string name, Vector3 at, float diameter, float height, Material material, Mesh mesh = null)
            {
                if (mesh == null)
                    return Primitive(PrimitiveType.Cylinder, name, at, new Vector3(diameter, height * .5f, diameter),
                        material, parent, false).GetComponent<Renderer>();
                var disc = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
                disc.transform.SetParent(parent);
                disc.transform.position = at;
                disc.transform.localScale = new Vector3(diameter, height, diameter);
                disc.GetComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = disc.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                return renderer;
            }

            private void Refinish(Transform item, Material material, float bevel)
            {
                item.GetComponent<Renderer>().sharedMaterial = material;
                item.GetComponent<MeshFilter>().sharedMesh = SaveMesh($"{roomName} Bevel {bevelIndex++:00}",
                    MakeBeveledBox(item.localScale, bevel));
            }

            private static Material ArtMaterial(string name, string shaderName, byte r, byte g, byte b,
                float metal = 0, float gloss = 0, Color emission = default)
            {
                string path = $"{PreviewFolder}/{name}.mat";
                var shader = Shader.Find(shaderName);
                if (shader == null) throw new InvalidDataException("Shader not found: " + shaderName);
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(shader);
                    AssetDatabase.CreateAsset(material, path);
                }
                material.shader = shader;
                if (material.HasProperty("_Color")) material.color = new Color32(r, g, b, 255);
                if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metal);
                if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", gloss);
                if (emission.maxColorComponent > 0)
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", emission);
                }
                EditorUtility.SetDirty(material);
                return material;
            }

            private static Mesh SaveMesh(string name, Mesh mesh)
            {
                string path = $"{PreviewFolder}/{name}.asset";
                var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                mesh.name = name;
                if (existing == null)
                {
                    AssetDatabase.CreateAsset(mesh, path);
                    return mesh;
                }
                EditorUtility.CopySerialized(mesh, existing);
                Object.DestroyImmediate(mesh);
                return existing;
            }

            private static Mesh MakeCollar(float innerRadius)
            {
                const int segments = 80;
                var profile = new[]
                {
                    new Vector2(innerRadius, .5f), new Vector2(.46f, .5f), new Vector2(.5f, .24f),
                    new Vector2(.5f, -.24f), new Vector2(.46f, -.5f), new Vector2(innerRadius, -.5f)
                };
                var vertices = new Vector3[segments * profile.Length];
                var indices = new int[vertices.Length * 6];
                int index = 0;
                for (int p = 0; p < profile.Length; p++)
                for (int i = 0; i < segments; i++)
                {
                    float angle = i * Mathf.PI * 2 / segments;
                    int next = (i + 1) % segments;
                    int row = p * segments;
                    int nextRow = (p + 1) % profile.Length * segments;
                    vertices[row + i] = new Vector3(Mathf.Cos(angle) * profile[p].x, profile[p].y,
                        Mathf.Sin(angle) * profile[p].x);
                    indices[index++] = row + i;
                    indices[index++] = row + next;
                    indices[index++] = nextRow + next;
                    indices[index++] = row + i;
                    indices[index++] = nextRow + next;
                    indices[index++] = nextRow + i;
                }
                var mesh = new Mesh { vertices = vertices, triangles = indices };
                mesh.RecalculateNormals();
                return mesh;
            }

            private static Mesh MakeBeveledBox(Vector3 size, float radius)
            {
                Vector3 half = size * .5f;
                radius = Mathf.Min(radius, Mathf.Min(half.x, half.y, half.z) * .77f);
                Vector3 core = half - Vector3.one * radius;
                Vector3[] faces = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
                const int side = 6;
                var vertices = new Vector3[6 * side * side];
                var normals = new Vector3[vertices.Length];
                var triangles = new int[6 * (side - 1) * (side - 1) * 6];
                int index = 0;
                for (int face = 0; face < faces.Length; face++)
                {
                    Vector3 normal = faces[face];
                    Vector3 u = normal.y == 0 ? Vector3.Cross(Vector3.up, normal) : Vector3.right;
                    Vector3 v = Vector3.Cross(normal, u);
                    Vector3 center = Vector3.Scale(normal, half);
                    float[] horizontal = Stops(Mathf.Abs(Vector3.Dot(u, half)), radius);
                    float[] vertical = Stops(Mathf.Abs(Vector3.Dot(v, half)), radius);
                    int start = face * side * side;
                    for (int y = 0; y < side; y++)
                    for (int x = 0; x < side; x++)
                    {
                        Vector3 point = center + u * horizontal[x] + v * vertical[y];
                        Vector3 inside = new Vector3(Mathf.Clamp(point.x, -core.x, core.x),
                            Mathf.Clamp(point.y, -core.y, core.y), Mathf.Clamp(point.z, -core.z, core.z));
                        Vector3 outward = (point - inside).normalized;
                        Vector3 rounded = inside + outward * radius;
                        int vertex = start + y * side + x;
                        vertices[vertex] = new Vector3(rounded.x / size.x, rounded.y / size.y, rounded.z / size.z);
                        normals[vertex] = Vector3.Scale(outward, size).normalized;
                        if (x == side - 1 || y == side - 1) continue;
                        triangles[index++] = vertex;
                        triangles[index++] = vertex + 1;
                        triangles[index++] = vertex + side + 1;
                        triangles[index++] = vertex;
                        triangles[index++] = vertex + side + 1;
                        triangles[index++] = vertex + side;
                    }
                }
                return new Mesh { vertices = vertices, normals = normals, triangles = triangles };
            }

            private static float[] Stops(float half, float bevel) => new[]
            {
                -half, -half + bevel * .46f, -half + bevel, half - bevel, half - bevel * .46f, half
            };
        }
    }
}
