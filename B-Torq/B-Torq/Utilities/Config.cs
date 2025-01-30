using System.Collections.Generic;
using System.IO;
using KSL.API;
using Newtonsoft.Json;

namespace B_Torq.Utilities;

public class Config
{
    public static Config Instance { get; }

    private static readonly string ConfigPath = Path.Combine(Kino.Paths.Config, "BTM.json");
    
    static Config()
    {
        if (!File.Exists(ConfigPath))
        {
            Instance = new Config();
            Instance.Save();
            Kino.Log.Warning("[BTM]: Config file not found, creating new one.");
            return;
        }
        
        Instance = File.ReadAllText(ConfigPath).FromJson<Config>();
    }

    public void Save()
    {
        File.WriteAllText(ConfigPath, this.ToJson());
    }
    
    public void AddSavedBrakeTorque(int carId, float brakeTorque)
    {
        SavedCars[carId] = brakeTorque;
    }
    
    public void RemoveSavedBrakeTorque(int carId)
    {
        if (SavedCars.ContainsKey(carId))
            SavedCars.Remove(carId);
        else
        {
            Kino.Log.Error($"Car {carId} not found in saved brake torques.");
        }
    }
    
    [JsonIgnore]
    public Dictionary<int, float> SavedCars { get; set; }
}