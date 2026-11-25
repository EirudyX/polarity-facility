using UnityEditor;
using UnityEngine;

namespace PolarityFacility
{
    public static partial class FacilityBuilder
    {
        private sealed partial class FoundryArt
        {
            private Material RailNorth => ArtMaterial("North rail electrode", "Standard", 174, 32, 47, .22f, .42f,
                new Color(.82f, .028f, .055f));
            private Material RailSouth => ArtMaterial("South rail electrode", "Standard", 43, 111, 214, .22f, .42f,
                new Color(.036f, .29f, .98f));

            internal void DressDualScene()
            {
                var root = new GameObject("Foundry finish · induction").transform;
                var level = Object.FindFirstObjectByType<FacilityLevel>();
                var settings = new SerializedObject(level);
                var decks = GameObject.Find("01  Suspended platforms").transform;
                var fittings = GameObject.Find("02  Machine details").transform;
                GameObject.Find("03  Lower facility").SetActive(false);
                ShadeExistingGeometry(decks, fittings);
                BuildDeckTrim(root, 6.05f, "01", "A1");
                DeckFacade(root, 9.0f, 15.65f, .08f, -.62f, 5.0f, "A2");
                FrameDeck(decks.Find("Shutter"), root, armor);
                Refinish(fittings.Find("Magnetic column"), armor, .055f);
                fittings.Find("Fixed pole face").GetComponent<Renderer>().sharedMaterial = RailSouth;
                foreach (Transform part in fittings)
                    if (part.name.StartsWith("Pole fins")) part.GetComponent<Renderer>().sharedMaterial = shell;
                InstallPad(settings, "socketLeft", "socketLightLeft", root, .66f);
                InstallPad(settings, "socketRight", "socketLightRight", root, .66f);
                Find<Renderer>(settings, "gateLight").sharedMaterial = idle;
                SocketSkin(root, Find<Transform>(settings, "anchor").position, .64f, safety, out _);
                var goal = Find<Transform>(settings, "exit");
                SocketSkin(root, goal.position, 1.48f, live, out _);
                DressExit(root, goal.position);
                DressLiftRoom(level, settings, root, .79f, "01", "双球磁力", "Dual-sphere magnetism",
                    "用锚位弹射一球，越过断口启动接收台", "Anchor one sphere and launch the other to the receiver",
                    "升桥已就位 · 两球各压住终点前的圆盘", "Bridge ready   /   Occupy both terminal plates");
                SetupRoomCamera(Find<Camera>(settings, "view"), root, new Vector3(1.1f, -1.55f, .65f),
                    new Vector3(7.5f, 17.2f, -25), 9.15f);
                BuildFoundry(root, new Vector3(.9f, 0, 1.8f));
            }

            internal void DressRailScene()
            {
                var root = new GameObject("Foundry finish · magnetic rail").transform;
                var level = Object.FindFirstObjectByType<FacilityLevel>();
                var settings = new SerializedObject(level);
                var decks = GameObject.Find("Suspended platforms").transform;
                var fittings = GameObject.Find("Rail relay hardware").transform;
                GameObject.Find("Lower facility").SetActive(false);
                ShadeExistingGeometry(decks, fittings);
                DeckFacade(root, -7f, -1.85f, -3.28f, 0, 4.0f, "03");
                DeckFacade(root, .20f, 4.6f, -3.28f, 0, 4.0f, "B1");
                DeckFacade(root, 4.6f, 7.06f, -3.28f, 0, 5.0f, "B2");
                Hazard(root, -1.86f, 0, -3.18f, -.73f, -1);
                Hazard(root, .22f, 0, -3.18f, -.73f, 1);
                FrameDeck(decks.Find("Corner shutter"), root, armor);
                FrameDeck(decks.Find("Corner barrier"), root, shell);
                FrameDeck(decks.Find("Walkway guard"), root, rib);
                fittings.Find("Fixed north guide pole strip").GetComponent<Renderer>().sharedMaterial = RailNorth;
                var relay = new SerializedObject(Find<RailRelay>(settings, "railRelay"));
                Material northRail = RailNorth;
                Put(relay, "north", northRail);
                Put(relay, "south", RailSouth);
                Put(relay, "idle", idle);
                Put(relay, "live", live);
                Find<Renderer>(relay, "railLamp").sharedMaterial = northRail;
                InstallPad(relay, "controlPad", "controlLamp", root, .75f);
                relay.ApplyModifiedPropertiesWithoutUndo();
                var goal = Find<Transform>(settings, "exit");
                SocketSkin(root, goal.position, 1.24f, live, out _);
                DressExit(root, goal.position, 1.33f, 5.7f);
                DressLiftRoom(level, settings, root, .81f, "03", "可翻转磁轨", "Reversible rail",
                    "A 压住控制盘，让 B 通过转角", "A holds the control pad   /   B rounds the corner",
                    "升桥已就位 · A 可以松开控制盘", "Bridge ready   /   A can leave the control pad");
                SetupRoomCamera(Find<Camera>(settings, "view"), root, new Vector3(-.1f, -.92f, .63f),
                    new Vector3(6.25f, 16.1f, -22), 6.48f);
                BuildFoundry(root, new Vector3(2.1f, 0, 3.2f));
            }

            internal void DressPumpScene()
            {
                var root = new GameObject("Foundry finish · rebound track").transform;
                var demo = new SerializedObject(Object.FindFirstObjectByType<PumpSurgeDemo>());
                Transform track = GameObject.Find("Pumping test track").transform;
                Transform fittings = GameObject.Find("Instrumentation").transform;
                ShadeExistingGeometry(track, fittings);
                DressCore(root, Find<Transform>(demo, "anchor").GetComponent<Rigidbody>(), north);
                DressCore(root, Find<Rigidbody>(demo, "runner"), south);
                BindCoreMaterials(demo, "north", "south");
                Find<LineRenderer>(demo, "field").sharedMaterial = signal;
                Find<TrailRenderer>(demo, "trail").sharedMaterial = signal;
                foreach (string name in new[] { "Rolling surface", "Launch ramp", "Receiving platform" })
                {
                    Transform deck = track.Find(name);
                    Refinish(deck, top, .04f);
                    AddTreads(deck, root, false);
                }
                DeckFacade(root, -1.18f, 3.04f, -1.3f, 0, 3.50f, "04");
                DeckFacade(root, PumpSurgeDemo.LandingStart, 8.48f, -1.3f, PumpSurgeDemo.LandingHeight, 3.25f, "C1");
                Hazard(root, PumpSurgeDemo.LandingStart + .015f, PumpSurgeDemo.LandingHeight, -1.20f, 1.20f, 1);
                Refinish(fittings.Find("Rebound wall"), armor, .026f);
                FrameDeck(track.Find("Receiver end stop"), root, armor);
                foreach (Transform part in track)
                    if (part.name.EndsWith("foundation") || part.name.EndsWith("underframe"))
                        Refinish(part, shell, .05f);
                SocketSkin(root, Vector3.zero, .69f, safety, out _);
                ReplaceLampAt(demo, "receiverLamp", root, .73f);
                demo.ApplyModifiedPropertiesWithoutUndo();
                SetupRoomCamera(Camera.main, root, new Vector3(3.70f, -.62f, .08f),
                    new Vector3(3.6f, 11.9f, -16.8f), 4.13f);
                BuildFoundry(root, new Vector3(7.5f, .15f, 2.1f));
            }

            internal void DressClimbScene()
            {
                var root = new GameObject("Foundry finish · adhesion gantry").transform;
                var demo = new SerializedObject(Object.FindFirstObjectByType<MagneticClimbDemo>());
                Transform decks = GameObject.Find("Magnetic chamber").transform;
                Transform fittings = GameObject.Find("Magnetic surface hardware").transform;
                ShadeExistingGeometry(decks, fittings);
                DressCore(root, Find<Rigidbody>(demo, "bodyA"), north);
                DressCore(root, Find<Rigidbody>(demo, "bodyB"), south);
                BindCoreMaterials(demo, "north", "south");
                Find<TrailRenderer>(demo, "trailA").sharedMaterial = signal;
                Find<TrailRenderer>(demo, "trailB").sharedMaterial = signal;
                Find<LineRenderer>(demo, "tether").sharedMaterial = signal;
                foreach (string name in new[] { "Starting deck", "Receiver deck", "Exit descent", "Shared exit deck" })
                {
                    Transform deck = decks.Find(name);
                    Refinish(deck, top, .04f);
                    AddTreads(deck, root, false);
                }
                FrameDeck(decks.Find("Vertical magnetic wall"), root, armor);
                FrameDeck(decks.Find("Ceiling magnetic deck"), root, shell);
                FrameDeck(decks.Find("Exit end stop"), root, rib);
                Material electrode = RailNorth;
                foreach (Transform part in decks)
                    if (part.name == "Rounded magnetic corner") Refinish(part, shell, .012f);
                foreach (Transform part in fittings)
                    if (part.name.StartsWith("N ")) part.GetComponent<Renderer>().sharedMaterial = electrode;
                DeckFacade(root, -.38f, 2.92f, -1.55f, 0, 2.85f, "05");
                DeckFacade(root, 8.53f, 10.43f, -1.55f, 0, 2.45f, "D1");
                ReplaceLampAt(demo, "receiverLamp", root, .67f);
                ReplaceLampAt(demo, "exitLamp", root, 1.02f);
                Put(demo, "foundryLiftLamp", DressBridge(Find<Transform>(demo, "bridge"), root));
                demo.ApplyModifiedPropertiesWithoutUndo();
                SetupRoomCamera(Camera.main, root, new Vector3(4.45f, 1.73f, 0),
                    new Vector3(5.13f, 5.10f, -22), 4.43f);
                BuildFoundry(root, new Vector3(7.15f, 0, 4.2f));
            }

            private void DressLiftRoom(FacilityLevel level, SerializedObject settings, Transform root, float receiverRadius,
                string sector, string title, string englishTitle, string objective, string englishObjective,
                string open, string englishOpen)
            {
                DressCore(root, Find<Rigidbody>(settings, "bodies", 0), north);
                DressCore(root, Find<Rigidbody>(settings, "bodies", 1), south);
                Find<LineRenderer>(settings, "halos", 0).enabled = false;
                Find<LineRenderer>(settings, "halos", 1).enabled = false;
                Find<LineRenderer>(settings, "field").sharedMaterial = signal;
                Find<TrailRenderer>(settings, "trails", 0).sharedMaterial = signal;
                Find<TrailRenderer>(settings, "trails", 1).sharedMaterial = signal;
                var receiver = Find<Transform>(settings, "farSwitch");
                var bridge = Find<Transform>(settings, "bridge");
                var sensor = Find<Renderer>(settings, "switchLight");
                sensor.enabled = false;
                Transform head = SocketSkin(root, receiver.position, receiverRadius, idle, out var receiverLamp);
                Vector3 direction = MagnetRules.Planar(bridge.position - receiver.position).normalized;
                Vector3 start = receiver.position + direction * (receiverRadius - .14f) + Vector3.up * .036f;
                Vector3 end = start + direction * 1.26f;
                Beam(root, "Receiver conduit", start - Vector3.up * .017f, end - Vector3.up * .017f,
                    .10f, .022f, recess);
                var presenter = level.gameObject.AddComponent<FacilityPreviewPresentation>();
                var display = new SerializedObject(presenter);
                Put(display, "level", level);
                Put(display, "receiverSensor", sensor);
                Put(display, "receiverHead", head);
                Put(display, "receiverLamp", receiverLamp);
                var pulse = MakeRingPlane(root, "Receiver shock ring");
                pulse.transform.position = receiver.position + Vector3.up * .11f;
                Put(display, "receiverPulse", pulse);
                Put(display, "bridgeLamp", DressBridge(bridge, root));
                Put(display, "bridgeSurface", bridge.GetComponent<Collider>());
                Put(display, "powered", live);
                Put(display, "unpowered", idle);
                Put(display, "signal", MakeSignal(root));
                display.FindProperty("signalStart").vector3Value = start;
                display.FindProperty("signalEnd").vector3Value = end;
                display.FindProperty("sector").stringValue = sector;
                display.FindProperty("title").stringValue = title;
                display.FindProperty("englishTitle").stringValue = englishTitle;
                display.FindProperty("goal").stringValue = objective;
                display.FindProperty("englishGoal").stringValue = englishObjective;
                display.FindProperty("routeOpen").stringValue = open;
                display.FindProperty("englishRouteOpen").stringValue = englishOpen;
                display.ApplyModifiedPropertiesWithoutUndo();
                BindCoreMaterials(settings, "northShell", "southShell", "standbyLight", "readyLight");
                Put(settings, "artPreview", presenter);
                settings.ApplyModifiedPropertiesWithoutUndo();
            }

            private void BindCoreMaterials(SerializedObject settings, string northKey, string southKey,
                string idleKey = "idle", string liveKey = "live")
            {
                Put(settings, northKey, north);
                Put(settings, southKey, south);
                Put(settings, idleKey, idle);
                Put(settings, liveKey, live);
            }

            private void InstallPad(SerializedObject settings, string location, string lamp, Transform root, float size)
            {
                Find<Renderer>(settings, lamp).enabled = false;
                SocketSkin(root, Find<Transform>(settings, location).position, size, idle, out var replacement);
                Put(settings, lamp, replacement);
            }

            private void ReplaceLampAt(SerializedObject settings, string property, Transform root, float radius)
            {
                Renderer lamp = Find<Renderer>(settings, property);
                lamp.enabled = false;
                SocketSkin(root, lamp.transform.position - Vector3.up * .051f, radius, idle, out var replacement);
                Put(settings, property, replacement);
            }

            private void SetupRoomCamera(Camera view, Transform root, Vector3 target, Vector3 offset, float size)
            {
                view.orthographicSize = size;
                view.transform.position = target + offset;
                view.transform.LookAt(target);
                LightScene(view, root, false);
            }

            private void AddTreads(Transform deck, Transform root, bool followDeck)
            {
                Vector3 size = deck.localScale;
                Vector3 normal = deck.up;
                Vector3 upper = deck.position + normal * (size.y / 2 + .009f);
                int count = Mathf.Clamp(Mathf.RoundToInt(size.x / .31f), 3, 20);
                float step = size.x / count;
                for (int i = 0; i < count; i++)
                {
                    float x = (i + .5f) * step - size.x / 2;
                    var plank = Box("Segmented steel tread", upper + deck.right * x,
                        new Vector3(Mathf.Max(.04f, step - .018f), .026f, size.z - .23f),
                        i % 4 == 0 ? top : lightTop, root, false);
                    plank.rotation = deck.rotation;
                    if (followDeck) plank.SetParent(deck, true);
                }
            }

            private void FrameDeck(Transform deck, Transform root, Material finish)
            {
                Refinish(deck, finish, .045f);
                Vector3 size = deck.localScale;
                Vector3 at = deck.position;
                for (int side = -1; side <= 1; side += 2)
                {
                    var casing = Box("Bolted edge rail", at + deck.forward * (side * (size.z * .5f + .027f)),
                        new Vector3(size.x * .88f, size.y * .84f, .075f), edge, root, false);
                    casing.rotation = deck.rotation;
                    casing.SetParent(deck, true);
                }
            }
        }
    }
}
