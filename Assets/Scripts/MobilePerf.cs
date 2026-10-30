using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Water2D;

public static class MobilePerf
{
    public enum DeviceTier
    {
        Low,
        Mid,
        High
    }

    public static DeviceTier Tier { get; private set; } = DeviceTier.High;
    public static int TargetFps { get; private set; } = 60;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Apply()
    {
        QualitySettings.vSyncCount = 0;
        Tier = DetectTier();
        TargetFps = Tier == DeviceTier.Low ? 30 : 60;
        Application.targetFrameRate = TargetFps;

#if UNITY_ANDROID && !UNITY_EDITOR
        QualitySettings.SetQualityLevel(Tier == DeviceTier.High ? 2 : 0, true);
        TunePipeline();
        if (Tier == DeviceTier.Low && Screen.height > 1280)
        {
            float scale = 1280f / Screen.height;
            Screen.SetResolution(Mathf.Max(640, Mathf.RoundToInt(Screen.width * scale)), 1280, true);
        }
#endif
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static DeviceTier DetectTier()
    {
#if !UNITY_ANDROID || UNITY_EDITOR
        return DeviceTier.High;
#else
        int ram = SystemInfo.systemMemorySize;
        int cores = SystemInfo.processorCount;
        int shader = SystemInfo.graphicsShaderLevel;

        if (ram <= 4096 || cores <= 4 || shader < 45)
            return DeviceTier.Low;
        if (ram >= 6144 && shader >= 45)
            return DeviceTier.High;
        return DeviceTier.Mid;
#endif
    }

    static void TunePipeline()
    {
        UniversalRenderPipelineAsset urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urp == null)
            urp = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
        if (urp == null)
            return;

        urp.supportsCameraOpaqueTexture = true;
        urp.supportsCameraDepthTexture = true;
        urp.msaaSampleCount = 1;

        if (Tier == DeviceTier.High)
        {
            urp.renderScale = 1f;
            urp.supportsHDR = true;
            urp.shadowDistance = 25f;
            return;
        }

        urp.supportsHDR = false;
        urp.shadowDistance = 0f;
        urp.renderScale = Tier == DeviceTier.Mid ? 0.8f : 0.65f;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (Tier == DeviceTier.High)
            SoftenHighEndWater();
        else
        {
            CheapenWater();
            DisableExtraWaterCameras();
        }
#endif
    }

    static void SoftenHighEndWater()
    {
        ModernWater2D[] waters = Object.FindObjectsByType<ModernWater2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < waters.Length; i++)
        {
            ModernWater2D water = waters[i];
            if (water == null || water.settings == null || water.settings._reflectionsSettings == null)
                continue;

            if (water.settings._reflectionsSettings.textureResolution.value > 0.5f)
                water.settings._reflectionsSettings.textureResolution.value = 0.5f;
        }
    }

    static void CheapenWater()
    {
        ModernWater2D[] waters = Object.FindObjectsByType<ModernWater2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < waters.Length; i++)
        {
            ModernWater2D water = waters[i];
            if (water == null)
                continue;

            water.enableReflections.value = false;
            water.enableSimulation.value = false;
            water.enableWavesSimulation.value = false;
            water.enableBlur.value = false;
            water.enableObstruction.value = false;

            if (water.settings != null)
            {
                if (water.settings._waterSettings != null)
                {
                    water.settings._waterSettings.waterType = WaterType.cheap;
                    water.settings._waterSettings._useLighting.value = false;
                    water.settings._waterSettings.enableBelowWater.value = false;
                    water.settings._waterSettings.pixelPerfect.value = false;
                }

                if (water.settings._reflectionsSettings != null)
                {
                    water.settings._reflectionsSettings.enableTopDownReflections.value = false;
                    water.settings._reflectionsSettings.enablePlatformerReflections.value = false;
                    water.settings._reflectionsSettings.enableRaymarchedReflections.value = false;
                    water.settings._reflectionsSettings.textureResolution.value = 0.25f;
                }

                if (water.settings._blurSettings != null)
                    water.settings._blurSettings.useBlur.value = false;
            }
        }
    }

    static void DisableExtraWaterCameras()
    {
        Camera main = Camera.main;
        ReflectionsSystem[] reflections = Object.FindObjectsByType<ReflectionsSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < reflections.Length; i++)
            DisableExtraCamera(reflections[i], main);

        ObstructorManager[] obstructors = Object.FindObjectsByType<ObstructorManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < obstructors.Length; i++)
            DisableExtraCamera(obstructors[i], main);

        SurfaceRenderingManager[] surfaces = Object.FindObjectsByType<SurfaceRenderingManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < surfaces.Length; i++)
            DisableExtraCamera(surfaces[i], main);
    }

    static void DisableExtraCamera(MonoBehaviour feature, Camera main)
    {
        if (feature == null)
            return;

        feature.enabled = false;
        Camera cam = feature.GetComponent<Camera>();
        if (cam != null && cam != main)
            cam.enabled = false;
    }
}
