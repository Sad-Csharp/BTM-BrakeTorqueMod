using System;
using GameOverlay;
using KSL.API;

namespace B_Torq;

public static class Sync
{
    public static event Action<ulong, float> OnBrakeTorqueDataReceived;
    public static event Action<ulong> OnBrakeTorqueRequestReceived;
    
    private static bool isAvailable_;
    private static ulong steamId_;
    private static int dataPacketId_;
    private static int requestPacketId_;

    public static void Init()
    {
        isAvailable_ = Kino.Sync.IsAvailable;
        if (!isAvailable_)
        {
            Kino.Log.Error("Sync is not available. Please install KSL.CarX extension, its required for sync to work.");
            return;
        }
        
        if (!Kino.Sync.TryRegisterPacket("bt_d", out dataPacketId_) || !Kino.Sync.TryRegisterPacket("bt_r", out requestPacketId_))
        {
            Kino.Log.Error("Unable to register drivetrain packets");
            return;
        }
        
        if (!Kino.Sync.SetCallback(dataPacketId_, OnDataPacketReceived))
        {
            Kino.Log.Error("Unable to register drivetrain data callback");
            return;
        }
        
        if (!Kino.Sync.SetCallback(requestPacketId_, OnRequestPacketReceived))
        {
            Kino.Log.Error("Unable to register drivetrain request callback");
            return;
        }
        
        SteamOverlay steamOverlay = Overlay.instance as SteamOverlay;
        if (steamOverlay != null)
        {
            steamId_ = steamOverlay.player.id.accountId;
        }
        Kino.Log.Info($"Synchronization initialized, SteamID: {steamId_}");
    }

    public static void Send(float brakeTorque, int nwId = -1)
    {
        if (!isAvailable_)
            return;

        IPacket packet = Kino.Sync.CreatePacket(dataPacketId_);
        packet.WriteUInt64(steamId_);
        packet.WriteFloat(brakeTorque);
        SendInternal(packet, nwId);
    }

    public static void Request(int nwId = -1)
    {
        if (!isAvailable_)
            return;

        IPacket packet = Kino.Sync.CreatePacket(requestPacketId_);
        packet.WriteUInt64(steamId_);
        SendInternal(packet, nwId);
    }
    private static void SendInternal(IPacket packet, int nwId)
    {
        if (nwId == -1)
        {
            Kino.Sync.SendToAll(packet);
            return;
        }

        Kino.Sync.SendTo(packet, (ulong)nwId);
    }

    private static void OnDataPacketReceived(IPacket packet)
    {
        if (!isAvailable_)
            return;

        if (packet.Id != requestPacketId_)
            return;

        ulong number = packet.ReadUInt64();
        float brakeTorque = packet.ReadFloat();
        Action<ulong, float> onBrakeTorqueDataReceived = OnBrakeTorqueDataReceived;
        if (onBrakeTorqueDataReceived == null)
            return;
        
        onBrakeTorqueDataReceived(number, brakeTorque);
    }
    
    private static void OnRequestPacketReceived(IPacket packet)
    {
        if (!isAvailable_)
            return;

        if (packet.Id != dataPacketId_)
            return;
        
        ulong number = packet.ReadUInt64();
        Action<ulong> onBrakeTorqueRequestReceived = OnBrakeTorqueRequestReceived;
        if (onBrakeTorqueRequestReceived == null)
            return;
        
        onBrakeTorqueRequestReceived(number);
    }
}