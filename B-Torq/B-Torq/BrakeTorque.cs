using System.Collections.Generic;
using System.Linq;
using System.Net;
using B_Torq.types;
using B_Torq.Utilities;
using HarmonyLib;
using KSL.API;
using SyncMultiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace B_Torq;

[KSLMeta("BTM", "1.0.0", "Mizar")]
public class BrakeTorque : BaseMod
{
    private static List<Player> Players { get; } = [];
    private Rect windowRect_ = new(10, 10, 350, 350);
    private Vector2 scrollPosition_ = Vector2.zero;
    private bool isOpen_;
    public static GUISkin Skin { get; set; }
    private static RaceCar LocalPlayerCar => !PlayerCarControl.instance ? GameObject.Find("CarPositionMarker").GetComponentInChildren<RaceCar>() : PlayerCarControl.instance.car;
    
    private void Start()
    {
        windowRect_ = Utilities.Utils.CenterWindow(windowRect_);
        Kino.Input.Bind(KeyCode.None,ToggleWindow, "[BTM] Window toggle");
        Utilities.Utils.LoadSkinOnStart();
        Patcher.Init();
        Patcher.TryPatch(typeof(BrakeTorque));
        Config.Instance.TryLoadConfig();
        SceneManager.sceneLoaded += OnSceneLoad;
    }

    private static void OnSceneLoad(Scene scene, LoadSceneMode mode)
    {
        if (States.mainCurrent is not GarageGUIState) 
            return;
        
        var car = GameObject.Find("CarPositionMarker").GetComponentInChildren<RaceCar>();
        if (car == null)
            return;
        
        Config.Instance.TryGetSavedBrakeTorque(car);
    }

    private void Update()
    {
        var game = NetworkController.InstanceGame;

        if (game == null)
            return;
        
        foreach (var player in Players.ToList().Where(player => game.Players.All(x => x.PlayerId.accountId != player.Id.accountId)))
        {
            Players.Remove(player);
        }
    }
    
    public void OnGUI()
    {
        if (!isOpen_)
            return;

        if (Skin)
        {
            GUI.skin = Skin;
        }
        windowRect_ = GUILayout.Window(GetHashCode(), windowRect_, BrakeTorqueWindow, "[BTM]-Brake Torque Mod", GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
    }

    private void BrakeTorqueWindow(int windowID)
    {
        NetworkGame game = NetworkController.InstanceGame;
        
        if (!LocalPlayerCar)
        {
            GUILayout.Label("<color=red>No local player found!</color>");
            GUILayout.Label("<color=red>Join a multiplayer lobby or single player game!</color>");
            GUI.DragWindow();
            return;
        }

        if (GUILayout.Button("Debug Info"))
        {
            Debug.Log("Item value: " + Utilities.Utils.TryGetDynoParamsItemView().item.value);
        }
        
        RaceCar localPlayer = LocalPlayerCar;
        float brakeTorque = LocalPlayerCar.carX.brakeTorque;
        GUILayout.Label("Torque range: 100-10,000");
        GUILayout.Label($"Current Brake Torque:  {brakeTorque:F0}");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("-10", GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(false)))
        {
            localPlayer.carX.brakeTorque = Mathf.Clamp(localPlayer.carX.brakeTorque - 10, 100, 10000);
        }
        
        if (Utilities.Utils.HorizontalSlider(ref brakeTorque, 100, 10000, 100, GUILayout.MinHeight(15)))
        {
            //if (Utilities.Utils.TryGetDynoParamsItemView())
            //{
            //    Utilities.Utils.TryGetDynoParamsItemView().item.value = Utilities.Utils.ConvertSliderValueToTorque(brakeTorque);
            //}
            //else
            //{
            //    Debug.LogError("DynoParamsItemView not found!");
            //}
            
            localPlayer.carX.brakeTorque = brakeTorque;
            Config.Instance.AddBrakeTorque(localPlayer, brakeTorque);
            Config.Instance.Save();
        }

        if (GUILayout.Button("+10", GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(false)))
        {
            localPlayer.carX.brakeTorque = Mathf.Clamp(localPlayer.carX.brakeTorque + 10, 100, 10000);
        }
        GUILayout.EndHorizontal();
        
        if (game == null)
        {
            GUILayout.Label("<color=red>No multiplayer game found!</color>");
            GUILayout.Label("<color=red>Player list will not populate!</color>");
            GUILayout.Label("<color=red>You must be in a multiplayer lobby!</color>");
            GUI.DragWindow();
            return;
        }
        
        // Check if we are in a multiplayer lobby, if not do not show the player list
        if (States.mainCurrent is not SyncNetFreerideRaceModeState)
        {
            GUI.DragWindow();
            return;
        }
        
        // Player list display
        GUILayout.Label($"Players: ({game.Players.Count}/16)");
        GUILayout.BeginVertical();
        GUILayout.BeginHorizontal();
        scrollPosition_ = GUILayout.BeginScrollView(scrollPosition_, GUILayout.Height(250));
        foreach (var networkPlayer in game.Players)
        {
            var player = Players.FirstOrDefault(x => x.Id.accountId == networkPlayer.PlayerId.accountId);
            if (player == null)
            {
                player = new Player(networkPlayer);
                Players.Add(player);
            }
            else
            {
                player.NetworkPlayer = networkPlayer;
            }
                
            if (player.NetworkPlayer.IsLocal) 
                continue;
                
            using (new GUILayout.HorizontalScope("box"))
            {
                GUILayout.Label(player.Avatar, GUI.skin.label, GUILayout.Width(35), GUILayout.Height(35));
                GUILayout.Label($"{player.Username}'s Brake Torque: {player.BrakeTorque:F0}");
            }
        }
        GUILayout.EndScrollView();
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
        GUI.DragWindow();
    }

    public override void OnAdditionalAboutUIDraw()
    {
        Kino.UI.Label("Brought back by request!");
        Kino.UI.Label("A basic mod to adjust the brake torque of your car, below the games 1,000 limit!");
        Kino.UI.Label("If you find any bugs or have any suggestions, please let me know.");
        Kino.UI.Label("Discord: Sad_User");
    }

    private void OnApplicationQuit()
    {
        Config.Instance.Save();
    }

    private void ToggleWindow()
    {
        isOpen_ = !isOpen_;
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(RaceCar), "OnCarLoaded")]
    private static void RaceCar_OnCarLoaded_Postfix(RaceCar __instance)
    {
        if (!__instance)
        {
            return;
        }

        foreach (var player in Players.ToList())
        {
            if (__instance.isLocalPlayer)
                continue;
            
            if (player.Id.accountId == __instance.networkPlayer.PlayerId.accountId)
                player.BrakeTorque = __instance.carX.brakeTorque;
            else
                Players.Remove(player);
        }

        if (!__instance.isLocalPlayer)
            return;
        Config.Instance.TryGetSavedBrakeTorque(__instance);
    }
}