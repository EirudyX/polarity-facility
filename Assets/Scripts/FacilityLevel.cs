using System;
using System.Collections;
using UnityEngine;

namespace PolarityFacility
{
    public sealed class FacilityLevel : MonoBehaviour
    {
        [SerializeField] private Rigidbody[] bodies;
        [SerializeField] private Renderer[] shells;
        [SerializeField] private LineRenderer[] halos;
        [SerializeField] private TrailRenderer[] trails;
        [SerializeField] private Material northShell, southShell;
        [SerializeField] private LineRenderer field;
        [SerializeField] private Transform anchor, farSwitch, rail, socketLeft, socketRight, exit;
        [SerializeField] private Transform bridge, gate;
        [SerializeField] private Renderer switchLight, socketLightLeft, socketLightRight, gateLight;
        [SerializeField] private Material standbyLight, readyLight;
        [SerializeField] private Camera view;

        private static readonly Color North = new Color(1f, .24f, .23f);
        private static readonly Color South = new Color(.18f, .68f, 1f);
        private static readonly Color Gold = new Color(1f, .77f, .3f);
        private readonly bool[] north = { true, false };
        private readonly Vector3[] drive = new Vector3[2];
        private readonly Vector3[] spawn = new Vector3[2];
        private readonly Vector3[] curve = new Vector3[37];
        private readonly Vector3[] ring = new Vector3[49];
        private readonly bool[] onGround = new bool[2];
        private readonly bool[] flip = new bool[2];

        private Vector3 bridgeRaised, gateClosed, cameraHome;
        private Collider bridgeCollider;
        private int primary, docked = -1;
        private float releaseHold, latchCooldown, launchControl, socketHold, shock, noDockUntil, elapsed;
        private bool latched, bridgePowered, gatePowered, complete, started, proof;
        private bool sawLatch, sawLaunch, sawLand, sawBothFlips;
        private bool reset;

        private void Awake()
        {
            bridgeRaised = bridge.position;
            bridgeCollider = bridge.GetComponent<Collider>();
            gateClosed = gate.position;
            cameraHome = view.transform.position;
            for (int i = 0; i < 2; i++) spawn[i] = bodies[i].position;
            ResetLevel(false);
            int option = Array.IndexOf(Environment.GetCommandLineArgs(), "-facilityProof");
            if (option >= 0 && option + 1 < Environment.GetCommandLineArgs().Length)
            {
                ResetLevel(true);
                proof = true;
                StartCoroutine(Prove(Environment.GetCommandLineArgs()[option + 1]));
            }
        }

        private void ResetLevel(bool play)
        {
            started = play;
            latched = bridgePowered = gatePowered = complete = false;
            sawLatch = sawLaunch = sawLand = sawBothFlips = false;
            releaseHold = latchCooldown = launchControl = socketHold = shock = noDockUntil = elapsed = 0;
            primary = 0;
            docked = -1;
            north[0] = true;
            north[1] = false;
            for (int i = 0; i < 2; i++)
            {
                drive[i] = Vector3.zero;
                flip[i] = false;
                bodies[i].isKinematic = false;
                bodies[i].constraints = RigidbodyConstraints.None;
                bodies[i].position = spawn[i];
                bodies[i].rotation = Quaternion.identity;
                bodies[i].velocity = Vector3.zero;
                bodies[i].angularVelocity = Vector3.zero;
                bodies[i].isKinematic = !play;
                trails[i].Clear();
            }
            bridge.position = bridgeRaised + Vector3.down * 3.4f;
            bridgeCollider.enabled = false;
            gate.position = gateClosed;
            gateLight.sharedMaterial = switchLight.sharedMaterial = socketLightLeft.sharedMaterial = socketLightRight.sharedMaterial = standbyLight;
            view.transform.position = cameraHome;
            ApplyPoles();
        }

        private void ApplyPoles()
        {
            for (int i = 0; i < 2; i++)
            {
                shells[i].sharedMaterial = north[i] ? northShell : southShell;
                trails[i].startColor = north[i] ? North : South;
                trails[i].endColor = new Color(trails[i].startColor.r, trails[i].startColor.g, trails[i].startColor.b, 0);
            }
        }

        private void Update()
        {
            if (proof) return;
            if (!started)
            {
                if (Input.GetKeyDown(KeyCode.Return)) ResetLevel(true);
                return;
            }
            if (Input.GetKeyDown(KeyCode.R)) reset = true;
            if (complete) return;
            if (Input.GetKeyDown(KeyCode.Tab)) primary = 1 - primary;
            drive[primary] = Vector3.ClampMagnitude(new Vector3(
                Axis(KeyCode.D, KeyCode.A), 0, Axis(KeyCode.W, KeyCode.S)), 1);
            drive[1 - primary] = Vector3.ClampMagnitude(new Vector3(
                Axis(KeyCode.RightArrow, KeyCode.LeftArrow), 0, Axis(KeyCode.UpArrow, KeyCode.DownArrow)), 1);
            flip[primary] |= Input.GetKeyDown(KeyCode.Q);
            flip[1 - primary] |= Input.GetKeyDown(KeyCode.RightShift);
        }

        private static int Axis(KeyCode plus, KeyCode minus) =>
            (Input.GetKey(plus) ? 1 : 0) - (Input.GetKey(minus) ? 1 : 0);

        private void FixedUpdate()
        {
            if (reset) { reset = false; ResetLevel(true); return; }
            if (!started || complete) return;
            float dt = Time.fixedDeltaTime;
            elapsed += dt;
            latchCooldown -= dt;
            launchControl -= dt;
            shock = Mathf.Max(0, shock - dt * 1.7f);
            for (int i = 0; i < 2; i++)
            {
                if (bodies[i].position.y < -4.8f)
                {
                    if (proof) Debug.LogError("PROOF FELL " + i + " at " + bodies[i].position);
                    ResetLevel(true);
                    return;
                }
                onGround[i] = Physics.Raycast(bodies[i].position + Vector3.up * .03f, Vector3.down,
                    MagnetRules.Radius + .15f, 1 << 0, QueryTriggerInteraction.Ignore);
            }

            Vector3 separation = MagnetRules.Planar(bodies[1].position - bodies[0].position);
            float distance = separation.magnitude;
            Vector3 line = distance > .001f ? separation / distance : Vector3.right;
            UpdateAnchor(dt);
            bool changed = flip[0] || flip[1];
            if (changed)
            {
                if (flip[0]) north[0] = !north[0];
                if (flip[1]) north[1] = !north[1];
                sawBothFlips |= flip[0] && flip[1];
                flip[0] = flip[1] = false;
                ApplyPoles();
                shock = 1;
            }

            bool same = north[0] == north[1];
            if (MagnetRules.IsRelease(latched, same))
            {
                latched = false;
                sawLaunch = true;
                latchCooldown = .55f;
                launchControl = .68f;
                for (int i = 0; i < 2; i++)
                {
                    if (i == docked) continue;
                    float speed = docked >= 0 ? MagnetRules.LaunchSpeed : 5.2f;
                    Vector3 impulse = line * (i == 0 ? -speed : speed);
                    bodies[i].velocity = Vector3.up * bodies[i].velocity.y + impulse;
                }
            }

            if (!latched && MagnetRules.CanLatch(same, distance,
                Mathf.Abs(bodies[0].position.y - bodies[1].position.y), latchCooldown))
            {
                latched = true;
                sawLatch = true;
                shock = .72f;
                Vector3 shared = docked >= 0 ? Vector3.zero :
                    (MagnetRules.Planar(bodies[0].velocity + bodies[1].velocity) * .27f);
                for (int i = 0; i < 2; i++)
                    if (i != docked) bodies[i].velocity = shared + Vector3.up * bodies[i].velocity.y;
            }
            if (latched && (distance > 1.6f || Mathf.Abs(bodies[0].position.y - bodies[1].position.y) > .7f))
                latched = false;

            float pair = MagnetRules.PairForce(same, distance);
            if (latched)
            {
                float separating = Vector3.Dot(bodies[1].velocity - bodies[0].velocity, line);
                pair = Mathf.Clamp(26 * (distance - .96f) + 6 * separating, -12, 24);
            }
            if (distance > MagnetRules.Range)
                pair += Mathf.Min(90, (distance - MagnetRules.Range) * 44f +
                    Mathf.Max(0, Vector3.Dot(bodies[1].velocity - bodies[0].velocity, line)) * 8);

            for (int i = 0; i < 2; i++)
            {
                if (i == docked) continue;
                Rigidbody body = bodies[i];
                Vector3 toward = i == 0 ? line : -line;
                Vector3 input = MagnetRules.LimitOutwardDrive(drive[i], toward, distance);
                float control = onGround[i] ? 1 : .12f;
                if (launchControl > 0) control *= .23f;
                Vector3 planar = MagnetRules.Planar(body.velocity);
                Vector3 acceleration = input * (latched ? 11 : 19) * control + toward * pair;
                acceleration -= planar * (launchControl > 0 ? .32f : onGround[i] ? 3.2f : .16f);

                Vector3 toRail = MagnetRules.Planar(rail.position - body.position);
                float railDistance = toRail.magnitude;
                if (railDistance < 3.6f && railDistance > .12f)
                {
                    float force = 7.2f * Mathf.Pow(1 - railDistance / 3.6f, 1.15f);
                    acceleration += toRail / railDistance * (north[i] ? force : -force);
                }
                body.AddForce(acceleration, ForceMode.Acceleration);
                if (planar.sqrMagnitude > MagnetRules.SpeedLimit * MagnetRules.SpeedLimit)
                    body.velocity = planar.normalized * MagnetRules.SpeedLimit + Vector3.up * body.velocity.y;
            }
            UpdateGates(dt);
        }

        private void UpdateAnchor(float dt)
        {
            if (docked >= 0)
            {
                releaseHold = drive[docked].x < -.65f || bridgePowered && drive[docked].x > .65f
                    ? releaseHold + dt : 0;
                if (releaseHold < .28f) return;
                bodies[docked].isKinematic = false;
                docked = -1;
                noDockUntil = elapsed + 1.1f;
                releaseHold = 0;
                return;
            }
            if (bridgePowered || elapsed < noDockUntil) return;
            for (int i = 0; i < 2; i++)
            {
                if (!At(i, anchor, .43f)) continue;
                docked = i;
                Rigidbody body = bodies[i];
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.isKinematic = true;
                body.position = anchor.position + Vector3.up * MagnetRules.Radius;
                shock = .65f;
                break;
            }
        }

        private bool At(int i, Transform marker, float radius) =>
            onGround[i] && Mathf.Abs(bodies[i].position.y - marker.position.y) < .72f
            && MagnetRules.Distance(bodies[i].position, marker.position) < radius;

        private void UpdateGates(float dt)
        {
            if (At(0, farSwitch, .83f) || At(1, farSwitch, .83f))
            {
                sawLand = true;
                bridgePowered = true;
            }
            if (bridgePowered)
            {
                bridge.position = Vector3.MoveTowards(bridge.position, bridgeRaised, dt * 3.5f);
                bridgeCollider.enabled = bridge.position.y > bridgeRaised.y - .18f;
            }

            bool leftA = At(0, socketLeft, .63f), leftB = At(1, socketLeft, .63f);
            bool rightA = At(0, socketRight, .63f), rightB = At(1, socketRight, .63f);
            switchLight.sharedMaterial = bridgePowered ? readyLight : standbyLight;
            socketLightLeft.sharedMaterial = leftA || leftB || gatePowered ? readyLight : standbyLight;
            socketLightRight.sharedMaterial = rightA || rightB || gatePowered ? readyLight : standbyLight;
            socketHold = MagnetRules.TwoSockets(leftA, rightA, leftB, rightB) ? socketHold + dt : 0;
            if (socketHold > .35f) gatePowered = true;
            if (gatePowered)
            {
                gate.position = Vector3.MoveTowards(gate.position, gateClosed + Vector3.up * 3.8f, dt * 3.2f);
                gateLight.sharedMaterial = readyLight;
            }
            if (gatePowered && At(0, exit, 1.68f) && At(1, exit, 1.68f))
            {
                complete = true;
                bodies[0].velocity = bodies[1].velocity = Vector3.zero;
                bodies[0].isKinematic = bodies[1].isKinematic = true;
                shock = 1;
                if (proof) Debug.Log("FACILITY COMPLETE");
            }
        }

        private void LateUpdate()
        {
            Vector3 a = bodies[0].transform.position, b = bodies[1].transform.position;
            Vector3 midpoint = (a + b) * .5f;
            Vector3 follow = cameraHome + Vector3.right * Mathf.Clamp(midpoint.x * .30f, -2.2f, 3.5f);
            view.transform.position = Vector3.Lerp(view.transform.position, follow, 1 - Mathf.Exp(-4f * Time.deltaTime));
            Vector3 tangent = Vector3.Cross((b - a).normalized, Vector3.up);
            Color ca = north[0] ? North : South, cb = north[1] ? North : South;
            for (int i = 0; i < curve.Length; i++)
            {
                float t = i / (curve.Length - 1f);
                float wave = Mathf.Sin(t * 19 - Time.time * (north[0] == north[1] ? -7 : 8));
                curve[i] = Vector3.Lerp(a, b, t) + Vector3.up * (.3f + .32f * Mathf.Sin(t * Mathf.PI))
                    + tangent * (wave * Mathf.Sin(t * Mathf.PI) * (.12f + shock * .23f));
            }
            field.SetPositions(curve);
            float alpha = latched ? .67f : Mathf.Lerp(.16f, .43f, shock);
            field.startColor = new Color(ca.r, ca.g, ca.b, alpha);
            field.endColor = new Color(cb.r, cb.g, cb.b, alpha);
            field.widthMultiplier = .038f + shock * .026f;
            for (int j = 0; j < 2; j++)
            {
                Vector3 at = bodies[j].transform.position;
                float radius = (j == docked ? .78f : .61f) + shock * .12f;
                for (int i = 0; i < ring.Length; i++)
                {
                    float radians = i * Mathf.PI * 2 / (ring.Length - 1);
                    ring[i] = at + new Vector3(Mathf.Cos(radians) * radius, -.38f, Mathf.Sin(radians) * radius);
                }
                halos[j].SetPositions(ring);
                Color color = j == docked ? Gold : north[j] ? North : South;
                halos[j].startColor = halos[j].endColor = color;
                trails[j].emitting = started && !complete && bodies[j].velocity.sqrMagnitude > 21;
            }
        }

        private GUIStyle text;
        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || proof) return;
            text ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            if (!started)
            {
                Tint(new Rect(0, 0, Screen.width, Screen.height), new Color(.02f,.04f,.075f,.62f));
                float y = Screen.height * .32f;
                Caption(y, 48, "POLARITY", Color.white);
                Caption(y + 71, 23, "N + N   ↔       S + S   ↔       N + S   →←", Gold);
                Caption(y + 117, 17, "SAME POLES REPEL   /   OPPOSITE POLES ATTRACT", Color.white);
                Caption(y + 167, 16, "WASD + Q       /       ARROWS + RIGHT SHIFT       /       TAB", Color.white);
                Caption(y + 214, 16, "ENTER", Gold);
            }
            else if (complete)
            {
                Tint(new Rect(0, 0, Screen.width, Screen.height), new Color(.13f,.59f,1,.10f));
                Caption(Screen.height - 63, 19, "R   ↻", Gold);
            }
        }

        private void Caption(float y, int size, string value, Color color)
        {
            text.normal.textColor = color;
            text.fontSize = Mathf.RoundToInt(size * Mathf.Clamp(Screen.height / 720f, .69f, 1.5f));
            GUI.Label(new Rect(0, y, Screen.width, 62), value, text);
        }

        private static void Tint(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        // Drives the same FixedUpdate path as live input for repeatable classroom verification.
        private IEnumerator Prove(string prefix)
        {
            yield return new WaitForSecondsRealtime(.7f);
            yield return Capture(prefix + "-start.png");
            Vector3 a = anchor.position;
            Vector3 bypass = a + new Vector3(.99f, 0, 1.65f);
            yield return Steer(a, new Vector3(-7.6f, 0, 1.6f), () => docked == 0, 8);
            if (docked != 0) { Fail("anchor pocket"); yield break; }
            yield return Steer(a, bypass,
                () => bodies[1].position.x > a.x + .82f && bodies[1].position.z > 1.15f, 8);
            yield return Steer(a, a + Vector3.right * .87f, () => docked == 0 && latched, 8);
            if (!sawLatch || docked != 0 || !latched) { Fail("anchor and contact latch"); yield break; }
            yield return Capture(prefix + "-latch.png");
            drive[0] = drive[1] = Vector3.zero;
            flip[1] = true;
            yield return Steer(a, farSwitch.position, () => bridgePowered, 9);
            if (!sawLaunch || !sawLand || !bridgePowered) { Fail("release across gap"); yield break; }
            yield return Capture(prefix + "-bridge.png");
            yield return new WaitForSeconds(1.05f);
            yield return Steer(new Vector3(2.9f,0,1), new Vector3(4.35f,0,2.2f),
                () => bodies[0].position.x > 2.5f && bodies[1].position.x > 3.8f, 10);
            flip[0] = flip[1] = true;
            yield return Steer(new Vector3(6.2f,0,2.1f), new Vector3(7.8f,0,2.55f),
                () => bodies[0].position.x > 5.7f && bodies[1].position.x > 7.45f, 10);
            yield return Steer(socketLeft.position, socketRight.position, () => gatePowered, 13);
            if (!gatePowered || !sawBothFlips) { Fail("dual sockets"); yield break; }
            yield return Capture(prefix + "-sockets.png");
            yield return new WaitForSeconds(1.25f);
            yield return Steer(exit.position + Vector3.back * .71f, exit.position + Vector3.forward * .71f,
                () => complete, 11);
            if (!complete) { Fail("dual exit"); yield break; }
            yield return Capture(prefix + "-exit.png");
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-facilityFrameCheck") >= 0)
                yield return CaptureCameraSweep(prefix);
            Debug.Log("PASS facility traversal: anchor, latch, launch, landing, bridge, dual flip, two sockets, exit");
            yield return new WaitForSecondsRealtime(.4f);
            Application.Quit(0);
        }

        private IEnumerator Steer(Vector3 a, Vector3 b, Func<bool> done, float seconds)
        {
            float stop = Time.time + seconds;
            while (!done() && Time.time < stop)
            {
                drive[0] = Vector3.ClampMagnitude(MagnetRules.Planar(a - bodies[0].position) * 1.8f, 1);
                drive[1] = Vector3.ClampMagnitude(MagnetRules.Planar(b - bodies[1].position) * 1.8f, 1);
                yield return null;
            }
            drive[0] = drive[1] = Vector3.zero;
        }

        private IEnumerator Capture(string file)
        {
            yield return new WaitForEndOfFrame();
            CaptureFrame(file);
            yield return new WaitForSecondsRealtime(.27f);
        }

        private IEnumerator CaptureCameraSweep(string prefix)
        {
            Vector3 start = view.transform.position;
            for (int frame = 0; frame < 16; frame++)
            {
                yield return new WaitForEndOfFrame();
                view.transform.position = start + Vector3.right * (-.6f + .08f * frame);
                CaptureFrame(prefix + $"-frame-{frame:00}.png");
            }
            view.transform.position = start;
            Debug.Log("PASS facility camera sweep: 16 frames");
        }

        private void CaptureFrame(string file)
        {
            var target = RenderTexture.GetTemporary(1600, 900, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 4);
            var previous = RenderTexture.active;
            var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            view.targetTexture = target;
            view.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            System.IO.File.WriteAllBytes(file, image.EncodeToPNG());
            view.targetTexture = null;
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            Destroy(image);
        }

        private void Fail(string step)
        {
            Debug.LogError($"FAILED {step} t={elapsed:F2} A={bodies[0].position} B={bodies[1].position} dock={docked} latch={latched}");
            Application.Quit(1);
        }
    }
}