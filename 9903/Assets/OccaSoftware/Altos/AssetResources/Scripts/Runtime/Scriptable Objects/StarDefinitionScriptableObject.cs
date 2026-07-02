using UnityEngine;

namespace OccaSoftware.Altos
{
    [CreateAssetMenu(fileName = "Star Definition", menuName = "Altos/Star Definition")]

    public class StarDefinitionScriptableObject : ScriptableObject
    {
        private void OnValidate()
        {
            StarObject temp = FindObjectOfType<StarObject>();
            if (temp != null)
                temp.Setup();
        }

        public uint count = 3000;

        public Texture2D texture = null;
        [GradientUsage(true)]
        public Gradient color = new Gradient();
        [Min(0)]
        public float size = 3f;
        [Min(0)]
        public float sizeRange = 3f;
        [Min(0)]
        public float peakBrightness = 2f;
        [Min(0)]
        public Vector2 flickerFrequencyMinMax = new Vector2(3f, 6f);
        
        [Range(0f, 360f)]
        public float textureAngleRandom = 0f;
        [Range(0f, 1f)]
        public float daytimeBrightness = 0f;

        public float rotationSpeed = 1f;
        [Range(0f,1f)]
        public float horizonFadeDistance = 0.2f;
    }

}
