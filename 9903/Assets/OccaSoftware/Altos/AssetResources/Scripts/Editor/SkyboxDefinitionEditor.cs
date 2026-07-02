using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace OccaSoftware.Altos.Editor
{
    [CustomEditor(typeof(SkyboxDefinitionScriptableObject))]
    [CanEditMultipleObjects]
    public class SkyboxDefinitionEditor : UnityEditor.Editor
    {
        private Dictionary<string, SerializedProperty> serializedProperties;
        private static GUILayoutOption miniButtonWidth = GUILayout.Width(20f);
        private List<PeriodOfDay> periods;


        private void OnEnable()
        {
            serializedProperties = new Dictionary<string, SerializedProperty>();
            // Periods of Day
            serializedProperties.Add("Periods of Day", serializedObject.FindProperty("periodsOfDay"));

            // Time Settings
            serializedProperties.Add("Time of Day", serializedObject.FindProperty("timeOfDay"));
            serializedProperties.Add("Active Time of Day", serializedObject.FindProperty("activeTimeOfDay"));
            serializedProperties.Add("Real Seconds to Game Hours", serializedObject.FindProperty("realSecondsToGameHours"));

            // Sun Settings
            serializedProperties.Add("Sun Base Rotation Y", serializedObject.FindProperty("sunBaseRotationY"));
            serializedProperties.Add("Light Intensity", serializedObject.FindProperty("sunLightIntensity"));
            serializedProperties.Add("Max Angle Below Horizon", serializedObject.FindProperty("sunMaxAngleBelowHorizon"));
            serializedProperties.Add("Sun Size", serializedObject.FindProperty("sunSize"));
            serializedProperties.Add("Sun Color Automatic", serializedObject.FindProperty("sunColorAutomatic"));
            serializedProperties.Add("Sun Color", serializedObject.FindProperty("sunColor"));


            // Cloud Settings
            serializedProperties.Add("Cloud Texture 1", serializedObject.FindProperty("cloudTexture1"));
            serializedProperties.Add("Texture 1 Zenith Tiling", serializedObject.FindProperty("texture1ZenithTiling"));
            serializedProperties.Add("Texture 1 Horizon Tiling", serializedObject.FindProperty("texture1HorizonTiling"));
            serializedProperties.Add("Cloud Texture 2", serializedObject.FindProperty("cloudTexture2"));
            serializedProperties.Add("Texture 2 Zenith Tiling", serializedObject.FindProperty("texture2ZenithTiling"));
            serializedProperties.Add("Texture 2 Horizon Tiling", serializedObject.FindProperty("texture2HorizonTiling"));

            // Cloud Density Settings
            serializedProperties.Add("Cloudiness", serializedObject.FindProperty("cloudiness"));
            serializedProperties.Add("Cloud Speed", serializedObject.FindProperty("cloudSpeed"));
            serializedProperties.Add("Cloud Sharpness", serializedObject.FindProperty("cloudSharpness"));
            serializedProperties.Add("Cloud Color", serializedObject.FindProperty("cloudColor"));
            serializedProperties.Add("Cloud Opacity", serializedObject.FindProperty("cloudOpacity"));
            serializedProperties.Add("Cloud Shading Color", serializedObject.FindProperty("cloudShadingColor"));
            serializedProperties.Add("Cloud Shading Threshold", serializedObject.FindProperty("cloudShadingThreshold"));
            serializedProperties.Add("Cloud Shading Sharpness", serializedObject.FindProperty("cloudShadingSharpness"));
            serializedProperties.Add("Cloud Shading Strength", serializedObject.FindProperty("cloudShadingStrength"));
            serializedProperties.Add("Night Luminance Multiplier", serializedObject.FindProperty("cloudNightLuminanceMultiplier"));

            // Cloud Distribution Settings
            serializedProperties.Add("Alternate UVs at Zenith", serializedObject.FindProperty("alternateUVAtZenith"));

            // Cloud Influence Settings
            serializedProperties.Add("Sun Cloud Influence", serializedObject.FindProperty("sunCloudInfluence"));
            serializedProperties.Add("Sky Color Cloud Influence", serializedObject.FindProperty("skyColorCloudInfluence"));

            // Dither Settings
            serializedProperties.Add("Dither Strength", serializedObject.FindProperty("ditherStrength"));

            // Fog Settings
            serializedProperties.Add("Fog Height Power", serializedObject.FindProperty("fogHeightPower"));
            serializedProperties.Add("Horizon Color Fog Intensity", serializedObject.FindProperty("fogColorBlend"));
            serializedProperties.Add("Base Fog Color", serializedObject.FindProperty("baseFogColor"));
            serializedProperties.Add("Fog Start Distance", serializedObject.FindProperty("fogStart"));
            serializedProperties.Add("Fog End Distance", serializedObject.FindProperty("fogEnd"));
            serializedProperties.Add("Fog Dithering", serializedObject.FindProperty("fogDithering"));
        }


        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            if (Event.current.type == EventType.MouseUp)
            {
                SkyboxDefinitionScriptableObject o = (SkyboxDefinitionScriptableObject)serializedObject.targetObject;
                o.SortPeriodsOfDay();
                //ManageList();
            }
            
            Draw();

            serializedObject.ApplyModifiedProperties();
        }


        private void ManageList()
        {
            
            for (int i = 0; i < serializedProperties["Periods of Day"].arraySize - 1; i++)
            {
                SerializedProperty periodOfDay_c = serializedProperties["Periods of Day"].GetArrayElementAtIndex(i);
                SerializedProperty periodOfDay_n = serializedProperties["Periods of Day"].GetArrayElementAtIndex(i + 1);
                
                if (periodOfDay_n.FindPropertyRelative("startTime").floatValue < periodOfDay_c.FindPropertyRelative("startTime").floatValue)
                {
                    serializedProperties["Periods of Day"].MoveArrayElement(i + 1, i);
                }
                
            }
        }

        private void Draw()
        {
            // Periods of Day Settings
            EditorGUILayout.LabelField(new GUIContent("Periods of Day Key Frames", "Periods of day are treated as keyframes. The sky will linearly interpolate between the current period's colorset and the next period's colorset."), EditorStyles.boldLabel);
            for (int i = 0; i < serializedProperties["Periods of Day"].arraySize; i++)
            {
                EditorGUILayout.Space(5f);
                SerializedProperty periodOfDay = serializedProperties["Periods of Day"].GetArrayElementAtIndex(i);

                SerializedProperty description_Prop = periodOfDay.FindPropertyRelative("description");
                SerializedProperty startTime_Prop = periodOfDay.FindPropertyRelative("startTime");
                SerializedProperty horizonColor_Prop = periodOfDay.FindPropertyRelative("horizonColor");
                SerializedProperty zenithColor_Prop = periodOfDay.FindPropertyRelative("zenithColor");
                SerializedProperty groundColor_Prop = periodOfDay.FindPropertyRelative("groundColor");

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(description_Prop);

                if (GUILayout.Button("-", EditorStyles.miniButtonRight, miniButtonWidth))
                {
                    serializedProperties["Periods of Day"].DeleteArrayElementAtIndex(i);
                }
                else
                {
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.PropertyField(startTime_Prop);
                    EditorGUILayout.PropertyField(horizonColor_Prop);
                    EditorGUILayout.PropertyField(zenithColor_Prop);
                    EditorGUILayout.PropertyField(groundColor_Prop);
                }
            }
            EditorGUILayout.Space();
            if (GUILayout.Button("+"))
            {
                serializedProperties["Periods of Day"].arraySize += 1;
            }
            EditorGUILayout.Space();

            // Time Settings
            EditorGUILayout.LabelField("Time Settings", EditorStyles.boldLabel);
            if (!Application.isPlaying)
            {
                serializedProperties["Active Time of Day"].floatValue = serializedProperties["Time of Day"].floatValue;
            }
            System.TimeSpan timeSpan = System.TimeSpan.FromHours(serializedProperties["Active Time of Day"].floatValue);
            EditorGUILayout.LabelField("Current in-game time: " + timeSpan.ToString("hh':'mm':'ss"), EditorStyles.boldLabel);
            EditorGUILayout.Slider(serializedProperties["Time of Day"], 0f, 24f);
            EditorGUILayout.Slider(serializedProperties["Real Seconds to Game Hours"], 0f, 3f);
            EditorGUILayout.Space();


            // Sun Settings
            EditorGUILayout.LabelField("Sun Settings", EditorStyles.boldLabel);
            EditorGUILayout.Slider(serializedProperties["Sun Base Rotation Y"], 0f, 360f);
            EditorGUILayout.Slider(serializedProperties["Light Intensity"], 0f, 10f);
            EditorGUILayout.Slider(serializedProperties["Max Angle Below Horizon"], 0f, 20f);
            EditorGUILayout.Slider(serializedProperties["Sun Size"], 0f, 1f);
            serializedProperties["Sun Color Automatic"].boolValue = EditorGUILayout.Toggle(new GUIContent("Automatic Sun Color and Brightness", "Altos will automatically set the sun color and brightness based on the light direction. You can control how this appears in the skybox and clouds by using their respective Sun Color Mask properties. When disabled, Altos will not control the light color or intensity."), serializedProperties["Sun Color Automatic"].boolValue);
            serializedProperties["Sun Color"].colorValue = EditorGUILayout.ColorField(new GUIContent("Sun Color Mask", "This value is multiplied by the sun color temperature, which is set automatically based on the time of day."), serializedProperties["Sun Color"].colorValue, false, false, true);
            EditorGUILayout.Space();


            // Cloud Settings
            EditorGUILayout.LabelField("Cloud Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedProperties["Cloud Texture 1"]);
            EditorGUILayout.PropertyField(serializedProperties["Texture 1 Zenith Tiling"]);
            EditorGUILayout.PropertyField(serializedProperties["Texture 1 Horizon Tiling"]);
            EditorGUILayout.PropertyField(serializedProperties["Cloud Texture 2"]);
            EditorGUILayout.PropertyField(serializedProperties["Texture 2 Zenith Tiling"]);
            EditorGUILayout.PropertyField(serializedProperties["Texture 2 Horizon Tiling"]);
            EditorGUILayout.Space();

            // Cloud Density Settings
            EditorGUILayout.Slider(serializedProperties["Cloudiness"], 0f, 1f);
            EditorGUILayout.Slider(serializedProperties["Cloud Speed"], 0f, 30f);
            EditorGUILayout.Slider(serializedProperties["Cloud Sharpness"], 0f, 1f);
            serializedProperties["Cloud Color"].colorValue = EditorGUILayout.ColorField(new GUIContent("Cloud Color"), serializedProperties["Cloud Color"].colorValue, false, false, true);
            EditorGUILayout.Slider(serializedProperties["Cloud Opacity"], 0f, 1f);
            serializedProperties["Cloud Shading Color"].colorValue = EditorGUILayout.ColorField(new GUIContent("Cloud Shading Color"), serializedProperties["Cloud Shading Color"].colorValue, false, false, true);
            EditorGUILayout.Slider(serializedProperties["Cloud Shading Threshold"], 0f, 0.3f);
            EditorGUILayout.Slider(serializedProperties["Cloud Shading Sharpness"], 0f, 1f);
            EditorGUILayout.Slider(serializedProperties["Cloud Shading Strength"], 0f, 1f);
            EditorGUILayout.Slider(serializedProperties["Night Luminance Multiplier"], 0f, 1f);
            EditorGUILayout.Space();

            // Cloud Distribution Settings
            EditorGUILayout.Slider(serializedProperties["Alternate UVs at Zenith"], 0f, 1f);
            EditorGUILayout.Space();

            // Cloud Influence Settings
            EditorGUILayout.Slider(serializedProperties["Sun Cloud Influence"], 0f, 1.0f);
            EditorGUILayout.Slider(serializedProperties["Sky Color Cloud Influence"], 0f, 1f);
            EditorGUILayout.Space();


            // Dither Settings
            EditorGUILayout.LabelField("Dither Settings", EditorStyles.boldLabel);
            EditorGUILayout.Slider(serializedProperties["Dither Strength"], 0f, 1f);
            EditorGUILayout.Space();


            // Fog Settings
            EditorGUILayout.LabelField("Fog Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedProperties["Fog Start Distance"]);
            EditorGUILayout.PropertyField(serializedProperties["Fog End Distance"]);
            EditorGUILayout.Slider(serializedProperties["Fog Height Power"], 0f, 30f);
            EditorGUILayout.Slider(serializedProperties["Horizon Color Fog Intensity"], 0f, 1f);
            serializedProperties["Base Fog Color"].colorValue = EditorGUILayout.ColorField(new GUIContent("Base Fog Color"), serializedProperties["Base Fog Color"].colorValue, false, false, true);
            EditorGUILayout.Slider(serializedProperties["Fog Dithering"], 0f, 0.1f);
        }
    }

}
