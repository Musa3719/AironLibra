using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Collections;
using UnityEngine.SceneManagement;

public class SaveSystemHandler : MonoBehaviour
{
    public static SaveSystemHandler _Instance;
    public int _ActiveSave { get; set; }

    private string SavePath(int index) => Path.Combine(Application.persistentDataPath, "Save" + index.ToString() + ".json");
    private void Awake()
    {
        if (_Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        _Instance = this;
        _ActiveSave = -1;
        DontDestroyOnLoad(gameObject);
    }
    public void SaveGame(int index)
    {
        List<NpcData> npcData = GetAllNPCData();
        PlayerData playerData = GetPlayerData();
        GameData data = new GameData
        {
            _ObjectsInChunk = GameManager._Instance._ObjectsInChunk,
            _ObjectPositionsInChunk = GameManager._Instance._ObjectPositionsInChunk,
            _NpcData = npcData,
            _PlayerData = playerData,
            _SnowLevel = Shader.GetGlobalFloat("_Global_SnowLevel"),
            _DaysInYear = WorldHandler._Instance._Date._DaysInYear,
            _DaysInSeason = WorldHandler._Instance._Date._DaysInSeason,
            _Hour = WorldHandler._Instance._Clock._Hour,
            _Minute = WorldHandler._Instance._Clock._Minute,
            _Season = WorldHandler._Instance._Date._Season,
            _Year = WorldHandler._Instance._Date._Year
        };

        SaveGameData(index, data);
    }

    public void LoadGame(int index)
    {
        GameManager._Instance.StopGame(false, false);
        GameData data = LoadGameData(index);

        if (data != null)
        {
            GameManager._Instance._ObjectsInChunk = data._ObjectsInChunk;
            GameManager._Instance._ObjectPositionsInChunk = data._ObjectPositionsInChunk;
            Shader.SetGlobalFloat("_Global_SnowLevel", data._SnowLevel);

            WorldHandler._Instance._Date._DaysInYear = data._DaysInYear;
            WorldHandler._Instance._Date._DaysInSeason = data._DaysInSeason;
            WorldHandler._Instance._Clock._Hour = data._Hour;
            WorldHandler._Instance._Clock._Minute = data._Minute;
            Gaia.GaiaAPI.SetTimeOfDayHour(data._Hour);
            Gaia.GaiaAPI.SetTimeOfDayMinute(data._Minute);
            WorldHandler._Instance._Date._Season = data._Season;
            WorldHandler._Instance._Date._Year = data._Year;
            Gaia.ProceduralWorldsGlobalWeather.Instance.Season = data._Season;

            WorldHandler._Instance._Player.transform.position = data._PlayerData._Pos;
            WorldHandler._Instance._Player.transform.localEulerAngles = data._PlayerData._Rot;
            WorldHandler._Instance._Player._DnaData = data._PlayerData._DnaData;
            WorldHandler._Instance._Player._WardrobeData = data._PlayerData._WardrobeData;
            if (WorldHandler._Instance._Player._UmaDynamicAvatar.BuildCharacterEnabled)
            {
                WorldHandler._Instance._Player._UmaDynamicAvatar.BuildCharacterEnabled = false;
                WorldHandler._Instance._Player._UmaDynamicAvatar.BuildCharacterEnabled = true;
            }

            RecreateAllNPCObjects(data);
            GameObject[] allNpcs = GameObject.FindGameObjectsWithTag("NPC");
            NPC npc;
            for (int i = 0; i < data._NpcData.Count; i++)
            {
                npc = allNpcs[i].GetComponent<NPC>();
                npc._NpcIndex = data._NpcData[i]._NpcIndex;
                npc.transform.position = data._NpcData[i]._Pos;
                npc.transform.localEulerAngles = data._NpcData[i]._Rot;
                npc._DnaData = data._NpcData[i]._DnaData;
                npc._WardrobeData = data._NpcData[i]._WardrobeData;

                if (npc._UmaDynamicAvatar != null && npc._UmaDynamicAvatar.BuildCharacterEnabled)
                {
                    npc._UmaDynamicAvatar.BuildCharacterEnabled = false;
                    npc._UmaDynamicAvatar.BuildCharacterEnabled = true;
                }
            }
        }
        else
        {
            GameManager._Instance.StartValuesForNewGame();
        }

        GameManager._Instance.ReloadAllChunks();

        GameManager._Instance.UnstopGame();
    }

    private PlayerData GetPlayerData()
    {
        PlayerData data = new PlayerData();
        data._Pos = WorldHandler._Instance._Player.transform.position;
        data._Rot = WorldHandler._Instance._Player.transform.localEulerAngles;
        data._DnaData = WorldHandler._Instance._Player._DnaData;
        data._WardrobeData = WorldHandler._Instance._Player._WardrobeData;
        return data;
    }
    private List<NpcData> GetAllNPCData()
    {
        List<NpcData> data = new List<NpcData>();
        NpcData npcData;
        GameObject[] allNpcs = GameObject.FindGameObjectsWithTag("NPC");
        for (int i = 0; i < allNpcs.Length; i++)
        {
            npcData = new NpcData();
            npcData._NpcIndex = allNpcs[i].GetComponent<NPC>()._NpcIndex;
            npcData._Pos = allNpcs[i].GetComponent<NPC>().transform.position;
            npcData._Rot = allNpcs[i].GetComponent<NPC>().transform.localEulerAngles;
            npcData._DnaData = allNpcs[i].GetComponent<NPC>()._DnaData;
            npcData._WardrobeData = allNpcs[i].GetComponent<NPC>()._WardrobeData;
            data.Add(npcData);
        }
        return data;
    }
    private void RecreateAllNPCObjects(GameData data)
    {
        GameObject[] allNpcs = GameObject.FindGameObjectsWithTag("NPC");
        foreach (GameObject npc in allNpcs)
        {
            Destroy(npc);
        }
        for (int i = 0; i < data._NpcData.Count; i++)
        {
            Instantiate(PrefabHolder._Instance._NpcParent);
        }
    }

    private void SaveGameData(int index, GameData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath(index), json);
        Debug.Log("Game saved to " + SavePath(index));
    }
    public GameData LoadGameData(int index)
    {
        if (!File.Exists(SavePath(index)))
        {
            Debug.LogWarning("Save file not found.");
            return null;
        }

        string json = File.ReadAllText(SavePath(index));
        return JsonUtility.FromJson<GameData>(json);
    }
    public void DeleteSaveFile(int saveIndex)
    {
        File.Delete(SavePath(saveIndex));
    }
}

[System.Serializable]
public class GameData
{
    public List<UnityEngine.AddressableAssets.AssetReferenceGameObject>[,] _ObjectsInChunk;
    public List<Vector3>[,] _ObjectPositionsInChunk;
    public PlayerData _PlayerData;
    public List<NpcData> _NpcData;
    public float _SnowLevel;
    public int _DaysInYear;
    public int _DaysInSeason;
    public int _Hour;
    public float _Minute;
    public float _Season;
    public int _Year;
}

[System.Serializable]
public class PlayerData
{
    public Vector3 _Pos;
    public Vector3 _Rot;
    public Dictionary<string, float> _DnaData;
    public List<UMA.UMATextRecipe> _WardrobeData;
}

[System.Serializable]
public class NpcData
{
    public ushort _NpcIndex;
    public Vector3 _Pos;
    public Vector3 _Rot;
    public Dictionary<string, float> _DnaData;
    public List<UMA.UMATextRecipe> _WardrobeData;
}

