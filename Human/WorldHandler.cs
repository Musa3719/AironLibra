using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class WorldHandler : MonoBehaviour
{
    public static WorldHandler _Instance;

    public int _DaysPassed { get; private set; }
    public Calendar _Date { get; private set; }
    public Clock _Clock { get; private set; }

    public List<Group> _Factions { get; private set; }
    public Group _PlayerFaction { get; private set; }

    [SerializeField] private Player _player;
    public Player _Player => _player;
    //settlements
    //religions
    //cultures
    //quests
    //more

    private void Awake()
    {
        _Instance = this;
        _PlayerFaction = new Group(_Player, "Input", null, new List<Humanoid>());
        var playerGroup = new List<Humanoid>();
        playerGroup.Add(_Player);
        _Factions = new List<Group>();
        _Date = new Calendar();
        _Date._Day = 1;
        _Date._Month = 1;
        _Date._Year = 15;
        _Clock = new Clock();
    }
    private void Update()
    {
        if (GameManager._Instance._IsGameStopped) return;

        _Clock.Update();
    }

    public void NextDay()
    {
        _Date.ToNextDay();
        _DaysPassed++;
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
    public int _Day;
    public int _Month;
    public int _Year;

    public void ToNextDay()
    {
        if (_Day != 30)
        {
            _Day++;
        }
        else
        {
            if (_Month != 12)
            {
                _Month++;
                _Day = 1;
            }
            else
            {
                _Year++;
                _Month = 1;
                _Day = 1;
            }
        }
    }
}
public class Clock
{
    public int _Hour;
    public int _Minute;

    private float _timer;

    public void Update()
    {
        if (_timer >= 5f)
        {
            _timer -= 5f;
            MinutePassed();
        }
        else
        {
            _timer += Time.deltaTime;
        }
    }
    public void MinutePassed()
    {
        if (_Minute != 59)
        {
            _Minute++;
        }
        else
        {
            if (_Hour != 23)
            {
                _Hour++;
                _Minute = 0;
            }
            else
            {
                WorldHandler._Instance.NextDay();
                _Hour = 0;
                _Minute = 0;
            }
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
        //more and make it not binary
    }

    public State _BelongsTo { get; private set; }
    public Humanoid _Protector { get; private set; }
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
    private List<NavMeshObstacle> _navMeshObstacles;

    public void AddFurniture(GameObject furniture)
    {
        if (_navMeshObstacles == null)
            _navMeshObstacles = new List<NavMeshObstacle>();

        _navMeshObstacles.Add(furniture.GetComponent<NavMeshObstacle>());
    }
    public void RemoveFurniture(GameObject furniture)
    {
        if (_navMeshObstacles != null && _navMeshObstacles.Contains(furniture.GetComponent<NavMeshObstacle>()))
            _navMeshObstacles.Remove(furniture.GetComponent<NavMeshObstacle>());
    }
    public void OpenObstacles()
    {
        foreach (var item in _navMeshObstacles)
        {
            item.enabled = true;
        }
    }
    public void CloseObstacles()
    {
        foreach (var item in _navMeshObstacles)
        {
            item.enabled = false;
        }
    }

    public class Private
    {
        public List<Humanoid> LivesInHere { get; private set; }
    }
    public class Public
    {
        public List<Humanoid> WorksInHere { get; private set; }
    }
}

public class Religion
{

}

public class Culture
{

}