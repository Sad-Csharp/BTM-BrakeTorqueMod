using System.Collections.Generic;
using HarmonyLib;

namespace B_Torq.types;

public static class Players
{
    //public static event Action<Player> OnPlayerLoaded;
    public static Player LocalPlayer { get; private set; }
    private static readonly Dictionary<int, Player> allPlayers_ = new();
    private static readonly List<int> playersToRemove_ = new(16);
    public static IReadOnlyCollection<Player> AllPlayers => allPlayers_.Values;

    public static void Init()
    {
        Patcher.TryPatch(typeof(Players));
    }

    public static void Update()
    {
        foreach (KeyValuePair<int, Player> keyValuePair in allPlayers_)
        {
            // check if the player is still in the game
            if (!keyValuePair.Value.RaceCar) //if its not true
                playersToRemove_.Add(keyValuePair.Key); // add it to the list of players to remove

            // if the count is 0, there are no players to remove
            if (playersToRemove_.Count == 0)
                return;
            
            // remove the players
            foreach (int num in playersToRemove_) // go through the list of players to remove
                allPlayers_.Remove(num); // remove the player if they are on the list
            
            playersToRemove_.Clear();
        }
    }

    private static void CreatePlayer(RaceCar car)
    {
        // if car doesn't exist, return
        if (!car)
            return;
        
        // create player
        Player player = new Player(car);
        
        // set local player
        if (player.IsLocal)
            LocalPlayer = player;
        
        // if player already exists, remove
        if (allPlayers_.ContainsKey(player.NetworkID))
            allPlayers_.Remove(player.NetworkID);
        
        // add new player
        allPlayers_.Add(player.NetworkID, player);
        
        // TODO: Allow this to be a callback that grabs their brake torque value
        //Action<Player> onPlayerLoaded = OnPlayerLoaded;
        //
        //if (onPlayerLoaded == null)
        //    return;
        //
        //onPlayerLoaded(player);
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(RaceCar), "OnCarLoaded")]
    private static void RaceCar_OnCarLoaded_Postfix(RaceCar __instance)
    {
        if (!__instance)
        {
            return;
        }
        CreatePlayer(__instance);
    }
}