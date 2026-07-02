namespace OccaSoftware.Altos
{
    public static class Helpers
    {
        public static float Remap(float value, float iMin, float iMax, float oMin, float oMax)
        {
            value = UnityEngine.Mathf.Clamp(value, iMin, iMax);
            float a = UnityEngine.Mathf.InverseLerp(iMin,iMax,value);
            return UnityEngine.Mathf.Lerp(oMin, oMax, a);
            
        }

        public static float Remap01(float value, float iMin, float iMax)
        {
            return UnityEngine.Mathf.Clamp01(Remap(value, iMin, iMax, 0f, 1f));
        }

        public static void RenderFeatureOnEnable(UnityEngine.Events.UnityAction<UnityEngine.SceneManagement.Scene, UnityEngine.SceneManagement.Scene> action)
        {
            #if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode += action;
            #endif

            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += action;
        }

        public static void RenderFeatureOnDisable(UnityEngine.Events.UnityAction<UnityEngine.SceneManagement.Scene, UnityEngine.SceneManagement.Scene> action)
        {
            #if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode -= action;
            #endif

            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= action;
        }
    }
}
