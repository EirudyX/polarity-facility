using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PolarityFacility
{
    public static partial class FacilityBuilder
    {
        private const string ClimbScenePath = "Assets/Scenes/MagneticClimb.unity";

        [MenuItem("Tools/Polarity Facility/Rebuild Magnetic Climb Scene")]
        public static void CreateMagneticClimbScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildMaterials();
            geometry = new GameObject("Magnetic chamber").transform;
            detail = new GameObject("Magnetic surface hardware").transform;
            const float depth = 3.1f;
            Box("Starting deck", new Vector3(1.27f, -.23f, 0), new Vector3(3.30f, .46f, depth),
                steel, geometry, true);
            Box("Start underframe", new Vector3(1.27f, -.71f, 0), new Vector3(3.12f, .46f, 2.9f),
                dark, geometry, false);
            Box("Vertical magnetic wall", new Vector3(-.17f, 1.5f, 0), new Vector3(.34f, 3, depth),
                steel, geometry, true);
            Box("N wall pole strip", new Vector3(.014f, 1.5f, -1.45f), new Vector3(.028f, 3, .13f),
                red, detail, false);
            BuildClimbCorner(depth);

            float roofStart = MagneticClimbDemo.CornerX;
            float roofEnd = MagneticClimbDemo.CeilingEnd + .05f;
            float roofY = MagneticClimbDemo.CeilingHeight;
            Box("Ceiling magnetic deck", new Vector3((roofStart + roofEnd) * .5f, roofY + .17f, 0),
                new Vector3(roofEnd - roofStart, .34f, depth), steel, geometry, true);
            Box("N ceiling pole strip", new Vector3((roofStart + roofEnd) * .5f, roofY - .014f, -1.45f),
                new Vector3(roofEnd - roofStart, .028f, .13f), red, detail, false);
            Box("Ceiling edge cap", new Vector3(roofEnd - .07f, roofY + .17f, 0),
                new Vector3(.14f, .34f, depth + .025f), stripe, detail, false);
            for (int i = 0; i < 5; i++)
            {
                float y = .40f + i * .53f;
                Box("Wall induction fin", new Vector3(-.24f, y, -1.61f),
                    new Vector3(.28f, .10f, .16f), dark, detail, false);
            }

            const float receiverEnd = 6.78f;
            const float upperLaneDepth = 1.75f, upperLaneZ = .52f;
            float receiverStart = MagneticClimbDemo.ReceiverStart;
            float receiverY = MagneticClimbDemo.ReceiverHeight;
            Transform receiver = Box("Receiver deck", new Vector3((receiverStart + receiverEnd) * .5f, receiverY - .20f, upperLaneZ),
                new Vector3(receiverEnd - receiverStart, .40f, upperLaneDepth), steel, geometry, true);
            Renderer receiverLamp = Socket(new Vector3(5.91f, receiverY, .48f), .67f, idle, "Upper receiver");
            Box("Receiver lip", new Vector3(receiverStart + .08f, receiverY + .015f, upperLaneZ),
                new Vector3(.16f, .03f, upperLaneDepth), stripe, detail, false);

            Vector3 rampStart = new Vector3(receiverEnd - .12f, receiverY, upperLaneZ);
            Vector3 rampEnd = new Vector3(8.80f, 0, upperLaneZ);
            Vector3 rampRun = rampEnd - rampStart;
            Quaternion slope = Quaternion.FromToRotation(Vector3.right, rampRun);
            Transform ramp = Box("Exit descent", (rampStart + rampEnd) * .5f - slope * Vector3.up * .16f,
                new Vector3(rampRun.magnitude, .32f, upperLaneDepth), steel, geometry, true);
            ramp.rotation = slope;

            Transform bridge = Box("Lower lift bridge", new Vector3(5.77f, -.23f, 0),
                new Vector3(5.92f, .46f, depth), steel, geometry, true);
            LocalBox(bridge, "Bridge guide", new Vector3(0, .515f, -.16f),
                new Vector3(.97f, .028f, .055f), ready);
            Transform exit = Box("Shared exit deck", new Vector3(9.48f, -.23f, 0),
                new Vector3(1.90f, .46f, depth), steel, geometry, true);
            Renderer exitLamp = Socket(new Vector3(9.40f, 0, 0), 1.02f, idle, "Dual exit");
            Box("Exit end stop", new Vector3(10.35f, .50f, 0), new Vector3(.16f, 1, depth),
                dark, geometry, true);
            Box("Exit front guide", new Vector3(9.44f, .065f, -1.48f), new Vector3(1.74f, .13f, .12f),
                ready, detail, false);

            Rigidbody a = Ball("Body A", new Vector3(2.02f, .50f, -.92f), red,
                out Renderer shellA, out LineRenderer haloA, out TrailRenderer trailA);
            Rigidbody b = Ball("Body B", new Vector3(1.01f, .50f, .48f), blue,
                out Renderer shellB, out LineRenderer haloB, out TrailRenderer trailB);
            a.constraints = b.constraints = RigidbodyConstraints.FreezePositionZ;
            Object.DestroyImmediate(haloA.gameObject);
            Object.DestroyImmediate(haloB.gameObject);
            Camera view = Lighting(false);
            view.orthographicSize = 4.30f;
            view.transform.position = new Vector3(10.4f, 6.7f, -22f);
            view.transform.LookAt(new Vector3(4.4f, 2.15f, 0));
            view.backgroundColor = new Color32(20, 33, 51, 255);
            RenderSettings.fogColor = view.backgroundColor;
            RenderSettings.fogDensity = .014f;

            var demo = new GameObject("Magnetic climb simulation").AddComponent<MagneticClimbDemo>();
            var setup = new SerializedObject(demo);
            Put(setup, "bodyA", a); Put(setup, "bodyB", b);
            Put(setup, "shellA", shellA); Put(setup, "shellB", shellB);
            Put(setup, "receiverLamp", receiverLamp); Put(setup, "exitLamp", exitLamp);
            Put(setup, "north", red); Put(setup, "south", blue);
            Put(setup, "idle", idle); Put(setup, "live", ready);
            Put(setup, "receiverDeck", receiver.GetComponent<Collider>());
            Put(setup, "exitDeck", exit.GetComponent<Collider>());
            Put(setup, "bridge", bridge); Put(setup, "trailA", trailA); Put(setup, "trailB", trailB);
            Put(setup, "tether", Line("Pair connection", 2, .022f));
            setup.ApplyModifiedPropertiesWithoutUndo();
            SavePlayableScene(scene, ClimbScenePath);
        }

        private static void BuildClimbCorner(float depth)
        {
            const int segments = 16;
            const float thickness = .30f;
            float radius = MagneticClimbDemo.CornerRadius + MagneticClimbDemo.SurfaceOffset;
            for (int i = 0; i < segments; i++)
            {
                float angle = (i + .5f) / segments * Mathf.PI * .5f;
                Vector3 radial = new Vector3(-Mathf.Cos(angle), Mathf.Sin(angle), 0);
                Vector3 center = new Vector3(MagneticClimbDemo.CornerX, MagneticClimbDemo.CornerBase, 0) +
                    radial * (radius + thickness * .5f);
                float length = 2 * (radius + thickness) * Mathf.Tan(Mathf.PI * .25f / segments);
                Transform block = Box("Rounded magnetic corner", center, new Vector3(thickness, length, depth),
                    steel, geometry, true);
                block.rotation = Quaternion.FromToRotation(Vector3.right, radial);
                Box("N corner pole marker", center - radial * (thickness * .5f + .014f) + Vector3.back * 1.45f,
                    new Vector3(.055f, .075f, .13f), red, detail, false);
            }
        }
    }
}
