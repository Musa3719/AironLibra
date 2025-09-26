using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FischlWorks;
using UMA;
using UMA.CharacterSystem;
using FIMSpace;

public abstract class Humanoid : MonoBehaviour, ICanGetHurt
{
    public Animator _Animator { get { if (_animator == null) { _animator = _LocomotionSystem.GetComponentInChildren<Animator>(); _animator.updateMode = AnimatorUpdateMode.Fixed; _animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms; } return _animator; } }
    private Animator _animator;
    public Rigidbody _Rigidbody { get; protected set; }
    public CapsuleCollider _MainCollider { get; protected set; }
    public LocomotionSystem _LocomotionSystem { get; protected set; }
    public csHomebrewIK _FootIKComponent { get; protected set; }
    public LeaningAnimator _LeaninganimatorComponent { get; protected set; }
    public DynamicCharacterAvatar _UmaDynamicAvatar { get; protected set; }
    public Dictionary<string, DnaSetter> _DNA { get; private set; }
    public Dictionary<string, float> _DnaData { get; set; }
    public List<string> _WardrobeData { get; set; }

    //Systems
    public string _Name { get; protected set; }
    public bool _IsMale { get; protected set; }
    public Class _Class { get; protected set; }
    public MovementState _MovementState { get; protected set; }
    public HandState _HandState { get; protected set; }
    public HealthSystem _HealthSystem { get; protected set; }
    public Inventory _Inventory { get; protected set; }
    public Family _Family { get; protected set; }
    public Group _AttachedGroup { get; protected set; }//not instance, a referance

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

    #region Method Parameters For Opt
    private Vector3 _distanceToPlayer;
    #endregion

    protected virtual void Awake()
    {
        _LocomotionSystem = GetComponentInChildren<LocomotionSystem>();
        _FootIKComponent = _LocomotionSystem.GetComponentInChildren<csHomebrewIK>();
        _LeaninganimatorComponent = _LocomotionSystem.GetComponentInChildren<LeaningAnimator>();
        _UmaDynamicAvatar = _LocomotionSystem.transform.Find("char").GetComponent<DynamicCharacterAvatar>();
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
        _distanceToPlayer = (GameManager._Instance._Player.transform.position - transform.position);
        _distanceToPlayer.y = 0f;

        ControlUmaDataRuntimeLoadUnload();
        ArrangePlaneSound();
        ArrangeStamina();
        ArrangeIsInClosedSpace();
        ArrangeSnowLayer();
        _MovementState.DoState();
        _HandState.DoState();

        //testing
        if (Input.GetKeyDown(KeyCode.R))
            WearWardrobe(UMAAssetIndexer.Instance.GetRecipe("MaleRobe"));
        if (Input.GetKeyDown(KeyCode.T))
            RemoveWardrobe(UMAAssetIndexer.Instance.GetRecipe("MaleRobe"));
        if (Input.GetKeyDown(KeyCode.Y))
            WearWardrobe(UMAAssetIndexer.Instance.GetRecipe("MaleShirt1"));


        if (_umaWaitingForCompletion && _Animator.avatar != null)
            UmaUpdateCompleted();
    }
    private void FixedUpdate()
    {
        if (_Rigidbody.isKinematic) return;

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
        bool wasBuildOpen = _UmaDynamicAvatar.BuildCharacterEnabled;
        _UmaDynamicAvatar.BuildCharacterEnabled = false;

        _headTransform = _UmaDynamicAvatar.transform.Find("Root").Find("Global").Find("Position").Find("Hips").Find("LowerBack").Find("Spine").Find("Spine1").Find("Neck").Find("Head");
        SetDna();
        SetWardrobe();
        UmaUpdated();

        if (wasBuildOpen)
            _UmaDynamicAvatar.BuildCharacterEnabled = true;
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
    }

    private void ControlUmaDataRuntimeLoadUnload()
    {
        if (this is Player) return;

        bool isBuildEnabled = _UmaDynamicAvatar.BuildCharacterEnabled;

        if (!isBuildEnabled && _distanceToPlayer.magnitude < 20f)
            _UmaDynamicAvatar.GetComponent<UMA.PoseTools.ExpressionPlayer>().enabled = true;
        else if (isBuildEnabled && _distanceToPlayer.magnitude > 40f)
            _UmaDynamicAvatar.GetComponent<UMA.PoseTools.ExpressionPlayer>().enabled = false;

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
        _UmaDynamicAvatar.BuildCharacterEnabled = true;
    }
    public void DisableHumanData()
    {
        _FootIKComponent.enabled = false;
        _Animator.enabled = false;
        _LeaninganimatorComponent.enabled = false;
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

    public void WearWardrobe(UMATextRecipe recipe)
    {
        if (_WardrobeData.Contains(recipe.name)) return;

        UMATextRecipe tempRecipe;
        int foundCount = 0;
        for (int i = 0; i < _WardrobeData.Count; i++)
        {
            tempRecipe = UMAAssetIndexer.Instance.GetRecipe(_WardrobeData[i]);
            if (recipe.wardrobeSlot == tempRecipe.wardrobeSlot)
            {
                if (foundCount == 0)
                {
                    foundCount++;
                }
                else
                {
                    foundCount++;
                    break;
                }
            }
        }
        if (foundCount > 1) return;

        if (foundCount == 1 && recipe.wardrobeSlot != "Chest" && recipe.wardrobeSlot != "Legs")
            return;

        _UmaDynamicAvatar.SetSlot(recipe);
        _WardrobeData.Add(recipe.name);

        if (_UmaDynamicAvatar.BuildCharacterEnabled)
        {
            _UmaDynamicAvatar.BuildCharacterEnabled = false;
            _UmaDynamicAvatar.BuildCharacterEnabled = true;
        }
    }
    public void RemoveWardrobe(UMATextRecipe recipe)
    {
        if (!_WardrobeData.Contains(recipe.name)) return;

        string removedSlot = recipe.wardrobeSlot;
        _UmaDynamicAvatar.ClearSlot(removedSlot);
        _WardrobeData.Remove(recipe.name);

        for (int i = 0; i < _WardrobeData.Count; i++)
        {
            recipe = UMAAssetIndexer.Instance.GetRecipe(_WardrobeData[i]);
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

    private void SetDna()
    {
        if (_DnaData == null) return;

        _DNA = _UmaDynamicAvatar.GetDNA();
        foreach (var item in _DNA.Keys)
        {
            if (_DnaData.ContainsKey(item))
                _DNA[item].Set(_DnaData[item]);
            else
                Debug.Log(item);
        }
    }
    private void SetWardrobe()
    {
        if (_WardrobeData == null) return;

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
            metallicTexture = ConvertToMaskMap(metallicTexture, smoothness);
            if (metallicTexture != null)
                block.SetTexture("_MetallicGlossMap", metallicTexture);
            _UmaDynamicAvatar.transform.Find("UMARenderer").GetComponent<SkinnedMeshRenderer>().SetPropertyBlock(block, i);
        }
        _UmaDynamicAvatar.transform.Find("UMARenderer").GetComponent<SkinnedMeshRenderer>().sharedMaterials = umaMaterials;
    }
    private Texture2D ConvertToMaskMap(Texture metallicTex, float smoothness)
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
    }

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

        if (_IsInClosedSpace)
            _targetAlphaForSnow = 0f;
        else
            _targetAlphaForSnow = 1f;

        if (_lastAlphaForSnow != _targetAlphaForSnow)
        {
            _lastAlphaForSnow = Mathf.MoveTowards(_lastAlphaForSnow, _targetAlphaForSnow, Time.deltaTime * 0.15f);
            int length = _UmaDynamicAvatar.transform.Find("UMARenderer").GetComponent<SkinnedMeshRenderer>().sharedMaterials.Length;
            MaterialPropertyBlock block;
            for (int i = 0; i < length; i++)
            {
                block = new MaterialPropertyBlock();
                block.SetColor("_PW_CoverLayer1Color", Color.white);
                _UmaDynamicAvatar.transform.Find("UMARenderer").GetComponent<SkinnedMeshRenderer>().GetPropertyBlock(block, i);
                block.SetColor("_PW_CoverLayer1Color", new Color(1f, 1f, 1f, _lastAlphaForSnow));
                _UmaDynamicAvatar.transform.Find("UMARenderer").GetComponent<SkinnedMeshRenderer>().SetPropertyBlock(block, i);
            }
        }
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
