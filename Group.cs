using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Group
{
    public Group(Humanoid leader, string name, State state = null, List<Humanoid> members = null, Settlement settlement = null)
    {
        _Leader = leader;
        _Name = name;
        _Members = members;
        _OwnedSettlement = settlement;
        _State = state;
    }

    public string _Name { get; private set; }
    public Humanoid _Leader { get; private set; }
    public List<Humanoid> _Members { get; private set; }

    public State _State;
    public float _Prestige { get; private set; }
    //public Dictionary<Group, float> _RelationsWithOtherGroups { get; private set; }

    public Settlement _OwnedSettlement { get; private set; }

    public bool IsLeader(Humanoid human)
    {
        if (human == null) return false;

        return human == _Leader;
    }
    /*public void ChangeRelationsWithFaction(Group group, float value)
    {
        if (!_RelationsWithOtherGroups.ContainsKey(group))
        {
            _RelationsWithOtherGroups.Add(group, 0f);
        }

        _RelationsWithOtherGroups[group] += value;
        _RelationsWithOtherGroups[group] = Mathf.Clamp(_RelationsWithOtherGroups[group], -100f, 100f);
    }*/
    
}
public class State
{
    public Humanoid _Ruler;
    public Group _RulingGroup;

    public List<Settlement> _StateLands;
    public List<Group> _StateGroups;

    public Religion _StateReligion;
    public Culture _StateCulture;
    //

    public void SetState(Humanoid ruler, Group rulingGroup, List<Settlement> stateLands, List<Group> stateGroups, Religion stateReligion, Culture stateCulture)
    {
        _Ruler = ruler;
        _RulingGroup = rulingGroup;
        _StateLands = stateLands;
        _StateGroups = stateGroups;
        _StateReligion = stateReligion;
        _StateCulture = stateCulture;
        //
    }
    public void ChangeTaxRate(Settlement settlement, float newRate, Humanoid orderGiver)
    {
        if (settlement == null || orderGiver == null) { Debug.Log("Change Tax Rate ERROR..."); return; }

        settlement.SetTaxRate(newRate, orderGiver);
    }

    public List<Humanoid> GetAllMembers()
    {
        List<Humanoid> list = new List<Humanoid>();
        foreach (var group in _StateGroups)
        {
            foreach (var member in group._Members)
            {
                list.Add(member);
            }
        }
        return list;
    }
   
    
}