using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        [SerializeField] private MassTransferStation massTransfer;
        [SerializeField] private RailRelay railRelay;

        private const float TransferPadRadius = .66f;

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
        private int proofFalls;

        private void Awake()
        {
            bridgeRaised = bridge.position;
            bridgeCollider = bridge.GetComponent<Collider>();
            if (gate != null) gateClosed = gate.position;
            cameraHome = view.transform.position;
            for (int i = 0; i < 2; i++) spawn[i] = bodies[i].position;
            if (railRelay != null) railRelay.Initialize();
            ResetLevel(false);
            string[] args = Environment.GetCommandLineArgs();
            int option = Array.IndexOf(args, "-facilityProof");
            if (option >= 0 && option + 1 < args.Length)
            {
                ResetLevel(true);
                proof = true;
                string prefix = args[option + 1];
                if (LevelNavigation.CampaignProof)
                {
                    string scene = SceneManager.GetActiveScene().name;
                    prefix += "-" + scene;
                    Debug.Log("CAMPAIGN ENTER " + scene);
                }
                if (railRelay != null) StartCoroutine(ProveRailRelay(prefix));
                else StartCoroutine(massTransfer != null ? ProveMassTransfer(prefix) : Prove(prefix));
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
                bodies[i].linearVelocity = Vector3.zero;
                bodies[i].angularVelocity = Vector3.zero;
                bodies[i].isKinematic = !play;
                trails[i].Clear();
            }
            bridge.position = bridgeRaised + Vector3.down * 3.4f;
            bridgeCollider.enabled = false;
            switchLight.sharedMaterial = standbyLight;
            if (gate != null)
            {
                gate.position = gateClosed;
                gateLight.sharedMaterial = socketLightLeft.sharedMaterial = socketLightRight.sharedMaterial = standbyLight;
            }
            if (massTransfer != null) massTransfer.ResetStation(bodies[0], bodies[1]);
            if (railRelay != null) railRelay.ResetRelay();
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
            if (Input.GetKeyDown(KeyCode.Escape) && LevelNavigation.MenuAvailable)
            {
                LevelNavigation.ReturnToMenu();
                return;
            }
            if (!started)
            {
                if (Input.GetKeyDown(KeyCode.Return)) ResetLevel(true);
                return;
            }
            if (Input.GetKeyDown(KeyCode.R)) reset = true;
            if (complete)
            {
                if (!reset && Input.GetKeyDown(KeyCode.Return) && LevelNavigation.MenuAvailable)
                    LevelNavigation.Advance();
                return;
            }
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
                    if (proof)
                    {
                        proofFalls++;
                        if (massTransfer == null) Debug.LogError("PROOF FELL " + i + " at " + bodies[i].position);
                        else Debug.Log($"Mass transfer proof fall: ball {i} at {bodies[i].position}, transferred={massTransfer.Transferred}");
                    }
                    ResetLevel(true);
                    return;
                }
                onGround[i] = Physics.Raycast(bodies[i].position + Vector3.up * .03f, Vector3.down,
                    MagnetRules.Radius + .15f, 1 << 0, QueryTriggerInteraction.Ignore);
            }

            Vector3 separation = MagnetRules.Planar(bodies[1].position - bodies[0].position);
            float distance = separation.magnitude;
            Vector3 line = distance > .001f ? separation / distance : Vector3.right;
            if (anchor != null) UpdateAnchor(dt);
            if (massTransfer != null)
                massTransfer.Step(bodies[0], bodies[1], BallAt(massTransfer.NearPad), BallAt(massTransfer.FarPad), dt);
            if (railRelay != null)
                railRelay.Step(At(0, railRelay.ControlPad, .66f), bridgePowered, dt);
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
                    Vector3 outward = i == 0 ? -line : line;
                    Rigidbody body = bodies[i];
                    Vector3 vertical = Vector3.up * body.linearVelocity.y;
                    if (massTransfer != null)
                    {
                        body.linearVelocity = vertical;
                        float impulse = docked >= 0 ? massTransfer.LaunchImpulse : speed;
                        body.AddForce(outward * impulse, ForceMode.Impulse);
                    }
                    else
                    {
                        body.linearVelocity = vertical + outward * speed;
                    }
                }
            }

            if (!latched && MagnetRules.CanLatch(same, distance,
                Mathf.Abs(bodies[0].position.y - bodies[1].position.y), latchCooldown))
            {
                latched = true;
                sawLatch = true;
                shock = .72f;
                Vector3 shared = docked >= 0 ? Vector3.zero :
                    (MagnetRules.Planar(bodies[0].linearVelocity + bodies[1].linearVelocity) * .27f);
                for (int i = 0; i < 2; i++)
                    if (i != docked) bodies[i].linearVelocity = shared + Vector3.up * bodies[i].linearVelocity.y;
            }
            if (latched && (distance > 1.6f || Mathf.Abs(bodies[0].position.y - bodies[1].position.y) > .7f))
                latched = false;

            float pair = MagnetRules.PairForce(same, distance);
            if (latched)
            {
                float separating = Vector3.Dot(bodies[1].linearVelocity - bodies[0].linearVelocity, line);
                pair = Mathf.Clamp(26 * (distance - .96f) + 6 * separating, -12, 24);
            }
            if (distance > MagnetRules.Range)
                pair += Mathf.Min(90, (distance - MagnetRules.Range) * 44f +
                    Mathf.Max(0, Vector3.Dot(bodies[1].linearVelocity - bodies[0].linearVelocity, line)) * 8);

            for (int i = 0; i < 2; i++)
            {
                if (i == docked) continue;
                Rigidbody body = bodies[i];
                Vector3 toward = i == 0 ? line : -line;
                Vector3 input = MagnetRules.LimitOutwardDrive(drive[i], toward, distance);
                float control = onGround[i] ? 1 : .12f;
                if (launchControl > 0) control *= .23f;
                Vector3 planar = MagnetRules.Planar(body.linearVelocity);
                Vector3 acceleration = input * (latched ? 11 : 19) * control + toward * pair;
                acceleration -= planar * (launchControl > 0 ? .32f : onGround[i] ? 3.2f : .16f);

                if (rail != null)
                {
                    Vector3 toRail = MagnetRules.Planar(rail.position - body.position);
                    float railDistance = toRail.magnitude;
                    if (railDistance < 3.6f && railDistance > .12f)
                    {
                        float force = 7.2f * Mathf.Pow(1 - railDistance / 3.6f, 1.15f);
                        acceleration += toRail / railDistance * (north[i] ? force : -force);
                    }
                }
                body.AddForce(acceleration, ForceMode.Acceleration);
                if (railRelay != null)
                    body.AddForce(railRelay.ForceAt(body.position, north[i]), ForceMode.Force);
                if (planar.sqrMagnitude > MagnetRules.SpeedLimit * MagnetRules.SpeedLimit)
                    body.linearVelocity = planar.normalized * MagnetRules.SpeedLimit + Vector3.up * body.linearVelocity.y;
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
                body.linearVelocity = Vector3.zero;
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

        private int BallAt(Transform pad)
        {
            if (At(0, pad, TransferPadRadius)) return 0;
            if (At(1, pad, TransferPadRadius)) return 1;
            return -1;
        }

        private void UpdateGates(float dt)
        {
            bool receiverReached = railRelay != null
                ? railRelay.Reversed && At(1, farSwitch, .83f)
                : At(0, farSwitch, .83f) || At(1, farSwitch, .83f);
            if (receiverReached)
            {
                sawLand = true;
                bridgePowered = true;
            }
            if (bridgePowered)
            {
                bridge.position = Vector3.MoveTowards(bridge.position, bridgeRaised, dt * 3.5f);
                bridgeCollider.enabled = bridge.position.y > bridgeRaised.y - .18f;
            }

            switchLight.sharedMaterial = bridgePowered ? readyLight : standbyLight;
            if (gate != null) UpdateTerminal(dt);
            bool exitPowered = gate != null ? gatePowered : bridgePowered;
            float exitRadius = railRelay != null ? railRelay.ExitRadius : 1.68f;
            if (exitPowered && At(0, exit, exitRadius) && At(1, exit, exitRadius))
            {
                complete = true;
                bodies[0].linearVelocity = bodies[1].linearVelocity = Vector3.zero;
                bodies[0].isKinematic = bodies[1].isKinematic = true;
                shock = 1;
                if (proof) Debug.Log("FACILITY COMPLETE");
            }
        }

        private void UpdateTerminal(float dt)
        {
            bool leftA = At(0, socketLeft, .63f), leftB = At(1, socketLeft, .63f);
            bool rightA = At(0, socketRight, .63f), rightB = At(1, socketRight, .63f);
            socketLightLeft.sharedMaterial = leftA || leftB || gatePowered ? readyLight : standbyLight;
            socketLightRight.sharedMaterial = rightA || rightB || gatePowered ? readyLight : standbyLight;
            socketHold = MagnetRules.TwoSockets(leftA, rightA, leftB, rightB) ? socketHold + dt : 0;
            if (socketHold > .35f) gatePowered = true;
            if (gatePowered)
            {
                gate.position = Vector3.MoveTowards(gate.position, gateClosed + Vector3.up * 3.8f, dt * 3.2f);
                gateLight.sharedMaterial = readyLight;
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
                trails[j].emitting = started && !complete && bodies[j].linearVelocity.sqrMagnitude > 21;
            }
        }

        private GUIStyle text;
        private void OnGUI()
        {
            if (proof) return;
            text ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            if (complete && LevelNavigation.MenuAvailable)
            {
                DrawCompletionMenu();
                return;
            }
            if (Event.current.type != EventType.Repaint) return;
            if (!started)
            {
                Tint(new Rect(0, 0, Screen.width, Screen.height), new Color(.02f,.04f,.075f,.62f));
                float y = Screen.height * .32f;
                Caption(y, 48, "POLARITY", Color.white);
                Caption(y + 71, 23, "N + N   ↔       S + S   ↔       N + S   →←", Gold);
                Caption(y + 117, 17, "SAME POLES REPEL   /   OPPOSITE POLES ATTRACT", Color.white);
                Caption(y + 167, 16, "WASD + Q       /       ARROWS + RIGHT SHIFT       /       TAB", Color.white);
                Caption(y + 214, 16, LevelNavigation.MenuAvailable ? "ENTER START   /   ESC LEVEL SELECT" : "ENTER", Gold);
            }
            else if (complete)
            {
                Tint(new Rect(0, 0, Screen.width, Screen.height), new Color(.13f,.59f,1,.10f));
                Caption(Screen.height - 63, 19, "R   ↻", Gold);
            }
        }

        private void DrawCompletionMenu()
        {
            Tint(new Rect(0, 0, Screen.width, Screen.height), new Color(.02f,.04f,.075f,.72f));
            float y = Screen.height * .28f;
            float width = Mathf.Min(368, Screen.width * .85f);
            float x = (Screen.width - width) / 2;
            Caption(y, 40, "LEVEL COMPLETE", Color.white);
            string next = LevelNavigation.NextLabel;
            if (next != null && GUI.Button(new Rect(x, y + 72, width, 42), "Next: " + next + "  [Enter]"))
            {
                LevelNavigation.Advance();
                return;
            }
            float menuY = y + (next != null ? 123 : 72);
            string keys = next != null ? "Esc" : "Enter / Esc";
            if (GUI.Button(new Rect(x, menuY, width, 42), "Level Select  [" + keys + "]"))
            {
                LevelNavigation.ReturnToMenu();
                return;
            }
            if (GUI.Button(new Rect(x, menuY + 51, width, 42), "Retry  [R]"))
                reset = true;
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

        private IEnumerator ProveRailRelay(string prefix)
        {
            yield return new WaitForSecondsRealtime(.7f);
            yield return Capture(prefix + "-start.png");
            Vector3 pad = railRelay.ControlPad.position;
            Vector3 waiting = pad + Vector3.left * 1.4f;
            Vector3 blocked = new Vector3(-.25f, 0, 1.72f);
            int falls = proofFalls;
            bool initialPole = north[1];

            yield return Steer(waiting, new Vector3(-4.8f, 0, 2.65f),
                () => proofFalls != falls || bodies[1].position.z > 2.40f, 5);
            yield return Steer(waiting, new Vector3(-2.25f, 0, 2.35f),
                () => proofFalls != falls || bodies[1].position.x > -2.65f, 7);
            yield return Steer(waiting, blocked,
                () => proofFalls != falls || bodies[1].position.x > -2.10f, 8);
            yield return Steer(waiting, blocked, () => proofFalls != falls, .7f);
            Vector3 before = bodies[1].position;
            float pull = railRelay.ForceAt(before, initialPole).z;
            if (proofFalls != falls || railRelay.Reversed || bridgePowered
                || before.x <= -3.60f || before.x >= -1.35f || before.z <= 1.45f || pull >= 0)
            {
                Fail("unpowered corner holds B at the rail");
                yield break;
            }
            yield return Capture(prefix + "-blocked.png");

            yield return Steer(pad, before, () => railRelay.Reversed || proofFalls != falls, 7, steerB: false);
            float push = railRelay.ForceAt(before, initialPole).z;
            yield return Steer(pad, before, () => bodies[1].position.z > 2.9f || proofFalls != falls, 5, steerB: false);
            if (proofFalls != falls || !railRelay.Reversed || push <= 0 || bodies[1].position.z <= 2.9f
                || north[1] != initialPole)
            {
                Fail("reversed rail deflects B without movement input or a pole change");
                yield break;
            }
            Debug.Log($"PASS A holds pad; B keeps its pole and moves sideways without input; rail Z force {pull:F1} to {push:F1} N");
            yield return Capture(prefix + "-deflection.png");

            Vector3 receiver = farSwitch.position;
            yield return Steer(pad, receiver, () => bridgePowered || proofFalls != falls, 12);
            if (proofFalls != falls || !bridgePowered || north[1] != initialPole)
            {
                Fail("B passes corner and powers A's bridge");
                yield break;
            }
            Debug.Log("PASS B reaches the receiver with its original pole and powers the bridge");
            yield return Capture(prefix + "-receiver.png");
            yield return new WaitForSeconds(1.05f);

            Vector3 aTurn = new Vector3(5.0f, 0, -1.88f);
            Vector3 bTurn = new Vector3(5.05f, 0, 2.65f);
            yield return Steer(aTurn, bTurn, () => bodies[0].position.x > 4.8f || proofFalls != falls, 14);
            if (proofFalls != falls || !bridgePowered || !railRelay.Reversed || bodies[0].position.x <= 4.8f)
            {
                Fail("A leaves the pad and crosses its bridge");
                yield break;
            }
            yield return Steer(exit.position + Vector3.back * .60f, exit.position + Vector3.forward * .60f,
                () => complete || proofFalls != falls, 10);
            if (!complete) { Fail("rail relay dual exit"); yield break; }
            yield return Capture(prefix + "-exit.png");
            if (AdvanceCampaign()) yield break;

            reset = true;
            yield return new WaitForFixedUpdate();
            if (complete || bridgePowered || railRelay.Reversed)
            {
                Fail("restart restores rail polarity and bridge");
                yield break;
            }
            Debug.Log("PASS rail relay traversal: blocked corner, pad, magnetic deflection, receiver, A bridge, dual exit, restart");
            Application.Quit(0);
        }

        private IEnumerator ProveMassTransfer(string prefix)
        {
            yield return new WaitForSecondsRealtime(.7f);
            yield return Capture(prefix + "-start.png");
            Vector3 approach = anchor.position + Vector3.left * 1.45f;
            Vector3 waitingAtPad = massTransfer.FarPad.position;
            yield return DockForProof(approach, waitingAtPad);
            if (!latched || docked != 0 || !Mathf.Approximately(bodies[1].mass, massTransfer.HeavyMass))
            {
                Fail("heavy ball at launch position");
                yield break;
            }

            Vector3 heavyOrigin = bodies[1].position;
            yield return Capture(prefix + "-heavy-ready.png");
            int falls = proofFalls;
            flip[1] = true;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            float heavySpeed = MagnetRules.Planar(bodies[1].linearVelocity).magnitude;
            yield return Steer(anchor.position, farSwitch.position, () => proofFalls > falls || bridgePowered, 5);
            if (proofFalls == falls || bridgePowered || massTransfer.Transferred
                || !Mathf.Approximately(bodies[1].mass, massTransfer.HeavyMass))
            {
                Fail("heavy ball falls and resets");
                yield break;
            }
            Debug.Log($"PASS heavy ball falls without powering bridge; initial speed {heavySpeed:F2} m/s");

            Vector3 near = massTransfer.NearPad.position, far = massTransfer.FarPad.position;
            Vector3 waiting = far + Vector3.right * 1.7f;
            yield return Steer(near, waiting, () => BallAt(massTransfer.NearPad) == 0, 6);
            yield return Steer(near, waiting, () => false, .7f);
            if (BallAt(massTransfer.NearPad) != 0 || massTransfer.Transferred)
            {
                Fail("one pad does not transfer ballast");
                yield break;
            }
            yield return Steer(near, far, () => massTransfer.Transferred, 8);
            if (!massTransfer.Transferred || !Mathf.Approximately(bodies[0].mass, massTransfer.CombinedMass)
                || !Mathf.Approximately(bodies[1].mass, massTransfer.LightMass))
            {
                Fail("two pads transfer ballast from B to A");
                yield break;
            }
            Debug.Log($"PASS two pads transfer ballast; A={bodies[0].mass:F1} kg, B={bodies[1].mass:F1} kg");
            yield return Capture(prefix + "-transfer.png");

            yield return DockForProof(approach, waitingAtPad);
            if (!latched || docked != 0 || !massTransfer.Transferred
                || !Mathf.Approximately(bodies[1].mass, massTransfer.LightMass)
                || Vector3.Distance(heavyOrigin, bodies[1].position) > .14f)
            {
                Fail("light ball at same launch position");
                yield break;
            }
            yield return Capture(prefix + "-light-ready.png");
            falls = proofFalls;
            flip[1] = true;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            float lightSpeed = MagnetRules.Planar(bodies[1].linearVelocity).magnitude;
            yield return Steer(anchor.position, farSwitch.position, () => bridgePowered || proofFalls > falls, 7);
            if (!bridgePowered || proofFalls != falls || lightSpeed <= heavySpeed)
            {
                Fail("light ball reaches receiver");
                yield break;
            }
            Debug.Log($"PASS same {massTransfer.LaunchImpulse:F1} N s impulse; light ball lands at receiver; initial speed {lightSpeed:F2} m/s");
            yield return Capture(prefix + "-landing.png");
            yield return new WaitForSeconds(1.05f);

            yield return Steer(exit.position + Vector3.back * .68f, exit.position + Vector3.forward * .68f,
                () => complete, 12);
            if (!complete) { Fail("both balls reach the exit"); yield break; }
            yield return Capture(prefix + "-exit.png");
            if (AdvanceCampaign()) yield break;

            reset = true;
            yield return new WaitForFixedUpdate();
            if (complete || bridgePowered || massTransfer.Transferred
                || !Mathf.Approximately(bodies[0].mass, massTransfer.HeavyMass)
                || !Mathf.Approximately(bodies[1].mass, massTransfer.HeavyMass))
            {
                Fail("restart restores mass and bridge");
                yield break;
            }
            Debug.Log("PASS mass transfer traversal: heavy fall, dual pads, light landing, bridge, dual exit, restart");
            Application.Quit(0);
        }

        private IEnumerator DockForProof(Vector3 approach, Vector3 waiting)
        {
            Vector3 a = anchor.position;
            int falls = proofFalls;
            yield return Steer(approach, waiting,
                () => proofFalls != falls || docked == 0 || MagnetRules.Distance(bodies[0].position, approach) < .25f, 8);
            if (proofFalls != falls) yield break;
            yield return Steer(a, waiting, () => proofFalls != falls || docked == 0, 8);
            if (docked != 0) yield break;

            Vector3 bypass = a + new Vector3(.85f, 0, 1.65f);
            yield return Steer(a, bypass,
                () => proofFalls != falls || (MagnetRules.Distance(bodies[1].position, bypass) < .24f
                    && MagnetRules.Planar(bodies[1].linearVelocity).magnitude < 1f), 8);
            if (proofFalls != falls) yield break;
            yield return Steer(a, a + Vector3.right * .96f,
                () => proofFalls != falls || (latched && Mathf.Abs(bodies[1].position.z - a.z) < .07f), 8);
        }

        // Drives the same FixedUpdate path as live input for repeatable classroom verification.
        private IEnumerator Prove(string prefix)
        {
            yield return new WaitForSecondsRealtime(.7f);
            yield return Capture(prefix + "-start.png");
            Vector3 a = anchor.position;
            Vector3 waiting = new Vector3(-8.8f, 0, 2.05f);
            yield return Steer(spawn[0], waiting,
                () => MagnetRules.Distance(bodies[1].position, waiting) < .4f, 6);
            yield return DockForProof(a + new Vector3(-1.75f, 0, -.95f), waiting);
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
            if (!AdvanceCampaign()) Application.Quit(0);
        }

        private bool AdvanceCampaign()
        {
            if (!LevelNavigation.CampaignProof) return false;
            Debug.Log("PASS campaign completion " + SceneManager.GetActiveScene().name);
            LevelNavigation.Advance();
            return true;
        }

        private IEnumerator Steer(Vector3 a, Vector3 b, Func<bool> done, float seconds, bool steerB = true)
        {
            float stop = Time.time + seconds;
            while (!done() && Time.time < stop)
            {
                drive[0] = Vector3.ClampMagnitude(MagnetRules.Planar(a - bodies[0].position) * 1.8f, 1);
                drive[1] = steerB ? Vector3.ClampMagnitude(MagnetRules.Planar(b - bodies[1].position) * 1.8f, 1) : Vector3.zero;
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
