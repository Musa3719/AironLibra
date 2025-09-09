using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameDataSave : MonoBehaviour
{
    public static GameDataSave Instance;

    public GameData SavedGameData;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
    }
}
