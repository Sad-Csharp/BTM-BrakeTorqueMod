using B_Torq.types;
using KSL.API;

namespace B_Torq;

[KSLMeta("BTM", "1.0.0", "Mizar")]
public class BrakeTorque : BaseMod
{
    private void Start()
    {
        Patcher.Init();
        Players.Init();
        //Players.OnPlayerLoaded -= OnPlayerLoaded;
        //Players.OnPlayerLoaded += OnPlayerLoaded;
    }

    private void Update()
    {
        Players.Update();
    }
        
    public override void OnUIDraw()
    {
        Kino.UI.Label("Players List");

        foreach (var player in Players.AllPlayers)
        {
            Kino.UI.Label(player.Name);
        }
    }
}