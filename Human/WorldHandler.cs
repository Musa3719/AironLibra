using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class WorldHandler : MonoBehaviour
{
    public static WorldHandler _Instance;
    public float _SeaLevel { get; private set; }
    public Calendar _Date { get; private set; }
    public Clock _Clock { get; private set; }

    public List<Group> _Factions { get; private set; }
    public Group _PlayerFaction { get; private set; }

    public Player _Player { get { if (_player == null) _player = GameManager._Instance._Player.GetComponent<Player>(); return _player; } }
    private Player _player;

    //settlements
    //religions
    //cultures
    //quests
    //more

    private void Awake()
    {
        _Instance = this;
        _SeaLevel = GameObject.Find("Water").transform.position.y;
        _PlayerFaction = new Group(_Player, "Input", null, new List<Humanoid>());
        var playerGroup = new List<Humanoid>();
        playerGroup.Add(_Player);
        _Factions = new List<Group>();
        _Date = new Calendar();
        _Date._Year = 15;
        _Clock = new Clock();
    }
    private void Update()
    {
        if (GameManager._Instance._IsGameStopped) return;

        _Clock.Update();
        //Debug.Log(Gaia.ProceduralWorldsGlobalWeather.Instance.Season + " : " + _DaysInSeason + " : " + _DaysInYear);
    }
    public void InitSeasonForNewGame()
    {
        Shader.SetGlobalFloat("_Global_SnowLevel", 0f);
        Gaia.ProceduralWorldsGlobalWeather.Instance.Season = 3f; //start from autumn 1
        _Date._DaysInSeason = 1;
        _Date._DaysInYear = 274;
    }
    public void NextDay()
    {
        _Date._DaysInYear++;
        _Date._DaysInSeason++;
        Gaia.ProceduralWorldsGlobalWeather.Instance.Season += 1f / 91.25f; //90 days for one season
        Gaia.ProceduralWorldsGlobalWeather.Instance.Season = Gaia.ProceduralWorldsGlobalWeather.Instance.Season % 4f;
        _Date.ToNextDay();
        //world stuff
    }
    public void NextYear()
    {
        _Date._Year++;
        _Date._DaysInYear = 1;
        _Date._DaysInSeason = 1;
        Gaia.ProceduralWorldsGlobalWeather.Instance.Season = 0f;
        //world stuff
    }

    public static Humanoid GetHuman(GameObject obj)
    {
        Transform parent = obj.transform;
        while (parent.parent != null)
        {
            parent = parent.parent;
            if (parent.GetComponent<Humanoid>() != null)
            {
                return parent.GetComponent<Humanoid>();
            }
        }
        return null;
    }
}
public class Calendar
{
    public float _Season;
    public int _Year;
    public int _DaysInYear;
    public int _DaysInSeason;

    private bool _nextYearPermission;
    private float _lastDaySeason;

    public void ToNextDay()
    {
        _lastDaySeason = _Season;
        _Season = Gaia.ProceduralWorldsGlobalWeather.Instance.Season;

        if (_Season > 3f && !_nextYearPermission)
            _nextYearPermission = true;
        if (_Season < 1f && _nextYearPermission)
        {
            _nextYearPermission = false;
            WorldHandler._Instance.NextYear();
        }

        if (((int)_lastDaySeason) != ((int)_Season))
        {
            _DaysInSeason = 1;
        }
    }
}
public class Clock
{
    public int _Hour;
    public float _Minute;

    private bool _nextDayPermission;
    public void Update()
    {
        _Hour = Gaia.GaiaAPI.GetTimeOfDayHour();
        _Minute = Gaia.GaiaAPI.GetTimeOfDayMinute();

        CheckForNextDay();
    }
    public void CheckForNextDay()
    {
        if (_Hour == 23 && !_nextDayPermission)
            _nextDayPermission = true;
        if (_Hour == 0 && _nextDayPermission)
        {
            _nextDayPermission = false;
            WorldHandler._Instance.NextDay();
        }
    }
}
public class Settlement
{
    public enum SettlementTypeEnum
    {
        Village,
        Town,
        City,
        Castle
        //more
    }

    public State _BelongsTo { get; private set; }
    public Humanoid _Governor { get; private set; }
    public SettlementTypeEnum _SettlementType { get; private set; }
    public List<Humanoid> _Guards { get; private set; }
    public float _TaxRate { get; private set; }

    public List<Building> _Buildings { get; private set; }

    //GetHumanCount

    public void SetTaxRate(float newRate, Humanoid orderGiver)
    {
        if (orderGiver == _BelongsTo._Ruler)
            _TaxRate = newRate;
    }
}

public abstract class Building
{
    public bool _IsPublic { get; private set; }
    public List<Humanoid> _Household { get; private set; }
    public List<GameObject> _CarriableObject { get; private set; }
    public List<Door> _Doors { get; private set; }

    public void AddCarriableObject(GameObject carriable)
    {
        if (_CarriableObject == null)
            _CarriableObject = new List<GameObject>();

        _CarriableObject.Add(carriable.GetComponent<GameObject>());
    }
    public void RemoveCarriableObject(GameObject carriable)
    {
        if (_CarriableObject != null && _CarriableObject.Contains(carriable.GetComponent<GameObject>()))
            _CarriableObject.Remove(carriable.GetComponent<GameObject>());
    }

    public void SetValues(bool isPublic, List<Humanoid> household, List<GameObject> carriables, List<Door> doors)
    {
        _IsPublic = isPublic;
        _Household = household;
        _CarriableObject = carriables;
        _Doors = doors;
    }

}

public class Religion
{

}

public class Culture
{

}