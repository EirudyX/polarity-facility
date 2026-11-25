using UnityEditor;
using UnityEngine;

namespace PolarityFacility
{
    internal sealed class FacilityInterfaceTextures : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Art/Resources/FoundryInterface/", System.StringComparison.Ordinal)) return;
            var settings = (TextureImporter)assetImporter;
            settings.npotScale = TextureImporterNPOTScale.None;
            settings.mipmapEnabled = false;
            settings.wrapMode = TextureWrapMode.Clamp;
            settings.textureCompression = TextureImporterCompression.CompressedHQ;
        }
    }
}
