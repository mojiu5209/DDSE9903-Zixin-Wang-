using UnityEngine;

namespace OccaSoftware.Altos
{
    [CreateAssetMenu(fileName = "Moon Definition", menuName = "Altos/Moon Definition")]
    public class MoonDefinitionScriptableObject : ScriptableObject
    {
        public float size = 2f;
        public float earthRotationSpeed = 1f;
        public float moonRotationSpeed = -0.1f;
        [Range(0f,1f)]
        public float phaseOffset = 0f;
        [ColorUsage(false, true)]
        public Color moonAlbedo = Color.white;
        [Range(0f,1f)]
        public float horizonFadeDistance = 0.1f;
        [Range(0f, 1f)]
        public float phaseAxisRotation = 0f;
        public float phasePassthroughSpeed = 0.1f;
        [Range(0f,1f)]
        public float atmosphereBlend = 0.99f;
        public Texture2D albedoMap = null;
        public Texture2D normalMap = null;
        public float normalStrength = 0.2f;
    }

}