using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PolarityFacility
{
    public sealed class PumpSurgeDemo : MonoBehaviour
    {
        public const float WallFace = 1.03f;
        private const float WallContact = WallFace + MagnetRules.Radius;
        public const float MeterPosition = 2.74f;
        public const float RampStart = 2.89f, RampLip = 5.22f, RampHeight = .55f;
        public const float LandingStart = 5.90f, LandingHeight = -.76f;
        private const float TrackDamping = .08f;

        [SerializeField] private Transform anchor;
        [SerializeField] private Rigidbody runner;
        [SerializeField] private Renderer shell, meterStrip, wallLamp, receiverLamp;
        [SerializeField] private Collider landingDeck;
        [SerializeField] private Material north, south, idle, live;
        [SerializeField] private LineRenderer field;
        [SerializeField] private TrailRenderer trail;

        private enum Stage { Ready, Running, Fell, Complete }

        private static readonly Color Cyan = new Color32(107, 221, 255, 255);
        private static readonly Color North = new Color32(255, 89, 91, 225);
        private static readonly Color South = new Color32(94, 181, 255, 225);

        private Stage stage;
        private Vector3 spawn;
        private Font font;
        private FacilityHud hud;
        private readonly FacilityCompletionMenu completionMenu = new FacilityCompletionMenu();
        private bool same, pendingFlip, proof;
        private float lastDistance, speed, baseline, best;
        private float incomingWallSpeed, outgoingWallSpeed, wallFlashUntil;
        private int passes;

        private float Distance => runner.position.x - anchor.position.x;

        private void Awake()
        {
            spawn = runner.position;
            font = LevelNavigation.FindChineseFont();
            ResetRun(Stage.Ready);
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
                string scene = SceneManager.GetActiveScene().name;
                Debug.Log("DEMO ENTER " + scene);
                prefix += "-" + scene;
            }
            StartCoroutine(Prove(prefix));
        }

        private void OnDestroy()
        {
            if (font != null) Destroy(font);
        }

        private void ResetRun(Stage next)
        {
            completionMenu.Reset();
            stage = next;
            same = pendingFlip = false;
            passes = 0;
            speed = baseline = best = 0;
            incomingWallSpeed = outgoingWallSpeed = wallFlashUntil = 0;
            runner.isKinematic = false;
            runner.position = spawn;
            runner.rotation = Quaternion.identity;
            runner.linearVelocity = runner.angularVelocity = Vector3.zero;
            runner.isKinematic = stage != Stage.Running;
            lastDistance = Distance;
            shell.sharedMaterial = south;
            trail.Clear();
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
                ResetRun(stage == Stage.Ready ? Stage.Ready : Stage.Running);
                return;
            }
            if (Input.GetKeyDown(KeyCode.Return))
            {
                if (stage == Stage.Ready || stage == Stage.Fell) ResetRun(Stage.Running);
                else if (stage == Stage.Complete && LevelNavigation.MenuAvailable)
                    LevelNavigation.ReturnToDemoList();
            }
            if (stage == Stage.Running)
                pendingFlip |= Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.RightShift);
        }

        private void FixedUpdate()
        {
            if (stage != Stage.Running) return;
            float distance = Distance;
            speed = runner.linearVelocity.x;
            if (runner.position.y < -2.4f)
            {
                StopAt(Stage.Fell);
                return;
            }
            if (distance >= LandingStart && Mathf.Abs(runner.linearVelocity.y) < .65f &&
                landingDeck.Raycast(new Ray(runner.position, Vector3.down), out _, MagnetRules.Radius + .07f))
            {
                StopAt(Stage.Complete);
                return;
            }
            if (distance <= WallContact && speed < 0)
            {
                ReflectAtWall(distance);
                distance = Distance;
            }
            if (pendingFlip)
            {
                pendingFlip = false;
                same = !same;
                shell.sharedMaterial = same ? north : south;
            }

            if (lastDistance < MeterPosition && distance >= MeterPosition && speed > 0)
            {
                if (++passes == 1) baseline = speed;
                best = Mathf.Max(best, speed);
            }
            lastDistance = distance;
            // The instrumented lane has lower drag than the regular platforms.
            float force = -MagnetRules.PairForce(same, distance) - MagnetRules.TetherForce(distance, speed);
            runner.AddForce(Vector3.right * (force - TrackDamping * speed), ForceMode.Acceleration);
            if (Mathf.Abs(speed) > MagnetRules.SpeedLimit)
            {
                Vector3 velocity = runner.linearVelocity;
                velocity.x = Mathf.Clamp(speed, -MagnetRules.SpeedLimit, MagnetRules.SpeedLimit);
                runner.linearVelocity = velocity;
            }
        }

        private void StopAt(Stage next)
        {
            stage = next;
            runner.linearVelocity = runner.angularVelocity = Vector3.zero;
            runner.isKinematic = true;
        }

        private void ReflectAtWall(float distance)
        {
            // Mirror the step's overshoot and reflect the lane velocity without damping.
            Vector3 position = runner.position;
            position.x += 2f * (WallContact - distance);
            runner.position = position;
            Vector3 incoming = runner.linearVelocity;
            Vector3 outgoing = incoming;
            outgoing.x = -outgoing.x;
            runner.linearVelocity = outgoing;

            // The track uses an ideal rolling rebound. Reverse the roll along with travel.
            Vector3 spin = runner.angularVelocity;
            runner.angularVelocity = new Vector3(spin.x, spin.y, -spin.z);
            incomingWallSpeed = incoming.x;
            outgoingWallSpeed = runner.linearVelocity.x;
            speed = outgoingWallSpeed;
            wallFlashUntil = Time.time + .22f;

            if (proof)
            {
                if ((runner.linearVelocity - outgoing).sqrMagnitude > .000001f ||
                    Mathf.Abs(runner.angularVelocity.sqrMagnitude - spin.sqrMagnitude) > .0001f)
                    Fail("the wall must reflect travel and preserve spin speed");
                else Debug.Log($"PASS elastic wall: {incomingWallSpeed:F2} → +{outgoingWallSpeed:F2} m/s; spin speed preserved");
            }
        }

        private void LateUpdate()
        {
            meterStrip.sharedMaterial = passes > 0 ? live : idle;
            wallLamp.sharedMaterial = Time.time < wallFlashUntil ? live : idle;
            receiverLamp.sharedMaterial = stage == Stage.Complete ? live : idle;
            Vector3 a = anchor.position + Vector3.up * .12f;
            Vector3 b = runner.transform.position + Vector3.up * .12f;
            field.SetPosition(0, a);
            field.SetPosition(1, Vector3.Lerp(a, b, .33f) + Vector3.up * .13f);
            field.SetPosition(2, Vector3.Lerp(a, b, .67f) + Vector3.up * .13f);
            field.SetPosition(3, b);
            field.startColor = North;
            field.endColor = same ? North : South;
            trail.emitting = stage == Stage.Running && Mathf.Abs(speed) > .65f;
            bool flying = Distance > RampLip && runner.position.y > LandingHeight + MagnetRules.Radius + .12f;
            trail.widthMultiplier = flying ? .20f : .11f;
            trail.startColor = flying ? Cyan : same ? North : South;
            Color fading = trail.startColor;
            fading.a = 0;
            trail.endColor = fading;
        }

        private void OnGUI()
        {
            if (stage == Stage.Complete && LevelNavigation.MenuAvailable)
            {
                var action = completionMenu.Draw(font, $"{baseline:F2} → {best:F2} m/s");
                if (!proof && action == FacilityCompletionMenu.Action.Return) LevelNavigation.ReturnToDemoList();
                if (!proof && action == FacilityCompletionMenu.Action.Retry) ResetRun(Stage.Running);
                return;
            }
            if (Event.current.type != EventType.Repaint) return;
            hud ??= new FacilityHud(font);
            Matrix4x4 previous = LevelNavigation.BeginScaledGUI();
            hud.Header(new Rect(34, 28, 267, 73), "04", Local("泵涌", "Magnetic pumping"), FacilityConsole.Cyan);
            FacilityConsole.Panel(new Rect(319, 28, 767, 73));
            FacilityConsole.Trace(334, 30, 52, FacilityConsole.Cyan);
            for (int i = 0; i < 3; i++)
                FacilityConsole.Fill(new Rect(493 + i * 165, 44, 1, 41), FacilityConsole.Line);
            hud.Metric(new Rect(336, 38, 150, 52), Local("首次过线", "First pass"), SpeedText(baseline, passes > 0));
            hud.Metric(new Rect(509, 38, 146, 52), Local("最快过线", "Best pass"), SpeedText(best, passes > 0));
            hud.Metric(new Rect(674, 38, 145, 52), Local("当前球速", "Ball speed"), SpeedText(Mathf.Abs(speed), stage == Stage.Running));
            hud.Metric(new Rect(839, 38, 231, 52), Local("撞墙前 → 撞墙后", "Wall in → out"),
                outgoingWallSpeed > 0 ? $"{incomingWallSpeed:F2} → +{outgoingWallSpeed:F2} m/s" : "—");
            Color state = stage == Stage.Fell ? FacilityConsole.Red
                : stage == Stage.Ready ? FacilityConsole.Amber : FacilityConsole.Cyan;
            hud.Status(new Rect(34, 540, 620, 63), Prompt(), Controls(), state,
                stage == Stage.Ready || stage == Stage.Fell ? "Enter" : null);
            GUI.matrix = previous;
        }

        private string Prompt()
        {
            switch (stage)
            {
                case Stage.Ready:
                    return Local("开始泵涌试验", "Start the rebound test");
                case Stage.Fell:
                    return Local("B 球落入断口。", "B fell into the gap.");
                case Stage.Complete:
                    return Local("跃过断口 · 接收台已启动", "Gap cleared · Receiver online");
                default:
                    return Local("让 B 球越过断口，落到对岸接收台。", "Get B across the gap and onto the receiver.");
            }
        }

        private string Controls()
        {
            if (stage == Stage.Ready)
                return Local("Enter 开始   ·   Space / 右 Shift 切极   ·   Esc 返回", "Enter Start   ·   Space / Right Shift Flip   ·   Esc Back");
            if (stage == Stage.Complete)
                return Local("Enter / Esc 返回演示列表   ·   R 再试一次", "Enter / Esc Demo list   ·   R Retry");
            if (stage == Stage.Fell)
                return Local("Enter / R 重试   ·   Esc 返回", "Enter / R Retry   ·   Esc Back");
            return Local("Space / 右 Shift 切极   ·   R 重开   ·   Esc 返回", "Space / Right Shift Flip   ·   R Retry   ·   Esc Back");
        }

        private string Local(string chinese, string english) => font != null ? chinese : english;

        private static string SpeedText(float value, bool hasValue) => hasValue ? $"{value:F2} m/s" : "—";

        private IEnumerator Prove(string prefix)
        {
            yield return LevelNavigation.CaptureFrame(prefix + "-ready.png");
            ResetRun(Stage.Running);
            yield return WaitFor(() => outgoingWallSpeed > 0, 6);
            if (incomingWallSpeed >= 0 || outgoingWallSpeed <= 0 ||
                Mathf.Abs(incomingWallSpeed + outgoingWallSpeed) > .0001f || same || passes != 0)
            {
                Fail("an unflipped ball must reverse at the wall at the same speed");
                yield break;
            }

            // Flip immediately after the first rebound to give the single-launch run its best chance.
            pendingFlip = true;
            yield return WaitFor(() => stage != Stage.Running || (Distance >= RampStart && speed < -.12f), 8);
            if (stage != Stage.Running || Distance < RampStart || Distance >= RampLip || speed >= -.12f || passes != 1)
            {
                Fail("a single launch must roll back before reaching the lip");
                yield break;
            }
            Debug.Log($"PASS best single launch: {baseline:F2} m/s at meter; rolls back at x={Distance:F2}, lip={RampLip:F2}");
            yield return LevelNavigation.CaptureFrame(prefix + "-rollback.png");

            ResetRun(Stage.Ready);
            if (outgoingWallSpeed != 0 || passes != 0 ||
                (runner.position - spawn).sqrMagnitude > .0001f || !runner.isKinematic)
            {
                Fail("retry must clear the wall readout and restore the starting position");
                yield break;
            }
            Debug.Log("PASS magnetic pumping: wall rebounds without a flip; retry restores start");

            ResetRun(Stage.Running);
            yield return WaitFor(() => outgoingWallSpeed > 0 && speed > 0 && Distance >= 1.74f, 6);
            pendingFlip = true;
            yield return WaitFor(() => same && speed > 0 && Distance >= 3.72f, 6);
            if (!same || speed <= 0 || Distance < 3.72f || passes != 1)
            {
                Fail("first pass must reach partway up the ramp");
                yield break;
            }
            pendingFlip = true;
            float firstImpact = outgoingWallSpeed;
            // Leave a short reaction interval after the second rebound before flipping.
            yield return WaitFor(() => !same && outgoingWallSpeed > firstImpact && speed > 0 && Distance >= 1.90f, 8);
            if (same || outgoingWallSpeed <= firstImpact || speed <= 0 || Distance < 1.90f)
            {
                Fail("flipping on the slope must draw the ball back to the wall");
                yield break;
            }
            pendingFlip = true;
            yield return WaitFor(() => stage != Stage.Running, 8);
            if (stage != Stage.Complete || passes != 2 || best < baseline * 1.3f)
            {
                Fail("one pumping cycle must gain speed and land the ball across the gap");
                yield break;
            }
            Debug.Log($"PASS one pumping cycle: {baseline:F2} → {best:F2} m/s at meter; lands at x={Distance:F2}");
            yield return LevelNavigation.CaptureFrame(prefix + "-complete.png");
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
            Debug.LogError($"FAIL magnetic pumping: {message}; x={Distance:F2}, v={speed:F2}, " +
                $"y={runner.position.y:F2}, passes={passes}, baseline={baseline:F2}, best={best:F2}");
            Application.Quit(1);
        }
    }
}
