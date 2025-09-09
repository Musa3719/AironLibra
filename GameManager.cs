using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager _Instance;

    public GameObject _MainCamera { get; private set; }
    public GameObject _StopScreen { get; private set; }
    public GameObject _MapScreen { get; private set; }
    public GameObject _InGameScreen { get; private set; }
    public GameObject _OptionsScreen { get; private set; }
    public GameObject _LoadScreen { get; private set; }
    public GameObject _SaveScreen { get; private set; }
    public GameObject _LoadingObject { get; private set; }

    public bool _IsGameStopped { get; private set; }

    public int _LevelIndex { get; private set; }

    public LayerMask _TerrainAndWaterMask;
    public LayerMask _TerrainAndSolidMask;
    public LayerMask _SolidAndHumanMask;


    private Language _lastActiveLanguage;

    private int _lastDeleteSaveIndex;

    private Coroutine _slowTimeCoroutine;

    private void Awake()
    {
        _Instance = this;
        _MainCamera = Camera.main.gameObject;

        //Application.targetFrameRate = 60;
        _OptionsScreen = GameObject.FindGameObjectWithTag("UI").transform.Find("Options").gameObject;
        _LoadingObject = GameObject.FindGameObjectWithTag("UI").transform.Find("Loading").gameObject;
        _LoadScreen = GameObject.FindGameObjectWithTag("UI").transform.Find("Load").gameObject;

        _LevelIndex = SceneManager.GetActiveScene().buildIndex;
        if (_LevelIndex != 0)
        {
            _StopScreen = GameObject.FindGameObjectWithTag("UI").transform.Find("StopScreen").gameObject;
            _MapScreen = GameObject.FindGameObjectWithTag("UI").transform.Find("MapScreen").gameObject;
            _InGameScreen = GameObject.FindGameObjectWithTag("UI").transform.Find("InGameScreen").gameObject;
            _SaveScreen = GameObject.FindGameObjectWithTag("UI").transform.Find("Save").gameObject;
        }
    }
    private void Update()
    {
        if (Input.GetButtonDown("Map") && _LevelIndex != 0)
        {
            if (_IsGameStopped)
            {
                if (_MapScreen.activeInHierarchy)
                    UnstopGame();
            }
            else
            {
                StopGame(true, false);
            }
        }

        if (Input.GetKeyDown(KeyCode.BackQuote) && _LevelIndex != 0)
        {
            if (_IsGameStopped && !_StopScreen.activeInHierarchy)
            {
                UnstopGame();
            }
            else
            {
                StopGame(false, true);
            }
        }

        if (Input.GetButtonDown("Esc"))
        {
            if (_LevelIndex != 0)
            {
                if (_IsGameStopped)
                {
                    if (_OptionsScreen.activeInHierarchy)
                        CloseOptionsScreen();
                    else if (_LoadScreen.activeInHierarchy)
                        CloseLoadScreen();
                    else if (_SaveScreen.activeInHierarchy)
                        CloseSaveScreen();
                    else if (_StopScreen.activeInHierarchy)
                        UnstopGame();
                    else
                        StopGame(false, false);
                }
                else
                {
                    StopGame(false, false);
                }
            }
            else
            {
                if (_OptionsScreen.activeInHierarchy)
                    CloseOptionsScreen();
                else if (_LoadScreen.activeInHierarchy)
                    CloseLoadScreen();
            }
        }

        if (Input.GetButtonDown("Language"))
        {
            if (Localization._Instance._ActiveLanguage != Language.EN)
            {
                _lastActiveLanguage = Localization._Instance._ActiveLanguage;
                Localization._Instance.SetLanguage(Language.EN);
            }
            else if (_lastActiveLanguage != Language.EN)
            {
                Localization._Instance.SetLanguage(_lastActiveLanguage);
            }
        }
    }

    #region CommonMethods

    public T GetRandomFromList<T>(List<T> list)
    {
        return list[UnityEngine.Random.Range(0, list.Count)];
    }
    public static void ShuffleList(List<string> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            string value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
    /// <param name="speed">1/second</param>
    public float LinearLerpFloat(float startValue, float endValue, float speed, float startTime)
    {
        float endTime = startTime + 1 / speed;
        return Mathf.Lerp(startValue, endValue, (Time.time - startTime) / (endTime - startTime));
    }
    /// <param name="speed">1/second</param>
    public Vector2 LinearLerpVector2(Vector2 startValue, Vector2 endValue, float speed, float startTime)
    {
        float endTime = startTime + 1 / speed;
        return Vector2.Lerp(startValue, endValue, (Time.time - startTime) / (endTime - startTime));
    }
    /// <param name="speed">1/second</param>
    public Vector3 LinearLerpVector3(Vector3 startValue, Vector3 endValue, float speed, float startTime)
    {
        float endTime = startTime + 1 / speed;
        return Vector3.Lerp(startValue, endValue, (Time.time - startTime) / (endTime - startTime));
    }

    /// <param name="speed">1/second</param>
    public float LimitLerpFloat(float startValue, float endValue, float speed)
    {
        if (endValue - startValue != 0f)
            return Mathf.Lerp(startValue, endValue, Time.deltaTime * speed * 7f * (endValue - startValue));
        return endValue;
    }
    /// <param name="speed">1/second</param>
    public Vector2 LimitLerpVector2(Vector2 startValue, Vector2 endValue, float speed)
    {
        if ((endValue - startValue).magnitude != 0f)
            return Vector2.Lerp(startValue, endValue, Time.deltaTime * speed * 7f / (endValue - startValue).magnitude);
        return endValue;
    }
    /// <param name="speed">1/second</param>
    public Vector3 LimitLerpVector3(Vector3 startValue, Vector3 endValue, float speed)
    {
        if ((endValue - startValue).magnitude != 0f)
            return Vector3.Lerp(startValue, endValue, Time.deltaTime * speed * 7f / (endValue - startValue).magnitude);
        return endValue;
    }

    public bool RandomPercentageChance(float percentage)
    {
        return percentage >= UnityEngine.Random.Range(1f, 99f);
    }

    public void CoroutineCall(ref Coroutine coroutine, IEnumerator method, MonoBehaviour script)
    {
        if (coroutine != null)
            script.StopCoroutine(coroutine);
        coroutine = script.StartCoroutine(method);
    }
    public void CallForAction(System.Action action, float time)
    {
        StartCoroutine(CallForActionCoroutine(action, time));
    }
    private IEnumerator CallForActionCoroutine(System.Action action, float time)
    {
        yield return new WaitForSeconds(time);
        action?.Invoke();
    }
    public Transform GetParent(Transform tr)
    {
        Transform parentTransform = tr.transform;
        while (parentTransform.parent != null)
        {
            parentTransform = parentTransform.parent;
        }
        return parentTransform;
    }
    public Vector3 RotateVector3OnYAxis(Vector3 baseVector, float angle)
    {
        return Quaternion.AngleAxis(angle, Vector3.up) * baseVector;

    }
    public void BufferActivated(ref bool buffer, MonoBehaviour coroutineHolderScript, ref Coroutine coroutine)
    {
        buffer = false;
        if (coroutine != null)
            coroutineHolderScript.StopCoroutine(coroutine);
    }
    #endregion
    public void LoadScene(int index)
    {
        LoadSceneAsync(index);
    }
    public void NextScene()
    {
        LoadSceneAsync(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void RestartLevel()
    {
        LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
    }
    public void LoadSceneWithSave(int number)
    {
        SaveSystemHandler._Instance.ReadFile(number);
        LoadSceneAsync(1);
    }
    public void LoadSceneAsync(int index)
    {
        if (_LoadingObject.activeInHierarchy) return;

        _LoadingObject.SetActive(true);
        CallForAction(() => SceneManager.LoadSceneAsync(index), 0.25f);
    }
    public void LoadSavedDataToGame()
    {
        //Day = GameDataSave.Instance.SavedGameData.Day;
    }
    public void SaveGame(int index)
    {
        if (index == Options._Instance._LoadedGamesCount)
        {
            Options._Instance._LoadedGamesCount += 1;
            PlayerPrefs.SetInt("LoadedGames", Options._Instance._LoadedGamesCount);
        }

        SaveSystemHandler._Instance.WriteFile(index);

        CloseSaveScreen();
    }

    public void DeleteSave()
    {
        SaveSystemHandler._Instance.DeleteFile(_lastDeleteSaveIndex);

        if (Options._Instance._LoadedGamesCount > 0)
        {
            Options._Instance._LoadedGamesCount -= 1;
            PlayerPrefs.SetInt("LoadedGames", Options._Instance._LoadedGamesCount);
        }

        CloseLoadScreen();
        OpenLoadScreen();
    }
    public void ToMenu()
    {
        if (SoundManager._Instance._CurrentMusicObject != null)
        {
            Destroy(SoundManager._Instance._CurrentMusicObject);
        }
        if (SoundManager._Instance._CurrentAtmosphereObject != null)
        {
            Destroy(SoundManager._Instance._CurrentAtmosphereObject);
        }
        CallForAction(() => LoadSceneAsync(0), 0.25f);
    }
    public void QuitGame()
    {
        Application.Quit();
    }

    public void PlayButtonSound()
    {
        SoundManager._Instance.PlaySound(SoundManager._Instance._Button, _MainCamera.transform.position, 0.15f, false, UnityEngine.Random.Range(0.9f, 1.1f));
    }

    public void OpenDeleteSaveScreen(int index)
    {
        GameObject.FindGameObjectWithTag("UI").transform.Find("Load").Find("DeleteSaveScreen").gameObject.SetActive(true);
        _lastDeleteSaveIndex = index;
    }
    public void CloseDeleteSaveScreen()
    {
        GameObject.FindGameObjectWithTag("UI").transform.Find("Load").Find("DeleteSaveScreen").gameObject.SetActive(false);
    }

    private void StopGame(bool isOpeningMap, bool isPausing)
    {
        if (isOpeningMap)
        {
            _MapScreen.SetActive(true);
            _InGameScreen.SetActive(false);
        }
        else if (!isPausing)
        {
            _StopScreen.SetActive(true);
            _InGameScreen.SetActive(false);
        }
        Time.timeScale = 0f;
        _IsGameStopped = true;
        SoundManager._Instance.PauseAllSound();
        //SoundManager.Instance.PauseMusic();
    }
    public void UnstopGame()
    {
        _StopScreen.SetActive(false);
        _MapScreen.SetActive(false);
        _InGameScreen.SetActive(true);
        CloseOptionsScreen(false);
        _IsGameStopped = false;
        Time.timeScale = 1f;
        SoundManager._Instance.ContinueAllSound();
        //SoundManager.Instance.ContinueMusic();
    }

    public void OpenOptionsScreen()
    {
        if (GameManager._Instance._LevelIndex == 0)
        {
            GameObject.FindGameObjectWithTag("UI").transform.Find("Options").gameObject.SetActive(true);
            GameObject.FindGameObjectWithTag("UI").transform.Find("MainMenu").gameObject.SetActive(false);
        }
        else
        {
            GameObject.FindGameObjectWithTag("UI").transform.Find("Options").gameObject.SetActive(true);
            GameObject.FindGameObjectWithTag("UI").transform.Find("StopScreen").gameObject.SetActive(false);
        }
    }
    public void CloseOptionsScreen(bool isOpeningMenu = true)
    {
        if (GameManager._Instance._LevelIndex == 0)
        {
            GameObject.FindGameObjectWithTag("UI").transform.Find("Options").gameObject.SetActive(false);
            if (isOpeningMenu)
                GameObject.FindGameObjectWithTag("UI").transform.Find("MainMenu").gameObject.SetActive(true);
        }
        else
        {
            GameObject.FindGameObjectWithTag("UI").transform.Find("Options").gameObject.SetActive(false);
            if (isOpeningMenu)
                GameObject.FindGameObjectWithTag("UI").transform.Find("StopScreen").gameObject.SetActive(true);
        }
    }
    public void OpenLoadScreen()
    {
        if (GameManager._Instance._LevelIndex == 0)
        {
            GameObject.FindGameObjectWithTag("UI").transform.Find("Load").gameObject.SetActive(true);
            GameObject.FindGameObjectWithTag("UI").transform.Find("MainMenu").gameObject.SetActive(false);
        }
        else
        {
            GameObject.FindGameObjectWithTag("UI").transform.Find("Load").gameObject.SetActive(true);
            GameObject.FindGameObjectWithTag("UI").transform.Find("StopScreen").gameObject.SetActive(false);
        }

        GameObject created = null;
        for (int i = 0; i < Options._Instance._LoadedGamesCount; i++)
        {
            int c = i;
            created = Instantiate(PrefabHolder._Instance._LoadPrefab, GameObject.FindGameObjectWithTag("UI").transform.Find("Load").Find("Saves"));
            created.GetComponent<Button>().onClick.AddListener(() => PlayButtonSound());
            created.GetComponent<Button>().onClick.AddListener(() => LoadSceneWithSave(c));
            created.transform.Find("DeleteButton").GetComponent<Button>().onClick.AddListener(() => PlayButtonSound());
            created.transform.Find("DeleteButton").GetComponent<Button>().onClick.AddListener(() => OpenDeleteSaveScreen(c));
        }
    }
    public void CloseLoadScreen()
    {
        for (int i = 0; i < GameObject.FindGameObjectWithTag("UI").transform.Find("Load").Find("Saves").transform.childCount; i++)
        {
            Destroy(GameObject.FindGameObjectWithTag("UI").transform.Find("Load").Find("Saves").transform.GetChild(i).gameObject);
        }

        if (GameManager._Instance._LevelIndex == 0)
        {
            GameObject.FindGameObjectWithTag("UI").transform.Find("MainMenu").gameObject.SetActive(true);
        }
        else
        {
            GameObject.FindGameObjectWithTag("UI").transform.Find("StopScreen").gameObject.SetActive(true);
        }

        GameObject.FindGameObjectWithTag("UI").transform.Find("Load").gameObject.SetActive(false);
        GameObject.FindGameObjectWithTag("UI").transform.Find("Load").Find("DeleteSaveScreen").gameObject.SetActive(false);

    }
    public void OpenSaveScreen()
    {
        if (GameManager._Instance._LevelIndex == 0)
        {
            GameObject.FindGameObjectWithTag("UI").transform.Find("Save").gameObject.SetActive(true);
            GameObject.FindGameObjectWithTag("UI").transform.Find("MainMenu").gameObject.SetActive(false);
        }
        else
        {
            GameObject.FindGameObjectWithTag("UI").transform.Find("Save").gameObject.SetActive(true);
            GameObject.FindGameObjectWithTag("UI").transform.Find("StopScreen").gameObject.SetActive(false);
        }

        GameObject created = null;
        for (int i = 0; i < Options._Instance._LoadedGamesCount + 1; i++)
        {
            int c = i;
            created = Instantiate(PrefabHolder._Instance._SavePrefab, GameObject.FindGameObjectWithTag("UI").transform.Find("Save").Find("Saves"));
            created.GetComponent<Button>().onClick.AddListener(() => PlayButtonSound());
            created.GetComponent<Button>().onClick.AddListener(() => SaveGame(c));
        }
    }
    public void CloseSaveScreen()
    {
        for (int i = 0; i < GameObject.FindGameObjectWithTag("UI").transform.Find("Save").Find("Saves").transform.childCount; i++)
        {
            Destroy(GameObject.FindGameObjectWithTag("UI").transform.Find("Save").Find("Saves").transform.GetChild(i).gameObject);
        }

        if (GameManager._Instance._LevelIndex == 0)
        {
            GameObject.FindGameObjectWithTag("UI").transform.Find("Save").gameObject.SetActive(false);
            GameObject.FindGameObjectWithTag("UI").transform.Find("MainMenu").gameObject.SetActive(true);
        }
        else
        {
            GameObject.FindGameObjectWithTag("UI").transform.Find("Save").gameObject.SetActive(false);
            GameObject.FindGameObjectWithTag("UI").transform.Find("StopScreen").gameObject.SetActive(true);
        }
    }
    public void Slowtime(float time)
    {
        CoroutineCall(ref _slowTimeCoroutine, SlowTimeCoroutine(time), this);
    }
    private IEnumerator SlowTimeCoroutine(float time)
    {
        SoundManager._Instance.SlowDownAllSound();

        float targetTimeScale = 0.2f;
        float slowInAndOutTime = 0.5f;

        float startTime = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - startTime < slowInAndOutTime)
        {
            Time.timeScale = Mathf.Lerp(Time.timeScale, targetTimeScale, (Time.realtimeSinceStartup - startTime) / slowInAndOutTime);
        }
        Time.timeScale = targetTimeScale;

        yield return new WaitForSecondsRealtime(time);

        startTime = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - startTime < slowInAndOutTime)
        {
            Time.timeScale = Mathf.Lerp(Time.timeScale, 1f, (Time.realtimeSinceStartup - startTime) / slowInAndOutTime);
        }
        Time.timeScale = 1f;

        SoundManager._Instance.UnSlowDownAllSound();
    }
    public string GetRandomCivilianName(bool isMale)
    {
        string path = "";
        if (isMale)
        {
            path = Application.streamingAssetsPath + "/NamesMan.txt";
        }
        else
        {
            path = Application.streamingAssetsPath + "/NamesWoman.txt";
        }
        StreamReader reader = new StreamReader(path);
        string line = "";
        List<string> list = new List<string>();
        while ((line = reader.ReadLine()) != null)
        {
            list.Add(line);
        }
        reader.Close();
        return list[Random.Range(0, list.Count)];
    }
}
