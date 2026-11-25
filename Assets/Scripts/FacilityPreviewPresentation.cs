using UnityEngine;

namespace PolarityFacility
{
    public sealed class FacilityPreviewPresentation : MonoBehaviour
    {
        [SerializeField] private FacilityLevel level;
        [SerializeField] private MassTransferStation station;
        [SerializeField] private string sector = "02", title = "配重试验场", englishTitle = "Ballast chamber";
        [SerializeField] private string goal = "让轻球飞过断口，触发接收台", englishGoal = "Launch the light sphere across to the receiver";
        [SerializeField] private string routeOpen = "升桥已就位 · 两球前往出口", englishRouteOpen = "Bridge ready   /   Bring both spheres to the exit";
        [SerializeField] private Renderer receiverSensor, receiverLamp, receiverPulse, bridgeLamp;
        [SerializeField] private Material powered, unpowered;
        [SerializeField] private Collider bridgeSurface;
        [SerializeField] private Transform receiverHead;
        [SerializeField] private LineRenderer signal;
        [SerializeField] private Vector3 signalStart, signalEnd;

        private static readonly Color Warm = new Color32(246, 182, 82, 255);
        private static readonly Color Cold = new Color32(115, 233, 255, 255);
        private static readonly int Tint = Shader.PropertyToID("_Color");
        private MaterialPropertyBlock ringProperties;
        private Vector3 headHome;
        private bool wasPowered;
        private float triggeredAt;
        private Font font;
        private FacilityHud hud;
        internal Font HudFont => font;

        private void Awake()
        {
            headHome = receiverHead.localPosition;
            ringProperties = new MaterialPropertyBlock();
            font = LevelNavigation.FindChineseFont();
        }

        private void OnDestroy()
        {
            if (font != null) Destroy(font);
        }

        private void LateUpdate()
        {
            bool isPowered = receiverSensor.sharedMaterial == powered;
            if (isPowered && !wasPowered) triggeredAt = Time.time;
            wasPowered = isPowered;
            float age = Time.time - triggeredAt;
            receiverHead.localPosition = Vector3.MoveTowards(receiverHead.localPosition,
                headHome + Vector3.down * (isPowered ? .045f : 0), Time.deltaTime * .36f);
            receiverLamp.sharedMaterial = isPowered ? powered : unpowered;
            bridgeLamp.sharedMaterial = bridgeSurface.enabled ? powered : unpowered;

            receiverPulse.enabled = isPowered && age < .68f;
            if (receiverPulse.enabled)
            {
                float fade = 1 - age / .68f;
                receiverPulse.transform.localScale = Vector3.one * (1.25f + (1 - fade) * 1.95f);
                Color tint = Cold;
                tint.a = fade;
                SetRingTint(receiverPulse, tint);
            }

            signal.enabled = isPowered && age < .55f;
            if (signal.enabled)
            {
                float t = Mathf.SmoothStep(0, 1, age / .55f);
                signal.SetPosition(0, Vector3.Lerp(signalStart, signalEnd, Mathf.Max(0, t - .23f)));
                signal.SetPosition(1, Vector3.Lerp(signalStart, signalEnd, t));
            }
        }

        private void SetRingTint(Renderer ring, Color color)
        {
            ringProperties.SetColor(Tint, color);
            ring.SetPropertyBlock(ringProperties);
        }

        private string Local(string chinese, string english) => font != null ? chinese : english;

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || level.IsComplete && LevelNavigation.MenuAvailable) return;
            hud ??= new FacilityHud(font);
            Matrix4x4 matrix = LevelNavigation.BeginScaledGUI();
            Color state = bridgeSurface.enabled ? Cold : Warm;
            hud.Header(new Rect(34, 28, 275, 73), sector, Local(title, englishTitle), state);

            string status = station != null && !station.Transferred
                ? Local("让两球分别停在配重盘上", "Park one sphere on each ballast pad") : Local(goal, englishGoal);
            if (!level.HasStarted) status = Local("开始试玩", "Start the demonstration");
            else if (level.IsComplete) status = Local("两球已抵达 · 按 R 重试", "Both spheres arrived   /   R to retry");
            else if (bridgeSurface.enabled) status = Local(routeOpen, englishRouteOpen);
            else if (wasPowered) status = Local("接收台已通电 · 升桥启动", "Receiver online   /   Bridge rising");

            string controls = Local("A  WASD / Q     B  方向键 / 右 Shift     R  重试",
                "A  WASD / Q     B  Arrows / Right Shift     R  Retry");
            if (LevelNavigation.MenuAvailable) controls += Local("     Esc  返回", "     Esc  Back");
            hud.Status(new Rect(34, 540, 565, 63), status, controls, state, !level.HasStarted ? "Enter" : null);
            GUI.matrix = matrix;
        }
    }
}
