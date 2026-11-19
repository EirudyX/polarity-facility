using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PolarityFacility
{
    public static partial class FacilityBuilder
    {
        private const string PumpScenePath = "Assets/Scenes/PumpSurge.unity";

        [MenuItem("Tools/Polarity Facility/Rebuild Pump Surge Scene")]
        public static void CreatePumpSurgeScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildMaterials();
            var track = new GameObject("Pumping test track").transform;
            detail = new GameObject("Instrumentation").transform;

            Box("Rolling surface", new Vector3(.95f, -.19f, 0), new Vector3(4.27f, .38f, 2.60f), steel, track, true);
            Box("Track foundation", new Vector3(.89f, -.60f, 0), new Vector3(4.05f, .45f, 2.45f), dark, track, false);
            for (int side = -1; side <= 1; side += 2)
            {
                Box("Guide edge", new Vector3(.89f, .09f, side * 1.24f),
                    new Vector3(4.14f, .18f, .13f), dark, track, false);
                Box("Guide accent", new Vector3(.89f, .183f, side * 1.24f),
                    new Vector3(4.14f, .019f, .038f), ready, detail, false);
                for (int meter = 1; meter < 3; meter++)
                    Box("Distance tick", new Vector3(meter, .018f, side * 1.03f),
                        new Vector3(.035f, .036f, .19f), pale, detail, false);
            }
            BuildPumpRamp(track);
            const float receiverEnd = 8.48f;
            const float receiverDepth = .48f;
            float receiverWidth = receiverEnd - PumpSurgeDemo.LandingStart;
            float receiverX = PumpSurgeDemo.LandingStart + receiverWidth * .5f;
            float receiverY = PumpSurgeDemo.LandingHeight;
            Transform receiver = Box("Receiving platform", new Vector3(receiverX, receiverY - receiverDepth * .5f, 0),
                new Vector3(receiverWidth, receiverDepth, 2.60f), steel, track, true);
            Box("Receiver underframe", new Vector3(receiverX, receiverY - .66f, 0),
                new Vector3(receiverWidth - .22f, .37f, 2.43f), dark, track, false);
            Renderer receiverLamp = Socket(new Vector3(receiverX + .07f, receiverY, 0), .73f, idle, "Landing receiver");
            Box("Receiver end stop", new Vector3(receiverEnd - .09f, receiverY + .32f, 0),
                new Vector3(.18f, .64f, 2.60f), dark, track, true);
            Box("Receiver lip marking", new Vector3(PumpSurgeDemo.LandingStart + .10f, receiverY + .019f, 0),
                new Vector3(.17f, .035f, 2.48f), stripe, detail, false);

            Socket(Vector3.zero, .69f, stripe, "Anchor recess");
            Box("Anchor backrest", new Vector3(-.57f, .32f, 0), new Vector3(.18f, .64f, 1.31f), dark, detail, false);
            Renderer wallLamp = BuildReboundWall();
            float meterX = PumpSurgeDemo.MeterPosition;
            Renderer meterStrip = Box("Speed meter line", new Vector3(meterX, .026f, 0),
                new Vector3(.046f, .05f, 2.37f), idle, detail, false).GetComponent<Renderer>();
            for (int side = -1; side <= 1; side += 2)
                Box("Speed meter post", new Vector3(meterX, .33f, 1.12f * side),
                    new Vector3(.11f, .66f, .13f), pale, detail, false);

            Rigidbody a = Ball("Anchored A", new Vector3(0, .48f, 0), red, out _, out var haloA, out var trailA);
            a.isKinematic = true;
            Rigidbody b = Ball("Test ball B", new Vector3(1.98f, .48f, 0), blue, out var shell, out var haloB, out var trail);
            b.constraints = RigidbodyConstraints.FreezePositionZ;
            b.angularDamping = 0;
            Object.DestroyImmediate(haloA.gameObject);
            Object.DestroyImmediate(haloB.gameObject);
            Object.DestroyImmediate(trailA.gameObject);

            Camera view = Lighting(false);
            view.orthographicSize = 4.15f;
            view.transform.position = new Vector3(5.25f, 12.3f, -13.6f);
            view.transform.LookAt(new Vector3(3.65f, -.05f, 0));
            view.backgroundColor = new Color32(20, 33, 51, 255);
            RenderSettings.fogColor = view.backgroundColor;

            var demo = new GameObject("Magnetic pumping").AddComponent<PumpSurgeDemo>();
            var setup = new SerializedObject(demo);
            Put(setup, "anchor", a.transform);
            Put(setup, "runner", b);
            Put(setup, "shell", shell);
            Put(setup, "meterStrip", meterStrip);
            Put(setup, "wallLamp", wallLamp);
            Put(setup, "receiverLamp", receiverLamp);
            Put(setup, "landingDeck", receiver.GetComponent<Collider>());
            Put(setup, "north", red);
            Put(setup, "south", blue);
            Put(setup, "idle", idle);
            Put(setup, "live", ready);
            Put(setup, "field", Line("Pair field", 4, .039f));
            Put(setup, "trail", trail);
            setup.ApplyModifiedPropertiesWithoutUndo();
            SavePlayableScene(scene, PumpScenePath);
        }

        private static Renderer BuildReboundWall()
        {
            // The controller resolves the impact at this face to keep the bounce exactly elastic.
            float center = PumpSurgeDemo.WallFace - .08f;
            Box("Rebound wall", new Vector3(center, .18f, 0), new Vector3(.16f, .36f, 2.46f), steel, detail, false);
            for (int side = -1; side <= 1; side += 2)
                Box("Rebound wall upright", new Vector3(center, .33f, side * 1.10f),
                    new Vector3(.17f, .66f, .20f), pale, detail, false);
            return Box("Rebound wall indicator", new Vector3(center, .38f, 0),
                new Vector3(.16f, .045f, 2.42f), idle, detail, false).GetComponent<Renderer>();
        }

        private static void BuildPumpRamp(Transform track)
        {
            Vector3 start = new Vector3(PumpSurgeDemo.RampStart, 0, 0);
            Vector3 end = new Vector3(PumpSurgeDemo.RampLip, PumpSurgeDemo.RampHeight, 0);
            Vector3 run = end - start;
            Quaternion slope = Quaternion.FromToRotation(Vector3.right, run);
            Vector3 normal = slope * Vector3.up;
            Vector3 center = (start + end) * .5f;
            Transform ramp = Box("Launch ramp", center - normal * .18f,
                new Vector3(run.magnitude, .36f, 2.60f), steel, track, true);
            ramp.rotation = slope;
            Transform lip = Box("Launch edge", end - run.normalized * .09f + normal * .017f,
                new Vector3(.18f, .032f, 2.50f), stripe, detail, false);
            lip.rotation = slope;
            for (int side = -1; side <= 1; side += 2)
            {
                Transform shoulder = Box("Ramp shoulder", center + normal * .046f + Vector3.forward * (1.24f * side),
                    new Vector3(run.magnitude, .09f, .12f), pale, detail, false);
                shoulder.rotation = slope;
            }
        }
    }
}
