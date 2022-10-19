using System;
using System.IO;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

[Serializable]
public class SaveData
{
    public float VolumeSfx;
    public float VolumeBgm;
    public PixelScaler.ScaleMode ScaleMode;
    public bool ClosedCaption;

    public SaveData()
    {
        VolumeSfx = 0.8f;
        VolumeBgm = 0.6f;
        ScaleMode = PixelScaler.ScaleMode.SharpBilinear;
        ClosedCaption = false;
    }
}

public class OptionsManager : MonoBehaviour
{
    public PixelScaler PixelScaler;
    public AudioMixer Mixer;

    public Toggle[] ScaleModeToggles;
    public Slider VolumeSfxSlider;
    public Slider VolumeBgmSlider;
    public Toggle ClosedCaptionToggle;

    public GameObject CaptionPanel;
    public RectTransform TrackPanel;

    public SaveData SaveData;

    public void Awake()
    {
        LoadSaveData();
        Application.quitting += SaveSaveData;
    }

    private void SetVolume(string parameter, float value)
    {
        Mixer.SetFloat(parameter, Mathf.Log10(value) * 20);
    }

    private void LoadSaveData()
    {
        if (File.Exists(Application.persistentDataPath + "/save.json"))
        {
            var saveData = JsonUtility.FromJson<SaveData>(File.ReadAllText(Application.persistentDataPath + "/save.json"));
            SaveData = saveData;
        }
        else
        {
            SaveData = new SaveData();
        }
    }

    private void SaveSaveData()
    {
        File.WriteAllText(Application.persistentDataPath + "/save.json", JsonUtility.ToJson(SaveData));
    }

    void Start()
    {
        VolumeSfxSlider.SetValueWithoutNotify(SaveData.VolumeSfx);
        VolumeBgmSlider.SetValueWithoutNotify(SaveData.VolumeBgm);

        PixelScaler.settings.scaleMode = SaveData.ScaleMode;

        VolumeSfxSlider.onValueChanged.AddListener((value) =>
        {
            SetVolume("Sfx", value);
            SaveData.VolumeSfx = value;
        });
        VolumeBgmSlider.onValueChanged.AddListener((value) =>
        {
            SetVolume("Bgm", value);
            SaveData.VolumeBgm = value;
        });

        ClosedCaptionToggle.SetIsOnWithoutNotify(SaveData.ClosedCaption);
        CaptionPanel.SetActive(SaveData.ClosedCaption);
        TrackPanel.anchoredPosition = new Vector2(0, SaveData.ClosedCaption ? 8 : 16);
        SaveData.ClosedCaption = SaveData.ClosedCaption;
        ClosedCaptionToggle.onValueChanged.AddListener((value) =>
        {
            CaptionPanel.SetActive(value);
            TrackPanel.anchoredPosition = new Vector2(0, value ? 8 : 16);
            SaveData.ClosedCaption = value;
        });

        SetVolume("Sfx", SaveData.VolumeSfx);
        SetVolume("Bgm", SaveData.VolumeBgm);

        foreach (var toggle in ScaleModeToggles)
        {
            if (toggle.name == SaveData.ScaleMode.ToString())
            {
                toggle.SetIsOnWithoutNotify(true);
            }

            toggle.onValueChanged.AddListener((on) =>
            {
                if (!on) return;
                if (Enum.TryParse(typeof(PixelScaler.ScaleMode), toggle.name, out var result))
                {
                    PixelScaler.settings.scaleMode = (PixelScaler.ScaleMode)result;
                    SaveData.ScaleMode = PixelScaler.settings.scaleMode;
                }
            });
        }
    }

    public void Quit()
    {
        Application.Quit();
    }
}
