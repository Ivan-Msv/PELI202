using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VideoSettingsUI : MonoBehaviour
{
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Toggle vsyncToggle;
    [SerializeField] private TMP_Dropdown targetResolution;
    [SerializeField] private TMP_Dropdown framerateLimit;
    private List<Resolution> availableResolutions = new();

    private void OnEnable()
    {
        fullscreenToggle.isOn = Screen.fullScreen;
        fullscreenToggle.onValueChanged.AddListener(enabled => { Screen.fullScreen = enabled; });

        vsyncToggle.isOn = QualitySettings.vSyncCount > 0;
        vsyncToggle.onValueChanged.AddListener(enabled => { QualitySettings.vSyncCount = enabled ? 1 : 0; });

        GetNewResolutions();
        targetResolution.onValueChanged.AddListener(index => { UpdateResolution(index); });

        UpdateFramerateVisual();
        framerateLimit.onValueChanged.AddListener(index => { SetTargetFramerate(index); });
    }

    private void OnDisable()
    {
        fullscreenToggle.onValueChanged.RemoveAllListeners();
        vsyncToggle.onValueChanged.RemoveAllListeners();
        targetResolution.onValueChanged.RemoveAllListeners();
        framerateLimit.onValueChanged.RemoveAllListeners();
    }

    private void GetNewResolutions()
    {
        availableResolutions = Screen.resolutions.ToList();
        availableResolutions.Reverse();

        List<TMP_Dropdown.OptionData> newResolutions = new();

        foreach (var resolution in availableResolutions)
        {
            TMP_Dropdown.OptionData newResolution = new($"{resolution.width}x{resolution.height}");

            if (newResolutions.Exists(res => newResolution.Equals(res)))
            {
                continue;
            }

            newResolutions.Add(newResolution);
        }

        targetResolution.options = newResolutions;
    }

    private void UpdateResolution(int resolutionIndex)
    {
        var resolution = availableResolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreenMode);

        GetNewResolutions();
    }

    private void UpdateFramerateVisual()
    {
        var index = framerateLimit.options.FindIndex(option => option.text.Contains(Application.targetFrameRate.ToString()));
        Debug.Log(Application.targetFrameRate);
        Debug.Log(index);
        framerateLimit.value = index;
    }

    private void SetTargetFramerate(int index)
    {
        if (!int.TryParse(framerateLimit.options[index].text, out int result))
        {
            Application.targetFrameRate = -1;
            return;
        }

        Application.targetFrameRate = result;
    }
}
