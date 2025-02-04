using System.Collections.Generic;
using System.IO;
using KSL.API;

namespace B_Torq.Utilities;

public class Config
{
    public static Config Instance { get; private set; }
    private static readonly string ConfigPath = Path.Combine(Kino.Paths.Config, "BTM.json");
    public Dictionary<int, float> SavedCars { get; } = new();
    public bool IsConfigLoadable = true;
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
        if (!IsConfigLoadable)
        {
            Kino.Log.Info("BTM config is disabled. Please enable it in the settings.");
            return;
        }
        
        if (SavedCars.TryGetValue(car.carId, out float savedBrakeTorque))
            car.carX.brakeTorque = savedBrakeTorque;
        else
        {
            Kino.Log.Error($"Car {car.carId} not found in saved brake torques.");
            SavedCars.Add(car.carId, car.carX.brakeTorque);
            Save();
            Kino.Log.Info($"Saved brake torque for car {car.carId}.");
        }
    }
}