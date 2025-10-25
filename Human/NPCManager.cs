using System.Collections.Generic;
using UnityEngine;
using System.Linq;

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
