using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public abstract class NPC : Humanoid
{
    public Job _Job;

    public bool _IsOnPlayersTeam { get; protected set; }
    public bool _IsOnPlayersActiveGroup { get; protected set; }
    public NavMeshAgent _Agent { get; protected set; }

    public override Vector2 _DirectionInput => _directionInputFromPath;
    private Vector2 _directionInputFromPath;
    private List<Vector3> _cornersFromPath;
    private Vector3 _lastCornerFromPath;
    private NavMeshPath _currentPath;

    public Vector3 _MoveTargetPosition { get; set; }
    public Vector3 _AimPosition { get; set; }
    public bool _WantsToJump { get; set; }
    public bool _WantsToAttack { get; set; }

    public bool _IsOnLinkMovement { get; private set; }

    protected void Awake()
    {
        base.Awake();
        transform.localScale *= Random.Range(0.98f, 1.02f);
        _Agent = GetComponent<NavMeshAgent>();
        _cornersFromPath = new List<Vector3>();
    }
    protected void Start()
    {
        base.Start();
        NavmeshToRigidbody();
    }
   
    protected void Update()
    {
        if (GameManager._Instance._IsGameStopped) return;

        base.Update();
        //IsLookingInput=getinput
    }

    private void ArrangeNewMovementTarget(Vector3 targetPos)
    {
        _MoveTargetPosition = targetPos;
        _currentPath = new NavMeshPath();
        NavMesh.CalculatePath(transform.position, _MoveTargetPosition, NavMesh.AllAreas, _currentPath);

        _cornersFromPath.Clear();
        foreach (var corner in _currentPath.corners)
        {
            _cornersFromPath.Add(corner);
        }
        if (_cornersFromPath.Count > 1)
        {
            _lastCornerFromPath = _cornersFromPath[1];
            _directionInputFromPath = new Vector2(_cornersFromPath[1].x - transform.position.x, _cornersFromPath[1].z - transform.position.z).normalized;
            _cornersFromPath.RemoveAt(1);
            _cornersFromPath.RemoveAt(0);
        }
        else
        {
            _cornersFromPath.Clear();
            _directionInputFromPath = Vector2.zero;
        }

    }
    public void ArrangeMovementCorners()
    {
        float dist = (_lastCornerFromPath - transform.position).magnitude;
        if (dist < 1.5f)
        {
            if (_cornersFromPath.Count > 0)
            {
                _lastCornerFromPath = _cornersFromPath[0];
                _directionInputFromPath = new Vector2(_cornersFromPath[0].x - transform.position.x, _cornersFromPath[0].z - transform.position.z).normalized;
                _cornersFromPath.RemoveAt(0);
            }
            else
            {
                _directionInputFromPath = Vector2.zero;
            }
            
        }
    }

    public void NavmeshToRigidbody()
    {
        _Agent.updatePosition = false;
        _Agent.updateRotation = false;
        _Agent.updateUpAxis = false;
        _Agent.isStopped = true;
        _Rigidbody.isKinematic = false;
        _Rigidbody.useGravity = true;
    }
    public void RigidbodyToNavmesh()
    {
        _Agent.updatePosition = true;
        _Agent.updateRotation = false;
        _Agent.updateUpAxis = false;
        _Agent.isStopped = false;
        _Rigidbody.isKinematic = true;
        _Rigidbody.useGravity = false;
    }
}

public abstract class Job
{
    public string _Name { get; protected set; }
    public float _Experience { get; protected set; }
    public float _Income { get; protected set; }
    public WeeklyRoutine _WorkRoutine;
    public void SetWeeklyRoutine()
    {
        /*WorkRoutine = new WeeklyRoutine();

        WorkRoutine.monday.startTime = 7;
        WorkRoutine.monday.endTime = 19;

        WorkRoutine.tuesday.startTime = 7;
        WorkRoutine.tuesday.endTime = 19;

        WorkRoutine.wednesday.startTime = 7;
        WorkRoutine.wednesday.endTime = 19;

        WorkRoutine.thursday.startTime = 7;
        WorkRoutine.thursday.endTime = 19;

        WorkRoutine.friday.startTime = 7;
        WorkRoutine.friday.endTime = 19;

        WorkRoutine.saturday.startTime = 9;
        WorkRoutine.saturday.endTime = 17;

        WorkRoutine.sunday.startTime = 9;
        WorkRoutine.sunday.endTime = 14;*/
    }
    public void Work()
    {

    }
    public void GoToWorkLocation()
    {

    }
    public void GoToHome()
    {

    }
}
public struct WeeklyRoutine
{
    public DailyRoutine monday;
    public DailyRoutine tuesday;
    public DailyRoutine wednesday;
    public DailyRoutine thursday;
    public DailyRoutine friday;
    public DailyRoutine saturday;
    public DailyRoutine sunday;
}
public struct DailyRoutine
{
    public int startTime;
    public int endTime;
}