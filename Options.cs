using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Options : MonoBehaviour
{
    public static Options _Instance;
    public bool _IsDebugAllowed { get; private set; }
    public bool _IsLastInputFromGamepadForAim { get; set; }
    public float _SoundVolume { get; private set; }
    public float _MusicVolume { get; private set; }

    private Slider _SoundSlider;
    private Slider _MusicSlider;

    private System.Diagnostics.Stopwatch _stopWatch;

    public int _LoadedGamesCount { get; set; }

    #region Game Settings
    public int _Quality { get; set; }
    public bool _IsExpressionPlayerEnabled { get; set; }
    public bool _IsFootIKEnabled { get; set; }
    public bool _IsLeaningEnabled { get; set; }
    public bool _IsLookForCamDistanceEnabled { get; set; }
    #endregion

    private void Awake()
    {
#if UNITY_EDITOR
        Debug.unityLogger.logEnabled = true;
#else
        Debug.unityLogger.logEnabled = false;
#endif
        _Instance = this;
        _SoundVolume = PlayerPrefs.GetFloat("Sound", 0.33f);
        _MusicVolume = PlayerPrefs.GetFloat("Music", 0.33f);
        _LoadedGamesCount = PlayerPrefs.GetInt("LoadedGames", 0);
        _Quality = PlayerPrefs.GetInt("Quality", 0);
        SetGraphicSetting(_Quality);
        //PlayerPrefs.SetInt("LoadedGames", 0);

        _SoundSlider = GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("Options").Find("SoundSlider").GetComponentInChildren<Slider>();
        _MusicSlider = GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("Options").Find("MusicSlider").GetComponentInChildren<Slider>();

        _SoundSlider.value = _SoundVolume;
        _MusicSlider.value = _MusicVolume;

        _IsExpressionPlayerEnabled = false;
        _IsFootIKEnabled = true;
        _IsLeaningEnabled = false;
        _IsLookForCamDistanceEnabled = true;
    }
    private void Start()
    {
        ChangeExpressionPlayerSetting(_IsExpressionPlayerEnabled);
        ChangeFootIKSetting(_IsFootIKEnabled);
        ChangeLeaningSetting(_IsLeaningEnabled);
    }
    private void Update()
    {
        if (Gamepad.current == null || (GameManager._Instance._GameHUD != null && GameManager._Instance._GameHUD.activeInHierarchy))
        {
            if (GamepadMouse._Instance._CursorRect.gameObject.activeInHierarchy)
                GamepadMouse._Instance._CursorRect.gameObject.SetActive(false);
        }
        else
        {
            if (!GamepadMouse._Instance._CursorRect.gameObject.activeInHierarchy)
                GamepadMouse._Instance._CursorRect.gameObject.SetActive(true);
        }
    }
    public void StartDebugWatch()
    {
        if (_stopWatch != null)
            _stopWatch.Restart();
        else
            _stopWatch = System.Diagnostics.Stopwatch.StartNew();
    }
    public void StopDebugWatch()
    {
        if (_stopWatch == null) return;

        _stopWatch.Stop();
        Debug.Log($"Elapsed: {_stopWatch.ElapsedMilliseconds} ms");
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
    public void ChangeExpressionPlayerSetting(bool isOpening)
    {
        _IsExpressionPlayerEnabled = isOpening;
        for (int i = 0; i < NPCManager._Instance._AllNPCs.Count; i++)
        {
            if (NPCManager._Instance._AllNPCs[i]._ExpressionPlayer != null)
            {
                NPCManager._Instance._AllNPCs[i]._ExpressionPlayer.enabled = isOpening;
                NPCManager._Instance._AllNPCs[i]._ExpressionPlayer.GetComponent<UMA.TwistBones>().enabled = isOpening;
            }
        }
    }
    public void ChangeFootIKSetting(bool isOpening)
    {
        _IsFootIKEnabled = isOpening;
        for (int i = 0; i < NPCManager._Instance._AllNPCs.Count; i++)
        {
            if (NPCManager._Instance._AllNPCs[i]._FootIKComponent != null)
                NPCManager._Instance._AllNPCs[i]._FootIKComponent.enabled = isOpening;
        }
    }
    public void ChangeLeaningSetting(bool isOpening)
    {
        _IsLeaningEnabled = isOpening;
        for (int i = 0; i < NPCManager._Instance._AllNPCs.Count; i++)
        {
            if (NPCManager._Instance._AllNPCs[i]._LeaninganimatorComponent != null)
                NPCManager._Instance._AllNPCs[i]._LeaninganimatorComponent.enabled = isOpening;
        }
    }
    public void SetGraphicSetting(int number)
    {
        _Quality = number;
        PlayerPrefs.SetInt("Quality", _Quality);
        QualitySettings.SetQualityLevel(number);
        ArrangeGraphics();
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
                sound.GetComponent<AudioSource>().volume = newValue * sound.transform.localEulerAngles.x;
            }
        }

        if (SoundManager._Instance != null && SoundManager._Instance._CurrentAtmosphereObject != null)
        {
            SoundManager._Instance._CurrentAtmosphereObject.GetComponent<AudioSource>().volume = 0f;
        }

        if (SoundManager._Instance != null)
        {
            if (SoundManager._Instance._RainAudioSource != null)
                SoundManager._Instance._RainAudioSource.volume = newValue * SoundManager._Instance._RainAudioSource.transform.localEulerAngles.x;
            if (SoundManager._Instance._SnowAudioSource != null)
                SoundManager._Instance._SnowAudioSource.volume = newValue * SoundManager._Instance._SnowAudioSource.transform.localEulerAngles.x;
            if (SoundManager._Instance._ThunderAudioSource != null)
                SoundManager._Instance._ThunderAudioSource.volume = newValue * SoundManager._Instance._ThunderAudioSource.transform.localEulerAngles.x;
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