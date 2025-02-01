using System.Linq;
using System.Reflection;
using UnityEngine;

namespace B_Torq.Utilities;

public static class Utils
{
    private static GameObject obj_;
    private static DynoParamsItemView dynoParamsItemView_;
    
    private static byte[] LoadFromMemory(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(assembly.GetManifestResourceNames().FirstOrDefault(x => x.EndsWith(resourceName)));

        if (stream == null) 
            return null;
        
        var buffer = new byte[stream.Length];
        _ = stream.Read(buffer, 0, buffer.Length);
        return buffer;
    }

    public static void LoadSkinOnStart()
    {
        var assset = LoadFromMemory("whiteskin");
        var bundle = AssetBundle.LoadFromMemory(assset);
        bundle.LoadAllAssets();
        BrakeTorque.Skin = bundle.LoadAsset<GUISkin>("whiteskin");
    }
    
    public static Rect CenterWindow(Rect windowRect)
    {
        windowRect.x = (Screen.width - windowRect.width) / 2f;
        windowRect.y = (Screen.height - windowRect.height) / 2f;
        return windowRect;
    }

    public static bool HorizontalSlider(ref float val, float min, float max, float increment, params GUILayoutOption[] options)
    {
        var newVal = GUILayout.HorizontalSlider(val, min, max, options);
        if (!Mathf.Approximately(newVal, val))
        {
            val = Mathf.Round(newVal / increment) * increment;
            return true;
        }
        
        return false;
    }
    
    public static float ConvertSliderValueToTorque(float sliderValue)
    {
        sliderValue = Mathf.Clamp01(sliderValue);
        return 100f + (10000f - 100f) * sliderValue;
    }
    
    public static DynoParamsItemView TryGetDynoParamsItemView()
    {
        if (dynoParamsItemView_ != null)
            return dynoParamsItemView_;

        if (obj_ == null)
        {
            obj_ = GameObject.Find("KeepAlive(Clone)/UGUI/Root/Contexts/UIDynostand/BrakesContainer/BottomMain/BrakesParamsList/Viewport/VirtualContent/UIDynoParamsItem");
        
            if (obj_ == null)
            {
                Debug.LogError("UIDynoParamsItem not found!");
                return null;
            }
        }

        dynoParamsItemView_ = obj_.GetComponent<DynoParamsItemView>();

        if (dynoParamsItemView_ == null)
        {
            Debug.LogError("DynoParamsItemView component not found on UIDynoParamsItem!");
        }

        return dynoParamsItemView_;
    }
}