using System;
using HarmonyLib;
using KSL.API;

namespace B_Torq.Utilities;

public static class Patcher
{
    private static Harmony _patcher;
		
    public static void Init()
    {
        if (_patcher != null)
        {
            return;
        }
        _patcher = new Harmony("BrakeTorque.patch");
    }
		
    public static void TryPatch(Type type)
    {
        try
        {
            _patcher.PatchAll(type);
        }
        catch (Exception ex)
        {
            Kino.Log.Error("Unable to apply patches for " + type.Name + ", error: " + ex.Message);
        }
    }
}