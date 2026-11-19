using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PolarityFacility
{
    public sealed class MagneticClimbDemo : MonoBehaviour
    {
        public const float SurfaceOffset = .52f;
        public const float CornerRadius = 1f, CornerBase = 3f;
        public const float CornerX = SurfaceOffset + CornerRadius;
        public const float CeilingHeight = CornerBase + CornerRadius + SurfaceOffset;
        public const float CeilingEnd = 4.55f;
        public const float ReceiverStart = 5.10f, ReceiverHeight = 1.10f;
        private const float WallStart = .50f;
        private const float WallLength = CornerBase - WallStart;
        private const float ArcLength = CornerRadius * Mathf.PI * .5f;
        private const float RouteLength = WallLength + ArcLength + CeilingEnd - CornerX;
        private const float TravelSpeed = 2.65f;

        [SerializeField] private Rigidbody bodyA, bodyB;
        [SerializeField] private Renderer shellA, shellB, receiverLamp, exitLamp;
        [SerializeField] private Material north, south, idle, live;
        [SerializeField] private Collider receiverDeck, exitDeck;
        [SerializeField] private Transform bridge;
        [SerializeField] private TrailRenderer trailA, trailB;
        [SerializeField] private LineRenderer tether;

        private enum Stage { Ready, Running, Fell, Complete }
        private Stage stage;
        private Vector3 spawnA, spawnB, bridgeHome;
        private Font font;
        private GUIStyle text;
        private float driveA, driveB, routeProgress, detachUntil;
        private float receiverHold, exitHold, lastRepulsion;
        private bool northA, northB, attached, bridgePowered, proof;
        private bool flipA, flipB, reachedWall, reachedCeiling;

        private void Awake()
        {
            spawnA = bodyA.position;
            spawnB = bodyB.position;
            bridgeHome = bridge.position;
            font = LevelNavigation.FindChineseFont();
            ResetRun(false);
        }

        private void Start()
        {
            string[] args = Environment.GetCommandLineArgs();
            int option = Array.IndexOf(args, "-facilityProof");
            if (option < 0 || option + 1 >= args.Length) return;
            proof = true;
            string prefix = args[option + 1];
            if (LevelNavigation.DemoTourProof)
            {
                prefix += "-" + SceneManager.GetActiveScene().name;
                Debug.Log("DEMO ENTER " + SceneManager.GetActiveScene().name);
            }
            StartCoroutine(Prove(prefix));
        }

        private void OnDestroy()
        {
            if (font != null) Destroy(font);
        }

        private void ResetRun(bool play)
        {
            stage = play ? Stage.Running : Stage.Ready;
            driveA = driveB = routeProgress = receiverHold = exitHold = lastRepulsion = detachUntil = 0;
            northA = true;
            northB = false;
            flipA = flipB = attached = bridgePowered = reachedWall = reachedCeiling = false;
            ResetBody(bodyA, spawnA, play);
            ResetBody(bodyB, spawnB, play);
            bridge.position = bridgeHome + Vector3.down * 3.6f;
            bridge.GetComponent<Collider>().enabled = false;
            receiverLamp.sharedMaterial = exitLamp.sharedMaterial = idle;
            trailA.Clear();
            trailB.Clear();
            ApplyPoles();
        }

        private static void ResetBody(Rigidbody body, Vector3 spawn, bool play)
        {
            body.isKinematic = false;
            body.position = spawn;
            body.rotation = Quaternion.identity;
            body.linearVelocity = body.angularVelocity = Vector3.zero;
            body.isKinematic = !play;
        }

        private void ApplyPoles()
        {
            shellA.sharedMaterial = northA ? north : south;
            shellB.sharedMaterial = northB ? north : south;
        }

        private void Update()
        {
            if (proof) return;
            if (Input.GetKeyDown(KeyCode.Escape) && LevelNavigation.MenuAvailable)
            {
                LevelNavigation.ReturnToDemoList();
                return;
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetRun(true);
                return;
            }
            if (Input.GetKeyDown(KeyCode.Return))
            {
                if (stage == Stage.Complete && LevelNavigation.MenuAvailable) LevelNavigation.ReturnToDemoList();
                else if (stage != Stage.Running) ResetRun(true);
            }
            if (stage != Stage.Running) return;
            driveA = Axis(KeyCode.D, KeyCode.A);
            driveB = Mathf.Clamp(Axis(KeyCode.RightArrow, KeyCode.LeftArrow) +
                Axis(KeyCode.UpArrow, KeyCode.DownArrow), -1, 1);
            flipA |= Input.GetKeyDown(KeyCode.Q);
            flipB |= Input.GetKeyDown(KeyCode.RightShift) || Input.GetKeyDown(KeyCode.Space);
        }

        private static int Axis(KeyCode plus, KeyCode minus) =>
            (Input.GetKey(plus) ? 1 : 0) - (Input.GetKey(minus) ? 1 : 0);

        private void FixedUpdate()
        {
            if (stage != Stage.Running) return;
            if (bodyA.position.y < -3.2f || bodyB.position.y < -3.2f)
            {
                StopAt(Stage.Fell);
                return;
            }
            if (flipA) { northA = !northA; flipA = false; }
            if (flipB)
            {
                northB = !northB;
                flipB = false;
                if (attached)
                {
                    attached = false;
                    detachUntil = Time.fixedTime + .45f;
                    SurfaceAt(routeProgress, out _, out _, out Vector3 releaseNormal);
                    bodyB.AddForce(releaseNormal * 2.5f, ForceMode.VelocityChange);
                }
            }
            ApplyPoles();
            DriveOnGround(bodyA, driveA);
            StepMagnet();
            UpdateReceiver();
        }

        private static void DriveOnGround(Rigidbody body, float input)
        {
            bool grounded = Physics.Raycast(body.position, Vector3.down, MagnetRules.Radius + .12f,
                1, QueryTriggerInteraction.Ignore);
            float acceleration = grounded ? (input * 2.8f - body.linearVelocity.x) * 12f : input * 1.7f;
            body.AddForce(Vector3.right * acceleration, ForceMode.Acceleration);
        }

        private void StepMagnet()
        {
            routeProgress = ClosestProgress(bodyB.position);
            SurfaceAt(routeProgress, out Vector3 target, out Vector3 tangent, out Vector3 normal);
            Vector3 offset = bodyB.position - target;
            float distance = offset.magnitude;
            if (attached && distance > .60f) attached = false;
            if (!northB && !attached && distance < .26f && Time.fixedTime >= detachUntil) attached = true;
            lastRepulsion = 0;

            if (attached)
            {
                float normalError = Vector3.Dot(target - bodyB.position, normal);
                float normalSpeed = Vector3.Dot(bodyB.linearVelocity, normal);
                float desiredSpeed = driveB * TravelSpeed;
                if (driveB > 0) desiredSpeed = Mathf.Min(desiredSpeed, (RouteLength - routeProgress) * 10f);
                if (driveB < 0) desiredSpeed = Mathf.Max(desiredSpeed, -routeProgress * 10f);
                float acceleration = (desiredSpeed - Vector3.Dot(bodyB.linearVelocity, tangent)) * 15f;
                Vector3 magnet = normal * (normalError * 150f - normalSpeed * 22f);
                // Magnetic adhesion carries the weight while the motor drives along the surface.
                bodyB.AddForce(magnet + tangent * acceleration - Physics.gravity, ForceMode.Acceleration);
                reachedWall |= routeProgress > .8f;
                reachedCeiling |= routeProgress > WallLength + ArcLength + .35f;
                return;
            }

            if (distance < .85f && northB)
            {
                lastRepulsion = 25f * (1 - distance / .85f);
                bodyB.AddForce(normal * lastRepulsion, ForceMode.Acceleration);
            }
            DriveOnGround(bodyB, driveB);
        }

        private static float ClosestProgress(Vector3 position)
        {
            float vertical = Mathf.Clamp(position.y, WallStart, CornerBase);
            Vector2 relative = new Vector2(CornerX - position.x, position.y - CornerBase);
            float angle = Mathf.Clamp(Mathf.Atan2(relative.y, relative.x), 0, Mathf.PI * .5f);
            float horizontal = Mathf.Clamp(position.x, CornerX, CeilingEnd);
            float[] candidates = { vertical - WallStart, WallLength + angle * CornerRadius,
                WallLength + ArcLength + horizontal - CornerX };
            float best = 0, distance = float.PositiveInfinity;
            foreach (float candidate in candidates)
            {
                SurfaceAt(candidate, out Vector3 point, out _, out _);
                float square = new Vector2(point.x - position.x, point.y - position.y).sqrMagnitude;
                if (square >= distance) continue;
                distance = square;
                best = candidate;
            }
            return best;
        }

        private static void SurfaceAt(float progress, out Vector3 point, out Vector3 tangent, out Vector3 normal)
        {
            if (progress <= WallLength)
            {
                point = new Vector3(SurfaceOffset, WallStart + progress, .48f);
                tangent = Vector3.up;
                normal = Vector3.right;
            }
            else if (progress < WallLength + ArcLength)
            {
                float angle = (progress - WallLength) / CornerRadius;
                point = new Vector3(CornerX - Mathf.Cos(angle) * CornerRadius,
                    CornerBase + Mathf.Sin(angle) * CornerRadius, .48f);
                tangent = new Vector3(Mathf.Sin(angle), Mathf.Cos(angle), 0);
                normal = new Vector3(Mathf.Cos(angle), -Mathf.Sin(angle), 0);
            }
            else
            {
                point = new Vector3(CornerX + progress - WallLength - ArcLength,
                    CornerBase + CornerRadius, .48f);
                tangent = Vector3.right;
                normal = Vector3.down;
            }
        }

        private void UpdateReceiver()
        {
            receiverHold = OnDeck(bodyB, receiverDeck) ? receiverHold + Time.fixedDeltaTime : 0;
            if (receiverHold > .10f) bridgePowered = true;
            if (bridgePowered)
            {
                bridge.position = Vector3.MoveTowards(bridge.position, bridgeHome, Time.fixedDeltaTime * 3.8f);
                bridge.GetComponent<Collider>().enabled = bridge.position.y >= bridgeHome.y - .07f;
                receiverLamp.sharedMaterial = live;
            }
            bool bothAtExit = bridgePowered && OnDeck(bodyA, exitDeck) && OnDeck(bodyB, exitDeck);
            exitHold = bothAtExit ? exitHold + Time.fixedDeltaTime : 0;
            if (exitHold < .12f) return;
            exitLamp.sharedMaterial = live;
            StopAt(Stage.Complete);
        }

        private static bool OnDeck(Rigidbody body, Collider deck) =>
            Mathf.Abs(body.linearVelocity.y) < .7f &&
            deck.Raycast(new Ray(body.position, Vector3.down), out _, MagnetRules.Radius + .12f);

        private void StopAt(Stage next)
        {
            stage = next;
            bodyA.linearVelocity = bodyA.angularVelocity = bodyB.linearVelocity = bodyB.angularVelocity = Vector3.zero;
            bodyA.isKinematic = bodyB.isKinematic = true;
        }

        private void LateUpdate()
        {
            tether.SetPosition(0, bodyA.transform.position + Vector3.up * .12f);
            tether.SetPosition(1, bodyB.transform.position + Vector3.up * .12f);
            tether.startColor = new Color(1, .4f, .25f, .22f);
            tether.endColor = new Color(.3f, .75f, 1, .22f);
            trailA.emitting = stage == Stage.Running && bodyA.linearVelocity.sqrMagnitude > 2;
            trailB.emitting = stage == Stage.Running && bodyB.linearVelocity.sqrMagnitude > 2;
            trailA.startColor = northA ? new Color(1, .3f, .25f) : new Color(.25f, .7f, 1);
            trailB.startColor = northB ? new Color(1, .3f, .25f) : new Color(.25f, .7f, 1);
        }

        private void OnGUI()
        {
            text ??= new GUIStyle(GUI.skin.label) { font = font != null ? font : GUI.skin.font };
            float scale = Mathf.Min(Screen.width / 1120f, Screen.height / 630f);
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1120 * scale) * .5f,
                (Screen.height - 630 * scale) * .5f, 0), Quaternion.identity, Vector3.one * scale);
            LevelNavigation.Fill(new Rect(42, 24, 1036, 83), new Color32(12, 24, 39, 239));
            Label(new Rect(64, 32, 670, 44), 31, Local("磁吸攀壁", "Magnetic climb"), Color.white);
            Label(new Rect(64, 78, 940, 24), 16,
                Local("两球抵达出口 · 墙面与顶棚均为 N 极", "Bring both balls to the exit · The wall and ceiling are N"),
                new Color(.67f, .77f, .87f));
            LevelNavigation.Fill(new Rect(42, 518, 1036, 91), new Color32(12, 24, 39, 239));
            string status = Local("让 B 球到对岸接收台，为 A 球升起通路。", "Get B to the receiver to raise A's bridge.");
            if (attached) status = Local("B 球已吸附 · ↑ / → 前进，↓ / ← 后退", "B attached · Up / Right forward, Down / Left back");
            if (bridgePowered) status = Local("接收台已启动 · 两球前往右侧出口", "Receiver online · Bring both balls to the exit");
            if (stage == Stage.Fell) status = Local("球已坠落 · Enter / R 重试", "A ball fell · Enter / R Retry");
            if (stage == Stage.Complete) status = Local("演示完成 · Enter / Esc 返回演示列表", "Demo complete · Enter / Esc Demo list");
            if (stage == Stage.Ready) status = Local("Enter 开始 · 让两球抵达右侧出口", "Enter Start · Bring both balls to the right-hand exit");
            Label(new Rect(64, 527, 990, 33), 22, status, new Color(.45f, .86f, 1));
            Label(new Rect(64, 572, 990, 25), 15,
                Local("A / D 移动 A · 方向键移动 B · 右 Shift / Space 切 B 极性 · R 重试 · Esc 返回",
                    "A / D Move A · Arrows Move B · Right Shift / Space Flip B · R Retry · Esc Back"), Color.white);
            GUI.matrix = previous;
        }

        private string Local(string chinese, string english) => font != null ? chinese : english;

        private void Label(Rect rect, int size, string value, Color color)
        {
            text.fontSize = size;
            text.normal.textColor = color;
            GUI.Label(rect, value, text);
        }

        private IEnumerator Prove(string prefix)
        {
            yield return LevelNavigation.CaptureFrame(prefix + "-ready.png");
            ResetRun(true);
            flipB = true;
            driveB = -1;
            yield return WaitFor(() => lastRepulsion > 2, 3);
            if (attached || lastRepulsion <= 2) { Fail("matching poles must repel at the wall"); yield break; }
            Debug.Log("PASS magnetic climb: matching poles repel and cannot attach");

            ResetRun(true);
            driveB = -1;
            yield return WaitFor(() => attached, 4);
            if (!attached) { Fail("opposite poles must attach on approach"); yield break; }
            driveB = 1;
            yield return WaitFor(() => reachedWall && bodyB.position.y > 2f, 4);
            if (!reachedWall || bodyB.position.y < 2) { Fail("climb vertical wall"); yield break; }
            yield return LevelNavigation.CaptureFrame(prefix + "-wall.png");
            yield return WaitFor(() => reachedCeiling, 6);
            if (!attached || !reachedCeiling) { Fail("round the corner onto the ceiling"); yield break; }
            driveB = 0;
            yield return new WaitForSeconds(.65f);
            if (!attached || bodyB.position.y < 3.85f || Mathf.Abs(bodyB.linearVelocity.y) > .25f)
            { Fail("hold on the ceiling against gravity"); yield break; }
            Debug.Log("PASS magnetic climb: wall, rounded corner and stationary ceiling adhesion");
            yield return LevelNavigation.CaptureFrame(prefix + "-ceiling.png");

            driveB = 1;
            yield return WaitFor(() => bodyB.position.x > 4.20f, 4);
            flipB = true;
            yield return WaitFor(() => !attached && bodyB.linearVelocity.y < -1f, 2);
            if (attached || !northB) { Fail("flip must release the ceiling"); yield break; }
            yield return LevelNavigation.CaptureFrame(prefix + "-release.png");
            driveB = .75f;
            yield return WaitFor(() => bridgePowered || stage != Stage.Running, 4);
            if (!bridgePowered) { Fail("land on receiver and power bridge"); yield break; }
            Debug.Log("PASS magnetic climb: ceiling flip releases B and landing powers the bridge");
            yield return LevelNavigation.CaptureFrame(prefix + "-receiver.png");
            yield return WaitFor(() => bridge.GetComponent<Collider>().enabled, 2);
            driveA = driveB = 1;
            yield return WaitFor(() => stage != Stage.Running, 9);
            if (stage != Stage.Complete) { Fail("both balls cross to exit"); yield break; }
            yield return LevelNavigation.CaptureFrame(prefix + "-complete.png");
            ResetRun(true);
            if (attached || bridgePowered || northB || (bodyB.position - spawnB).sqrMagnitude > .001f ||
                bridge.GetComponent<Collider>().enabled)
            { Fail("retry restores polarity, position and bridge"); yield break; }
            Debug.Log("PASS magnetic climb: both balls exit; retry restores the scene");
            if (LevelNavigation.DemoTourProof)
            {
                Debug.Log("PASS demo completion " + SceneManager.GetActiveScene().name);
                LevelNavigation.ReturnToDemoList();
            }
            else Application.Quit(0);
        }

        private static IEnumerator WaitFor(Func<bool> condition, float seconds)
        {
            float until = Time.fixedTime + seconds;
            while (!condition() && Time.fixedTime < until) yield return new WaitForFixedUpdate();
        }

        private void Fail(string message)
        {
            Debug.LogError($"FAIL magnetic climb: {message}; B={bodyB.position}, v={bodyB.linearVelocity}, " +
                $"A={bodyA.position}, attached={attached}, path={routeProgress:F2}, bridge={bridgePowered}");
            Application.Quit(1);
        }
    }
}
