using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using UMA;
using UMA.CharacterSystem;
using UnityEngine;
using static NameCreator;

public static class NPCManager
{
    public static List<NPC> _AllNPCs { get; set; }
    public static NPCDistanceComparer _Comparer { get; set; }
    private static NPC[] _npcPool;
    private static float[] _distancePool;
    private static float _sortCounter;
    private static Dictionary<NPC, float> _awayNpcUpdateCounters;

    public static void AddToList(NPC npc)
    {
        if (!_AllNPCs.Contains(npc))
            _AllNPCs.Add(npc);
    }
    public static void RemoveFromList(NPC npc)
    {
        if (_AllNPCs.Contains(npc))
            _AllNPCs.Remove(npc);
    }
    public static void Start()
    {
        //for (int i = 0; i < 4 * System.Enum.GetValues(typeof(CultureTypeForName)).Length; i++)
            //Debug.Log((CultureTypeForName)(i / 4f) + " : " + (i % 4 < 2 ? "Male" : "Female") + " : " + NameCreator.GetName((CultureTypeForName)(i / 4f), i % 4 < 2));

        _npcPool = new NPC[GameManager._Instance._NumberOfNpcs];
        _distancePool = new float[GameManager._Instance._NumberOfNpcs];
        _awayNpcUpdateCounters = new Dictionary<NPC, float>();
        for (int i = 0; i < GameManager._Instance._NumberOfNpcs; i++)
        {
            _awayNpcUpdateCounters.Add(_AllNPCs[i], Random.Range(2f, 18f));
        }
    }
    public static void Update()
    {
        _sortCounter += Time.deltaTime;
        if (_sortCounter > 2f)
        {
            _sortCounter = 0f;
            SortNPCsByDistance();
        }

        foreach (var npc in _AllNPCs)
        {
            _awayNpcUpdateCounters[npc] -= Time.deltaTime;
            if (_awayNpcUpdateCounters[npc] < 0f)
            {
                _awayNpcUpdateCounters[npc] = Random.Range(8f, 12f);
                if (npc.transform.childCount == 0)
                    npc.UpdateWhenAway();
            }
        }
        if (M_Input.GetKeyDownForTesting(KeyCode.L))
        {
            GameManager._Instance.LoadChunk(0, 0);
        }
        if (M_Input.GetKeyDownForTesting(KeyCode.U))
        {
            GameManager._Instance.UnloadChunk(0, 0);
        }
        if (M_Input.GetKeyDownForTesting(KeyCode.R))
        {
            GameManager._Instance.ReloadChunk(0, 0);
        }

        if (M_Input.GetKeyDownForTesting(KeyCode.H))
        {
            new WeaponItem(LongSword_1._Instance).Equip(WorldHandler._Instance._Player._Inventory);
            foreach (var npc in _AllNPCs)
            {
                new WeaponItem(LongSword_1._Instance).Equip(npc._Inventory);
                npc.ActivateCombatMode();
            }
        }
    }

    public static List<UMATextRecipe> GetRandomHair(bool isMale)
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

    public static List<UMATextRecipe> GetRandomCloth(bool isMale)
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
    private static void SortNPCsByDistance()
    {
        if (_AllNPCs.Count == 0) return;

        for (int i = 0; i < _AllNPCs.Count; i++)
        {
            _npcPool[i] = _AllNPCs[i];
            _distancePool[i] = (_AllNPCs[i].transform.position - WorldHandler._Instance._Player.transform.position).sqrMagnitude;
        }

        System.Array.Sort(_distancePool, _npcPool, 0, _AllNPCs.Count);
        _AllNPCs.Clear();
        for (int i = 0; i < _npcPool.Length; i++)
            _AllNPCs.Add(_npcPool[i]);

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
