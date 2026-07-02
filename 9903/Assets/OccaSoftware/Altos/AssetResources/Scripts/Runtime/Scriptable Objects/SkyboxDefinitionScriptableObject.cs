using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace OccaSoftware.Altos
{
    [CreateAssetMenu(fileName = "Skybox Definition", menuName = "Altos/Skybox Definition")]
    public class SkyboxDefinitionScriptableObject : ScriptableObject
    {
        private void OnValidate()
        {
            // Reorder the Periods of Day list so that they occur sorted by Start Time ascending.
            // We change the input (in the inspector) so that it is easy for the user to see if a change has taken place.
            // We also adjust the Start Time of all of the periods so that they are not too close to the subsequent period of day.
            // This code has some exceptions/use cases when you stack up a bunch of time periods close to each other, I'm not solving for it right now. Just avoid it.
            

            // Clamp Period of Day Start Time
            for (int i = 0; i < periodsOfDay.Count - 1; i++)
            {
                periodsOfDay[i].startTime = Mathf.Clamp(periodsOfDay[i].startTime, 0, 24);
            }

            fogStart = Mathf.Max(fogStart, 0);
            fogEnd = Mathf.Max(fogStart + 1, fogEnd);

            texture1HorizonTiling.x = Mathf.Max(1f, texture1HorizonTiling.x);
            texture2HorizonTiling.x = Mathf.Max(1f, texture2HorizonTiling.x);

            texture1HorizonTilingSterile = texture1HorizonTiling;
            texture1HorizonTilingSterile.x = Mathf.FloorToInt(texture1HorizonTiling.x);

            texture2HorizonTilingSterile = texture2HorizonTiling;
            texture2HorizonTilingSterile.x = Mathf.FloorToInt(texture2HorizonTiling.x);
        }

        public void SortPeriodsOfDay()
		{
            periodsOfDay = periodsOfDay.OrderBy(x => x.startTime).ToList();
        }

        public List<PeriodOfDay> periodsOfDay = new List<PeriodOfDay>();

        public float timeOfDay = 0f;
        public float activeTimeOfDay = 0f;

        public float realSecondsToGameHours = 0.1f;

        public float hoursForTransition = 1f;

        public float sunBaseRotationY = 0f;

        public float sunLightIntensity = 1f;
        public float sunMaxAngleBelowHorizon = 20f;

        public float cloudiness = 0.2f;
        public float cloudSpeed = 1f;
        public float sunSize = 0.04f;

        public bool sunColorAutomatic = true;
        public Color sunColor = Color.white;

        public float sunInfluenceSize = 1.2f;
        public float sunInfluenceIntensity = 0.005f;

        public float cloudSharpness = 0.2f;

        public Color cloudColor = Color.white;
        public Color cloudShadingColor = Color.white;

        public float cloudShadingThreshold = 0.1f;

        public float cloudShadingSharpness = 0.2f;

        public float cloudShadingStrength = 0.5f;

        public float sunCloudInfluence = 0.01f;
        public float skyColorCloudInfluence = 0.5f;

        public float cloudOpacity = 0.8f;
        public float cloudNightLuminanceMultiplier = 0.2f;


        public float alternateUVAtZenith = 0.2f;

        public Texture2D cloudTexture1 = null;
        public Texture2D cloudTexture2 = null;


        public float ditherStrength = 0.3f;

        public Vector2 texture1ZenithTiling = new Vector2(0.5f, 0.5f);
        public Vector2 texture2ZenithTiling = new Vector2(0.25f, 0.25f);
        
        public Vector2 texture1HorizonTiling = new Vector2(1f, 1f);
        public Vector2 texture2HorizonTiling = new Vector2(2f, 1f);

        public Vector2 texture1HorizonTilingSterile;
        public Vector2 texture2HorizonTilingSterile;

        public float fogHeightPower = 10f;
        public float fogColorBlend = 0.3f;
        public Color baseFogColor = new Color(0.8f, 0.8f, 0.8f);
        public float fogStart = 0f;
        public float fogEnd = 100f;
        public float fogDithering = 0.02f;
    }

}
