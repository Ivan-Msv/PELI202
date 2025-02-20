using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using System;

public struct SettingsData
{
    public float soundVolume;
    public float musicVolume;
    public bool soundMute;
    public bool musicMute;
    public int fpsValue;

    public static SettingsData CreateDefault()
    {
        return new SettingsData()
        {
            soundVolume = 0.5f,
            musicVolume = 0.5f,
            musicMute = false,
            soundMute = false,
            fpsValue = -1
        };
    }
}

public class DataSaving : MonoBehaviour
{
    public static DataSaving instance;
    public SettingsData dataSettings = SettingsData.CreateDefault();
    private static readonly string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Kruunun Kaappaus");
    private static readonly string settingsPath = Path.Combine(folderPath, "Settings.KK");

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
        SetupSettings();
    }

    private void SetupSettings()
    {
        // Audio
        AudioManager.instance.SetMusicVolume(dataSettings.musicVolume);
        AudioManager.instance.SetSoundVolume(dataSettings.soundVolume);
        AudioManager.instance.MuteMusic(dataSettings.musicMute);
        AudioManager.instance.MuteSound(dataSettings.soundMute);
        Application.targetFrameRate = dataSettings.fpsValue;
    }

    private void GetSettings()
    {
        dataSettings.musicVolume = AudioManager.instance.MusicVolume;
        dataSettings.soundVolume = AudioManager.instance.SoundVolume;
        dataSettings.musicMute = AudioManager.instance.MusicMute;
        dataSettings.soundMute = AudioManager.instance.SoundMute;
    }

    private void TryLoadSettings()
    {
        if (!File.Exists(settingsPath)) { return; }

        using (FileStream stream = new(settingsPath, FileMode.OpenOrCreate))
        {
            using (StreamReader reader = new(stream))
            {
                string jsonContent = reader.ReadToEnd();
                dataSettings = JsonConvert.DeserializeObject<SettingsData>(jsonContent);
            }
        }
    }

    private void SaveSettings()
    {
        GetSettings();

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
