using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Options : MonoBehaviour
{
    public static Options _Instance;
    public float _SoundVolume { get; private set; }
    public float _MusicVolume { get; private set; }

    private Slider _SoundSlider;
    private Slider _MusicSlider;

    public int _LoadedGamesCount;

    private void Awake()
    {
        Debug.unityLogger.logEnabled = Debug.isDebugBuild;
        _Instance = this;
        _SoundVolume = PlayerPrefs.GetFloat("Sound", 0.33f);
        _MusicVolume = PlayerPrefs.GetFloat("Music", 0.33f);
        _LoadedGamesCount = PlayerPrefs.GetInt("LoadedGames", 0);
        //PlayerPrefs.SetInt("LoadedGames", 0);

        _SoundSlider = GameObject.FindGameObjectWithTag("UI").transform.Find("Options").Find("SoundSlider").GetComponentInChildren<Slider>();
        _MusicSlider = GameObject.FindGameObjectWithTag("UI").transform.Find("Options").Find("MusicSlider").GetComponentInChildren<Slider>();

        _SoundSlider.value = _SoundVolume;
        _MusicSlider.value = _MusicVolume;
    }
    private void Start()
    {
        ArrangeGraphics();
    }
    public void SoundVolumeChanged(float newValue)
    {
        _SoundVolume = newValue;
        PlayerPrefs.SetFloat("Sound", newValue);
        ArrangeActiveSoundVolumes(newValue);
    }
    public void MusicVolumeChanged(float newValue)
    {
        _MusicVolume = newValue;
        PlayerPrefs.SetFloat("Music", newValue);
        ArrangeActiveMusicVolumes(newValue);
        if (SoundManager._Instance != null)
            SoundManager._Instance.ContinueMusic();
    }

    public void SetGraphicSetting(int number)
    {
        QualitySettings.SetQualityLevel(number);
    }
    public void ArrangeGraphics()
    {
        int quality = QualitySettings.GetQualityLevel();

        if (SceneManager.GetActiveScene().buildIndex != 0)
        {
            Camera cam = Camera.main;
            
            if (quality == 0)
            {
                cam.farClipPlane = 135f;
            }
            else if (quality == 1)
            {
                cam.farClipPlane = 270f;
            }
            else if (quality == 2)
            {
                cam.farClipPlane = 400f;
            }
        }

    }

    private void ArrangeActiveSoundVolumes(float newValue)
    {
        if (SoundManager._Instance != null && SoundManager._Instance._SoundObjectsParent != null)
        {
            foreach (Transform sound in SoundManager._Instance._SoundObjectsParent.transform)
            {
                if (newValue != 0f)
                    sound.GetComponent<AudioSource>().volume = newValue * sound.transform.localEulerAngles.x;
            }
        }

        if (SoundManager._Instance != null && SoundManager._Instance._CurrentAtmosphereObject != null)
        {
            if (newValue != 0f)
                SoundManager._Instance._CurrentAtmosphereObject.GetComponent<AudioSource>().volume = newValue * SoundManager._Instance._CurrentAtmosphereObject.transform.localEulerAngles.x;
            else
                SoundManager._Instance._CurrentAtmosphereObject.GetComponent<AudioSource>().volume = 0f;
        }
    }
    private void ArrangeActiveMusicVolumes(float newValue)
    {
        if (SoundManager._Instance != null && SoundManager._Instance._CurrentMusicObject != null)
        {
            if (newValue != 0f)
                SoundManager._Instance._CurrentMusicObject.GetComponent<AudioSource>().volume = newValue * SoundManager._Instance._CurrentMusicObject.transform.localEulerAngles.x;
            else
                SoundManager._Instance._CurrentMusicObject.GetComponent<AudioSource>().volume = 0f;
        }
    }
}