using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FischlWorks;
using UMA;
using UMA.CharacterSystem;
using FIMSpace;

public abstract class Humanoid : MonoBehaviour, ICanGetHurt
{
    public Animator _Animator { get { if (_animator == null) { _animator = _LocomotionSystem.GetComponentInChildren<Animator>(); _animator.updateMode = AnimatorUpdateMode.Fixed; _animator.cullingMode = AnimatorCullingMode.CullCompletely; } return _animator; } }
    private Animator _animator;
    public Rigidbody _Rigidbody { get; protected set; }
    public CapsuleCollider _MainCollider { get; protected set; }
    public LocomotionSystem _LocomotionSystem { get; protected set; }
    public csHomebrewIK _FootIKComponent { get; protected set; }
    public LeaningAnimator _LeaninganimatorComponent { get; protected set; }
    public UMA.PoseTools.ExpressionPlayer _ExpressionPlayer { get; protected set; }
    public DynamicCharacterAvatar _UmaDynamicAvatar { get; protected set; }
    public Dictionary<string, DnaSetter> _DNA { get; private set; }
    public Dictionary<string, float> _DnaData { get; set; }
    public List<UMATextRecipe> _WardrobeData { get; set; }

    //Systems
    public string _Name { get; protected set; }
    public bool _IsMale { get; set; }
    public Class _Class { get; protected set; }
    public Family _Family { get; protected set; }
    public Group _AttachedGroup { get; protected set; }//not instance, a referance
    public Inventory _Inventory { get; protected set; }
    public HealthSystem _HealthSystem { get; protected set; }
    public MovementState _MovementState { get; protected set; }
    public HandState _HandState { get; protected set; }

    //public Characteristic _Characteristic { get; private set; }


    public float _SizeMultiplier { get; private set; }
    public virtual Vector2 _DirectionInput { get; }
    public bool _RunInput { get; set; }
    public bool _SprintInput { get; set; }
    public bool _IsInCombatMode { get; set; }
    public bool _CrouchInput { get; set; }
    public bool _JumpInput { get; set; }
    public bool _InteractInput { get; set; }
    public bool _AttackInput { get; set; }

    public bool _IsStrafing { get; set; }
    public bool _IsJumping { get; set; }
    public bool _IsGrounded { get; set; }
    public bool _IsSprinting { get; set; }
    public bool _StopMove { get; set; }

    private Transform _headTransform;

    [HideInInspector] public float _JumpTimer = 0.15f;
    [HideInInspector] public float _JumpCounter;
    public float _AimSpeed { get { if (_MovementState is Crouch) return 3f; else if (_MovementState is Prone) return 1.25f; else return 5f; } }
    public float _LastTimeRotated { get; set; }

    public RaycastHit _RayFoLook;
    public Coroutine _RotateAroundCoroutine;
    public bool _IsInClosedSpace { get; private set; }

    private float _walkSoundCounter;
    private bool _umaWaitingForCompletion;

    private float _checkForClosedSpaceCounter;
    private float _lastAlphaForSnow;
    private float _targetAlphaForSnow;

    public bool _ChangeShaderCompleted { get; private set; }
    private Coroutine _changeShaderFinishedCoroutine;
    private Vector3 _distanceToPlayer;
    private Dictionary<int, MaterialPropertyBlock> _matPropBlocks;

    #region Method Parameters For Opt
    #endregion

    protected virtual void Awake()
    {
        _matPropBlocks = new Dictionary<int, MaterialPropertyBlock>();
        _LocomotionSystem = GetComponentInChildren<LocomotionSystem>();
        _FootIKComponent = _LocomotionSystem.GetComponentInChildren<csHomebrewIK>();
        _LeaninganimatorComponent = _LocomotionSystem.GetComponentInChildren<LeaningAnimator>();
        _UmaDynamicAvatar = _LocomotionSystem.transform.Find("char").GetComponent<DynamicCharacterAvatar>();
        _ExpressionPlayer = _UmaDynamicAvatar.GetComponent<UMA.PoseTools.ExpressionPlayer>();
        if (_UmaDynamicAvatar != null)
            WorldAndNpcCreation.SetGender(_UmaDynamicAvatar, _IsMale);
        InitOrLoadUmaCharacter();
    }
    protected virtual void Start()
    {
        _LocomotionSystem.Init();
        ArrangeStartingStates();
    }
    protected virtual void Update()
    {
        if (GameManager._Instance._IsGameStopped) return;
        _distanceToPlayer = new Vector3(GameManager._Instance._Player.transform.position.x - transform.position.x, 0f, GameManager._Instance._Player.transform.position.z - transform.position.z);

        if (_UmaDynamicAvatar != null)
            ControlUmaDataRuntimeLoadUnload();

        if (_UmaDynamicAvatar != null && _UmaDynamicAvatar.BuildCharacterEnabled && _MovementState != null)
        {
            ArrangePlaneSound();
            ArrangeStamina();
            ArrangeIsInClosedSpace();
            ArrangeSnowLayer();
            _MovementState.DoState();
            _HandState.DoState();
        }

        if (_umaWaitingForCompletion && _Animator.avatar != null)
            UmaUpdateCompleted();
    }
    private void FixedUpdate()
    {
        if (_Rigidbody.isKinematic) return;

        if (_UmaDynamicAvatar != null && _UmaDynamicAvatar.BuildCharacterEnabled && _MovementState != null)
            _MovementState.FixedUpdate();
    }
    private void OnAnimatorMove()
    {
        ControlAnimatorRootMotion();
    }
    private void InitOrLoadUmaCharacter()
    {
        _UmaDynamicAvatar.CharacterCreated.AddListener((a) => UmaCreated());
        _UmaDynamicAvatar.CharacterUpdated.AddListener((a) => UmaUpdated());
    }
    public void UmaCreated()
    {
        _UmaDynamicAvatar.BuildCharacterEnabled = false;

        _headTransform = _UmaDynamicAvatar.transform.Find("Root").Find("Global").Find("Position").Find("Hips").Find("LowerBack").Find("Spine").Find("Spine1").Find("Neck").Find("Head");
        SetDna(true);
        SetWardrobe();
        UmaUpdated();
    }
    public void UmaUpdated()
    {
        _umaWaitingForCompletion = true;
    }
    private void UmaUpdateCompleted()
    {
        if (_headTransform == null) return;

        _umaWaitingForCompletion = false;
        _SizeMultiplier = (_headTransform.position.y - transform.position.y) / 1.77f;
        ChangeShader();
        if (GetComponentInChildren<csHomebrewIK>() != null)
            GetComponentInChildren<csHomebrewIK>().StartForUma();
        _UmaDynamicAvatar.BuildCharacterEnabled = true;
        _Rigidbody.freezeRotation = false; // animation bug workaround
        GameManager._Instance.CallForAction(() =>
        {
            _animator = _LocomotionSystem.GetComponentInChildren<Animator>(); _animator.updateMode = AnimatorUpdateMode.Fixed; _animator.cullingMode = AnimatorCullingMode.CullCompletely; _Rigidbody.freezeRotation = true;
        }, 0.1f);
    }

    private void ControlUmaDataRuntimeLoadUnload()
    {
        if (this is Player) return;

        bool isBuildEnabled = _UmaDynamicAvatar.BuildCharacterEnabled;

        if (_ExpressionPlayer != null)
        {
            if (!_ExpressionPlayer.enabled && _distanceToPlayer.magnitude < 10f)
                _ExpressionPlayer.enabled = true;
            else if (_ExpressionPlayer.enabled && _distanceToPlayer.magnitude > 15f)
                _ExpressionPlayer.enabled = false;
        }

        if (!isBuildEnabled && _distanceToPlayer.magnitude < 40f)
            EnableHumanData();
        else if (isBuildEnabled && _distanceToPlayer.magnitude > 60f)
            DisableHumanData();
    }
    public void EnableHumanData()
    {
        if ((_MovementState is Locomotion) || (_MovementState is Crouch))
            _FootIKComponent.enabled = true;
        _Animator.enabled = true;
        _LeaninganimatorComponent.enabled = true;
        _LeaninganimatorComponent.GetComponent<TwistBones>().enabled = true;
        _UmaDynamicAvatar.BuildCharacterEnabled = true;

        SetDna(true);
        SetWardrobe();
    }
    public void DisableHumanData()
    {
        _FootIKComponent.enabled = false;
        _Animator.enabled = false;
        _LeaninganimatorComponent.enabled = false;
        _LeaninganimatorComponent.GetComponent<TwistBones>().enabled = false;

        SkinnedMeshRenderer smr = _UmaDynamicAvatar?.transform.Find("UMARenderer")?.GetComponent<SkinnedMeshRenderer>();
        if (smr != null && smr.sharedMesh != null)
        {
            Destroy(smr.sharedMesh);
            smr.sharedMesh = null;
        }

        _UmaDynamicAvatar.BuildCharacterEnabled = false;
        _UmaDynamicAvatar.umaData.CleanAvatar();
        _UmaDynamicAvatar.umaData.CleanMesh(false);
        _UmaDynamicAvatar.umaData.CleanTextures();
    }

    public void WearWardrobe(UMATextRecipe recipe, bool isRefresh = false)
    {
        if (!isRefresh)
        {
            if (_WardrobeData.Contains(recipe)) return;

            UMATextRecipe tempRecipe;
            for (int i = 0; i < _WardrobeData.Count; i++)
            {
                tempRecipe = _WardrobeData[i];
                if (recipe.wardrobeSlot == tempRecipe.wardrobeSlot)
                {
                    Debug.LogError("Wardrobe slot is equipped!");
                    return;
                }
            }

            _WardrobeData.Add(recipe);
        }

        if (_UmaDynamicAvatar == null) return;

        _UmaDynamicAvatar.SetSlot(recipe);

        if (_UmaDynamicAvatar.BuildCharacterEnabled)
        {
            _UmaDynamicAvatar.BuildCharacterEnabled = false;
            _UmaDynamicAvatar.BuildCharacterEnabled = true;
        }
    }
    public void RemoveWardrobe(UMATextRecipe recipe)
    {
        if (!_WardrobeData.Contains(recipe)) return;

        string removedSlot = recipe.wardrobeSlot;
        _WardrobeData.Remove(recipe);

        if (_UmaDynamicAvatar == null) return;

        _UmaDynamicAvatar.ClearSlot(removedSlot);

        for (int i = 0; i < _WardrobeData.Count; i++)
        {
            recipe = _WardrobeData[i];
            if (recipe.wardrobeSlot == removedSlot)
            {
                _UmaDynamicAvatar.SetSlot(recipe);
                break;
            }
        }

        if (_UmaDynamicAvatar.BuildCharacterEnabled)
        {
            _UmaDynamicAvatar.BuildCharacterEnabled = false;
            _UmaDynamicAvatar.BuildCharacterEnabled = true;
        }
    }
    public void ChangeMuscleAmount(bool isIncreasing, int amount = 1, bool isRebuilding = true)
    {
        string muscleName = "armWidth";
        float newAmount = _DnaData[muscleName];
        if (isIncreasing)
            newAmount += 0.0125f * amount;
        else
            newAmount -= 0.0125f * amount;
        newAmount = Mathf.Clamp01(newAmount);
        ChangeDna(muscleName, newAmount, false);

        muscleName = "forearmWidth";
        newAmount = _DnaData[muscleName];
        if (isIncreasing)
            newAmount += 0.0125f * amount;
        else
            newAmount -= 0.0125f * amount;
        newAmount = Mathf.Clamp01(newAmount);
        ChangeDna(muscleName, newAmount, false);

        muscleName = "upperMuscle";
        newAmount = _DnaData[muscleName];
        if (isIncreasing)
            newAmount += 0.025f * amount;
        else
            newAmount -= 0.025f * amount;
        newAmount = Mathf.Clamp01(newAmount);
        ChangeDna(muscleName, newAmount, false);

        muscleName = "lowerMuscle";
        newAmount = _DnaData[muscleName];
        if (isIncreasing)
            newAmount += 0.025f * amount;
        else
            newAmount -= 0.025f * amount;
        newAmount = Mathf.Clamp01(newAmount);
        ChangeDna(muscleName, newAmount, isRebuilding);
    }
    public void ChangeWeightAmount(bool isIncreasing, int amount = 1, bool isRebuilding = true)
    {
        string muscleName = "armWidth";
        float newAmount = _DnaData[muscleName];
        if (isIncreasing)
            newAmount += 0.0125f * amount;
        else
            newAmount -= 0.0125f * amount;
        newAmount = Mathf.Clamp01(newAmount);
        ChangeDna(muscleName, newAmount, false);

        muscleName = "forearmWidth";
        newAmount = _DnaData[muscleName];
        if (isIncreasing)
            newAmount += 0.0125f * amount;
        else
            newAmount -= 0.0125f * amount;
        newAmount = Mathf.Clamp01(newAmount);
        ChangeDna(muscleName, newAmount, false);

        muscleName = "upperWeight";
        newAmount = _DnaData[muscleName];
        if (isIncreasing)
            newAmount += 0.025f * amount;
        else
            newAmount -= 0.025f * amount;
        newAmount = Mathf.Clamp01(newAmount);
        ChangeDna(muscleName, newAmount, false);

        muscleName = "lowerWeight";
        newAmount = _DnaData[muscleName];
        if (isIncreasing)
            newAmount += 0.025f * amount;
        else
            newAmount -= 0.025f * amount;
        newAmount = Mathf.Clamp01(newAmount);
        ChangeDna(muscleName, newAmount, false);

        muscleName = "belly";
        newAmount = _DnaData[muscleName];
        if (isIncreasing)
            newAmount += 0.025f * amount;
        else
            newAmount -= 0.025f * amount;
        newAmount = Mathf.Clamp01(newAmount);
        ChangeDna(muscleName, newAmount, isRebuilding);
    }
    private void ChangeDna(string name, float value, bool isRebuilding)
    {
        if (_DnaData == null) return;
        _DnaData[name] = value;
        SetDna(isRebuilding);
    }
    private void SetDna(bool isRebuilding)
    {
        if (_DnaData == null || _UmaDynamicAvatar == null) return;

        _DNA = _UmaDynamicAvatar.GetDNA();
        foreach (var item in _DNA.Keys)
        {
            if (_DnaData.ContainsKey(item))
                _DNA[item].Set(_DnaData[item]);
        }

        if (isRebuilding && _UmaDynamicAvatar.BuildCharacterEnabled)
        {
            _UmaDynamicAvatar.BuildCharacterEnabled = false;
            _UmaDynamicAvatar.BuildCharacterEnabled = true;
        }
    }
    private void SetWardrobe()
    {
        if (_WardrobeData == null || _UmaDynamicAvatar == null) return;

        _UmaDynamicAvatar.ClearSlots();
        foreach (var wardrobe in _WardrobeData)
        {
            WearWardrobe(wardrobe, true);
        }
    }
    private void ChangeShader()
    {
        if (_UmaDynamicAvatar == null || _UmaDynamicAvatar.transform.Find("UMARenderer") == null || _UmaDynamicAvatar.transform.Find("UMARenderer").GetComponent<SkinnedMeshRenderer>().sharedMaterials.Length == 0)
        {
            Debug.LogError("Uma Null! Cannot Change Shader");
            return;
        }

        float smoothness;
        Texture baseTexture, metallicTexture, normalTexture;

        _matPropBlocks.Clear();
        Material[] umaMaterials = _UmaDynamicAvatar.transform.Find("UMARenderer").GetComponent<SkinnedMeshRenderer>().sharedMaterials;
        for (int i = 0; i < umaMaterials.Length; i++)
        {
            if (umaMaterials[i].shader.name.StartsWith("UMA"))
            {
                if (!umaMaterials[i].shader.name.StartsWith("UMA/Diffuse_Normal_Metallic"))
                {
                    _UmaDynamicAvatar.transform.Find("UMARenderer").GetComponent<SkinnedMeshRenderer>().SetPropertyBlock(null, i);
                    continue;
                }

                smoothness = umaMaterials[i].GetFloat("_SmoothnessModulation");
                baseTexture = umaMaterials[i].GetTexture("_BaseMap");
                metallicTexture = umaMaterials[i].GetTexture("_MetallicGlossMap");
                normalTexture = umaMaterials[i].GetTexture("_BumpMap");

                umaMaterials[i] = PrefabHolder._Instance._Pw_URP_Shared;
            }
            else
            {
                smoothness = umaMaterials[i].GetFloat("_Glossiness");
                baseTexture = umaMaterials[i].GetTexture("_MainTex");
                metallicTexture = umaMaterials[i].GetTexture("_MetallicGlossMap");
                normalTexture = umaMaterials[i].GetTexture("_BumpMap");
            }

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            if (baseTexture != null)
                block.SetTexture("_MainTex", baseTexture);
            if (normalTexture != null)
                block.SetTexture("_BumpMap", normalTexture);
            metallicTexture = metallicTexture == null ? null : ConvertToMaskMap(metallicTexture, smoothness);
            if (metallicTexture != null)
                block.SetTexture("_MetallicGlossMap", metallicTexture);
            _matPropBlocks.Add(i, block);
            _UmaDynamicAvatar.transform.Find("UMARenderer").GetComponent<SkinnedMeshRenderer>().SetPropertyBlock(block, i);
        }
        _UmaDynamicAvatar.transform.Find("UMARenderer").GetComponent<SkinnedMeshRenderer>().sharedMaterials = umaMaterials;

        GameManager._Instance.CoroutineCall(ref _changeShaderFinishedCoroutine, ChangeShaderFinishedCoroutine(), this);
    }
    private IEnumerator ChangeShaderFinishedCoroutine()
    {
        _ChangeShaderCompleted = false;
        yield return new WaitForSeconds(1f);
        _ChangeShaderCompleted = true;
    }
    private Texture2D ConvertToMaskMap(Texture metallicTex, float smoothness)
    {
        if (metallicTex == null) return null;

        RenderTexture tmp = RenderTexture.GetTemporary(metallicTex.width, metallicTex.height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(metallicTex, tmp);

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = tmp;

        Texture2D src = new Texture2D(metallicTex.width, metallicTex.height, TextureFormat.RGBA32, false);
        src.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
        src.Apply();

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(tmp);

        // pixel düzenle
        Color[] pixels = src.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
        {
            float r = pixels[i].r;
            float s = pixels[i].a;
            pixels[i] = new Color(r, 1f, 1f, s == 1f ? smoothness : s);
        }

        Texture2D mask = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
        mask.SetPixels(pixels);
        mask.Apply();

        return mask;
    }
    /*private Texture2D ConvertToMaskMap(Texture metallicTex, float smoothness)
    {
        if (metallicTex == null) return null;

        Texture2D src = metallicTex as Texture2D;
        if (src == null)
        {
            RenderTexture tmp = RenderTexture.GetTemporary(metallicTex.width, metallicTex.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(metallicTex, tmp);

            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = tmp;

            src = new Texture2D(metallicTex.width, metallicTex.height, TextureFormat.RGBA32, false);
            src.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            src.Apply();

            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(tmp);
        }

        Color[] pixels = src.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
        {
            float r = pixels[i].r;   // metallic
            float s = pixels[i].a;   // smoothness
            pixels[i] = new Color(r, 1f, 1f, s == 1f ? smoothness : s);
        }

        Texture2D mask = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
        mask.SetPixels(pixels);
        mask.Apply();

        return mask;
    }*/

    private void ArrangeIsInClosedSpace()
    {
        if (_checkForClosedSpaceCounter > 0.5f)
        {
            _checkForClosedSpaceCounter = 0f;

            if (GameManager._Instance.IsInClosedSpace(transform.position))
                _IsInClosedSpace = true;
            else
                _IsInClosedSpace = false;
        }
        else
            _checkForClosedSpaceCounter += Time.deltaTime;
    }
    private void ArrangeSnowLayer()
    {
        if (_UmaDynamicAvatar == null || _UmaDynamicAvatar.transform.Find("UMARenderer") == null || _UmaDynamicAvatar.transform.Find("UMARenderer").GetComponent<SkinnedMeshRenderer>().sharedMaterials == null) return;

        if (Gaia.ProceduralWorldsGlobalWeather.Instance.IsSnowing && !_IsInClosedSpace) // && _Rigidbody.linearVelocity.magnitude <= 2f)
            _targetAlphaForSnow = 1f;
        else
            _targetAlphaForSnow = 0f;

        /*if (_lastAlphaForSnow > 0.4f && _Rigidbody.linearVelocity.magnitude > 2f)
        {
            _lastAlphaForSnow = 0f;
            _targetAlphaForSnow = 0f;
            Destroy(Instantiate(PrefabHolder._Instance._SnowFallsVFX, transform.position + Vector3.up, Quaternion.identity), 2f);
        }*/

        if (_lastAlphaForSnow != _targetAlphaForSnow)
        {
            _lastAlphaForSnow = Mathf.MoveTowards(_lastAlphaForSnow, _targetAlphaForSnow, Time.deltaTime * 0.15f);
            Material[] mats = _UmaDynamicAvatar.transform.Find("UMARenderer").GetComponent<SkinnedMeshRenderer>().sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (!mats[i].name.StartsWith("PW_")) continue;

                _matPropBlocks[i].SetColor("_PW_CoverLayer1Color", new Color(1f, 1f, 1f, _lastAlphaForSnow));
                _UmaDynamicAvatar.transform.Find("UMARenderer").GetComponent<SkinnedMeshRenderer>().SetPropertyBlock(_matPropBlocks[i], i);
            }
        }
    }

    public void StartCarryingObject(GameObject obj)
    {

    }
    public void StopCarryingObject(GameObject obj)
    {

    }
    public void ControlAnimatorRootMotion()
    {
        if (!this.enabled) return;

        if (_LocomotionSystem.inputSmooth == Vector3.zero)
        {
            transform.position = _Animator.rootPosition;
            transform.rotation = _Animator.rootRotation;
        }
    }

    private void ArrangeStartingStates()
    {
        //arrange 2 states, health system and inventory from saves or create it

        EnterState(new Locomotion(this));//get from save
        EnterState(new Empty(this));//get from save

        //Arrange Max Speed
        float maxSpeedMultiplier = 1f;//get dexterity, exhaust level, % carry weight, health system
        _LocomotionSystem.AnimatorMaxSpeedMultiplier = maxSpeedMultiplier;
        //LocomotionSystem.FreeMovementSetting.sprintSpeed = 5.5f * maxSpeedMultiplier;

        //Arrange Stamina
        float maxStamina = 220f;//get exhaust level, str and health system
        _LocomotionSystem.MaxStamina = maxStamina;
        _LocomotionSystem.Stamina = maxStamina;

        //Arrange
    }
    public void ChangeAnimation(string name, float fadeTime = 0.2f)
    {
        _Animator.CrossFadeInFixedTime(name, fadeTime);
    }
    public void EnterState(MovementState newState)
    {
        if (_MovementState != null)
            _MovementState.ExitState(newState);
        MovementState oldState = _MovementState;
        _MovementState = newState;
        _MovementState.EnterState(oldState);
    }
    public void EnterState(HandState newState)
    {
        if (_HandState != null)
            _HandState.ExitState(newState);
        HandState oldState = _HandState;
        _HandState = newState;
        _HandState.EnterState(oldState);
    }

    private void ArrangePlaneSound()
    {
        if (_distanceToPlayer.magnitude > 40f) return;

        Physics.Raycast(transform.position + Vector3.up, -Vector3.up, out RaycastHit hit, 2f, GameManager._Instance._TerrainAndSolidMask);
        float speed = _Rigidbody.linearVelocity.magnitude;
        if (hit.collider != null)
        {
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Water"))
            {
                if (_walkSoundCounter <= 0f)
                {
                    SoundManager._Instance.PlayPlaneSound(PlaneSoundType.Swimming, transform.position, speed);
                    _walkSoundCounter += 1f;
                }
            }
            else if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Terrain"))
            {
                if (_walkSoundCounter <= 0f)
                {
                    SoundManager._Instance.PlayPlaneSound(GetPlaneSoundTypeFromTerrain(hit), transform.position, speed);
                    _walkSoundCounter += 1f;
                }
            }
            else if (hit.collider.GetComponent<PlaneSound>() != null)
            {
                if (_walkSoundCounter <= 0f)
                {
                    SoundManager._Instance.PlayPlaneSound(hit.collider.GetComponent<PlaneSound>().PlaneSoundType, transform.position, speed);
                    _walkSoundCounter += 1f;
                }
            }
        }
        _walkSoundCounter -= Time.deltaTime * 1.65f * speed;

    }
    private PlaneSoundType GetPlaneSoundTypeFromTerrain(RaycastHit hit)
    {
        if (hit.collider == null) return PlaneSoundType.Dirt;
        Terrain terrain = hit.collider.GetComponent<Terrain>();
        if (terrain == null) return PlaneSoundType.Dirt;

        Vector3 localPos = hit.transform.InverseTransformPoint(hit.point);
        int x = Mathf.FloorToInt(localPos.x / terrain.terrainData.size.x * terrain.terrainData.alphamapWidth);
        int y = Mathf.FloorToInt(localPos.z / terrain.terrainData.size.z * terrain.terrainData.alphamapHeight);

        //Debug.Log(hit.collider.gameObject.GetComponent<TerrainBehaviour>()._SoundTypeMap[x, y]);
        return hit.collider.gameObject.GetComponent<TerrainBehaviour>()._SoundTypeMap[x, y];
    }

    private void ArrangeStamina()
    {
        if (_IsSprinting)
        {
            if (_IsStrafing)
            {
                if (_LocomotionSystem.AimingMovementSetting.walkByDefault)
                    _LocomotionSystem.Stamina -= Time.deltaTime * 1.5f;
                else
                    _LocomotionSystem.Stamina -= Time.deltaTime * 10f;
            }
            else
            {
                if (_LocomotionSystem.FreeMovementSetting.walkByDefault)
                    _LocomotionSystem.Stamina -= Time.deltaTime * 1.5f;
                else
                    _LocomotionSystem.Stamina -= Time.deltaTime * 10f;
            }
        }
        else
        {
            _LocomotionSystem.Stamina += Time.deltaTime * 5f;
        }
    }
    public void LookAt(Vector3 pos, float lerpSpeed = 10f)
    {
        if (pos == transform.position) return;

        transform.forward = Vector3.Lerp(transform.forward, (pos - transform.position).normalized, Time.deltaTime * lerpSpeed);
        transform.eulerAngles = new Vector3(0f, transform.eulerAngles.y, 0f);
    }


    public virtual void TakeDamage(Damage damage)
    {
        //Health -= damage; arrange health system
        //if (Health <= 0f)
        //Die();
    }
    public virtual void Die()
    {
        //IsDead = true; make state dead
    }

}
