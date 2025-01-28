
using System;
using System.Collections.Generic;
using GameOverlay;
using HarmonyLib;
using SyncMultiplayer;

namespace B_Torq.types;

public class Player
{
    public string Name { get; } = string.Empty;
    public RaceCar RaceCar { get; }
    public bool IsLocal { get; }
    public int CarId { get; }
    public int NetworkID { get; } = -1;
    public ulong SteamID { get; }

    public Player(RaceCar car)
    {
        RaceCar = car;
        IsLocal = !car.isNetworkCar;
        if (IsLocal)
        {
            Name = car.networkPlayer.FilteredNickName;
            SteamID = car.networkPlayer.PlayerId.accountId;
        }
        else
        {
            Name = RaceCar.networkPlayer.FilteredNickName;
            NetworkID = RaceCar.networkPlayer.NetworkID;
            SteamID = RaceCar.networkPlayer.PlayerId.accountId;
        }
        CarId = RaceCar.carId;
    }
}