using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UMA;
using UMA.CharacterSystem;

public static class NPCManager
{
    public static List<NPC> _AllNPCs { get; set; }
    public static NPCDistanceComparer _Comparer { get; set; }
    private static float _sortCounter;

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

    public static void Update()
    {
        _sortCounter += Time.deltaTime;
        if (_sortCounter > 0.75f)
        {
            _sortCounter = 0f;
            SortNPCsByDistance();
        }

        if (M_Input.GetKeyDown(KeyCode.Q))
        {
            foreach (var npc in _AllNPCs)
            {
                npc.SpawnNPCChild();
            }
        }
    }

    public static void SetGender(DynamicCharacterAvatar avatar, bool isMale)
    {
        if (avatar == null) return;

        if (isMale)
        {
            avatar.ChangeRace("HumanMale", DynamicCharacterAvatar.ChangeRaceOptions.none);
            avatar.GetComponent<UMA.PoseTools.ExpressionPlayer>().overrideMecanimJaw = false;
        }
        else
        {
            avatar.ChangeRace("HumanFemale", DynamicCharacterAvatar.ChangeRaceOptions.none);
            avatar.GetComponent<UMA.PoseTools.ExpressionPlayer>().overrideMecanimJaw = true;
        }

    }
    public static void ChangeColor(DynamicCharacterAvatar avatar, string colorName, Color newColor)
    {
        if (avatar == null) return;

        avatar.SetColorValue(colorName, newColor);

        /*if (avatar.BuildCharacterEnabled)
        {
            avatar.BuildCharacterEnabled = false;
            avatar.BuildCharacterEnabled = true;
        }*/
    }
    public static void ChangeDna(Dictionary<string, float> dnaData, Humanoid human, string name, float value, bool isRebuilding)
    {
        if (dnaData == null) return;
        dnaData[name] = value;
        if (human != null)
            human.SetDna(isRebuilding);
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

            int random = Random.Range(0, 3);
            if (random == 0)
                umaTextRecipes.Add(UMAAssetIndexer.Instance.GetRecipe("MaleShirt1"));
            else if (random == 1)
                umaTextRecipes.Add(UMAAssetIndexer.Instance.GetRecipe("MaleShirt2"));
            else if (random == 2)
                umaTextRecipes.Add(UMAAssetIndexer.Instance.GetRecipe("MaleShirt3"));
            //umaTextRecipes.Add(UMAAssetIndexer.Instance.GetRecipe("TestChestArmor_Recipe"));


            random = Random.Range(0, 2);
            if (random == 0)
                umaTextRecipes.Add(UMAAssetIndexer.Instance.GetRecipe("MaleShorts1"));
            else if (random == 1)
                umaTextRecipes.Add(UMAAssetIndexer.Instance.GetRecipe("MaleShorts2"));
        }
        else
        {
            umaTextRecipes.Add(UMAAssetIndexer.Instance.GetRecipe("FemaleDefaultUnderwear"));

            int random = Random.Range(0, 3);
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
                umaTextRecipes.Add(UMAAssetIndexer.Instance.GetRecipe("FemalePants2"));
        }

        return umaTextRecipes;
    }
    private static void SortNPCsByDistance()
    {
        if (_AllNPCs.Count == 0) return;
        _AllNPCs.Sort(_Comparer);
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
