using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using System;
using System.Collections.Generic;
using static UnityEngine.Rendering.DebugUI;

public class DataSaving : MonoBehaviour
{
    public static DataSaving instance;
    private static readonly string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Crown Catchers");
    private static readonly string settingsPath = Path.Combine(folderPath, "Settings.Crown");

    private Dictionary<string, object> dataSettings = new();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }

        TryLoadSettings();
    }

    private void Start()
    {
        InitializeSettings();
    }

    public Dictionary<string, object> GetDefaults()
    {
        // Add default settings HERE
        var defaultSettings = new Dictionary<string, object>();

        // General

        // Audio
        defaultSettings["SoundVolume"] = 0.5f;
        defaultSettings["MusicVolume"] = 0.5f;
        defaultSettings["MusicMute"] = false;
        defaultSettings["SoundMute"] = false;

        // Video
        defaultSettings["Vsync"] = 0; // Disabled
        defaultSettings["FpsValue"] = -1; // Unlimited

        return defaultSettings;
    }

    // T tries to get "object" whether it's float or an integer (example)
    public T GetData<T>(string key)
    {
        // This checks if the key even exists, and also return value if true
        // If it doesn't, return default
        if (!dataSettings.TryGetValue(key, out var value))
        {
            Debug.LogError($"Could not find given key ({key}), trying to get default");
            return GetDefaultValue<T>(key);
        }

        if (value is T givenType)
        {
            return givenType;
        }

        Debug.LogError($"Value type ({value.GetType()}) for '{key}' is not equal to {typeof(T)}, returning default.");
        return default;
    }

    private T GetDefaultValue<T>(string key)
    {
        var tempList = GetDefaults();

        if (tempList[key] is T givenType)
        {
            return givenType;
        }

        Debug.LogError($"Could not find default value either, returning full default");
        return default;
    }

    // Sets given key or creates new one with value given
    public void SetData<T>(string key, T value)
    {
        dataSettings[key] = value;
    }

    private void InitializeSettings()
    {
        // Because video setting gameobject is inactive by default, initialize those settings here
        Application.targetFrameRate = GetData<int>("FpsValue");
        QualitySettings.vSyncCount = GetData<int>("Vsync");
    }

    private void TryLoadSettings()
    {
        if (!File.Exists(settingsPath))
        {
            dataSettings = GetDefaults();
            return;
        }

        using (FileStream stream = new(settingsPath, FileMode.OpenOrCreate))
        {
            using (StreamReader reader = new(stream))
            {
                string jsonContent = reader.ReadToEnd();
                var temporaryDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonContent);

                // Because the json serialization converts floats into doubles
                // And integers into long, we try to turn them back
                foreach (var keypair in temporaryDictionary)
                {
                    if (keypair.Value is long longType)
                    {
                        dataSettings[keypair.Key] = (int)longType;
                    }
                    else if (keypair.Value is double doubleType)
                    {
                        dataSettings[keypair.Key] = (float)doubleType;
                    }
                    else
                    {
                        dataSettings[keypair.Key] = keypair.Value;
                    }
                }

            }
        }
    }

    private void SaveSettings()
    {
        if (!File.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        using (FileStream stream = new(settingsPath, FileMode.Create))
        {
            using (StreamWriter writer = new(stream))
            {
                writer.Write(JsonConvert.SerializeObject(dataSettings));
            }
        }
    }

    private void OnApplicationQuit()
    {
        SaveSettings();
    }
}
