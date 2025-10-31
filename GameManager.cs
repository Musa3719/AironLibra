using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UMA;
using UMA.CharacterSystem;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager _Instance;
    public float _TerrainDimensionMagnitude => 1024f;
    public int _NumberOfColumnsForTerrains => 1;
    public int _NumberOfRowsForTerrains => 1;

    public Dictionary<string, AssetReferenceGameObject> _ItemNameToPrefab;
    public Dictionary<string, AssetReferenceSprite> _ItemNameToSprite;

    public List<AssetReferenceGameObject>[,] _ObjectsInChunk;
    public List<Transform>[,] _ObjectParentsInChunk;
    public List<Vector3>[,] _ObjectPositionsInChunk;
    public List<Vector3>[,] _ObjectRotationsInChunk;
    //chest data
    //npc data

    public GameObject _ListenerObj { get { if (_Player == null) return _MainCamera; return _Player; } }
    public GameObject _MainCamera { get; private set; }
    public GameObject _Player { get; private set; }
    public GameObject _StopScreen { get; private set; }
    public GameObject _InGameMenu { get; private set; }
    public GameObject _InventoryScreen { get; private set; }
    public GameObject _AnotherInventoryList { get; private set; }
    public GameObject _AnotherInventoryChestList { get; private set; }
    public Inventory _AnotherInventory { get; private set; }
    public GameObject _InventoryItemInteractPopup { get; private set; }
    public GameObject _InventoryItemInfoPopup { get; private set; }
    public GameObject _MapScreen { get; private set; }
    public GameObject _DialogueScreen { get; private set; }
    public GameObject _GameHUD { get; private set; }
    public GameObject _OptionsScreen { get; private set; }
    public GameObject _LoadScreen { get; private set; }
    public GameObject _SaveScreen { get; private set; }
    public GameObject _LoadingObject { get; private set; }
    public Transform _EnvironmentTransform { get; private set; }
    public ObjectPool _NPCPool { get; private set; }
    public InventorySlotUI _LastClickedSlotUI { get; set; }

    private Sprite[] _equipmentSlotDefaultImages;
    private Image[] _playerInventoryImages;
    private Image[] _playerEquipmentImages;
    private Image[] _playerInventoryBackCarryImages;
    private Image[] _anotherInventoryImages;
    private Image[] _anotherEquipmentImages;
    private Image[] _anotherInventoryChestImages;
    private Image[] _anotherInventoryBackCarryImages;
    private Dictionary<string, AsyncOperationHandle<Sprite>> _nameToLoadedSprites;

    public bool _IsGameStopped { get; private set; }
    public int _InGameMenuNumber { get; private set; }

    public InteractBoxUI _InteractBoxUI { get; private set; }
    public int _SplitAmount { get; set; }

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
        _Instance = this;
        transform.Find("CharacterCreation").GetComponent<CharacterCreation>().Init();
        Shader.EnableKeyword("_USEGLOBALSNOWLEVEL");
        Shader.EnableKeyword("_PW_GLOBAL_COVER_LAYER");
        Shader.EnableKeyword("_PW_COVER_ENABLED");
        NPCManager._AllNPCs = new List<NPC>();

        _maleDnaNames = UMAGlobalContext.Instance.GetRace("HumanMale").GetDNANames();
        _femaleDnaNames = UMAGlobalContext.Instance.GetRace("HumanFemale").GetDNANames();

        NPCManager._Comparer = new NPCDistanceComparer();
        _ObjectsInChunk = new List<AssetReferenceGameObject>[_NumberOfColumnsForTerrains, _NumberOfRowsForTerrains];
        _ObjectPositionsInChunk = new List<Vector3>[_NumberOfColumnsForTerrains, _NumberOfRowsForTerrains];
        _ObjectRotationsInChunk = new List<Vector3>[_NumberOfColumnsForTerrains, _NumberOfRowsForTerrains];
        _ObjectParentsInChunk = new List<Transform>[_NumberOfColumnsForTerrains, _NumberOfRowsForTerrains];
        _MainCamera = Camera.main.gameObject;
        _Player = GameObject.FindGameObjectWithTag("Player");

        //Application.targetFrameRate = 60;
        _OptionsScreen = GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("Options").gameObject;
        _LoadingObject = GameObject.FindGameObjectWithTag("UI").transform.Find("Loading").gameObject;
        _LoadScreen = GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("Load").gameObject;

        _LevelIndex = SceneManager.GetActiveScene().buildIndex;
        if (_LevelIndex != 0)
        {
            _EnvironmentTransform = GameObject.Find("Environment").transform;
            _StopScreen = GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("StopScreen").gameObject;
            _InGameMenu = GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("InGameMenu").gameObject;
            _InventoryScreen = GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("InGameMenu").Find("InventoryScreen").gameObject;
            _AnotherInventoryList = GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("InGameMenu").Find("InventoryScreen").Find("AnotherInventory").gameObject;
            _AnotherInventoryChestList = GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("InGameMenu").Find("InventoryScreen").Find("AnotherInventoryChest").gameObject;
            _InventoryItemInteractPopup = GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("InGameMenu").Find("InventoryScreen").Find("InteractScreen").gameObject;
            _InteractBoxUI = GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("InGameMenu").Find("InventoryScreen").Find("InteractScreen").Find("InteractBox").GetComponent<InteractBoxUI>();
            _InventoryItemInfoPopup = GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("InGameMenu").Find("InventoryScreen").Find("InfoScreen").gameObject;
            _MapScreen = GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("InGameMenu").Find("MapScreen").gameObject;
            _DialogueScreen = GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("InGameMenu").Find("DialogueScreen").gameObject;
            _GameHUD = GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("InGameScreen").gameObject;
            _SaveScreen = GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("Save").gameObject;

            _NPCPool = transform.Find("NPC_Pool").GetComponent<ObjectPool>();

            Transform playerItemList = _InventoryScreen.transform.Find("OwnInventory").Find("ItemList");
            _playerInventoryImages = new Image[playerItemList.childCount];
            for (int i = 0; i < playerItemList.childCount; i++)
            {
                _playerInventoryImages[i] = playerItemList.GetChild(i).GetComponent<Image>();
            }
            Transform playerEquipmentList = _InventoryScreen.transform.Find("OwnInventory").Find("Equipments");
            _playerEquipmentImages = new Image[playerEquipmentList.childCount];
            _equipmentSlotDefaultImages = new Sprite[playerEquipmentList.childCount];
            for (int i = 0; i < playerEquipmentList.childCount; i++)
            {
                _playerEquipmentImages[i] = playerEquipmentList.GetChild(i).GetComponent<Image>();
                _equipmentSlotDefaultImages[i] = _playerEquipmentImages[i].sprite;
            }
            Transform playerBackCarryList = _InventoryScreen.transform.Find("OwnInventory").Find("BackCarry").Find("BackCarryList");
            _playerInventoryBackCarryImages = new Image[playerBackCarryList.childCount];
            for (int i = 0; i < playerBackCarryList.childCount; i++)
            {
                _playerInventoryBackCarryImages[i] = playerBackCarryList.GetChild(i).GetComponent<Image>();
            }

            Transform anotherInvItemList = _AnotherInventoryList.transform.Find("ItemList");
            _anotherInventoryImages = new Image[anotherInvItemList.childCount];
            for (int i = 0; i < anotherInvItemList.childCount; i++)
            {
                _anotherInventoryImages[i] = anotherInvItemList.GetChild(i).GetComponent<Image>();
            }
            Transform anotherEquipmentList = _InventoryScreen.transform.Find("AnotherInventory").Find("Equipments");
            _anotherEquipmentImages = new Image[anotherEquipmentList.childCount];
            for (int i = 0; i < anotherEquipmentList.childCount; i++)
            {
                _anotherEquipmentImages[i] = anotherEquipmentList.GetChild(i).GetComponent<Image>();
            }
            Transform anotherInvBackCarryList = _AnotherInventoryList.transform.Find("BackCarry").Find("BackCarryList");
            _anotherInventoryBackCarryImages = new Image[anotherInvBackCarryList.childCount];
            for (int i = 0; i < anotherInvBackCarryList.childCount; i++)
            {
                _anotherInventoryBackCarryImages[i] = anotherInvBackCarryList.GetChild(i).GetComponent<Image>();
            }
            Transform anotherInvChestList = _AnotherInventoryChestList.transform.Find("ItemList");
            _anotherInventoryChestImages = new Image[anotherInvChestList.childCount];
            for (int i = 0; i < anotherInvChestList.childCount; i++)
            {
                _anotherInventoryChestImages[i] = anotherInvChestList.GetChild(i).GetComponent<Image>();
            }
            _nameToLoadedSprites = new Dictionary<string, AsyncOperationHandle<Sprite>>();
        }
    }
    private void Start()
    {
        if (_LevelIndex != 0)
            SaveSystemHandler._Instance.LoadGame(SaveSystemHandler._Instance._ActiveSave);
        else
            Time.timeScale = 1f;

        if (_LevelIndex != 0)
        {
            InitDictionaries();
        }
    }

    private void Update()
    {
        if (_LevelIndex != 0)
        {
            if (_LastClickedSlotUI != null && M_Input.GetButtonDown("Fire1") && !_InteractBoxUI.IsHovered())
            {
                _InventoryItemInteractPopup.SetActive(false);
            }
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
        if (_LevelIndex != 0 && _InGameMenu.activeInHierarchy)
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

                    if (_LevelIndex != 0 && _InGameMenu.activeInHierarchy)
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

    public void InitDictionaries()
    {
        _ItemNameToPrefab = new Dictionary<string, AssetReferenceGameObject>();
        _ItemNameToPrefab.Add("Apple", AddressablesController._Instance._AppleItem);
        //_ItemNameToPrefab.Add("Copper Coin", AddressablesController._Instance._AppleItem);


        _ItemNameToSprite = new Dictionary<string, AssetReferenceSprite>();
        _ItemNameToSprite.Add("Apple", AddressablesController._Instance._AppleSprite);
        //_ItemNameToSprite.Add("Copper Coin", AddressablesController._Instance._AppleSprite);
    }
    #region CommonMethods
    public Sprite TextureToSprite(Texture texture)
    {
        RenderTexture rt = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
        Graphics.Blit(texture, rt);

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D tex2D = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
        tex2D.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
        tex2D.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        return Sprite.Create(tex2D, new Rect(0, 0, tex2D.width, tex2D.height), new Vector2(0.5f, 0.5f));
    }
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
        AddressablesController._Instance.SpawnNpcs(x, y);
        //Spawn Animals
        AddressablesController._Instance._IsChunkLoadedToScene[x, y] = true;
    }
    public void UnloadChunk(int x, int y)
    {
        AddressablesController._Instance._IsChunkLoadedToScene[x, y] = false;
        AddressablesController._Instance.DespawnNpcs(x, y);
        //Despawn Animals
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
    public void CreateEnvironmentPrefabToWorld(AssetReferenceGameObject objRef, Transform parent, Vector3 pos, Vector3 angles, ref ItemHandleData itemHandleData)
    {
        Vector2Int chunk = GetChunkFromPosition(pos);
        if (_ObjectsInChunk[chunk.x, chunk.y] == null)
            _ObjectsInChunk[chunk.x, chunk.y] = new List<AssetReferenceGameObject>();
        _ObjectsInChunk[chunk.x, chunk.y].Add(objRef);
        if (_ObjectPositionsInChunk[chunk.x, chunk.y] == null)
            _ObjectPositionsInChunk[chunk.x, chunk.y] = new List<Vector3>();
        if (_ObjectRotationsInChunk[chunk.x, chunk.y] == null)
            _ObjectRotationsInChunk[chunk.x, chunk.y] = new List<Vector3>();
        if (_ObjectParentsInChunk[chunk.x, chunk.y] == null)
            _ObjectParentsInChunk[chunk.x, chunk.y] = new List<Transform>();
        _ObjectPositionsInChunk[chunk.x, chunk.y].Add(pos);
        _ObjectRotationsInChunk[chunk.x, chunk.y].Add(angles);
        _ObjectParentsInChunk[chunk.x, chunk.y].Add(parent);

        itemHandleData._XChunk = chunk.x;
        itemHandleData._YChunk = chunk.y;
        itemHandleData._AssetRef = objRef;

        ReloadChunk(chunk.x, chunk.y);
    }
    public void DestroyEnvironmentPrefabFromWorld(ItemHandleData itemHandleData)
    {
        int x = itemHandleData._XChunk, y = itemHandleData._YChunk;
        int i = _ObjectsInChunk[x, y].IndexOf(itemHandleData._AssetRef);
        if (_ObjectsInChunk[x, y] == null || i >= _ObjectsInChunk[x, y].Count) return;
        if (_ObjectPositionsInChunk[x, y] == null || i >= _ObjectPositionsInChunk[x, y].Count) return;
        if (_ObjectRotationsInChunk[x, y] == null || i >= _ObjectRotationsInChunk[x, y].Count) return;
        if (_ObjectParentsInChunk[x, y] == null || i >= _ObjectParentsInChunk[x, y].Count) return;

        _ObjectsInChunk[x, y].RemoveAt(i);
        _ObjectPositionsInChunk[x, y].RemoveAt(i);
        _ObjectRotationsInChunk[x, y].RemoveAt(i);
        _ObjectParentsInChunk[x, y].RemoveAt(i);

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
        NPCManager.SetGender(WorldHandler._Instance._Player._UmaDynamicAvatar, WorldHandler._Instance._Player._IsMale);

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

        WorldHandler._Instance._Player._MuscleLevel = WorldHandler._Instance._Player._DnaData["upperMuscle"];
        WorldHandler._Instance._Player._FatLevel = WorldHandler._Instance._Player._DnaData["upperWeight"];
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
            NPCManager.SetGender(WorldHandler._Instance._Player._UmaDynamicAvatar, WorldHandler._Instance._Player._IsMale);
            SetRandomDNA(WorldHandler._Instance._Player);
            SetRandomWardrobe(WorldHandler._Instance._Player, WorldHandler._Instance._Player._IsMale);
        }

        ushort numberOfNpcs = 1;
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

        human._MuscleLevel = Random.Range(0.15f, 0.5f);
        human._FatLevel = Random.Range(0.15f, 0.75f);
        float headSize = 0.41f + (human._FatLevel * 1.5f + human._MuscleLevel) * 0.1f;
        float neckSize = (human._FatLevel / 2f) + (human._MuscleLevel / 2f);

        foreach (var dnaName in dnaNames)
        {
            value = Random.Range(0.42f, 0.58f) + effectsAll;
            if (dnaName == "armLength" || dnaName == "forearmLength" || dnaName == "feetSize" || dnaName == "handsSize" || dnaName == "legsSize")
                value = 0.5f;
            else if (dnaName == "height")
                value = 0.5f + effectsAll * 1.8f;
            else if (dnaName == "headSize")
                value = headSize;
            else if (dnaName == "neckThickness")
                value = neckSize;
            else if (dnaName == "upperMuscle" || dnaName == "lowerMuscle" || dnaName == "armWidth" || dnaName == "forearmWidth" || dnaName == "bodyFitness")
                value = human._MuscleLevel;
            else if (dnaName == "upperWeight" || dnaName == "lowerWeight" || dnaName == "belly" || dnaName == "waist")
                value = human._FatLevel;
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
            NPCManager.ChangeColor(avatar, "Skin", color);

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
            NPCManager.ChangeColor(avatar, "Hair", color);

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
            NPCManager.ChangeColor(avatar, "Eyes", color);

        if (avatar != null && avatar.BuildCharacterEnabled)
        {
            avatar.BuildCharacterEnabled = false;
            avatar.BuildCharacterEnabled = true;
        }
    }
    private void SetRandomWardrobe(Humanoid human, bool isMale)
    {
        human._WardrobeData = new List<UMATextRecipe>();
        var list = NPCManager.GetRandomHair(isMale);
        foreach (UMATextRecipe recipe in list)
        {
            human.WearWardrobe(recipe);
        }
        list = NPCManager.GetRandomCloth(isMale);
        foreach (UMATextRecipe recipe in list)
        {
            human.WearWardrobe(recipe);
        }
    }

    public int GetStrLevel(float height, float muscle, float fat, bool isMale)
    {
        return Mathf.RoundToInt(MapToAnother(muscle, 0.15f, 0.75f, 0f, 18f) * 0.6f + MapToAnother(fat, 0.15f, 0.75f, 0f, 18f) * 0.1f + MapToAnother(height, 0.38f, 0.62f, 0f, 18f) * 0.3f) + (isMale ? 2 : 0);
    }
    public int GetAgiLevel(float height, float muscle, float fat, bool isMale)
    {
        return 20 - Mathf.RoundToInt(MapToAnother(muscle, 0.15f, 0.75f, 0f, 18f) * 0.1f + MapToAnother(fat, 0.15f, 0.75f, 0f, 18f) * 0.5f + MapToAnother(height, 0.38f, 0.62f, 0f, 18f) * 0.4f) - (isMale ? 2 : 0);
    }
    public float MapToAnother(float value, float inMin, float inMax, float outMin, float outMax)
    {
        value = Mathf.Clamp(value, inMin, inMax);
        float t = Mathf.InverseLerp(inMin, inMax, value);
        return Mathf.Lerp(outMin, outMax, t);
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
    public void PlayButtonSound(float vol, float pitch)
    {
        SoundManager._Instance.PlaySound(SoundManager._Instance._Button, _ListenerObj.transform.position, vol, false, pitch);
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
        OpenInGameMenu(_InGameMenuNumber);
    }
    public void CloseInGameMenuScreen()
    {
        _InGameMenu.SetActive(false);
        _AnotherInventoryList.SetActive(false);
        _AnotherInventoryChestList.SetActive(false);
        _AnotherInventory = null;
        UnloadSpriteHandles();
    }
    private void UnloadSpriteHandles()
    {
        foreach (var handle in _nameToLoadedSprites)
        {
            if (handle.Value.IsValid())
            {
                handle.Value.Release();
            }
        }
        _nameToLoadedSprites.Clear();
    }
    private void OpenInGameMenu(int number)
    {
        if (number == 0)
        {
            _InventoryScreen.SetActive(true);
            UpdateInventoryUI();
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

    public void TakeOrSendFromInteractMenu()
    {
        TakeOrSendFromInteractCommon(_LastClickedSlotUI._ItemRef);
        _InventoryItemInteractPopup.SetActive(false);
    }
    private void TakeOrSendFromInteractCommon(Item item)
    {
        bool isTake = !_LastClickedSlotUI._IsPlayerInventory;
        if (isTake)
            item.Take(WorldHandler._Instance._Player._Inventory);
        else if (_AnotherInventory != null)
            item.Take(_AnotherInventory);
    }

    public void EquipOrUnequipFromInteractMenu()
    {
        if (!_LastClickedSlotUI._ItemRef._ItemDefinition._CanBeEquipped) return;

        bool isEquipped = _LastClickedSlotUI._ItemRef._IsEquipped;
        if (isEquipped)
            _LastClickedSlotUI._ItemRef.Unequip(false, true);
        else
            _LastClickedSlotUI._ItemRef.Equip(WorldHandler._Instance._Player._Inventory);

        _InventoryItemInteractPopup.SetActive(false);
    }
    public void ConsumeItemFromInteractMenu()
    {
        if (_LastClickedSlotUI._ItemRef._ItemDefinition is ICanBeConsumed iConsumed)
        {
            iConsumed.Consume(_LastClickedSlotUI._ItemRef);
            _InventoryItemInteractPopup.SetActive(false);
        }
    }
    public void SplitItemFromInteractMenu()
    {
        if (_SplitAmount == 0) return;
        if (_LastClickedSlotUI == null) return;
        if (_LastClickedSlotUI._ItemRef._Count < _SplitAmount) return;

        Item splitItem = _LastClickedSlotUI._ItemRef.Copy();
        splitItem._IsSplittingBuffer = true;
        splitItem._Count = _SplitAmount;
        _LastClickedSlotUI._ItemRef._Count -= _SplitAmount;

        TakeOrSendFromInteractCommon(splitItem);

        if (_LastClickedSlotUI._ItemRef._Count == 0)
            _LastClickedSlotUI._ItemRef.Drop(false);

        _InventoryItemInteractPopup.SetActive(false);
    }
    public void SplitAmountArrange(float normalizedSplit)
    {
        if (_LastClickedSlotUI == null) return;

        _SplitAmount = Mathf.RoundToInt(_LastClickedSlotUI._ItemRef._Count * normalizedSplit);
        _InventoryItemInteractPopup.transform.Find("Split").GetComponent<Button>().interactable = _SplitAmount > 0;
        _InventoryItemInteractPopup.transform.Find("Split").Find("SplitAmountText").GetComponent<TextMeshProUGUI>().text = _SplitAmount.ToString();
    }
    public void TakeAllButton(int i)
    {
        switch (i)
        {
            case 0:
                TakeAll(_anotherInventoryChestImages);
                break;
            case 1:
                TakeAll(_anotherInventoryImages);
                break;
            case 2:
                TakeAll(_anotherEquipmentImages);
                break;
            case 3:
                TakeAll(_anotherInventoryBackCarryImages);
                break;
            default:
                break;
        }
    }
    private void TakeAll(Image[] itemImages)
    {
        for (int i = 0; i < itemImages.Length; i++)
        {
            if (itemImages[i].GetComponent<InventorySlotUI>()._ItemRef != null)
                itemImages[i].GetComponent<InventorySlotUI>()._ItemRef.Take(WorldHandler._Instance._Player._Inventory);
        }
    }

    public void OpenAnotherInventory(Inventory anotherInv)
    {
        _AnotherInventory = anotherInv;
        if (anotherInv._CanEquip)
            _AnotherInventoryList.SetActive(true);
        else
            _AnotherInventoryChestList.SetActive(true);
        StopGame(true, false);
        OpenInGameMenuScreen();
        OpenInGameMenu(0);
    }
    public void UpdateInventoryUI()
    {
        if (!_InventoryScreen.activeSelf) return;

        _InventoryScreen.transform.Find("OwnInventory").Find("Name").GetComponentInChildren<TextMeshProUGUI>().text = WorldHandler._Instance._Player._Inventory._Name;
        UpdateOneInventory(WorldHandler._Instance._Player._Inventory, _playerInventoryImages, _playerInventoryBackCarryImages);
        UpdateEquipmentUI(WorldHandler._Instance._Player._Inventory, _playerEquipmentImages);

        if (_AnotherInventory != null)
        {

            if (_AnotherInventory._IsHuman)
            {
                if (!_AnotherInventoryList.activeSelf)
                    _AnotherInventoryList.SetActive(true);
                if (_AnotherInventoryChestList.activeSelf)
                    _AnotherInventoryChestList.SetActive(false);
                _AnotherInventoryList.transform.Find("Name").GetComponentInChildren<TextMeshProUGUI>().text = _AnotherInventory._Name;
                UpdateOneInventory(_AnotherInventory, _anotherInventoryImages, _anotherInventoryBackCarryImages);
                UpdateEquipmentUI(_AnotherInventory, _anotherEquipmentImages);
            }
            else
            {
                if (_AnotherInventoryList.activeSelf)
                    _AnotherInventoryList.SetActive(false);
                if (!_AnotherInventoryChestList.activeSelf)
                    _AnotherInventoryChestList.SetActive(true);
                UpdateOneInventory(_AnotherInventory, _anotherInventoryChestImages, null);
            }

        }

    }
    private void UpdateEquipmentUI(Inventory inventory, Image[] equipmentImages)
    {
        Humanoid human = inventory.GetComponent<Humanoid>();
        if (human == null) { Debug.LogError("human is null!"); return; }
        Item item = null;
        for (int i = 0; i < equipmentImages.Length; i++)
        {
            switch (i)
            {
                case 0:
                    item = human._HeadGear;
                    break;
                case 1:
                    item = human._Gloves;
                    break;
                case 2:
                    item = human._BackCarryItemRef;
                    break;
                case 3:
                    item = human._Clothing;
                    break;
                case 4:
                    item = human._ChestArmor;
                    break;
                case 5:
                    item = human._LegsArmor;
                    break;
                case 6:
                    item = human._Boots;
                    break;
                case 7:
                    item = human._LeftHandEquippedItemRef;
                    break;
                case 8:
                    item = human._RightHandEquippedItemRef;
                    break;
                default:
                    Debug.LogError("unknown equipment slot!");
                    break;
            }

            if (item == null)
                equipmentImages[i].sprite = _equipmentSlotDefaultImages[i];
            else
                SetSprite(equipmentImages[i], item._ItemDefinition._Name);

            SetDurability(equipmentImages[i], item);
        }
    }
    private void UpdateOneInventory(Inventory inventory, Image[] ownItemImageComponents, Image[] carryingInventoryItemImageComponents)
    {
        int itemCount = inventory._Items.Count;
        for (int i = 0; i < ownItemImageComponents.Length; i++)
        {
            if (i >= itemCount)
            {
                ownItemImageComponents[i].GetComponent<InventorySlotUI>()._ItemRef = null;
                ownItemImageComponents[i].sprite = PrefabHolder._Instance._EmptyItemBackground;
                ownItemImageComponents[i].transform.Find("Count").gameObject.SetActive(false);
                ownItemImageComponents[i].transform.Find("DurabilityBackground").gameObject.SetActive(false);
            }
            else
            {
                ownItemImageComponents[i].GetComponent<InventorySlotUI>()._ItemRef = inventory._Items[i];
                ownItemImageComponents[i].sprite = PrefabHolder._Instance._LoadingAdressableProcessSprite;
                int iForAction = i;
                SetSprite(ownItemImageComponents[iForAction], inventory._Items[iForAction]._ItemDefinition._Name);
                SetDurability(ownItemImageComponents[iForAction], inventory._Items[iForAction]);
            }
        }
        if (!inventory._CanEquip) { }
        else if (inventory.GetComponent<Humanoid>()._BackCarryItemRef == null)
        {
            carryingInventoryItemImageComponents[0].transform.parent.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.07f);
            carryingInventoryItemImageComponents[0].transform.parent.parent.Find("BackCarryFrame").GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.07f);
            for (int i = 0; i < carryingInventoryItemImageComponents.Length; i++)
            {
                carryingInventoryItemImageComponents[i].color = new Color(1f, 1f, 1f, 0.07f);
                carryingInventoryItemImageComponents[i].GetComponent<InventorySlotUI>()._ItemRef = null;
                carryingInventoryItemImageComponents[i].sprite = PrefabHolder._Instance._EmptyItemBackground;
                carryingInventoryItemImageComponents[i].transform.Find("Count").gameObject.SetActive(false);
                carryingInventoryItemImageComponents[i].transform.Find("DurabilityBackground").gameObject.SetActive(false);
            }
        }
        else
        {
            itemCount = (inventory.GetComponent<Humanoid>()._BackCarryItemRef as IHaveInventory)._Inventory._Items.Count;

            carryingInventoryItemImageComponents[0].transform.parent.GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f);
            carryingInventoryItemImageComponents[0].transform.parent.parent.Find("BackCarryFrame").GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.8f);
            for (int i = 0; i < carryingInventoryItemImageComponents.Length; i++)
            {
                carryingInventoryItemImageComponents[i].color = new Color(1f, 1f, 1f, 1f);
                if (i >= itemCount)
                {
                    carryingInventoryItemImageComponents[i].GetComponent<InventorySlotUI>()._ItemRef = null;
                    carryingInventoryItemImageComponents[i].sprite = PrefabHolder._Instance._EmptyItemBackground;
                    carryingInventoryItemImageComponents[i].transform.Find("Count").gameObject.SetActive(false);
                    carryingInventoryItemImageComponents[i].transform.Find("DurabilityBackground").gameObject.SetActive(false);
                }
                else
                {
                    carryingInventoryItemImageComponents[i].GetComponent<InventorySlotUI>()._ItemRef = inventory._Items[i];
                    carryingInventoryItemImageComponents[i].sprite = PrefabHolder._Instance._LoadingAdressableProcessSprite;
                    int iForAction = i;
                    SetSprite(carryingInventoryItemImageComponents[iForAction], inventory._Items[iForAction]._ItemDefinition._Name);
                    SetDurability(carryingInventoryItemImageComponents[iForAction], inventory._Items[iForAction]);
                }
            }
        }
    }
    private void SetDurability(Image image, Item item)
    {
        if (item == null)
        {
            if (image.transform.Find("Count") != null)
                image.transform.Find("Count").gameObject.SetActive(false);
            image.transform.Find("DurabilityBackground").gameObject.SetActive(false);
            return;
        }

        if (item is IHaveItemDurability)
        {
            image.transform.Find("Count").gameObject.SetActive(false);
            image.transform.Find("DurabilityBackground").gameObject.SetActive(true);
            float normalizedDurability = (item as IHaveItemDurability)._Durability / (item as IHaveItemDurability)._DurabilityMax;
            image.transform.Find("DurabilityBackground").Find("Durability").GetComponent<Image>().color = GetDurabilityColor(normalizedDurability);
            image.transform.Find("DurabilityBackground").Find("Durability").GetComponent<RectTransform>().localScale = new Vector3(normalizedDurability, 1f, 1f);
        }
        else
        {
            image.transform.Find("DurabilityBackground").gameObject.SetActive(false);
            image.transform.Find("Count").gameObject.SetActive(true);
            image.transform.Find("Count").GetComponent<TextMeshProUGUI>().text = item._Count.ToString();
        }
    }
    private void SetSprite(Image image, string itemName)
    {
        if (!_nameToLoadedSprites.ContainsKey(itemName))
        {
            var handle = _ItemNameToSprite[itemName].LoadAssetAsync();
            handle.Completed += (handle) => { if (!handle.IsValid()) return; image.sprite = handle.Result; };
            _nameToLoadedSprites.Add(itemName, handle);
        }
        else
        {
            if (_nameToLoadedSprites[itemName].IsDone)
                image.sprite = _nameToLoadedSprites[itemName].Result;
            else
            {
                _nameToLoadedSprites[itemName].Completed += (handle) => { if (!handle.IsValid()) return; image.sprite = handle.Result; };
            }
        }
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
