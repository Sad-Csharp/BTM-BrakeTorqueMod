using GameOverlay;
using SyncMultiplayer;
using UnityEngine;

namespace B_Torq.types;

public class Player
{
    public string Username { get; set; }
    public string SteamUsername { get; private set; }
    public PlayerId Id { get; set; }
    public NetworkPlayer NetworkPlayer { get; set; }
    public float BrakeTorque { get; set; }
    public Texture2D Avatar { get; private set; } = GameManager.instance.defaultAvatar;

    public Player(NetworkPlayer networkPlayer)
    {
        NetworkPlayer = networkPlayer;
        Username = networkPlayer.FilteredNickName;
        Id = networkPlayer.PlayerId;
        if (networkPlayer.userCar && networkPlayer.userCar.carX)
        {
            BrakeTorque = networkPlayer.userCar.carX.brakeTorque;
        }
        else
        {
            // We also adjust this after their car loads in OnCarLoaded Harmony patch
            BrakeTorque = 0; // or some other default value
        }
        
        if (Id.platform == UserPlatform.Id.Steam)
        {
            Overlay.instance.RequestUserAvatarAndName(Id, GetUsernameAndAvatar, AvatarSize.Large);
        }
    }
    
    private void GetUsernameAndAvatar(PlayerId id, string username, Texture2D avatar)
    {
        SteamUsername = username;
        Avatar = avatar;
    }
}