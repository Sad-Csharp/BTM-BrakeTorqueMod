using System.Collections.Generic;
using System.IO;
using KSL.API;

namespace B_Torq.Utilities;

public class Config
{
    public static Config Instance { get; private set; }
    private static readonly string ConfigPath = Path.Combine(Kino.Paths.Config, "BTM.json");
    
    // must be public so json can save and load from it! It will break config if private
    public Dictionary<int, float> SavedCars { get; } = new();
    public bool IsModEnabled = true;

    static Config()
    { 
            Instance = new Config();
    }
        
    public void TryLoadConfig()
    {
        if (!File.Exists(ConfigPath))
        {
            Instance = new Config();
            Instance.Save();
            Kino.Log.Error("Config file not found, creating new one.");
        }
        else
        {
            Instance = File.ReadAllText(ConfigPath).FromJson<Config>();
            Kino.Log.Info("Config file loaded.");
        }
    }

    public void Save()
    {
        File.WriteAllText(ConfigPath, this.ToJson());
        Kino.Log.Info("Config file saved.");
    }

    public void AddBrakeTorque(RaceCar car, float torque)
    {
        if (SavedCars.ContainsKey(car.carId))
            SavedCars[car.carId] = torque;
        else
        {
            SavedCars.Add(car.carId, torque);
            Save();
        }
    }
    
    public void TryGetSavedBrakeTorque(RaceCar car)
    {
        if (!IsModEnabled)
        {
            Kino.Log.Info("BTM is disabled. Please enable it.");
            return;
        }

        if (SavedCars.TryGetValue(car.carId, out float savedBrakeTorque))
        {
            if (car == null)
                return;
            
            car.carX.brakeTorque = savedBrakeTorque;
        }
            
        else
        {
            Kino.Log.Error($"Car {car.carId} not found in saved brake torques.");
            SavedCars.Add(car.carId, car.carX.brakeTorque);
            Save();
            Kino.Log.Info($"Saved brake torque for car {car.carId}.");
        }
    }
}