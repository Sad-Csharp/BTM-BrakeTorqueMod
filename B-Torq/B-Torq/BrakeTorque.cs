using System.Collections.Generic;
using System.Linq;
using B_Torq.types;
using B_Torq.Utilities;
using HarmonyLib;
using KSL.API;
using SyncMultiplayer;
using UnityEngine;

namespace B_Torq;

[KSLMeta("BTM", "1.0.0", "Mizar")]
public class BrakeTorque : BaseMod
{
    private static List<Player> Players { get; } = [];
    private Rect windowRect_ = new(10, 10, 350, 350);
    private Vector2 scrollPosition_ = Vector2.zero;
    private bool isOpen_;
    private static float cachedBrakeTorque_;
    private bool prevModEnabled = Config.Instance.IsModEnabled;
    private bool prevConfigLoadable = Config.Instance.IsConfigLoadable;
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
        Sync.Init();
        Sync.OnBrakeTorqueDataReceived -= OnBrakeTorqueDataReceived;
        Sync.OnBrakeTorqueDataReceived += OnBrakeTorqueDataReceived;
        Sync.OnBrakeTorqueRequestReceived -= OnBrakeTorqueRequestReceived;
        Sync.OnBrakeTorqueRequestReceived += OnBrakeTorqueRequestReceived;
    }

    private void Update()
    {
        var game = NetworkController.InstanceGame;

        if (game == null)
            return;
        
        Players.RemoveAll(player => game.Players.All(t => t.PlayerId.accountId != player.Id.accountId) && !player.NetworkPlayer.isNetworkCarLoading);
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
        bool modEnabled = GUILayout.Toggle(Config.Instance.IsModEnabled, "Enable-Disable BTM");
        if (modEnabled != prevModEnabled)
        {
            Config.Instance.IsModEnabled = modEnabled;
            
            if (!modEnabled)
                LocalPlayerCar.carX.brakeTorque = cachedBrakeTorque_;
        }
        prevModEnabled = modEnabled;
        
        bool configLoadable = GUILayout.Toggle(Config.Instance.IsConfigLoadable, "Allow loading of saved torque values?");
        if (configLoadable != prevConfigLoadable)
        {
            Config.Instance.IsConfigLoadable = configLoadable;
        }
        if (!Config.Instance.IsConfigLoadable)
            GUILayout.Label("<color=red>BTM config is disabled. BTM will not load torque values!</color>");
        prevConfigLoadable = configLoadable;
        
        if (!LocalPlayerCar)
        {
            GUILayout.Label("<color=red>No local player found!</color>");
            GUI.DragWindow();
            return;
        }
        
        NetworkGame game = NetworkController.InstanceGame;
        
		// Check if we are in a multiplayer lobby, if not do not show anything but a label and return
        if (States.mainCurrent is not SyncNetFreerideRaceModeState)
        {
            GUI.DragWindow();
			GUILayout.Label("<color=red>Join a multiplayer lobby!</color>");
            return;
        }

        if (Config.Instance.IsModEnabled)
        {
            RaceCar localPlayer = LocalPlayerCar;
            float brakeTorque = LocalPlayerCar.carX.brakeTorque;
            GUILayout.Label("Torque range: 100-10,000");
            GUILayout.Label($"Current Brake Torque:  {brakeTorque:F0}");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("-10", GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(false)))
            {
                localPlayer.carX.brakeTorque = Mathf.Clamp(localPlayer.carX.brakeTorque - 10, 100, 10000);
                Sync.Send(localPlayer.carX.brakeTorque);
            }
        
            if (Utilities.Utils.HorizontalSlider(ref brakeTorque, 100, 10000, 100, GUILayout.Height(40)))
            {
                localPlayer.carX.brakeTorque = brakeTorque;
                Config.Instance.AddBrakeTorque(localPlayer, brakeTorque);
                Sync.Send(localPlayer.carX.brakeTorque);
            }

            if (GUILayout.Button("+10", GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(false)))
            {
                localPlayer.carX.brakeTorque = Mathf.Clamp(localPlayer.carX.brakeTorque + 10, 100, 10000);
                Sync.Send(localPlayer.carX.brakeTorque);
            }
            GUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.Label("<color=red>BTM is disabled!</color>");
        }
        
        if (game == null)
        {
            GUILayout.Label("<color=red>No multiplayer game found!</color>");
            GUILayout.Label("<color=red>Player list will not populate!</color>");
            GUILayout.Label("<color=red>You must be in a multiplayer lobby!</color>");
            GUI.DragWindow();
            return;
        }
        
        // Player list display
        GUILayout.Label($"Players: ({game.Players.Count}/16)");
        GUILayout.BeginVertical();
        GUILayout.BeginHorizontal();
        scrollPosition_ = GUILayout.BeginScrollView(scrollPosition_, GUILayout.Height(250));

        foreach (NetworkPlayer networkPlayer in game.Players)
        {
            Player player = Players.FirstOrDefault(x => x.Id.accountId == networkPlayer.PlayerId.accountId);
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

    #region Methods

    private void OnBrakeTorqueDataReceived(ulong steamId, float brakeTorque)
    {
        NetworkGame game = NetworkController.InstanceGame;

        if (game == null)
            return;
        
        foreach (var player in Players)
        {
            if (player == null)
                continue;
            
            if (player.Id.accountId == steamId)
            {
                player.BrakeTorque = brakeTorque;
                break;
            }
        }
    }

    private void OnBrakeTorqueRequestReceived(ulong steamId)
    {
        if (LocalPlayerCar == null)
            return;

        foreach (Player player in Players)
        {
            if (player == null)
                continue;
            
            if (player.Id.accountId == steamId)
            {
                Sync.Send(LocalPlayerCar.carX.brakeTorque, player.NetworkPlayer.NetworkID);
            }
        }
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
            return;

        if (!__instance.isLocalPlayer)
        {
            if (States.mainCurrent is not SyncNetFreerideRaceModeState)
                return;
            
            Sync.Request(__instance.networkPlayer.NetworkID);
        }

        if (!__instance.isLocalPlayer)
            return;
        
        cachedBrakeTorque_ = __instance.carX.brakeTorque;
        Config.Instance.TryGetSavedBrakeTorque(__instance);
        if (States.mainCurrent is SyncNetFreerideRaceModeState)
            Sync.Send(__instance.carX.brakeTorque, __instance.networkPlayer.NetworkID);
    }

    #endregion
    
}