using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using Unity.AI.Navigation;
using UMA;
using UMA.CharacterSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager _Instance;
    public float _TerrainDimensionMagnitude => 1024f;
    public int _NumberOfColumnsForTerrains => 1;
    public int _NumberOfRowsForTerrains => 1;
    public List<AssetReferenceGameObject>[,] _ObjectsInChunk;
    public List<Vector3>[,] _ObjectPositionsInChunk;
    //chest data
    //npc data

    public GameObject _ListenerObj { get { if (_Player == null) return _MainCamera; return _Player; } }
    public GameObject _MainCamera { get; private set; }
    public GameObject _Player { get; private set; }
    public GameObject _StopScreen { get; private set; }
    public GameObject _InGameMenu { get; private set; }
    public GameObject _InventoryScreen { get; private set; }
    public GameObject _MapScreen { get; private set; }
    public GameObject _DialogueScreen { get; private set; }
    public GameObject _GameHUD { get; private set; }
    public GameObject _OptionsScreen { get; private set; }
    public GameObject _LoadScreen { get; private set; }
    public GameObject _SaveScreen { get; private set; }
    public GameObject _LoadingObject { get; private set; }
    public ObjectPool _NPCPool { get; private set; }

    public bool _IsGameStopped { get; private set; }
    public int _InGameMenuNumber { get; private set; }

    public int _LevelIndex { get; private set; }

    public LayerMask _TerrainSolidAndWaterMask;
    public LayerMask _TerrainAndWaterMask;
    public LayerMask _TerrainAndSolidMask;
    public LayerMask _SolidAndHumanMask;

    private List<string> _maleDnaNames;
    private List<string> _femaleDnaNames;

    private Language _lastActiveLanguage;

    private int _lastDeleteSaveIndex;

    private Coroutine _slowTimeCoroutine;

    private float _snowSettingsTimer;
    private bool _isSnowing;
    private bool _isRaining;
    private float _isSnowingTimer;
    private float _isRainingTimer;

    private void Awake()
    {
        transform.Find("CharacterCreation").GetComponent<CharacterCreation>().Init();
        Shader.EnableKeyword("_USEGLOBALSNOWLEVEL");
        Shader.EnableKeyword("_PW_GLOBAL_COVER_LAYER");
        Shader.EnableKeyword("_PW_COVER_ENABLED");
        NPCManager._AllNPCs = new List<NPC>();

        _maleDnaNames = UMAGlobalContext.Instance.GetRace("HumanMale").GetDNANames();
        _femaleDnaNames = UMAGlobalContext.Instance.GetRace("HumanFemale").GetDNANames();

        NPCManager._Comparer = new NPCDistanceComparer();
        _Instance = this;
        _ObjectsInChunk = new List<AssetReferenceGameObject>[_NumberOfColumnsForTerrains, _NumberOfRowsForTerrains];
        _ObjectPositionsInChunk = new List<Vector3>[_NumberOfColumnsForTerrains, _NumberOfRowsForTerrains];
        _MainCamera = Camera.main.gameObject;
        _Player = GameObject.FindGameObjectWithTag("Player");

        //Application.targetFrameRate = 60;
        _OptionsScreen = GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("Options").gameObject;
        _LoadingObject = GameObject.FindGameObjectWithTag("UI").transform.Find("Loading").gameObject;
        _LoadScreen = GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("Load").gameObject;

        _LevelIndex = SceneManager.GetActiveScene().buildIndex;
        if (_LevelIndex != 0)
        {
            _StopScreen = GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("StopScreen").gameObject;
            _InGameMenu = GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("InGameMenu").gameObject;
            _InventoryScreen = GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("InGameMenu").Find("InventoryScreen").gameObject;
            _MapScreen = GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("InGameMenu").Find("MapScreen").gameObject;
            _DialogueScreen = GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("InGameMenu").Find("DialogueScreen").gameObject;
            _GameHUD = GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("InGameScreen").gameObject;
            _SaveScreen = GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("Save").gameObject;

            _NPCPool = transform.Find("NPC_Pool").GetComponent<ObjectPool>();
        }

    }
    private void Start()
    {
        if (_LevelIndex != 0)
            SaveSystemHandler._Instance.LoadGame(SaveSystemHandler._Instance._ActiveSave);
        else
            Time.timeScale = 1f;

    }

    private void Update()
    {
        if (_LevelIndex != 0)
        {
            NPCManager.Update();

            if (Gaia.ProceduralWorldsGlobalWeather.Instance.IsSnowing)
            {
                _isSnowing = true;
                _isSnowingTimer = 0f;
            }
            else
            {
                if (_isSnowingTimer < 20f)
                    _isSnowingTimer += Time.deltaTime;
                else
                    _isSnowing = false;
            }
            if (Gaia.ProceduralWorldsGlobalWeather.Instance.IsRaining)
            {
                _isRaining = true;
                _isRainingTimer = 0f;
            }
            else
            {
                if (_isRainingTimer < 25f)
                    _isRainingTimer += Time.deltaTime;
                else
                    _isRaining = false;
            }

            if (_snowSettingsTimer > 0.2f)
            {
                Shader.SetGlobalFloat("_Global_SnowLevel", Mathf.MoveTowards(Shader.GetGlobalFloat("_Global_SnowLevel"), _isSnowing ? 0.85f : (_isRaining ? 0.3f : 0f), 0.02f * ((_isSnowing || _isRaining) ? 0.4f : 0.15f)));
                _snowSettingsTimer = 0f;
            }
            else
                _snowSettingsTimer += Time.deltaTime;
        }

        if (M_Input.GetButtonDown("InGameMenu") && _LevelIndex != 0)
        {
            if (_IsGameStopped)
            {
                if (_InGameMenu.activeInHierarchy)
                {
                    UnstopGame();
                    CloseInGameMenuScreen();
                }
            }
            else
            {
                StopGame(true, false);
                OpenInGameMenuScreen();
            }
        }
        if (_InGameMenu.activeInHierarchy)
        {
            if (M_Input.GetButtonDown("UIRight"))
            {
                _InGameMenuNumber++;
                _InGameMenuNumber = _InGameMenuNumber % 3;
                if (_InGameMenuNumber < 0)
                    _InGameMenuNumber = 2;
                CloseAllInGameMenus();
                OpenInGameMenu(_InGameMenuNumber);
            }
            else if (M_Input.GetButtonDown("UILeft"))
            {
                _InGameMenuNumber--;
                _InGameMenuNumber = _InGameMenuNumber % 3;
                if (_InGameMenuNumber < 0)
                    _InGameMenuNumber = 2;
                CloseAllInGameMenus();
                OpenInGameMenu(_InGameMenuNumber);
            }
        }
        if (M_Input.GetButtonDown("Esc"))
        {
            if (_LevelIndex != 0)
            {
                if (_IsGameStopped)
                {
                    if (_LoadingObject.activeInHierarchy) return;

                    if (_InGameMenu.activeInHierarchy)
                    {
                        CloseInGameMenuScreen();
                        UnstopGame();
                    }
                    else if (_OptionsScreen.activeInHierarchy)
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


    }

    #region CommonMethods

    public Vector3 Vector2ToVector3(Vector2 vec)
    {
        return new Vector3(vec.x, 0f, vec.y);
    }
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
        return percentage >= UnityEngine.Random.Range(float.MinValue, 100f);
    }

    public void CoroutineCall(ref Coroutine coroutine, IEnumerator method, MonoBehaviour script)
    {
        if (coroutine != null)
            script.StopCoroutine(coroutine);
        coroutine = script.StartCoroutine(method);
    }
    public void CallForAction(System.Action action, float time, bool isRealtime = false)
    {
        StartCoroutine(CallForActionCoroutine(action, time, isRealtime));
    }
    private IEnumerator CallForActionCoroutine(System.Action action, float time, bool isRealtime)
    {
        if (isRealtime)
            yield return new WaitForSecondsRealtime(time);
        else
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
    public float GetTerrainOrWaterHeightOnPosition(Vector3 pos)
    {
        Physics.Raycast(pos + Vector3.up * 1500f, -Vector3.up, out RaycastHit hit, 2000f, _TerrainAndWaterMask);
        if (hit.collider != null)
            return hit.point.y;

        Debug.LogError("TerrainOrWater Ray Did not Hit!");
        return pos.y;
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

    public void PauseGame()
    {
        if (!_IsGameStopped)
        {
            StopGame(false, true);
        }
    }
    public void UnPauseGame()
    {
        if (_IsGameStopped && !_StopScreen.activeInHierarchy)
        {
            UnstopGame();
        }
    }


    public void LoadChunk(int x, int y)
    {
        AddressablesController._Instance.LoadTerrainObjects(x, y);
        //load chests
        AddressablesController._Instance.SpawnNpcs(x, y);
        AddressablesController._Instance._IsChunkLoadedToScene[x, y] = true;
    }
    public void UnloadChunk(int x, int y)
    {
        AddressablesController._Instance._IsChunkLoadedToScene[x, y] = false;
        AddressablesController._Instance.DespawnNpcs(x, y);
        //unload chests
        AddressablesController._Instance.UnloadTerrainObjects(x, y);
    }

    public void ReloadAllChunks()
    {
        for (int x = 0; x < _NumberOfColumnsForTerrains; x++)
        {
            for (int y = 0; y < _NumberOfRowsForTerrains; y++)
            {
                ReloadChunk(x, y);
            }
        }
    }
    public void ReloadChunk(int x, int y)
    {
        if (!AddressablesController._Instance._IsChunkLoadedToScene[x, y]) return;

        UnloadChunk(x, y);
        LoadChunk(x, y);
    }
    public Vector2Int GetChunkFromPosition(Vector3 pos)
    {
        int x = (int)(pos.x / _TerrainDimensionMagnitude);
        int y = (int)(pos.z / _TerrainDimensionMagnitude);
        return new Vector2Int(x, y);
    }
    public void CreateEnvironmentPrefabToWorld(AssetReferenceGameObject objRef, Vector3 pos)
    {
        Vector2Int chunk = GetChunkFromPosition(pos);
        if (_ObjectsInChunk[chunk.x, chunk.y] == null)
            _ObjectsInChunk[chunk.x, chunk.y] = new List<AssetReferenceGameObject>();
        _ObjectsInChunk[chunk.x, chunk.y].Add(objRef);
        if (_ObjectPositionsInChunk[chunk.x, chunk.y] == null)
            _ObjectPositionsInChunk[chunk.x, chunk.y] = new List<Vector3>();
        _ObjectPositionsInChunk[chunk.x, chunk.y].Add(pos);

        ReloadChunk(chunk.x, chunk.y);
    }
    public void DestroyEnvironmentPrefabFromWorld(int x, int y, int i)
    {
        if (_ObjectsInChunk[x, y] == null || i >= _ObjectsInChunk[x, y].Count) return;
        if (_ObjectPositionsInChunk[x, y] == null || i >= _ObjectPositionsInChunk[x, y].Count) return;

        _ObjectsInChunk[x, y].RemoveAt(i);
        _ObjectPositionsInChunk[x, y].RemoveAt(i);

        ReloadChunk(x, y);
    }
    public void SetTerrainLinks(GameObject obj)
    {
        var links = obj.GetComponentsInChildren<NavMeshLink>();
        foreach (NavMeshLink item in links)
        {
            if (item.CompareTag("LinkWithTerrain"))
            {
                Physics.Raycast(item.transform.position + item.startPoint + Vector3.up * 2f, -Vector3.up, out RaycastHit hit, 5f, _TerrainAndWaterMask);
                if (hit.collider != null)
                    item.startPoint = hit.point - item.transform.position;
                else
                    Debug.LogError("LinkWithTerrain Ray Did Not Hit!!");
            }
        }
    }
    public void LoadScene(int index)
    {
        LoadSceneAsync(index);
    }

    public void RestartLevel()
    {
        LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadSceneAsync(int index)
    {
        if (_LoadingObject.activeInHierarchy) return;

        _LoadingObject.SetActive(true);
        CallForAction(() => SceneManager.LoadSceneAsync(index), 0.1f, true);
    }

    public void StartRain()
    {
        Gaia.ProceduralWorldsGlobalWeather.Instance.PlayRain();
    }
    public void StopRain()
    {
        Gaia.ProceduralWorldsGlobalWeather.Instance.StopRain();
    }
    public void StartSnow()
    {
        Gaia.ProceduralWorldsGlobalWeather.Instance.PlaySnow();
    }
    public void StopSnow()
    {
        Gaia.ProceduralWorldsGlobalWeather.Instance.StopSnow();
    }
    private void SetPlayerDataFromMenuCreation()
    {
        SaveSystemHandler._Instance._IsSettingPlayerDataForCreation = false;

        WorldHandler._Instance._Player._IsMale = SaveSystemHandler._Instance._PlayerIsMaleForCreation;
        WorldAndNpcArranger.SetGender(WorldHandler._Instance._Player._UmaDynamicAvatar, WorldHandler._Instance._Player._IsMale);

        WorldHandler._Instance._Player._DnaData = SaveSystemHandler._Instance._PlayerDnaDataForCreation;

        WorldHandler._Instance._Player._CharacterColors = new Dictionary<string, Color>();
        var colors = SaveSystemHandler._Instance._PlayerCharacterColorsForCreation;
        foreach (var color in colors)
        {
            if (WorldHandler._Instance._Player._CharacterColors.ContainsKey(color.Key))
                WorldHandler._Instance._Player._CharacterColors[color.Key] = color.Value;
            else
                WorldHandler._Instance._Player._CharacterColors.Add(color.Key, color.Value);
        }

        List<UMATextRecipe> wardrobeRecipes = SaveSystemHandler._Instance._PlayerWardrobeDataForCreation;
        for (int i = 0; i < wardrobeRecipes.Count; i++)
        {
            WorldHandler._Instance._Player.WearWardrobe(wardrobeRecipes[i], true);
        }
    }
    public void StartValuesForNewGame()
    {
        WorldHandler._Instance.InitSeasonForNewGame();

        Vector3 pos = _Player.transform.position;//change it with player start pos
        _Player.transform.position = pos;

        if (SaveSystemHandler._Instance._IsSettingPlayerDataForCreation)
        {
            SetPlayerDataFromMenuCreation();
        }
        else
        {
            Debug.LogError("Player Character Not Set!");
            WorldAndNpcArranger.SetGender(WorldHandler._Instance._Player._UmaDynamicAvatar, WorldHandler._Instance._Player._IsMale);
            SetRandomDNA(WorldHandler._Instance._Player);
            SetRandomWardrobe(WorldHandler._Instance._Player, WorldHandler._Instance._Player._IsMale);
        }

        ushort numberOfNpcs = 20;
        NPC createdNpc;
        Vector2Int chunk;
        for (int i = 0; i < numberOfNpcs; i++)
        {
            createdNpc = Instantiate(PrefabHolder._Instance._NpcParent).GetComponent<NPC>();
            SetRandomNPCValues(createdNpc);
            pos = GetSpawnPosition(createdNpc);
            createdNpc.transform.position = pos;
            createdNpc._NpcIndex = (ushort)i;
            createdNpc._IsMale = false;
            SetRandomDNA(createdNpc);
            SetRandomWardrobe(createdNpc, createdNpc._IsMale);

            chunk = GetChunkFromPosition(pos);
            if (AddressablesController._Instance._NpcListForChunk[chunk.x, chunk.y] == null)
                AddressablesController._Instance._NpcListForChunk[chunk.x, chunk.y] = new List<GameObject>();
            AddressablesController._Instance._NpcListForChunk[chunk.x, chunk.y].Add(createdNpc.gameObject);
        }
    }
    private void SetRandomNPCValues(NPC npc)
    {
        //gender, name, characteristics, social class, location, religion and culture, group, family, past events, equipment and ownerships, current goals
    }
    private Vector3 GetSpawnPosition(NPC npc)
    {
        Vector3 pos = _Player.transform.position + new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)) * Random.Range(1f, 5f);//////////////
        pos.y = GetTerrainOrWaterHeightOnPosition(pos);
        return pos;
    }
    private void SetRandomDNA(Humanoid human)
    {
        List<string> dnaNames = human._IsMale ? _maleDnaNames : _femaleDnaNames;
        Dictionary<string, float> newDna = new Dictionary<string, float>();
        float effectsAll = Random.Range(-0.06f, 0.06f);
        float value;
        SetRandomColors(human, human._UmaDynamicAvatar);
        foreach (var dnaName in dnaNames)
        {
            value = Random.Range(0.42f, 0.58f) + effectsAll;
            if (dnaName == "headSize" || dnaName == "armLength" || dnaName == "feetSize" || dnaName == "forearmLength" || dnaName == "handsSize" || dnaName == "legsSize")
                value = 0.5f;
            else if (dnaName == "height")
                value = 0.5f + effectsAll;
            newDna.Add(dnaName, value);
        }
        human._DnaData = newDna;
    }
    private void SetRandomColors(Humanoid human, DynamicCharacterAvatar avatar)
    {
        if (human._CharacterColors == null)
            human._CharacterColors = new Dictionary<string, Color>();

        float skinValue = Random.Range(0f, 2f);
        if (skinValue > 1f) skinValue /= 2f;
        Color color = new Color(skinValue, skinValue, skinValue, 1f);

        if (human._CharacterColors.ContainsKey("Skin"))
            human._CharacterColors["Skin"] = color;
        else
            human._CharacterColors.Add("Skin", color);

        if (avatar != null)
            WorldAndNpcArranger.ChangeColor(avatar, "Skin", color);

        float redValue = Random.Range(0.1f, 0.6f);
        float greenValue = Random.Range(0.1f, 0.6f);
        float blueValue = Random.Range(0.1f, 0.6f);
        if (redValue > 0.3f) redValue /= 2f;
        if (greenValue > 0.3f) greenValue /= 2f;
        if (blueValue > 0.3f) blueValue /= 2f;
        color = new Color(redValue, greenValue, blueValue, 1f);

        if (human._CharacterColors.ContainsKey("Hair"))
            human._CharacterColors["Hair"] = color;
        else
            human._CharacterColors.Add("Hair", color);

        if (avatar != null)
            WorldAndNpcArranger.ChangeColor(avatar, "Hair", color);

        redValue = Random.Range(0.25f, 0.6f);
        greenValue = Random.Range(0.25f, 0.6f);
        blueValue = Random.Range(0.25f, 0.6f);
        if (redValue > 0.3f) redValue /= 2f;
        if (greenValue > 0.3f) greenValue /= 2f;
        if (blueValue > 0.3f) blueValue /= 2f;
        color = new Color(redValue, greenValue, blueValue, 1f);

        if (human._CharacterColors.ContainsKey("Eyes"))
            human._CharacterColors["Eyes"] = color;
        else
            human._CharacterColors.Add("Eyes", color);

        if (avatar != null)
            WorldAndNpcArranger.ChangeColor(avatar, "Eyes", color);

        if (avatar != null && avatar.BuildCharacterEnabled)
        {
            avatar.BuildCharacterEnabled = false;
            avatar.BuildCharacterEnabled = true;
        }
    }
    private void SetRandomWardrobe(Humanoid human, bool isMale)
    {
        human._WardrobeData = new List<UMATextRecipe>();
        var list = WorldAndNpcArranger.GetRandomHair(isMale);
        foreach (UMATextRecipe recipe in list)
        {
            human.WearWardrobe(recipe);
        }
        list = WorldAndNpcArranger.GetRandomCloth(isMale);
        foreach (UMATextRecipe recipe in list)
        {
            human.WearWardrobe(recipe);
        }
    }

    public void SaveGame(int index)
    {
        if (index == Options._Instance._LoadedGamesCount)
        {
            Options._Instance._LoadedGamesCount += 1;
            PlayerPrefs.SetInt("LoadedGames", Options._Instance._LoadedGamesCount);
        }

        SaveSystemHandler._Instance.SaveGame(index);

        CloseSaveScreen();
    }

    public void DeleteSave()
    {
        SaveSystemHandler._Instance.DeleteSaveFile(_lastDeleteSaveIndex);

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
        LoadSceneAsync(0);
    }
    public void NewGame()
    {
        OpenCharacterCreation();
    }
    public void QuitGame()
    {
        Application.Quit();
    }

    public void PlayButtonSound()
    {
        SoundManager._Instance.PlaySound(SoundManager._Instance._Button, _ListenerObj.transform.position, 0.15f, false, UnityEngine.Random.Range(0.9f, 1.1f));
    }

    public void OpenDeleteSaveScreen(int index)
    {
        GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("Load").Find("DeleteSaveScreen").gameObject.SetActive(true);
        _lastDeleteSaveIndex = index;
    }
    public void CloseDeleteSaveScreen()
    {
        GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("Load").Find("DeleteSaveScreen").gameObject.SetActive(false);
    }

    public void OpenCharacterCreation()
    {
        CharacterCreation._Instance.gameObject.SetActive(true);

        if (_LevelIndex != 0)
            StopGame(false, false);
    }
    public void CharacterCreationFinished()
    {
        CharacterCreation._Instance.transform.Find("Canvas").Find("Loading").gameObject.SetActive(true);
        CharacterCreationDataToPlayer();
        if (_LevelIndex == 0)
            LoadSceneAsync(1);
        else
            RestartGameWithNewCharacter();

    }
    private void CharacterCreationDataToPlayer()
    {
        SaveSystemHandler._Instance._IsSettingPlayerDataForCreation = true;

        SaveSystemHandler._Instance._PlayerIsMaleForCreation = CharacterCreation._Instance.transform.Find("CharacterPreview").GetComponent<DynamicCharacterAvatar>().activeRace.name == "HumanMale";

        SaveSystemHandler._Instance._PlayerDnaDataForCreation = CharacterCreation._Instance.transform.Find("CharacterPreview").GetComponent<DynamicCharacterAvatar>().GetDNAValues();

        SaveSystemHandler._Instance._PlayerCharacterColorsForCreation = new Dictionary<string, Color>();
        var colors = CharacterCreation._Instance.transform.Find("CharacterPreview").GetComponent<DynamicCharacterAvatar>().characterColors.Colors;
        for (int i = 0; i < colors.Count; i++)
        {
            SaveSystemHandler._Instance._PlayerCharacterColorsForCreation.Add(colors[i].Name, colors[i].Color);
        }

        SaveSystemHandler._Instance._PlayerWardrobeDataForCreation = new List<UMATextRecipe>();
        UMATextRecipe[] wardrobeRecipes = CharacterCreation._Instance.transform.Find("CharacterPreview").GetComponent<DynamicCharacterAvatar>().GetVisibleWearables();
        for (int i = 0; i < wardrobeRecipes.Length; i++)
        {
            SaveSystemHandler._Instance._PlayerWardrobeDataForCreation.Add(wardrobeRecipes[i]);
        }
    }
    private void RestartGameWithNewCharacter()
    {
        SaveSystemHandler._Instance._ActiveSave = -1;
        UnstopGame();
        RestartLevel();
    }
    public void StopGame(bool isOpeningInGameMenu, bool isPausing)
    {
        if (isOpeningInGameMenu)
        {
            _InGameMenu.SetActive(true);
            _GameHUD.SetActive(false);
        }
        else if (!isPausing)
        {
            _StopScreen.SetActive(true);
            _GameHUD.SetActive(false);
        }
        Time.timeScale = 0f;
        _IsGameStopped = true;
        SoundManager._Instance.PauseAllSound();
        //SoundManager.Instance.PauseMusic();
    }
    public void UnstopGame()
    {
        _StopScreen.SetActive(false);
        _InGameMenu.SetActive(false);
        _GameHUD.SetActive(true);
        CloseOptionsScreen(false);
        _IsGameStopped = false;
        Time.timeScale = 1f;
        SoundManager._Instance.ContinueAllSound();
        //SoundManager.Instance.ContinueMusic();
    }
    public bool IsInClosedSpace(Vector3 position)
    {
        return Physics.Raycast(position, Vector3.up, 150f, LayerMask.GetMask("SolidObject"));
    }

    public void OpenInGameMenuScreen()
    {
        _InGameMenu.SetActive(true);
    }
    public void CloseInGameMenuScreen()
    {
        _InGameMenu.SetActive(false);
    }
    private void OpenInGameMenu(int number)
    {
        if (number == 0)
        {
            _InventoryScreen.SetActive(true);
        }
        else if (number == 1)
        {
            _MapScreen.SetActive(true);
        }
        else if (number == 2)
        {
            _DialogueScreen.SetActive(true);
        }
    }
    private void CloseAllInGameMenus()
    {
        _InventoryScreen.SetActive(false);
        _MapScreen.SetActive(false);
        _DialogueScreen.SetActive(false);
    }
    public void OpenOptionsScreen()
    {
        if (GameManager._Instance._LevelIndex == 0)
        {
            GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("Options").gameObject.SetActive(true);
            GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("MainMenu").gameObject.SetActive(false);
        }
        else
        {
            GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("Options").gameObject.SetActive(true);
            GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("StopScreen").gameObject.SetActive(false);
        }
    }
    public void CloseOptionsScreen(bool isOpeningMenu = true)
    {
        if (GameManager._Instance._LevelIndex == 0)
        {
            GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("Options").gameObject.SetActive(false);
            if (isOpeningMenu)
                GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("MainMenu").gameObject.SetActive(true);
        }
        else
        {
            GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("Options").gameObject.SetActive(false);
            if (isOpeningMenu)
                GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("StopScreen").gameObject.SetActive(true);
        }
    }
    public void OpenLoadScreen()
    {
        if (GameManager._Instance._LevelIndex == 0)
        {
            GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("Load").gameObject.SetActive(true);
            GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("MainMenu").gameObject.SetActive(false);
        }
        else
        {
            GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("Load").gameObject.SetActive(true);
            GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("StopScreen").gameObject.SetActive(false);
        }

        GameObject created = null;
        for (int i = 0; i < Options._Instance._LoadedGamesCount; i++)
        {
            int c = i;
            created = Instantiate(PrefabHolder._Instance._LoadPrefab, GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("Load").Find("Saves"));
            created.GetComponent<Button>().onClick.AddListener(() => PlayButtonSound());
            created.GetComponent<Button>().onClick.AddListener(() => { SaveSystemHandler._Instance._ActiveSave = c; LoadScene(1); });
            created.transform.Find("DeleteButton").GetComponent<Button>().onClick.AddListener(() => PlayButtonSound());
            created.transform.Find("DeleteButton").GetComponent<Button>().onClick.AddListener(() => OpenDeleteSaveScreen(c));
        }
    }
    public void CloseLoadScreen()
    {
        for (int i = 0; i < GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("Load").Find("Saves").transform.childCount; i++)
        {
            Destroy(GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("Load").Find("Saves").transform.GetChild(i).gameObject);
        }

        if (GameManager._Instance._LevelIndex == 0)
        {
            GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("MainMenu").gameObject.SetActive(true);
        }
        else
        {
            GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("StopScreen").gameObject.SetActive(true);
        }

        GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("Load").gameObject.SetActive(false);
        GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("Load").Find("DeleteSaveScreen").gameObject.SetActive(false);

    }
    public void OpenSaveScreen()
    {
        if (GameManager._Instance._LevelIndex == 0)
        {
            GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("Save").gameObject.SetActive(true);
            GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("MainMenu").gameObject.SetActive(false);
        }
        else
        {
            GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("Save").gameObject.SetActive(true);
            GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("StopScreen").gameObject.SetActive(false);
        }

        GameObject created = null;
        for (int i = 0; i < Options._Instance._LoadedGamesCount + 1; i++)
        {
            int c = i;
            created = Instantiate(PrefabHolder._Instance._SavePrefab, GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("Save").Find("Saves"));
            created.GetComponent<Button>().onClick.AddListener(() => PlayButtonSound());
            created.GetComponent<Button>().onClick.AddListener(() => SaveGame(c));
        }
    }
    public void CloseSaveScreen()
    {
        for (int i = 0; i < GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("Save").Find("Saves").transform.childCount; i++)
        {
            Destroy(GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("Save").Find("Saves").transform.GetChild(i).gameObject);
        }

        if (GameManager._Instance._LevelIndex == 0)
        {
            GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("Save").gameObject.SetActive(false);
            GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("MainMenu").gameObject.SetActive(true);
        }
        else
        {
            GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("Save").gameObject.SetActive(false);
            GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("StopScreen").gameObject.SetActive(true);
        }
    }
    public void UpdateInventoryUI(Inventory anotherInventory = null)
    {

    }
    private Color GetDurabilityColor(float normalizedDurability)
    {
        if (normalizedDurability < 0.5f)
        {
            float t = normalizedDurability / 0.5f;
            return Color.Lerp(Color.red, Color.yellow, t);
        }
        else
        {
            float t = (normalizedDurability - 0.5f) / 0.5f;
            return Color.Lerp(Color.yellow, Color.green, t);
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
