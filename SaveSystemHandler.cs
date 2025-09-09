using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class GameData
{
    public int _Day;

    public GameData(int day)
    {
        _Day = day;
    }
}
public class SaveSystemHandler : MonoBehaviour
{
    public static SaveSystemHandler _Instance;

    private string _saveDir;
    private void Awake()
    {
        _Instance = this;
        _saveDir = Application.streamingAssetsPath + "/Saves/";
    }
    private void Start()
    {
        if (GameDataSave.Instance != null && GameDataSave.Instance.SavedGameData != null)
        {
            GameManager._Instance.LoadSavedDataToGame();
        }

    }
    public void ReadFile(int index)
    {
        // Does the file exist?
        if (File.Exists(_saveDir + index.ToString() + ".json"))
        {
            // Read the entire file and save its contents.
            string fileContents = File.ReadAllText(_saveDir + index.ToString() + ".json");

            // Deserialize the JSON data 
            //  into a pattern matching the GameData class.
            GameData gameData = JsonUtility.FromJson<GameData>(fileContents);
            if (GameDataSave.Instance != null)
                GameDataSave.Instance.SavedGameData = gameData;
        }
        else
            Debug.LogError("Save File Not Found!");
    }

    public void WriteFile(int index)
    {
        //GameData gameData = new GameData(GameManager.Instance.Day);
        GameData gameData = new GameData(1);

        // Serialize the object into JSON and save string.
        string jsonString = JsonUtility.ToJson(gameData);

        // Write JSON to file.
        File.WriteAllText(_saveDir + index.ToString() + ".json", jsonString);
    }
    public void DeleteFile(int index)
    {
        int c = index + 1;
        while (Options._Instance._LoadedGamesCount > c)
        {
            if (File.Exists(_saveDir + (c - 1).ToString() + ".json"))
                File.Delete(_saveDir + (c - 1).ToString() + ".json");
            File.Copy(_saveDir + c.ToString() + ".json", _saveDir + (c - 1).ToString() + ".json");
            c++;
        }
        File.Delete(_saveDir + (c - 1).ToString() + ".json");
    }


}
