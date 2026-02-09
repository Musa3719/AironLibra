using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UMA;
using UMA.CharacterSystem;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEditor.PlayerSettings;

public class GameManager : MonoBehaviour
{
    public bool _Is1;
    public bool _Is2;
    public bool _Is3;
    public bool _Is4;
    public bool _Is5;
    public bool _Is6;

    public static GameManager _Instance;
    public float _TerrainDimensionMagnitude => 1024f;
    public int _NumberOfColumnsForTerrains => 1;
    public int _NumberOfRowsForTerrains => 1;

    public Transform _RangedAimMesh { get; private set; }
    public GraphicRaycaster _GraphicRaycaster { get; private set; }

    public Dictionary<string, AssetReferenceGameObject> _NameToPrefabMesh;
    public Dictionary<string, AssetReferenceGameObject> _NameToPrefabCollider;
    public Dictionary<string, AssetReferenceSprite> _NameToSprite;

    public Dictionary<string, float> _AnimNameToAttackStartTime;
    public Dictionary<string, float> _AnimNameToAttackEndTime;

    public List<ItemHandleData>[,] _ItemHandleDatasInChunk;
    public List<Transform>[,] _ObjectParentsInChunk;
    public List<Vector3>[,] _ObjectPositionsInChunk;
    public List<Vector3>[,] _ObjectRotationsInChunk;
    //chest data
    //npc data
    public GameObject _ListenerObj { get { if (_Player == null) return _MainCamera; return _Player; } }
    public GameObject _MainCamera { get; private set; }
    public GameObject _Player { get; private set; }
    public Vector3 _PlayerPos { get; private set; }

    public ReflectionProbe _MainReflectionProbe { get; private set; }
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
    public TextMeshProUGUI _FpsText { get; private set; }
    public Image _StaminaHUDImage { get; private set; }
    public Image _StaminaBackgroundHUDImage { get; private set; }
    public RectTransform _StaminaHUDRect { get; private set; }
    public GameObject _HealthInfoHUD { get; private set; }
    public GameObject _OptionsScreen { get; private set; }
    public GameObject _LoadScreen { get; private set; }
    public GameObject _SaveScreen { get; private set; }
    public GameObject _LoadingObject { get; private set; }
    public Transform _EnvironmentTransform { get; private set; }
    public Transform _NPCHolderTransform { get; private set; }
    public InventorySlotUI _InteractMenuSlotUI { get; set; }
    public InventorySlotUI _LastClickedSlotUI { get; set; }
    public InventorySlotUI _InventoryCarryModeSlotUI { get; set; }
    public bool _IsCarryUIFromGamepad { get; set; }
    public Image _InventoryCarryModeSlotUIImage { get; set; }
    public RectTransform _InventoryCarryModeSlotUITransform { get; set; }


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

    private bool _updateInventoryBuffer;
    public InteractBoxUI _InteractBoxUI { get; private set; }
    public float _CarryUITimerForMouse { get; set; }
    public float _CarryUITimerForGamepad { get; set; }
    public int _SplitAmount { get; set; }

    public int _LevelIndex { get; private set; }

    public ushort _NumberOfNpcs;

    public LayerMask _HumanMask;
    public LayerMask _TerrainSolidWaterHumanMask;
    public LayerMask _TerrainSolidWaterMask;
    public LayerMask _TerrainWaterMask;
    public LayerMask _TerrainSolidMask;
    public LayerMask _SolidHumanMask;

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

    private float _mainReflectionProbeUpdateCounter;

    public Queue<float> _fpsValues;

    private void Awake()
    {
        _Instance = this;
        _GraphicRaycaster = FindFirstObjectByType<GraphicRaycaster>();
        _fpsValues = new Queue<float>();
        transform.Find("CharacterCreation").GetComponent<CharacterCreation>().Init();
        Shader.EnableKeyword("_USEGLOBALSNOWLEVEL");
        Shader.EnableKeyword("_PW_GLOBAL_COVER_LAYER");
        Shader.EnableKeyword("_PW_COVER_ENABLED");

        _maleDnaNames = UMAGlobalContext.Instance.GetRace("HumanMale").GetDNANames();
        _femaleDnaNames = UMAGlobalContext.Instance.GetRace("HumanFemaleHighPoly").GetDNANames();

        _ItemHandleDatasInChunk = new List<ItemHandleData>[_NumberOfColumnsForTerrains, _NumberOfRowsForTerrains];
        _ObjectPositionsInChunk = new List<Vector3>[_NumberOfColumnsForTerrains, _NumberOfRowsForTerrains];
        _ObjectRotationsInChunk = new List<Vector3>[_NumberOfColumnsForTerrains, _NumberOfRowsForTerrains];
        _ObjectParentsInChunk = new List<Transform>[_NumberOfColumnsForTerrains, _NumberOfRowsForTerrains];

        _MainCamera = Camera.main.gameObject;
        _Player = GameObject.FindGameObjectWithTag("Player");
        _MainReflectionProbe = _Player.transform.Find("MainReflectionProbe").GetComponent<ReflectionProbe>();

        //Application.targetFrameRate = 60;
        _OptionsScreen = GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("Options").gameObject;
        _LoadingObject = GameObject.FindGameObjectWithTag("UI").transform.Find("Loading").gameObject;
        _LoadScreen = GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("Load").gameObject;

        _LevelIndex = SceneManager.GetActiveScene().buildIndex;
        if (_LevelIndex != 0)
        {
            _mainReflectionProbeUpdateCounter = 100f;
            _EnvironmentTransform = GameObject.FindGameObjectWithTag("EnvironmentTransform").transform;
            _NPCHolderTransform = GameObject.FindGameObjectWithTag("NPCHolderTransform").transform;
            _RangedAimMesh = _EnvironmentTransform.transform.GetChild(0);
            _StopScreen = GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("StopScreen").gameObject;
            _InGameMenu = GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("InGameMenu").gameObject;
            _InventoryScreen = GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("InGameMenu").Find("InventoryScreen").gameObject;
            _AnotherInventoryList = GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("InGameMenu").Find("InventoryScreen").Find("AnotherInventory").gameObject;
            _AnotherInventoryChestList = GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("InGameMenu").Find("InventoryScreen").Find("AnotherInventoryChest").gameObject;
            _InventoryItemInteractPopup = GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("InGameMenu").Find("InventoryScreen").Find("InteractScreen").gameObject;
            _InventoryCarryModeSlotUIImage = GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("InGameMenu").Find("InventoryScreen").Find("CarryImageBackground").Find("CarryImage").GetComponent<Image>();
            _InventoryCarryModeSlotUITransform = GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("InGameMenu").Find("InventoryScreen").Find("CarryImageBackground").GetComponent<RectTransform>();
            _InteractBoxUI = GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("InGameMenu").Find("InventoryScreen").Find("InteractScreen").Find("InteractBox").GetComponent<InteractBoxUI>();
            _InventoryItemInfoPopup = GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("InGameMenu").Find("InventoryScreen").Find("InfoScreen").gameObject;
            _MapScreen = GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("InGameMenu").Find("MapScreen").gameObject;
            _DialogueScreen = GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("InGameMenu").Find("DialogueScreen").gameObject;
            _GameHUD = GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("InGameScreen").gameObject;
            _FpsText = _GameHUD.transform.Find("Fps").GetComponent<TextMeshProUGUI>();
            _StaminaHUDImage = _GameHUD.transform.Find("StaminaBack").Find("Stamina").GetComponent<Image>();
            _StaminaBackgroundHUDImage = _GameHUD.transform.Find("StaminaBack").GetComponent<Image>();
            _StaminaHUDRect = _GameHUD.transform.Find("StaminaBack").Find("Stamina").GetComponent<RectTransform>();
            _HealthInfoHUD = _GameHUD.transform.parent.Find("HealthInfo").gameObject;
            _SaveScreen = GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").Find("Save").gameObject;

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
                _equipmentSlotDefaultImages[i] = _playerEquipmentImages[i].transform.Find("DynamicImage").GetComponent<Image>().sprite;
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
            PredefinedNpcLogic.Init();
            InitDictionaries();
        }
    }
    private void LateUpdate()
    {
        if (!_updateInventoryBuffer) return;

        _updateInventoryBuffer = false;
        UpdateInventoryUI();
    }
    private void Update()
    {
        if (_LevelIndex != 0)
        {
            _PlayerPos = _Player.transform.position;
            UpdateMainReflectionProbe();

            if (_LastClickedSlotUI != null)
            {
                if (M_Input.GetButton("Fire1") && !_LastClickedSlotUI._IsHoverFromGamepad)
                    _CarryUITimerForMouse += Time.unscaledDeltaTime;
                else if (!M_Input.GetButton("Fire1"))
                {
                    _CarryUITimerForMouse = 0f;
                    _LastClickedSlotUI = null;
                }

                if (_CarryUITimerForMouse > 0.06f && !_LastClickedSlotUI._IsCarryMode && !_LastClickedSlotUI._IsHoverFromGamepad && _InventoryCarryModeSlotUI == null)
                {
                    _LastClickedSlotUI.CarryStarted();
                    _IsCarryUIFromGamepad = false;
                }
            }
            if (_InventoryCarryModeSlotUI != null)
            {
                if (_IsCarryUIFromGamepad)
                {
                    if (_InventoryCarryModeSlotUI._IsCarryMode && !M_Input.IsCarryUIPressedForGamepad())
                        _InventoryCarryModeSlotUI.CarryEnded();
                    _InventoryCarryModeSlotUITransform.position = GamepadMouse._Instance._CursorRect.position;
                }
                else
                {
                    if (_InventoryCarryModeSlotUI._IsCarryMode && !M_Input.GetButton("Fire1"))
                        _InventoryCarryModeSlotUI.CarryEnded();
                    _InventoryCarryModeSlotUITransform.position = Input.mousePosition;
                }

            }

            if (_InteractMenuSlotUI != null && ((M_Input.GetButtonDown("Fire1") && !_InteractBoxUI.IsHovered(false)) || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame && !_InteractBoxUI.IsHovered(true))))
            {
                _InventoryItemInteractPopup.SetActive(false);
            }

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

        if (Input.GetMouseButton(0) && (_GameHUD != null && !_GameHUD.activeInHierarchy))//gamepad click for split slider handled in gamepad mouse
        {
            Slider slider = GetOnSplitSlider();
            if (slider != null)
            {
                UpdateSliderFromCursor(slider, Input.mousePosition);
            }
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
        _NameToPrefabMesh = new Dictionary<string, AssetReferenceGameObject>();
        _NameToPrefabMesh.Add("Punch", AddressablesController._Instance._ItemContainer);
        _NameToPrefabMesh.Add("Apple", AddressablesController._Instance._ItemContainer);
        _NameToPrefabMesh.Add("ChestArmor_1", AddressablesController._Instance._ChestArmor_1_Item);
        _NameToPrefabMesh.Add("LongSword_1", AddressablesController._Instance._LongSword_1_Item);
        _NameToPrefabMesh.Add("Crossbow", AddressablesController._Instance._Crossbow_Item);
        _NameToPrefabMesh.Add("SurvivalBow", AddressablesController._Instance._SurvivalBow_Item);
        _NameToPrefabMesh.Add("HuntingBow", AddressablesController._Instance._HuntingBow_Item);
        _NameToPrefabMesh.Add("CompositeBow", AddressablesController._Instance._CompositeBow_Item);
        _NameToPrefabMesh.Add("BoltArrow", AddressablesController._Instance._ItemContainer);
        _NameToPrefabMesh.Add("Arrow", AddressablesController._Instance._ItemContainer);
        _NameToPrefabMesh.Add("Backpack", AddressablesController._Instance._Backpack_Item);
        _NameToPrefabMesh.Add("Apple2", AddressablesController._Instance._ItemContainer);
        _NameToPrefabMesh.Add("Apple3", AddressablesController._Instance._ItemContainer);
        _NameToPrefabMesh.Add("Apple4", AddressablesController._Instance._ItemContainer);
        _NameToPrefabMesh.Add("Apple5", AddressablesController._Instance._ItemContainer);
        _NameToPrefabMesh.Add("Apple6", AddressablesController._Instance._ItemContainer);

        _NameToPrefabCollider = new Dictionary<string, AssetReferenceGameObject>();
        _NameToPrefabCollider.Add("Punch", AddressablesController._Instance._ItemContainer);
        _NameToPrefabCollider.Add("Apple", AddressablesController._Instance._ItemContainer);
        _NameToPrefabCollider.Add("ChestArmor_1", AddressablesController._Instance._ChestArmor_1_Item);
        _NameToPrefabCollider.Add("LongSword_1", AddressablesController._Instance._LongSword_1_Item);
        _NameToPrefabCollider.Add("Crossbow", AddressablesController._Instance._Crossbow_Item);
        _NameToPrefabCollider.Add("SurvivalBow", AddressablesController._Instance._SurvivalBow_Item);
        _NameToPrefabCollider.Add("HuntingBow", AddressablesController._Instance._HuntingBow_Item);
        _NameToPrefabCollider.Add("CompositeBow", AddressablesController._Instance._CompositeBow_Item);
        _NameToPrefabCollider.Add("BoltArrow", AddressablesController._Instance._ItemContainer);
        _NameToPrefabCollider.Add("Arrow", AddressablesController._Instance._ItemContainer);
        _NameToPrefabCollider.Add("Backpack", AddressablesController._Instance._Backpack_Item);
        _NameToPrefabCollider.Add("Apple2", AddressablesController._Instance._ItemContainer);
        _NameToPrefabCollider.Add("Apple3", AddressablesController._Instance._ItemContainer);
        _NameToPrefabCollider.Add("Apple4", AddressablesController._Instance._ItemContainer);
        _NameToPrefabCollider.Add("Apple5", AddressablesController._Instance._ItemContainer);
        _NameToPrefabCollider.Add("Apple6", AddressablesController._Instance._ItemContainer);


        _NameToSprite = new Dictionary<string, AssetReferenceSprite>();
        _NameToSprite.Add("Apple", AddressablesController._Instance._Apple_Sprite);
        _NameToSprite.Add("ChestArmor_1", AddressablesController._Instance._ChestArmor_1_Sprite);
        _NameToSprite.Add("LongSword_1", AddressablesController._Instance._LongSword_1_Sprite);
        _NameToSprite.Add("Crossbow", AddressablesController._Instance._Crossbow_Sprite);
        _NameToSprite.Add("SurvivalBow", AddressablesController._Instance._SurvivalBow_Sprite);
        _NameToSprite.Add("HuntingBow", AddressablesController._Instance._HuntingBow_Sprite);
        _NameToSprite.Add("CompositeBow", AddressablesController._Instance._CompositeBow_Sprite);
        _NameToSprite.Add("BoltArrow", AddressablesController._Instance._Bolt_Sprite);
        _NameToSprite.Add("Arrow", AddressablesController._Instance._Arrow_Sprite);
        _NameToSprite.Add("Backpack", AddressablesController._Instance._Backpack_Sprite);
        _NameToSprite.Add("Apple2", AddressablesController._Instance._Apple_Sprite);
        _NameToSprite.Add("Apple3", AddressablesController._Instance._Apple_Sprite);
        _NameToSprite.Add("Apple4", AddressablesController._Instance._Apple_Sprite);
        _NameToSprite.Add("Apple5", AddressablesController._Instance._Apple_Sprite);
        _NameToSprite.Add("Apple6", AddressablesController._Instance._Apple_Sprite);
        //_ItemNameToSprite.Add("Copper Coin", AddressablesController._Instance._AppleSprite);

        _AnimNameToAttackStartTime = new Dictionary<string, float>();
        _AnimNameToAttackEndTime = new Dictionary<string, float>();
        _AnimNameToAttackStartTime.Add("Right_Punch", 0.2f);
        _AnimNameToAttackEndTime.Add("Right_Punch", 0.45f);
        _AnimNameToAttackStartTime.Add("Left_Punch", 0.2f);
        _AnimNameToAttackEndTime.Add("Left_Punch", 0.45f);
        _AnimNameToAttackStartTime.Add("Right_Kick", 0.2f);
        _AnimNameToAttackEndTime.Add("Right_Kick", 0.45f);
        _AnimNameToAttackStartTime.Add("Left_Kick", 0.2f);
        _AnimNameToAttackEndTime.Add("Left_Kick", 0.45f);
        _AnimNameToAttackStartTime.Add("LongSword_Right", 0.3f);
        _AnimNameToAttackEndTime.Add("LongSword_Right", 0.5f);
        _AnimNameToAttackStartTime.Add("LongSword_Left", 0.3f);
        _AnimNameToAttackEndTime.Add("LongSword_Left", 0.5f);

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
    public float LinearLerpFloat(float startValue, float endValue, float speed, double startTime)
    {
        double endTime = startTime + 1 / speed;
        return Mathf.Lerp(startValue, endValue, (float)((Time.timeAsDouble - startTime) / (endTime - startTime)));
    }
    /// <param name="speed">1/second</param>
    public Vector2 LinearLerpVector2(Vector2 startValue, Vector2 endValue, float speed, double startTime)
    {
        double endTime = startTime + 1 / speed;
        return Vector2.Lerp(startValue, endValue, (float)((Time.timeAsDouble - startTime) / (endTime - startTime)));
    }
    /// <param name="speed">1/second</param>
    public Vector3 LinearLerpVector3(Vector3 startValue, Vector3 endValue, float speed, double startTime)
    {
        double endTime = startTime + 1 / speed;
        return Vector3.Lerp(startValue, endValue, (float)((Time.timeAsDouble - startTime) / (endTime - startTime)));
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
        if (percentage == 0f) return false;
        return percentage >= UnityEngine.Random.Range(0f, 100f);
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
        Physics.Raycast(pos + Vector3.up * 1500f, -Vector3.up, out RaycastHit hit, 2000f, _TerrainWaterMask);
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


    public void LoadChunk(int x, int y, bool isFromReloadChunk = false)
    {
        if (AddressablesController._Instance._IsChunkLoading[x, y]) return;
        AddressablesController._Instance._IsChunkLoading[x, y] = true;
        AddressablesController._Instance._IsChunkLoadedToScene[x, y] = true;
        CoroutineCall(ref AddressablesController._Instance._IsChunkLoadingCoroutines[x, y], LoadChunkCoroutine(x, y, isFromReloadChunk), this);
    }
    private IEnumerator LoadChunkCoroutine(int x, int y, bool isFromReloadChunk)
    {
        while (AddressablesController._Instance._IsChunkUnloading[x, y])
        {
            yield return null;
        }
        var handles = new List<AsyncOperationHandle>();
        handles.AddRange(AddressablesController._Instance.LoadTerrainObjects(x, y));
        //if (!isFromReloadChunk)
        //handles.AddRange(AddressablesController._Instance.SpawnNpcs(x, y));
        // animals, plants vs...
        var groupHandle = Addressables.ResourceManager.CreateGenericGroupOperation(handles);

        yield return null;
        yield return groupHandle;
        AddressablesController._Instance._IsChunkLoading[x, y] = false;
    }
    public void UnloadChunk(int x, int y, bool isFromReloadChunk = false)
    {
        if (AddressablesController._Instance._IsChunkUnloading[x, y]) return;
        AddressablesController._Instance._IsChunkUnloading[x, y] = true;
        AddressablesController._Instance._IsChunkLoadedToScene[x, y] = false;
        CoroutineCall(ref AddressablesController._Instance._IsChunkUnloadingCoroutines[x, y], UnloadChunkCoroutine(x, y, isFromReloadChunk), this);
    }
    private IEnumerator UnloadChunkCoroutine(int x, int y, bool isFromReloadChunk)
    {
        while (AddressablesController._Instance._IsChunkLoading[x, y])
        {
            yield return null;
        }

        AddressablesController._Instance.UnloadTerrainObjects(x, y);
        //if (!isFromReloadChunk)
        //AddressablesController._Instance.DespawnNpcs(x, y);
        // animals, plants vs...

        yield return null;

        AddressablesController._Instance._IsChunkUnloading[x, y] = false;
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

        UnloadChunk(x, y, true);
        LoadChunk(x, y, true);
    }
    public Vector2Int GetChunkFromPosition(Vector3 pos)
    {
        int x = (int)(pos.x / _TerrainDimensionMagnitude);
        int y = (int)(pos.z / _TerrainDimensionMagnitude);
        return new Vector2Int(x, y);
    }
    public ItemHandleData CreateEnvironmentPrefabToWorld(string name, Transform parent, Vector3 pos, Vector3 angles)
    {
        //For Mesh Spawn
        ItemHandleData itemHandleData = new ItemHandleData();
        CreateItemPrefabToWorld(itemHandleData, parent, pos, angles);
        itemHandleData._NameForEnvironmentMeshes = name;

        //For Collider Spawn
        itemHandleData._ColliderSpawnHandle = AddressablesController._Instance.SpawnConstantColliderToWorld(itemHandleData, _NameToPrefabCollider[name], pos, angles, parent);

        return itemHandleData;
    }
    public void DestroyEnvironmentPrefabFromWorld(ItemHandleData itemHandleData)
    {
        //For Mesh Destroy
        DestroyItemPrefabFromWorld(itemHandleData);

        //For Collider Destroy
        if (itemHandleData._ColliderSpawnHandle.HasValue)
            AddressablesController._Instance.DestroyConstantColliderFromWorld(itemHandleData._ColliderSpawnHandle.Value);
    }
    public void CreateNewCarriableObjectToWorld(Item item, Vector3 spawnPos)
    {
        Physics.Raycast(spawnPos, -Vector3.up, out RaycastHit hit, 30f, _TerrainSolidWaterMask);
        spawnPos = hit.point;
        CreateItemPrefabToWorld(item._ItemHandleData, _EnvironmentTransform, spawnPos, Vector3.zero);
    }
    public void CreateItemPrefabToWorld(ItemHandleData itemHandleData, Transform parent, Vector3 pos, Vector3 angles)
    {
        if (itemHandleData == null) { Debug.LogError("itemhandledata is null! cannot create."); return; }
        Vector2Int chunk = GetChunkFromPosition(pos);
        if (_ItemHandleDatasInChunk[chunk.x, chunk.y] != null && _ItemHandleDatasInChunk[chunk.x, chunk.y].Contains(itemHandleData)) { Debug.LogError("itemhandledata already exist in list! cannot create."); return; }

        if (_ItemHandleDatasInChunk[chunk.x, chunk.y] == null)
            _ItemHandleDatasInChunk[chunk.x, chunk.y] = new List<ItemHandleData>();
        _ItemHandleDatasInChunk[chunk.x, chunk.y].Add(itemHandleData);
        if (_ObjectPositionsInChunk[chunk.x, chunk.y] == null)
            _ObjectPositionsInChunk[chunk.x, chunk.y] = new List<Vector3>();
        if (_ObjectRotationsInChunk[chunk.x, chunk.y] == null)
            _ObjectRotationsInChunk[chunk.x, chunk.y] = new List<Vector3>();
        if (_ObjectParentsInChunk[chunk.x, chunk.y] == null)
            _ObjectParentsInChunk[chunk.x, chunk.y] = new List<Transform>();
        _ObjectPositionsInChunk[chunk.x, chunk.y].Add(pos);
        _ObjectRotationsInChunk[chunk.x, chunk.y].Add(angles);
        _ObjectParentsInChunk[chunk.x, chunk.y].Add(parent);

        if (itemHandleData._ItemRef != null)
        {
            if (itemHandleData._ColliderSpawnHandle.HasValue)
                Debug.LogError("_ColliderSpawnHandle is not null! : " + itemHandleData._ColliderSpawnHandle.Value.ToString());
            itemHandleData._ColliderSpawnHandle = AddressablesController._Instance.SpawnConstantColliderToWorld(itemHandleData, itemHandleData._ItemRef._ItemDefinition._AssetRefCollider, pos, angles, parent);
        }

        ReloadChunk(chunk.x, chunk.y);
    }
    public void DestroyItemPrefabFromWorld(ItemHandleData itemHandleData)
    {
        if (itemHandleData == null) { Debug.LogError("itemhandledata is null! cannot destroy."); return; }
        if (itemHandleData._CarriableObjectReferance == null) { Debug.LogError("_CarriableObjectReferance is null! cannot destroy."); return; }

        int x = itemHandleData._CarriableObjectReferance._Chunk.x, y = itemHandleData._CarriableObjectReferance._Chunk.y;
        int i = _ItemHandleDatasInChunk[x, y].IndexOf(itemHandleData);
        if (_ItemHandleDatasInChunk[x, y] == null || i >= _ItemHandleDatasInChunk[x, y].Count) return;
        if (_ObjectPositionsInChunk[x, y] == null || i >= _ObjectPositionsInChunk[x, y].Count) return;
        if (_ObjectRotationsInChunk[x, y] == null || i >= _ObjectRotationsInChunk[x, y].Count) return;
        if (_ObjectParentsInChunk[x, y] == null || i >= _ObjectParentsInChunk[x, y].Count) return;

        var handles = AddressablesController._Instance._HandlesForSpawned;
        /*if (itemHandleData._SpawnHandle.HasValue && handles[x, y] != null && handles[x, y].Contains(itemHandleData._SpawnHandle.Value))
        {
            AddressablesController._Instance.DespawnObj(itemHandleData._SpawnHandle.Value);
            handles[x, y].Remove((itemHandleData._SpawnHandle.Value));
        }*/
        itemHandleData._MeshSpawnHandle = null;
        _ItemHandleDatasInChunk[x, y].RemoveAt(i);
        _ObjectPositionsInChunk[x, y].RemoveAt(i);
        _ObjectRotationsInChunk[x, y].RemoveAt(i);
        _ObjectParentsInChunk[x, y].RemoveAt(i);

        if (itemHandleData._ColliderSpawnHandle.HasValue && itemHandleData._ColliderSpawnHandle.Value.IsValid())
            AddressablesController._Instance.DestroyConstantColliderFromWorld(itemHandleData._ColliderSpawnHandle.Value);

        ReloadChunk(x, y);
    }
    public void DestroyProjectileFromWorld(CarriableObject carriableObject)
    {
        Destroy(carriableObject.gameObject);
    }
    public void SetTerrainLinks(GameObject obj)
    {
        var links = obj.GetComponentsInChildren<NavMeshLink>();
        foreach (NavMeshLink item in links)
        {
            if (item.CompareTag("LinkWithTerrain"))
            {
                Physics.Raycast(item.transform.position + item.startPoint + Vector3.up * 2f, -Vector3.up, out RaycastHit hit, 5f, _TerrainWaterMask);
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
        WorldHandler._Instance._Player.SetGender(WorldHandler._Instance._Player._IsMale);

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
        WorldHandler._Instance._Player._Height = WorldHandler._Instance._Player._DnaData["height"];
    }
    public void StartValuesForNewGame()
    {
        WorldHandler._Instance.InitSeasonForNewGame();

        Vector3 pos = _Player.transform.position;//change it with player start pos
        _Player.GetComponent<Humanoid>().SetPosition(pos, true);

        if (SaveSystemHandler._Instance._IsSettingPlayerDataForCreation)
        {
            SetPlayerDataFromMenuCreation();
        }
        else
        {
            //Debug.LogError("Player Character Not Set!");
            WorldHandler._Instance._Player._IsMale = true;
            WorldHandler._Instance._Player.SetGender(WorldHandler._Instance._Player._IsMale);
            SetRandomDNA(WorldHandler._Instance._Player);
            SetRandomWardrobe(WorldHandler._Instance._Player, WorldHandler._Instance._Player._IsMale);
        }

        StartCoroutine(SpawnNpcCoroutine());
    }
    private IEnumerator SpawnNpcCoroutine()
    {
        Vector3 pos;
        NPC createdNpc;
        //Vector2Int chunk;

        int waitCounter = 0;
        for (int i = 0; i < _NumberOfNpcs; i++)
        {
            createdNpc = Instantiate(PrefabHolder._Instance._NpcParent, new Vector3(0f, 100f, 0f), Quaternion.identity, _NPCHolderTransform).GetComponent<NPC>();
            createdNpc.StartCounters(i, _NumberOfNpcs);
            SetRandomNPCValues(createdNpc);
            pos = GetSpawnPosition(createdNpc);
            createdNpc.GetComponent<Humanoid>().SetPosition(pos, true);
            createdNpc._NpcIndex = (ushort)i;
            createdNpc._IsMale = false;
            SetRandomDNA(createdNpc);
            SetRandomWardrobe(createdNpc, createdNpc._IsMale);

            //chunk = GetChunkFromPosition(pos);
            //if (AddressablesController._Instance._NpcListForChunk[chunk.x, chunk.y] == null)
            //AddressablesController._Instance._NpcListForChunk[chunk.x, chunk.y] = new List<GameObject>();
            //AddressablesController._Instance._NpcListForChunk[chunk.x, chunk.y].Add(createdNpc.gameObject);

            if (++waitCounter == 7)
            {
                waitCounter = 0;
                yield return null;
            }
        }
        NPCManager._Instance._IsReady = true;
    }

    private void SetRandomNPCValues(NPC npc)
    {
        //gender, name, characteristics, social class, location, religion and culture, group, family, past events, equipment and ownerships, current goals
    }
    private Vector3 GetSpawnPosition(NPC npc)
    {
        Vector3 pos = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)) * Random.Range(1f, 5f);//////////////
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
        float headSize = 0.4f + (human._FatLevel * 0.5f + human._MuscleLevel) * 0.1f;
        float breastSize = Random.Range(0.1f, 0.325f);
        float neckSize = (human._FatLevel / 2f) + (human._MuscleLevel / 2f);
        if (!human._IsMale) { headSize -= 0.04f; neckSize += 0.05f; }

        foreach (var dnaName in dnaNames)
        {
            value = Random.Range(0.42f, 0.58f) + effectsAll;
            if (dnaName == "feetSize")
                value = 0.435f;
            else if (dnaName == "armLength" || dnaName == "forearmLength" || dnaName == "handsSize" || dnaName == "legsSize")
                value = 0.5f;
            else if (dnaName == "height")
            {
                value = 0.55f + effectsAll * 1.5f;
                if (!human._IsMale) value -= 0.1f;
                human._Height = value;
            }
            else if (dnaName == "breastSize")
                value = breastSize;
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

        float skinValue = Mathf.Pow(Random.Range(0.2f, 1f), 0.25f);
        Color color = new Color(skinValue, skinValue, skinValue, 1f);

        if (human._CharacterColors.ContainsKey("Skin"))
            human._CharacterColors["Skin"] = color;
        else
            human._CharacterColors.Add("Skin", color);

        if (avatar != null)
            human.ChangeColor("Skin", color);

        float redValue = Random.Range(0.1f, 0.5f);
        float greenValue = Random.Range(0.1f, 0.5f);
        float blueValue = Random.Range(0.1f, 0.5f);
        color = new Color(redValue, greenValue, blueValue, 1f);

        if (human._CharacterColors.ContainsKey("Hair"))
            human._CharacterColors["Hair"] = color;
        else
            human._CharacterColors.Add("Hair", color);

        if (avatar != null)
            human.ChangeColor("Hair", color);

        redValue = Random.Range(0.25f, 0.6f);
        greenValue = Random.Range(0.25f, 0.6f);
        blueValue = Random.Range(0.25f, 0.6f);
        color = new Color(redValue, greenValue, blueValue, 1f);

        if (human._CharacterColors.ContainsKey("Eyes"))
            human._CharacterColors["Eyes"] = color;
        else
            human._CharacterColors.Add("Eyes", color);

        if (avatar != null)
            human.ChangeColor("Eyes", color);

        if (avatar != null && avatar.BuildCharacterEnabled)
        {
            avatar.BuildCharacterEnabled = false;
            avatar.BuildCharacterEnabled = true;
        }
    }
    private void SetRandomWardrobe(Humanoid human, bool isMale)
    {
        human._WardrobeData = new List<UMATextRecipe>();
        var list = NPCManager._Instance.GetRandomHair(isMale);
        foreach (UMATextRecipe recipe in list)
        {
            human.WearWardrobe(recipe);
        }
        list = NPCManager._Instance.GetRandomCloth(isMale);
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
        return Physics.Raycast(position, Vector3.up, 150f, _TerrainSolidMask);
    }
    public ICanGetHurt GetHurtable(Transform tr)
    {
        Transform parent = tr;
        ICanGetHurt component;
        while (parent.parent != null)
        {
            parent = parent.parent;
            if (parent.gameObject.TryGetComponent(out component))
            {
                return component;
            }
        }
        return null;
    }
    public void OpenInGameMenuScreen()
    {
        _InGameMenu.SetActive(true);
        OpenInGameMenu(_InGameMenuNumber);
    }
    public void CloseInGameMenuScreen()
    {
        if (_InventoryCarryModeSlotUI != null)
            _InventoryCarryModeSlotUI.CarryEnded(false);

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
            UpdateInventoryUIBuffer();
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
        if (_InventoryCarryModeSlotUI != null)
            _InventoryCarryModeSlotUI.CarryEnded(false);

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
            GamepadMouse._Instance._RectTransformTargets.Add(created.GetComponent<RectTransform>());
            created.GetComponent<Button>().onClick.AddListener(() => PlayButtonSound());
            created.GetComponent<Button>().onClick.AddListener(() => { SaveSystemHandler._Instance._ActiveSave = c; LoadScene(1); });
            GamepadMouse._Instance._RectTransformTargets.Add(created.transform.Find("DeleteButton").GetComponent<RectTransform>());
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

    public void TakeOrSendFromInteractMenu(bool isBackCarry)
    {
        if (_InteractMenuSlotUI == null || _InteractMenuSlotUI._ItemRef == null) return;
        TakeOrSend(_InteractMenuSlotUI, isBackCarry, false);
    }
    public void TakeOrSend(InventorySlotUI slotUI, bool isBackCarry, bool isSendingAll)
    {
        if (slotUI._ItemRef is CarryItem && isBackCarry) { Debug.LogError("carry item cannot go into the carry inventory!"); return; }

        if (slotUI._ItemRef is ICanBeEquipped)
            TakeOrSendCommon(slotUI._ItemRef, isBackCarry, slotUI._IsPlayerInventory, slotUI._IsBackCarryInventory);
        else
            TakeOrSendWithSplit(slotUI, isBackCarry, isSendingAll);
        _InventoryItemInteractPopup.SetActive(false);
    }
    private bool TakeOrSendCommon(Item itemref, bool isBackCarry, bool isPlayerInventory, bool isBackCarryInventory)
    {
        if (isBackCarry && (isPlayerInventory ? WorldHandler._Instance._Player._BackCarryItemRef == null : _AnotherInventory?._InventoryHolder?._Human?._BackCarryItemRef == null)) return false;

        Inventory takerInventory = null;
        if (isBackCarry)
            takerInventory = isBackCarryInventory ? (isPlayerInventory ? WorldHandler._Instance._Player._Inventory : _AnotherInventory) : (isPlayerInventory ? (WorldHandler._Instance._Player._BackCarryItemRef as CarryItem)?._Inventory : (_AnotherInventory?._InventoryHolder?._Human?._BackCarryItemRef as CarryItem)?._Inventory);
        else
            takerInventory = isPlayerInventory ? _AnotherInventory : WorldHandler._Instance._Player._Inventory;

        if (takerInventory == null)
        {
            //Debug.Log("target inv is null, dropping.");
            if (itemref._IsEquipped)
                itemref.Unequip(true, false);
            else
                itemref.DropFrom(true);
        }
        else
        {
            if (!takerInventory.CanTakeThisItem(itemref)) return false;
            if (itemref._IsEquipped)
                itemref.Unequip(false, false);

            itemref.TakenTo(takerInventory);
        }
        return true;
    }
    public void TakeOrSendWithSplit(InventorySlotUI slotUI, bool isBackCarry, bool isSendingAll)
    {
        if (slotUI == null || slotUI._ItemRef == null) return;
        int amount = isSendingAll ? slotUI._ItemRef._Count : _SplitAmount;
        if (amount == 0) return;
        if (slotUI._ItemRef is ICanBeEquipped) return;
        if (slotUI._ItemRef._Count < amount) return;

        Item splitItem = slotUI._ItemRef.Copy();
        splitItem._IsSplittingBuffer = true;
        splitItem._Count = amount;

        Inventory baseInv = slotUI._ItemRef._AttachedInventory;

        bool isProcessed = TakeOrSendCommon(splitItem, isBackCarry, slotUI._IsPlayerInventory, slotUI._IsBackCarryInventory);
        if (isProcessed)
        {
            slotUI._ItemRef._Count -= amount;
            if (slotUI._ItemRef._Count == 0)
                slotUI._ItemRef.DropFrom(false);
            else
                slotUI._ItemRef.SetCurrentCarryCapacityUse(baseInv);

            if (baseInv.IsInventoryVisibleInScreen())
                UpdateInventoryUIBuffer();
        }

        _InventoryItemInteractPopup.SetActive(false);
    }
    public void SplitAmountArrange(float normalizedSplit)
    {
        if (_InteractMenuSlotUI == null) return;
        normalizedSplit = normalizedSplit > 1f ? 1f : normalizedSplit;
        _InventoryItemInteractPopup.transform.Find("SplitSlider").GetComponent<Slider>().value = normalizedSplit;

        _SplitAmount = Mathf.RoundToInt(_InteractMenuSlotUI._ItemRef._Count * normalizedSplit);
        _InventoryItemInteractPopup.transform.Find("SplitSlider").Find("SplitAmountText").GetComponent<TextMeshProUGUI>().text = _SplitAmount.ToString();
        _InventoryItemInteractPopup.transform.Find("TakeSend").GetComponent<Button>().interactable = _SplitAmount != 0 || _InteractMenuSlotUI._ItemRef is ICanBeEquipped;
        _InventoryItemInteractPopup.transform.Find("TakeSendBackCarry").GetComponent<Button>().interactable = (_SplitAmount != 0 || _InteractMenuSlotUI._ItemRef is ICanBeEquipped) && IsInventoryHaveBackCarryOrSelf(_InteractMenuSlotUI._ItemRef._AttachedInventoryCommon) && !(_InteractMenuSlotUI._ItemRef is CarryItem);
    }
    public void UpdateSliderFromCursor(Slider slider, Vector3 pos)
    {
        RectTransform sliderRect = slider.GetComponent<RectTransform>();
        Vector2 localPos;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            sliderRect,
            pos,
            null,
            out localPos
        );

        float normalized = Mathf.InverseLerp(
            sliderRect.rect.xMin,
            sliderRect.rect.xMax,
            localPos.x
        );

        slider.value = normalized;
    }
    private Slider GetOnSplitSlider()
    {
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, Input.mousePosition);
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPos,
            radius = Vector2.one
        };
        List<RaycastResult> results = new List<RaycastResult>();
        _GraphicRaycaster.Raycast(pointerData, results);
        Slider realSlider = null;
        bool isBlocked = false;
        foreach (var item in results)
        {
            if (item.gameObject != null && item.gameObject.name.Equals("SliderBlocker"))
                isBlocked = true;
            if (item.gameObject != null && item.gameObject.name == "Background" && item.gameObject.transform.parent.TryGetComponent<Slider>(out Slider slider) && slider.name.Equals("SplitSlider"))
                realSlider = slider;
        }
        if (isBlocked) realSlider = null;
        return realSlider;
    }
    public void EquipOrUnequipFromInteractMenu()
    {
        if (!_InteractMenuSlotUI._ItemRef._ItemDefinition._CanBeEquipped) return;

        bool isEquipped = _InteractMenuSlotUI._ItemRef._IsEquipped;
        if (isEquipped)
            _InteractMenuSlotUI._ItemRef.Unequip(false, true);
        else
            _InteractMenuSlotUI._ItemRef.Equip(WorldHandler._Instance._Player._Inventory);

        _InventoryItemInteractPopup.SetActive(false);
    }
    public void ConsumeItemFromInteractMenu()
    {
        if (_InteractMenuSlotUI._ItemRef._ItemDefinition is ICanBeConsumed iConsumed)
        {
            iConsumed.Consume(_InteractMenuSlotUI._ItemRef);
            _InventoryItemInteractPopup.SetActive(false);
        }
    }

    public bool IsInventoryHaveBackCarryOrSelf(Inventory inventory)
    {
        if (inventory._IsBackCarry) return true;
        if (inventory._IsHuman && inventory._InventoryHolder?._Human?._BackCarryItemRef != null) return true;
        return false;
    }
    public void TakeAllButton(int i)
    {
        switch (i)
        {
            case 0:
                TakeAll(_anotherInventoryChestImages, WorldHandler._Instance._Player._Inventory);
                break;
            case 1:
                TakeAll(_anotherInventoryImages, WorldHandler._Instance._Player._Inventory);
                break;
            case 2:
                TakeAll(_anotherEquipmentImages, WorldHandler._Instance._Player._Inventory);
                break;
            case 3:
                TakeAll(_anotherInventoryBackCarryImages, WorldHandler._Instance._Player._Inventory);
                break;
            default:
                break;
        }
    }
    public void TakeAll(Image[] itemImages, Inventory takerInventory)
    {
        List<Item> tempItems = new List<Item>();
        foreach (var itemImage in itemImages)
        {
            tempItems.Add(itemImage.GetComponent<InventorySlotUI>()._ItemRef);
        }

        for (int i = 0; i < tempItems.Count; i++)
        {
            if (tempItems[i] != null)
                tempItems[i].TakenTo(takerInventory);
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
    private float GetAvarageFps()
    {
        int count = 0;
        float sum = 0f;
        foreach (float value in _fpsValues)
        {
            count++;
            sum += value;
        }
        return sum / count;
    }
    public void UpdateInGameUI(float staminaNormalized)
    {
        if (Time.unscaledDeltaTime != 0f)
        {
            _fpsValues.Enqueue(1f / Time.unscaledDeltaTime);
            if (_fpsValues.Count > 60)
                _fpsValues.Dequeue();
            _FpsText.text = GetAvarageFps().ToString("F0");
        }
        else
            _FpsText.text = 0f.ToString("F0");

        _StaminaHUDRect.localScale = new Vector3(staminaNormalized, 1f, 1f);

        Color color = GetNormalizedColor(staminaNormalized);
        color.a = (1 - staminaNormalized) * 0.75f;
        _StaminaHUDImage.color = color;

        color = _StaminaBackgroundHUDImage.color;
        color.a = Mathf.Clamp01((1f - staminaNormalized) * 10f) * 0.75f;
        _StaminaBackgroundHUDImage.color = color;

        ArrangeHealthHUD();
    }
    private void ArrangeHealthHUD()
    {
        Player player = WorldHandler._Instance._Player;
        if (player._HealthSystem._IsDead)
            OpenOrCloseHealthUI(_HealthInfoHUD.transform.Find("Dead").gameObject, true);
        else
            OpenOrCloseHealthUI(_HealthInfoHUD.transform.Find("Dead").gameObject, false);

        if (player._HealthSystem._IsUnconscious)
            OpenOrCloseHealthUI(_HealthInfoHUD.transform.Find("Unconscious").gameObject, true);
        else
            OpenOrCloseHealthUI(_HealthInfoHUD.transform.Find("Unconscious").gameObject, false);

        if (player._HealthSystem._BloodLevel < 100f)
        {
            OpenOrCloseHealthUI(_HealthInfoHUD.transform.Find("BloodLevel").gameObject, true);
            float amount = player._HealthSystem._BloodLevel / 100f;
            _HealthInfoHUD.transform.Find("BloodLevel").Find("SlicedImage").GetComponent<SlicedFilledImage>().fillAmount = amount;
            _HealthInfoHUD.transform.Find("BloodLevel").GetComponent<Image>().color = new Color(1f, 1f, 1f, Mathf.Pow(1f - amount, 0.4f));
            _HealthInfoHUD.transform.Find("BloodLevel").Find("SlicedImage").GetComponent<SlicedFilledImage>().color = new Color(1f, 0f, 0f, Mathf.Pow(1f - amount, 0.4f));
        }
        else
            OpenOrCloseHealthUI(_HealthInfoHUD.transform.Find("BloodLevel").gameObject, false);

        if (player._HealthSystem._BleedingOverTime != 0f)
        {
            OpenOrCloseHealthUI(_HealthInfoHUD.transform.Find("Bleeding").gameObject, true);
            float amount = player._HealthSystem._BleedingOverTime / player._HealthSystem._BleedingMaxValue;
            _HealthInfoHUD.transform.Find("Bleeding").Find("SlicedImage").GetComponent<SlicedFilledImage>().fillAmount = amount;
            _HealthInfoHUD.transform.Find("Bleeding").GetComponent<Image>().color = new Color(1f, 1f, 1f, Mathf.Pow(amount, 0.4f));
            _HealthInfoHUD.transform.Find("Bleeding").Find("SlicedImage").GetComponent<SlicedFilledImage>().color = new Color(1f, 0f, 0f, Mathf.Pow(amount, 0.4f));
        }
        else
            OpenOrCloseHealthUI(_HealthInfoHUD.transform.Find("Bleeding").gameObject, false);

        if (player._HealthSystem._Sickness != 0f)
        {
            OpenOrCloseHealthUI(_HealthInfoHUD.transform.Find("Sickness").gameObject, true);
            float amount = player._HealthSystem._Sickness / 100f;
            _HealthInfoHUD.transform.Find("Sickness").Find("SlicedImage").GetComponent<SlicedFilledImage>().fillAmount = amount;
            _HealthInfoHUD.transform.Find("Sickness").GetComponent<Image>().color = new Color(1f, 1f, 1f, Mathf.Pow(amount, 0.4f));
            _HealthInfoHUD.transform.Find("Sickness").Find("SlicedImage").GetComponent<SlicedFilledImage>().color = new Color(1f, 0f, 0f, Mathf.Pow(amount, 0.4f));
        }
        else
            OpenOrCloseHealthUI(_HealthInfoHUD.transform.Find("Sickness").gameObject, false);

        if (player._HealthSystem._HeadWoundAmount != 0f)
        {
            OpenOrCloseHealthUI(_HealthInfoHUD.transform.Find("HeadDamage").gameObject, true);
            float amount = player._HealthSystem._HeadWoundAmount / 100f;
            _HealthInfoHUD.transform.Find("HeadDamage").Find("SlicedImage").GetComponent<SlicedFilledImage>().fillAmount = amount;
            _HealthInfoHUD.transform.Find("HeadDamage").GetComponent<Image>().color = new Color(1f, 1f, 1f, Mathf.Pow(amount, 0.4f));
            _HealthInfoHUD.transform.Find("HeadDamage").Find("SlicedImage").GetComponent<SlicedFilledImage>().color = new Color(1f, 0f, 0f, Mathf.Pow(amount, 0.4f));
        }
        else
            OpenOrCloseHealthUI(_HealthInfoHUD.transform.Find("HeadDamage").gameObject, false);

        if (player._HealthSystem._HandsWoundAmount != 0f)
        {
            OpenOrCloseHealthUI(_HealthInfoHUD.transform.Find("HandsDamage").gameObject, true);
            float amount = player._HealthSystem._HandsWoundAmount / 100f;
            _HealthInfoHUD.transform.Find("HandsDamage").Find("SlicedImage").GetComponent<SlicedFilledImage>().fillAmount = amount;
            _HealthInfoHUD.transform.Find("HandsDamage").GetComponent<Image>().color = new Color(1f, 1f, 1f, Mathf.Pow(amount, 0.4f));
            _HealthInfoHUD.transform.Find("HandsDamage").Find("SlicedImage").GetComponent<SlicedFilledImage>().color = new Color(1f, 0f, 0f, Mathf.Pow(amount, 0.4f));
        }
        else
            OpenOrCloseHealthUI(_HealthInfoHUD.transform.Find("HandsDamage").gameObject, false);

        if (player._HealthSystem._ChestWoundAmount != 0f)
        {
            OpenOrCloseHealthUI(_HealthInfoHUD.transform.Find("ChestDamage").gameObject, true);
            float amount = player._HealthSystem._ChestWoundAmount / 100f;
            _HealthInfoHUD.transform.Find("ChestDamage").Find("SlicedImage").GetComponent<SlicedFilledImage>().fillAmount = amount;
            _HealthInfoHUD.transform.Find("ChestDamage").GetComponent<Image>().color = new Color(1f, 1f, 1f, Mathf.Pow(amount, 0.4f));
            _HealthInfoHUD.transform.Find("ChestDamage").Find("SlicedImage").GetComponent<SlicedFilledImage>().color = new Color(1f, 0f, 0f, Mathf.Pow(amount, 0.4f));
        }
        else
            OpenOrCloseHealthUI(_HealthInfoHUD.transform.Find("ChestDamage").gameObject, false);

        if (player._HealthSystem._LegsWoundAmount != 0f)
        {
            OpenOrCloseHealthUI(_HealthInfoHUD.transform.Find("LegsDamage").gameObject, true);
            float amount = player._HealthSystem._LegsWoundAmount / 100f;
            _HealthInfoHUD.transform.Find("LegsDamage").Find("SlicedImage").GetComponent<SlicedFilledImage>().fillAmount = amount;
            _HealthInfoHUD.transform.Find("LegsDamage").GetComponent<Image>().color = new Color(1f, 1f, 1f, Mathf.Pow(amount, 0.4f));
            _HealthInfoHUD.transform.Find("LegsDamage").Find("SlicedImage").GetComponent<SlicedFilledImage>().color = new Color(1f, 0f, 0f, Mathf.Pow(amount, 0.4f));
        }
        else
            OpenOrCloseHealthUI(_HealthInfoHUD.transform.Find("LegsDamage").gameObject, false);
    }
    private void OpenOrCloseHealthUI(GameObject uiObj, bool isOpening)
    {
        if (isOpening && !uiObj.activeSelf)
            uiObj.SetActive(true);
        else if (!isOpening && uiObj.activeSelf)
            uiObj.SetActive(false);
    }

    private string FormatWeight(float value)
    {
        string formatted = value.ToString("F2");
        string[] parts = formatted.Split(',');
        if (parts.Length == 2)
            return $"{parts[0]},<size=70%>{parts[1]}</size>";
        else
            return formatted;
    }
    public void UpdateInventoryUIBuffer()
    {
        _updateInventoryBuffer = true;
    }
    public void UpdateInventoryUIInstant()
    {
        UpdateInventoryUI();
    }
    private void UpdateInventoryUI()
    {
        if (!_InventoryScreen.activeSelf) return;

        //Debug.Log(WorldHandler._Instance._Player._Inventory._CarryCapacityUse + " : " + WorldHandler._Instance._Player._Inventory.ArrangeCurrentCarryCapacityUse());
        string formattedCurrent = FormatWeight(WorldHandler._Instance._Player._Inventory._CarryCapacityUse);
        string formattedLimit = FormatWeight(WorldHandler._Instance._Player._Inventory._CarryCapacity);
        _InventoryScreen.transform.Find("OwnInventory").Find("Info").Find("CarryCapacityText").GetComponent<TextMeshProUGUI>().text = $"{formattedCurrent} / {formattedLimit}";
        _InventoryScreen.transform.Find("OwnInventory").Find("Info").Find("NeedSleep").GetComponent<TextMeshProUGUI>().text = WorldHandler._Instance._Player._NeedSleepAmount.ToString();
        _InventoryScreen.transform.Find("OwnInventory").Find("Info").Find("NeedCleaning").GetComponent<TextMeshProUGUI>().text = WorldHandler._Instance._Player._NeedCleaningAmount.ToString();
        _InventoryScreen.transform.Find("OwnInventory").Find("Info").Find("NeedEat").GetComponent<TextMeshProUGUI>().text = WorldHandler._Instance._Player._NeedEatAmount.ToString();
        _InventoryScreen.transform.Find("OwnInventory").Find("Info").Find("NeedDrink").GetComponent<TextMeshProUGUI>().text = WorldHandler._Instance._Player._NeedDrinkAmount.ToString();
        _InventoryScreen.transform.Find("OwnInventory").Find("Info").Find("NeedPissing").GetComponent<TextMeshProUGUI>().text = WorldHandler._Instance._Player._NeedPissingAmount.ToString();
        _InventoryScreen.transform.Find("OwnInventory").Find("Info").Find("NeedPooping").GetComponent<TextMeshProUGUI>().text = WorldHandler._Instance._Player._NeedPoopingAmount.ToString();

        _InventoryScreen.transform.Find("PrivacyImage").gameObject.SetActive(false);
        _InventoryScreen.transform.Find("OwnInventory").Find("Name").GetComponentInChildren<TextMeshProUGUI>().text = WorldHandler._Instance._Player._Inventory._Name;
        UpdateOneInventory(WorldHandler._Instance._Player._Inventory, _playerInventoryImages, _playerInventoryBackCarryImages);
        UpdateEquipmentUI(WorldHandler._Instance._Player._Inventory, _playerEquipmentImages);

        if (_AnotherInventory != null)
        {
            if (!_AnotherInventory._IsPublic)
                _InventoryScreen.transform.Find("PrivacyImage").gameObject.SetActive(true);

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
        Humanoid human = inventory._InventoryHolder?._Human;
        if (human == null) { Debug.LogError("human is null!"); return; }
        Item item = null;
        for (int i = 0; i < equipmentImages.Length; i++)
        {
            switch (i)
            {
                case 0:
                    item = human._HeadGearItemRef;
                    break;
                case 1:
                    item = human._GlovesItemRef;
                    break;
                case 2:
                    item = human._BackCarryItemRef;
                    break;
                case 3:
                    item = human._ClothingItemRef;
                    break;
                case 4:
                    item = human._ChestArmorItemRef;
                    break;
                case 5:
                    item = human._LegsArmorItemRef;
                    break;
                case 6:
                    item = human._BootsItemRef;
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
            {
                equipmentImages[i].GetComponent<InventorySlotUI>()._ItemRef = null;
                equipmentImages[i].GetComponent<InventorySlotUI>()._Inventory = inventory;
                equipmentImages[i].transform.Find("DynamicImage").GetComponent<Image>().sprite = _equipmentSlotDefaultImages[i];
            }
            else
            {
                equipmentImages[i].GetComponent<InventorySlotUI>()._ItemRef = item;
                equipmentImages[i].GetComponent<InventorySlotUI>()._Inventory = inventory;
                SetSpriteUI(equipmentImages[i].transform.Find("DynamicImage").GetComponent<Image>(), item._ItemDefinition._Name);
            }

            SetDurabilityAndCountUI(equipmentImages[i], item);
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
                ownItemImageComponents[i].GetComponent<InventorySlotUI>()._Inventory = inventory;
                ownItemImageComponents[i].sprite = PrefabHolder._Instance._EmptyItemBackground;
                ownItemImageComponents[i].transform.Find("Count").gameObject.SetActive(false);
                ownItemImageComponents[i].transform.Find("DurabilityBackground").gameObject.SetActive(false);
            }
            else
            {
                ownItemImageComponents[i].GetComponent<InventorySlotUI>()._ItemRef = inventory._Items[i];
                ownItemImageComponents[i].GetComponent<InventorySlotUI>()._Inventory = inventory;
                ownItemImageComponents[i].sprite = PrefabHolder._Instance._LoadingAdressableProcessSprite;
                int iForAction = i;
                SetSpriteUI(ownItemImageComponents[iForAction], inventory._Items[iForAction]._ItemDefinition._Name);
                SetDurabilityAndCountUI(ownItemImageComponents[iForAction], inventory._Items[iForAction]);
            }
        }
        if (!inventory._CanEquip) { }
        else if (inventory._InventoryHolder._Human._BackCarryItemRef == null)
        {
            carryingInventoryItemImageComponents[0].transform.parent.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.07f);
            carryingInventoryItemImageComponents[0].transform.parent.parent.Find("BackCarryFrame").GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.07f);
            for (int i = 0; i < carryingInventoryItemImageComponents.Length; i++)
            {
                carryingInventoryItemImageComponents[i].color = new Color(1f, 1f, 1f, 0.07f);
                carryingInventoryItemImageComponents[i].GetComponent<InventorySlotUI>()._ItemRef = null;
                carryingInventoryItemImageComponents[i].GetComponent<InventorySlotUI>()._Inventory = inventory;
                carryingInventoryItemImageComponents[i].sprite = PrefabHolder._Instance._EmptyItemBackground;
                carryingInventoryItemImageComponents[i].transform.Find("Count").gameObject.SetActive(false);
                carryingInventoryItemImageComponents[i].transform.Find("DurabilityBackground").gameObject.SetActive(false);
            }
        }
        else
        {
            inventory = (inventory._InventoryHolder._Human._BackCarryItemRef as CarryItem)._Inventory;
            itemCount = inventory._Items.Count;

            carryingInventoryItemImageComponents[0].transform.parent.GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f);
            carryingInventoryItemImageComponents[0].transform.parent.parent.Find("BackCarryFrame").GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.8f);
            for (int i = 0; i < carryingInventoryItemImageComponents.Length; i++)
            {
                carryingInventoryItemImageComponents[i].color = new Color(1f, 1f, 1f, 1f);
                if (i >= itemCount)
                {
                    carryingInventoryItemImageComponents[i].GetComponent<InventorySlotUI>()._ItemRef = null;
                    carryingInventoryItemImageComponents[i].GetComponent<InventorySlotUI>()._Inventory = inventory;
                    carryingInventoryItemImageComponents[i].sprite = PrefabHolder._Instance._EmptyItemBackground;
                    carryingInventoryItemImageComponents[i].transform.Find("Count").gameObject.SetActive(false);
                    carryingInventoryItemImageComponents[i].transform.Find("DurabilityBackground").gameObject.SetActive(false);
                }
                else
                {
                    carryingInventoryItemImageComponents[i].GetComponent<InventorySlotUI>()._ItemRef = inventory._Items[i];
                    carryingInventoryItemImageComponents[i].GetComponent<InventorySlotUI>()._Inventory = inventory;
                    carryingInventoryItemImageComponents[i].sprite = PrefabHolder._Instance._LoadingAdressableProcessSprite;
                    int iForAction = i;
                    SetSpriteUI(carryingInventoryItemImageComponents[iForAction], inventory._Items[iForAction]._ItemDefinition._Name);
                    SetDurabilityAndCountUI(carryingInventoryItemImageComponents[iForAction], inventory._Items[iForAction]);
                }
            }
        }
    }
    public void SetDurabilityAndCountUI(Image image, Item item)
    {
        if (item == null)
        {
            if (image.transform.Find("Count") != null)
                image.transform.Find("Count").gameObject.SetActive(false);
            image.transform.Find("DurabilityBackground").gameObject.SetActive(false);
            return;
        }

        if (item is ICanBeEquipped)
        {
            if (image.transform.Find("Count") != null)
                image.transform.Find("Count").gameObject.SetActive(false);
            image.transform.Find("DurabilityBackground").gameObject.SetActive(true);
            float normalizedDurability = (item as ICanBeEquipped)._Durability / (item as ICanBeEquipped)._DurabilityMax;
            image.transform.Find("DurabilityBackground").Find("Durability").GetComponent<Image>().color = GetNormalizedColor(normalizedDurability);
            image.transform.Find("DurabilityBackground").Find("Durability").GetComponent<RectTransform>().localScale = new Vector3(normalizedDurability, 1f, 1f);
        }
        else
        {
            if (image.transform.Find("DurabilityBackground") != null)
                image.transform.Find("DurabilityBackground").gameObject.SetActive(false);
            image.transform.Find("Count").gameObject.SetActive(true);
            image.transform.Find("Count").GetComponent<TextMeshProUGUI>().text = item._Count.ToString();
        }
    }
    private void SetSpriteUI(Image image, string itemName)
    {
        if (!_nameToLoadedSprites.ContainsKey(itemName))
        {
            var handle = _NameToSprite[itemName].LoadAssetAsync();
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

    private Color GetNormalizedColor(float normalizedDurability)
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
    public GameObject GetInventorySlotUIFromPosition(Vector3 pos)
    {
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, pos);
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPos
        };
        List<RaycastResult> results = new List<RaycastResult>();
        _GraphicRaycaster.Raycast(pointerData, results);
        foreach (var item in results)
        {
            if (item.gameObject != null && item.gameObject.GetComponent<InventorySlotUI>() != null)
                return item.gameObject;
        }
        return null;
    }
    public bool IsCursorOnUI(Vector3 pos)
    {
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, pos);
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPos
        };
        List<RaycastResult> results = new List<RaycastResult>();
        _GraphicRaycaster.Raycast(pointerData, results);
        foreach (var item in results)
        {
            if (item.gameObject != null && item.gameObject.GetComponent<Image>() != null && !item.gameObject.name.EndsWith("Screen"))
                return true;
        }
        return false;
    }
    public void UpdateMainReflectionProbe()
    {
        _mainReflectionProbeUpdateCounter += Time.deltaTime;
        if (_mainReflectionProbeUpdateCounter < (IsInClosedSpace(_Player.transform.position) ? 2.5f : 15f))
            return;

        _mainReflectionProbeUpdateCounter = 0f;
        _MainReflectionProbe.RenderProbe();
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

}
