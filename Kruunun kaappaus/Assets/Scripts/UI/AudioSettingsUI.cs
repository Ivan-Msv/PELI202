using System;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class AudioSettingsUI : MonoBehaviour
{
    [SerializeField] private Slider musicSlider;
    [SerializeField] private TextMeshProUGUI musicLabel;
    [SerializeField] private Slider soundSlider;
    [SerializeField] private TextMeshProUGUI soundLabel;
    [SerializeField] private Toggle musicMuteToggle, soundMuteToggle;

    private void OnEnable()
    {
        musicSlider.value = AudioManager.instance.MusicVolume;
        UpdateText();

        musicSlider.onValueChanged.AddListener(value => { AudioManager.instance.SetMusicVolume(value); UpdateText(); });
        musicMuteToggle.onValueChanged.AddListener(enabled => { AudioManager.instance.MuteMusic(enabled); });

        soundSlider.value = AudioManager.instance.SoundVolume;
        UpdateText();

        soundSlider.onValueChanged.AddListener(value => { AudioManager.instance.SetSoundVolume(value); UpdateText(); });
        soundMuteToggle.onValueChanged.AddListener(enabled => { AudioManager.instance.MuteSound(enabled); });
    }

    private void OnDisable()
    {
        musicSlider.onValueChanged.RemoveAllListeners();
        soundSlider.onValueChanged.RemoveAllListeners();

        soundMuteToggle.onValueChanged.RemoveAllListeners();
        musicMuteToggle.onValueChanged.RemoveAllListeners();
    }

    private void UpdateText()
    {
        musicLabel.text = $"Music Volume ({Math.Round(musicSlider.value, 2)})";
        soundLabel.text = $"Sound Volume ({Math.Round(soundSlider.value, 2)})";
    }
}
