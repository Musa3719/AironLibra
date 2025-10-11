using System.Collections.Generic;
using UnityEngine;

public static class NPCManager
{
    public static List<NPC> _AllNPCs { get; set; }

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
        if (M_Input.GetKeyDown(KeyCode.Q))
        {
            foreach (var npc in _AllNPCs)
            {
                npc.SpawnNPCChild();
            }
        }
    }
}
