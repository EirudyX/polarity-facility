using UnityEngine;

namespace PolarityFacility
{
    public sealed class FacilityCoreGlow : MonoBehaviour
    {
        [SerializeField] private Renderer shell, groundRing;
        [SerializeField] private Light coreLight;
        [SerializeField] private Material north;

        private static readonly int Tint = Shader.PropertyToID("_Color");
        private MaterialPropertyBlock properties;
        private bool wasNorth;
        private float flippedAt = -10;

        private void Start()
        {
            wasNorth = shell.sharedMaterial == north;
            properties = new MaterialPropertyBlock();
        }

        private void LateUpdate()
        {
            bool isNorth = shell.sharedMaterial == north;
            if (isNorth != wasNorth) flippedAt = Time.time;
            wasNorth = isNorth;
            Color tint = isNorth ? new Color(1, .16f, .24f) : new Color(.13f, .50f, 1);
            coreLight.color = tint;
            float pulse = 1 - Mathf.Clamp01((Time.time - flippedAt) / .24f);
            coreLight.intensity = .74f + pulse * .49f;

            groundRing.enabled = Physics.Raycast(transform.position + Vector3.up * .04f, Vector3.down,
                out RaycastHit ground, .72f, 1, QueryTriggerInteraction.Ignore);
            if (!groundRing.enabled) return;
            groundRing.transform.SetPositionAndRotation(ground.point + ground.normal * .023f,
                Quaternion.FromToRotation(Vector3.back, ground.normal));
            groundRing.transform.localScale = Vector3.one * (1.5f + pulse * .42f);
            tint.a = .73f + pulse * .27f;
            properties.SetColor(Tint, tint);
            groundRing.SetPropertyBlock(properties);
        }
    }
}
