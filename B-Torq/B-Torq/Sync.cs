using System;
using GameOverlay;
using KSL.API;
using UnityEngine;

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
            Kino.Log.Error("Unable to register btm packets");
            return;
        }
        
        if (!Kino.Sync.SetCallback(dataPacketId_, OnDataPacketReceived))
        {
            Kino.Log.Error("Unable to register btm data callback");
            return;
        }
        
        if (!Kino.Sync.SetCallback(requestPacketId_, OnRequestPacketReceived))
        {
            Kino.Log.Error("Unable to register btm request callback");
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
        {
            Debug.Log("Send: Not available, skipping.");
            return;
        }

        try
        {
            IPacket packet = Kino.Sync.CreatePacket(dataPacketId_);
            packet.WriteUInt64(steamId_);
            packet.WriteFloat(brakeTorque);
            SendInternal(packet, nwId);
            Debug.Log($"Sent brake torque packet: steamId={steamId_}, brakeTorque={brakeTorque}, nwId={nwId}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error sending brake torque packet: {ex.Message}");
        }
    }

    public static void Request(int nwId = -1)
    {
        if (!isAvailable_)
        {
            Debug.Log("Request: Not available, skipping.");
            return;
        }

        try
        {
            IPacket packet = Kino.Sync.CreatePacket(requestPacketId_);
            packet.WriteUInt64(steamId_);
            SendInternal(packet, nwId);
            Debug.Log($"Sent request packet: steamId={steamId_}, nwId={nwId}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error sending request packet: {ex.Message}");
        }
    }
    
    private static void SendInternal(IPacket packet, int nwId)
    {
        if (nwId == -1)
        {
            Debug.Log("Sending packet to all players.");
            Kino.Sync.SendToAll(packet);
            return;
        }

        try
        {
            Debug.Log($"Sending packet to player with nwId={nwId}.");
            Kino.Sync.SendTo(packet, (ulong)nwId);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error sending packet to player with nwId={nwId}: {ex.Message}");
        }
    }

    private static void OnDataPacketReceived(IPacket packet)
    {
        if (!isAvailable_)
        {
            Debug.Log("OnDataPacketReceived: Not available, skipping.");
            return;
        }

        if (packet.Id != requestPacketId_)
        {
            Debug.Log($"Ignoring packet with id {packet.Id}, expected {requestPacketId_}.");
            return;
        }

        try
        {
            ulong number = packet.ReadUInt64();
            float brakeTorque = packet.ReadFloat();
            Debug.Log($"Received brake torque data: number={number}, brakeTorque={brakeTorque}");

            Action<ulong, float> onBrakeTorqueDataReceived = OnBrakeTorqueDataReceived;
            if (onBrakeTorqueDataReceived == null)
            {
                Debug.LogWarning("OnBrakeTorqueDataReceived callback is null, ignoring data.");
                return;
            }

            onBrakeTorqueDataReceived(number, brakeTorque);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error processing data packet: {ex.Message}");
        }
    }
    
    private static void OnRequestPacketReceived(IPacket packet)
    {
        if (!isAvailable_)
        {
            Debug.Log("OnRequestPacketReceived: Not available, skipping.");
            return;
        }

        if (packet.Id != dataPacketId_)
        {
            Debug.Log($"Ignoring packet with id {packet.Id}, expected {dataPacketId_}.");
            return;
        }

        try
        {
            ulong number = packet.ReadUInt64();
            Debug.Log($"Received brake torque request: number={number}");

            Action<ulong> onBrakeTorqueRequestReceived = OnBrakeTorqueRequestReceived;
            if (onBrakeTorqueRequestReceived == null)
            {
                Debug.LogWarning("OnBrakeTorqueRequestReceived callback is null, ignoring request.");
                return;
            }

            onBrakeTorqueRequestReceived(number);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error processing request packet: {ex.Message}");
        }
    }
}