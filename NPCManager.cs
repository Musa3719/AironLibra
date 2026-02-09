using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using UMA;
using UMA.CharacterSystem;
using UnityEngine;

public class NPCManager : MonoBehaviour
{
    public static NPCManager _Instance;

    public bool _IsReady { get; set; }
    public List<NPC> _AllNPCs { get; private set; }

    //public NPCDistanceComparer _Comparer { get; private set; }
    private NPC[] _npcPool;
    private float[] _distancePool;
    private float _sortCounter;

    private NPC _tempNpcForUpdate;
    private int _pathfindCounterForFrame;

    public void Awake()
    {
        _Instance = this;
        _IsReady = false;
        _sortCounter = 0f;
        _pathfindCounterForFrame = 0;
        _AllNPCs = new List<NPC>();
    }

    public void AddToList(NPC npc)
    {
        if (!_AllNPCs.Contains(npc))
            _AllNPCs.Add(npc);
    }
    public void RemoveFromList(NPC npc)
    {
        if (_AllNPCs.Contains(npc))
            _AllNPCs.Remove(npc);
    }
    public void Start()
    {
        //for (int i = 0; i < 4 * System.Enum.GetValues(typeof(CultureTypeForName)).Length; i++)
        //Debug.Log((CultureTypeForName)(i / 4f) + " : " + (i % 4 < 2 ? "Male" : "Female") + " : " + NameCreator.GetName((CultureTypeForName)(i / 4f), i % 4 < 2));

        _npcPool = new NPC[GameManager._Instance._NumberOfNpcs];
        _distancePool = new float[GameManager._Instance._NumberOfNpcs];
    }
    public void Update()
    {
        _pathfindCounterForFrame = 0;

        _sortCounter += Time.deltaTime;
        if (_sortCounter > 5f && _IsReady)
        {
            _sortCounter = 0f;
            SortNPCsByDistance();
        }

        if (_IsReady)
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < _AllNPCs.Count; i++)
            {
                _tempNpcForUpdate = _AllNPCs[i];

                _tempNpcForUpdate.FrameUpdateNpc();

                _tempNpcForUpdate._UpdateNpcCounter -= dt;
                if (_tempNpcForUpdate._UpdateNpcCounter <= 0f)
                {
                    _tempNpcForUpdate._UpdateNpcCounter += 2f;
                    _tempNpcForUpdate.SparseUpdateNpc();
                }

                if (_tempNpcForUpdate._NpcRequestForPathfinding != null && !_tempNpcForUpdate._NpcRequestForPathfinding.Value)
                {
                    if (_pathfindCounterForFrame < 50)
                    {
                        _tempNpcForUpdate._NpcRequestForPathfinding = true;
                        _pathfindCounterForFrame++;
                    }
                }
            }
        }

        if (M_Input.GetKeyDownForTesting(KeyCode.L))
        {
            //GameManager._Instance.LoadChunk(0, 0);
            foreach (var npc in _AllNPCs)
            {
                npc._HealthSystem.ToFullHealth();
            }
        }
        if (M_Input.GetKeyDownForTesting(KeyCode.U))
        {
            //GameManager._Instance.UnloadChunk(0, 0);
            foreach (var npc in _AllNPCs)
            {
                Damage damage = new Damage();
                damage._DamagePart = DamagePart.Head;
                damage._Amount = 200f;
                npc._HealthSystem.TakeDamage(damage, 100f);
            }
        }
        if (M_Input.GetKeyDownForTesting(KeyCode.R))
        {
            GameManager._Instance.ReloadChunk(0, 0);
        }

        if (M_Input.GetKeyDownForTesting(KeyCode.H))
        {
            new WeaponItem(CompositeBow._Instance).Equip(WorldHandler._Instance._Player._Inventory);
            foreach (var npc in _AllNPCs)
            {
                new WeaponItem(LongSword_1._Instance).Equip(npc._Inventory);
                npc.ActivateCombatMode();
            }
        }

    }

    public List<UMATextRecipe> GetRandomHair(bool isMale)
    {
        List<UMATextRecipe> umaTextRecipes = new List<UMATextRecipe>();
        if (isMale)
        {
            int random = Random.Range(0, 3);
            if (random == 0)
                umaTextRecipes.Add(UMAAssetIndexer.Instance.GetRecipe("MaleHair1"));
            else if (random == 1)
                umaTextRecipes.Add(UMAAssetIndexer.Instance.GetRecipe("MaleHair2"));
            else if (random == 2)
                umaTextRecipes.Add(UMAAssetIndexer.Instance.GetRecipe("MaleHair3"));

            random = Random.Range(0, 5);
            if (random == 0)
                umaTextRecipes.Add(UMAAssetIndexer.Instance.GetRecipe("MaleBeard1"));
            else if (random == 1)
                umaTextRecipes.Add(UMAAssetIndexer.Instance.GetRecipe("MaleBeard2"));
            else if (random == 2)
                umaTextRecipes.Add(UMAAssetIndexer.Instance.GetRecipe("MaleBeard3"));
        }
        else
        {
            int random = Random.Range(0, 3);
            if (random == 0)
                umaTextRecipes.Add(UMAAssetIndexer.Instance.GetRecipe("FemaleHair1"));
            else if (random == 1)
                umaTextRecipes.Add(UMAAssetIndexer.Instance.GetRecipe("FemaleHair2"));
            else if (random == 2)
                umaTextRecipes.Add(UMAAssetIndexer.Instance.GetRecipe("FemaleHair3"));
        }

        return umaTextRecipes;
    }

    public List<UMATextRecipe> GetRandomCloth(bool isMale)
    {
        List<UMATextRecipe> umaTextRecipes = new List<UMATextRecipe>();
        if (isMale)
        {
            umaTextRecipes.Add(UMAAssetIndexer.Instance.GetRecipe("MaleDefaultUnderwear"));

            //umaTextRecipes.Add(UMAAssetIndexer.Instance.GetRecipe("TestChestArmor_Recipe"));
            /*int random = Random.Range(0, 3);
            if (random == 0)
                umaTextRecipes.Add(UMAAssetIndexer.Instance.GetRecipe("MaleShirt1"));
            else if (random == 1)
                umaTextRecipes.Add(UMAAssetIndexer.Instance.GetRecipe("MaleShirt2"));
            else if (random == 2)
                umaTextRecipes.Add(UMAAssetIndexer.Instance.GetRecipe("MaleShirt3"));


            random = Random.Range(0, 2);
            if (random == 0)
                umaTextRecipes.Add(UMAAssetIndexer.Instance.GetRecipe("MaleShorts1"));
            else if (random == 1)
                umaTextRecipes.Add(UMAAssetIndexer.Instance.GetRecipe("MaleShorts2"));*/
        }
        else
        {
            umaTextRecipes.Add(UMAAssetIndexer.Instance.GetRecipe("FemaleDefaultUnderwear"));

            /*int random = Random.Range(0, 3);
            if (random == 0)
                umaTextRecipes.Add(UMAAssetIndexer.Instance.GetRecipe("FemaleShirt1"));
            else if (random == 1)
                umaTextRecipes.Add(UMAAssetIndexer.Instance.GetRecipe("FemaleShirt2"));
            else if (random == 2)
                umaTextRecipes.Add(UMAAssetIndexer.Instance.GetRecipe("FemaleShirt3"));
            //umaTextRecipes.Add(UMAAssetIndexer.Instance.GetRecipe("TestChestArmorF_Recipe"));

            random = Random.Range(0, 2);
            if (random == 0)
                umaTextRecipes.Add(UMAAssetIndexer.Instance.GetRecipe("FemalePants1"));
            else if (random == 1)
                umaTextRecipes.Add(UMAAssetIndexer.Instance.GetRecipe("FemalePants2"));*/
        }

        return umaTextRecipes;
    }
    private void SortNPCsByDistance()
    {
        if (_AllNPCs.Count == 0) return;

        for (int i = 0; i < _AllNPCs.Count; i++)
        {
            _npcPool[i] = _AllNPCs[i];
            _distancePool[i] = (_AllNPCs[i]._DistanceToPlayer).sqrMagnitude;
        }

        System.Array.Sort(_distancePool, _npcPool, 0, _AllNPCs.Count);
        // _AllNPCs.Clear();
        for (int i = 0; i < _npcPool.Length; i++)
        {
            //_AllNPCs.Add(_npcPool[i]);
            _npcPool[i]._NpcDistanceListIndex = i;
        }

        //_AllNPCs.Sort(_Comparer);
        //_AllNPCs = _AllNPCs.OrderBy(npc => Vector3.Distance(npc.transform.position, WorldHandler._Instance._Player.transform.position)).ToList();
    }

}

public class NPCDistanceComparer : IComparer<NPC>
{
    private Vector3 _playerPosition => WorldHandler._Instance._Player.transform.position;

    public int Compare(NPC a, NPC b)
    {
        float distA = (a.transform.position - _playerPosition).magnitude;
        float distB = (b.transform.position - _playerPosition).magnitude;
        return distA.CompareTo(distB);
    }
}
