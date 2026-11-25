using UnityEngine;

namespace PolarityFacility
{
    [RequireComponent(typeof(Camera))]
    public sealed class FacilityFoundryAtmosphere : MonoBehaviour
    {
        [SerializeField] private Shader shader;
        private Camera view;
        private Material atmosphere;

        private void Awake()
        {
            view = GetComponent<Camera>();
            view.depthTextureMode |= DepthTextureMode.Depth;
            if (shader != null && shader.isSupported) atmosphere = new Material(shader);
        }

        private void OnDestroy()
        {
            if (atmosphere != null) Destroy(atmosphere);
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (atmosphere == null)
            {
                Graphics.Blit(source, destination);
                return;
            }
            Transform eye = view.transform;
            atmosphere.SetVector("_RayOrigin", eye.position);
            atmosphere.SetVector("_RayDirection", eye.forward);
            atmosphere.SetVector("_RayX", eye.right * (view.orthographicSize * view.aspect));
            atmosphere.SetVector("_RayY", eye.up * view.orthographicSize);
            RenderTexture mist = RenderTexture.GetTemporary(Mathf.Max(1, source.width / 2),
                Mathf.Max(1, source.height / 2), 0, RenderTextureFormat.ARGBHalf);
            int width = Mathf.Max(1, source.width / 4);
            int height = Mathf.Max(1, source.height / 4);
            RenderTexture glow = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGBHalf);
            RenderTexture blur = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGBHalf);
            Graphics.Blit(source, mist, atmosphere, 0);
            Graphics.Blit(source, glow, atmosphere, 1);
            atmosphere.SetVector("_BlurAxis", new Vector2(1.4f, 0));
            Graphics.Blit(glow, blur, atmosphere, 2);
            atmosphere.SetVector("_BlurAxis", new Vector2(0, 1.4f));
            Graphics.Blit(blur, glow, atmosphere, 2);
            atmosphere.SetTexture("_Mist", mist);
            atmosphere.SetTexture("_Glow", glow);
            Graphics.Blit(source, destination, atmosphere, 3);
            RenderTexture.ReleaseTemporary(blur);
            RenderTexture.ReleaseTemporary(glow);
            RenderTexture.ReleaseTemporary(mist);
        }
    }
}
