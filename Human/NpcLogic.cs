using System.Collections.Generic;
using UnityEngine;
public enum Characteristic : byte
{
    Bravery, Honesty, Agression, Composure, Greed, Selfishness, Emotionality, Innocence, Talkativeness, Sociability, Sarcasm, Swearing, Fortitude, Arrogance, Stubbornness, Jealousy, Politeness, Optimism, Indecision,
    Determination, Piety, Diligence, Obedience, Loyalty, Obession, Empathy, Hedonizm, Artistry, Adventurousness, AnalyticIntelligence, SocialIntelligence
}
public class NpcLogic
{
    public NPC _Npc { get; private set; }
    public NpcLogic(NPC npc)
    {
        _Npc = npc;
        _Purposes = new List<Purpose>();
        _EventMemories = new List<EventMemory>();
        _KnowledgeToBeliefLevel = new Dictionary<KnowledgeInformation, byte>();
        _Purposes.Add(new PurposeBasicFour(this));
        _Purposes.Add(new PurposeDoYourJob(this));
        _Purposes.Add(new PurposeRest(this));
        _Purposes.Add(new PurposeCleanYourself(this));
        _Purposes.Add(new PurposeHobbyMode(this));
        _Purposes.Add(new PurposeDefensiveStruggle(this));
        _Purposes.Add(new PurposeAgressiveStruggle(this));
        _Characteristics = new byte[System.Enum.GetNames(typeof(Characteristic)).Length];

        //get from save
        for (int i = 0; i < _Characteristics.Length; i++)
        {
            _Characteristics[i] = (byte)Random.Range(0, 256);
        }
    }
    public float _WarrantLevel => 0f;
    public byte[] _Characteristics { get; private set; }
    public byte GetCharacteristic(Characteristic characteristic) => _Characteristics[(int)characteristic];

    /* 
    cesaret                         -> bravery
    dürüstlük                       -> honesty
    agresiflik                      -> aggression
    soðukkanlýlýk                   -> composure
    açgözlülük                      -> greed
    bencillik                       -> selfishness
    duygusallýk                     -> emotionality
    saflýk                          -> innocence
    konuþkanlýk                     -> talkativeness
    sosyallik                       -> sociability
    sarkazm                         -> sarcasm
    küfürbazlýk                     -> swearing
    dayanma gücü                    -> fortitude
    kibir                           -> arrogance
    inatçýlýk                       -> stubbornness
    kýskançlýk                      -> jealousy
    kibarlýk                        -> politeness
    optimistlik                     -> optimism
    kararsýzlýk                     -> indecision
    azim                            -> determination
    dindarlýk                       -> piety
    disiplin / çalýþkanlýk          -> diligence
    itaatkarlýk                     -> obedience
    sadakat                         -> loyalty
    þüphe / takýntý                 -> obsession
    empati                          -> empathy
    eðlenceye odak                  -> hedonism
    sanata odak                     -> artistry
    maceraya / aksiyona odak        -> adventurousness
    analitik zeka                   -> analytical
    sosyal zeka                     -> socialIntelligence
     */

    public List<EventMemory> _EventMemories { get; private set; }
    public Dictionary<KnowledgeInformation, byte> _KnowledgeToBeliefLevel { get; private set; }
    public List<Purpose> _Purposes { get; private set; }
    public Purpose _CurrentPurpose { get; private set; }

    public void UpdateLogic()
    {
        if (_Purposes.Count == 0)
        {
            _CurrentPurpose = null;
            Debug.Log("Purpose Count 0!");
        }

        Purpose tempPurpose = null;
        float tempImportance = float.MinValue;
        foreach (var purpose in _Purposes)
        {
            float importance = GetImportanceOfPurpose(purpose);
            //Debug.Log(purpose + " : " + importance);
            if (importance > tempImportance)
            {
                tempImportance = importance;
                tempPurpose = purpose;
            }
        }

        if (tempPurpose != null && tempPurpose != _CurrentPurpose)
        {
            if (_CurrentPurpose != null && tempImportance - GetImportanceOfPurpose(_CurrentPurpose) - GetCostOfLeavingPurpose(_CurrentPurpose) <= 0f) { Debug.Log("Leaving Purpose Cost stopped purpose change."); return; }

            _CurrentPurpose = tempPurpose;
            _CurrentPurpose.ArrangeTask();
            //Debug.Log(_CurrentPurpose);
        }
    }

    private float GetImportanceOfPurpose(Purpose purpose)
    {
        float importance = purpose.GetImportance();
        importance *= Random.Range(0.97f, 1.03f);

        float cost = purpose.GetCost();

        return importance - cost;
    }
    private float GetCostOfLeavingPurpose(Purpose purpose)
    {
        return purpose.GetCostOfLeavingPurpose();
    }
}

public enum PurposeType : byte { BasicFour, Rest, CleanYourself, DoYourJob, HobbyMode, DefensiveStruggle, AgressiveStruggle, SurvivalMode, MedicalHelpMode, Help, Revenge, Migrate, SeekJusticeForOwn, Wander, SpreadReligion, GetFamous, GetRich, GetPower }
public abstract class Purpose
{
    public NpcLogic _NpcLogic { get; private set; }
    public Purpose(NpcLogic logic, EventMemory createdFrom = null)
    {
        _NpcLogic = logic;
        _CreatedFrom = createdFrom;
        _TasksForThisPurpose = new List<Task>();
        //set tasks with type
    }
    public EventMemory _CreatedFrom { get; private set; }
    public PurposeType _PurposeType { get; protected set; }
    public float _BaseUrgency => PredefinedNpcLogic._PurposeTypeToBaseUrgency[_PurposeType];
    public List<Task> _TasksForThisPurpose { get; private set; }
    public Task _ActiveTask { get; private set; }

    public void ArrangeTask()
    {
        Task tempTask = null;
        float tempImportance = float.MinValue;

        foreach (var task in _TasksForThisPurpose)
        {
            float importance = GetImportanceOfTask(task);
            if (importance > tempImportance)
            {
                tempImportance = importance;
                tempTask = task;
            }
        }

        if (tempTask != null && tempTask != _ActiveTask)
        {
            _ActiveTask = tempTask;
        }
    }
    private float GetImportanceOfTask(Task task)
    {
        float importance = task.GetImportance();
        importance *= Random.Range(0.97f, 1.03f);

        float cost = task.GetCost();

        return importance - cost;
    }
    public float GetImportance()
    {
        return _BaseUrgency
        + PersonalModifierForUrgency()
        + MemoryModifierForUrgency()
        + KnowledgeModifierForUrgency()
        + HabitModifierForUrgency()
        + MoodModifierForUrgency()
        + EnvironmentModifierForUrgency();
    }
    public float CalculateEffectForCharacteristics(sbyte[] characteristicsModifiers, byte[] characteristicsForOneHuman)
    {
        float value = 0f;
        for (int i = 0; i < characteristicsModifiers.Length; i++)
        {
            value += (float)characteristicsModifiers[i] * (float)characteristicsForOneHuman[i] / 127; //  127 / 127 -> modifier used %100
        }
        return value;
    }
    protected abstract float PersonalModifierForUrgency();
    protected abstract float MemoryModifierForUrgency();
    protected abstract float KnowledgeModifierForUrgency();
    protected abstract float HabitModifierForUrgency();
    protected abstract float MoodModifierForUrgency();
    protected abstract float EnvironmentModifierForUrgency();
    public abstract float GetCost();
    public abstract float GetCostOfLeavingPurpose();

}
#region Purposes
public class PurposeBasicFour : Purpose
{
    public PurposeBasicFour(NpcLogic logic) : base(logic)
    {
        _PurposeType = PurposeType.BasicFour;
    }

    private float GetNeedFromVariable(float variableValue) => variableValue * variableValue * 0.05f - 60f;
    protected override float PersonalModifierForUrgency()
    {
        //personal needs, characteristics, religion, wantedLevel, etc..
        float value = 0f;
        value += GetNeedFromVariable(_NpcLogic._Npc._NeedEatAmount) + GetNeedFromVariable(_NpcLogic._Npc._NeedDrinkAmount) + GetNeedFromVariable(_NpcLogic._Npc._NeedPissingAmount) + GetNeedFromVariable(_NpcLogic._Npc._NeedPoopingAmount);
        value += CalculateEffectForCharacteristics(PredefinedNpcLogic._PurposeTypeToCharacteristicModifiers[_PurposeType], _NpcLogic._Characteristics);
        return value;
    }
    protected override float MemoryModifierForUrgency()
    {
        return 0f;//
    }
    protected override float KnowledgeModifierForUrgency()
    {
        return 0f;//
    }
    protected override float HabitModifierForUrgency()
    {
        return 0f;//
    }
    protected override float MoodModifierForUrgency()
    {
        return 0f;//
    }
    protected override float EnvironmentModifierForUrgency()
    {
        return 0f;//
    }
    public override float GetCost()
    {
        return 0f;//
    }
    public override float GetCostOfLeavingPurpose()
    {
        return 0f;// current task progression level, if task is irreversible, if task is irreversible and if it's a PROMISE 

    }
}
public class PurposeRest : Purpose
{
    public PurposeRest(NpcLogic logic) : base(logic)
    {
        _PurposeType = PurposeType.Rest;
    }
    protected override float PersonalModifierForUrgency()
    {
        //personal needs, characteristics, religion, wantedLevel, etc..
        return 0f;//
    }
    protected override float MemoryModifierForUrgency()
    {
        return 0f;//
    }
    protected override float KnowledgeModifierForUrgency()
    {
        return 0f;//
    }
    protected override float HabitModifierForUrgency()
    {
        return 0f;//
    }
    protected override float MoodModifierForUrgency()
    {
        return 0f;//
    }
    protected override float EnvironmentModifierForUrgency()
    {
        return 0f;//
    }
    public override float GetCost()
    {
        return 0f;//
    }
    public override float GetCostOfLeavingPurpose()
    {
        return 0f;//
    }
}
public class PurposeCleanYourself : Purpose
{
    public PurposeCleanYourself(NpcLogic logic) : base(logic)
    {
        _PurposeType = PurposeType.CleanYourself;
    }
    protected override float PersonalModifierForUrgency()
    {
        //personal needs, characteristics, religion, wantedLevel, etc..
        return 0f;//
    }
    protected override float MemoryModifierForUrgency()
    {
        return 0f;//
    }
    protected override float KnowledgeModifierForUrgency()
    {
        return 0f;//
    }
    protected override float HabitModifierForUrgency()
    {
        return 0f;//
    }
    protected override float MoodModifierForUrgency()
    {
        return 0f;//
    }
    protected override float EnvironmentModifierForUrgency()
    {
        return 0f;//
    }
    public override float GetCost()
    {
        return 0f;//
    }
    public override float GetCostOfLeavingPurpose()
    {
        return 0f;//
    }
}
public class PurposeDoYourJob : Purpose
{
    public PurposeDoYourJob(NpcLogic logic) : base(logic)
    {
        _PurposeType = PurposeType.DoYourJob;
    }
    protected override float PersonalModifierForUrgency()
    {
        //personal needs, characteristics, religion, wantedLevel, etc..
        return 0f;//
    }
    protected override float MemoryModifierForUrgency()
    {
        return 0f;//
    }
    protected override float KnowledgeModifierForUrgency()
    {
        return 0f;//
    }
    protected override float HabitModifierForUrgency()
    {
        return 0f;//
    }
    protected override float MoodModifierForUrgency()
    {
        return 0f;//
    }
    protected override float EnvironmentModifierForUrgency()
    {
        return 0f;//
    }
    public override float GetCost()
    {
        return 0f;//
    }
    public override float GetCostOfLeavingPurpose()
    {
        return 0f;//
    }
}
public class PurposeHobbyMode : Purpose
{
    public PurposeHobbyMode(NpcLogic logic) : base(logic)
    {
        _PurposeType = PurposeType.HobbyMode;
    }
    protected override float PersonalModifierForUrgency()
    {
        //personal needs, characteristics, religion, wantedLevel, etc..
        return 0f;//
    }
    protected override float MemoryModifierForUrgency()
    {
        return 0f;//
    }
    protected override float KnowledgeModifierForUrgency()
    {
        return 0f;//
    }
    protected override float HabitModifierForUrgency()
    {
        return 0f;//
    }
    protected override float MoodModifierForUrgency()
    {
        return 0f;//
    }
    protected override float EnvironmentModifierForUrgency()
    {
        return 0f;//
    }
    public override float GetCost()
    {
        return 0f;//
    }
    public override float GetCostOfLeavingPurpose()
    {
        return 0f;//
    }
}
public class PurposeDefensiveStruggle : Purpose
{
    public PurposeDefensiveStruggle(NpcLogic logic) : base(logic)
    {
        _PurposeType = PurposeType.DefensiveStruggle;
    }
    protected override float PersonalModifierForUrgency()
    {
        //personal needs, characteristics, religion, wantedLevel, etc..
        return 0f;//
    }
    protected override float MemoryModifierForUrgency()
    {
        return 0f;//
    }
    protected override float KnowledgeModifierForUrgency()
    {
        return 0f;//
    }
    protected override float HabitModifierForUrgency()
    {
        return 0f;//
    }
    protected override float MoodModifierForUrgency()
    {
        return 0f;//
    }
    protected override float EnvironmentModifierForUrgency()
    {
        return 0f;//
    }
    public override float GetCost()
    {
        return 0f;//
    }
    public override float GetCostOfLeavingPurpose()
    {
        return 0f;//
    }
}
public class PurposeAgressiveStruggle : Purpose
{
    public PurposeAgressiveStruggle(NpcLogic logic) : base(logic)
    {
        _PurposeType = PurposeType.AgressiveStruggle;
    }
    protected override float PersonalModifierForUrgency()
    {
        //personal needs, characteristics, religion, wantedLevel, etc..
        return 0f;//
    }
    protected override float MemoryModifierForUrgency()
    {
        return 0f;//
    }
    protected override float KnowledgeModifierForUrgency()
    {
        return 0f;//
    }
    protected override float HabitModifierForUrgency()
    {
        return 0f;//
    }
    protected override float MoodModifierForUrgency()
    {
        return 0f;//
    }
    protected override float EnvironmentModifierForUrgency()
    {
        return 0f;//
    }
    public override float GetCost()
    {
        return 0f;//
    }
    public override float GetCostOfLeavingPurpose()
    {
        return 0f;//
    }
}
public class PurposeSurvivalMode : Purpose
{
    public PurposeSurvivalMode(NpcLogic logic) : base(logic)
    {
        _PurposeType = PurposeType.SurvivalMode;
    }
    protected override float PersonalModifierForUrgency()
    {
        //personal needs, characteristics, religion, wantedLevel, etc..
        return 0f;//
    }
    protected override float MemoryModifierForUrgency()
    {
        return 0f;//
    }
    protected override float KnowledgeModifierForUrgency()
    {
        return 0f;//
    }
    protected override float HabitModifierForUrgency()
    {
        return 0f;//
    }
    protected override float MoodModifierForUrgency()
    {
        return 0f;//
    }
    protected override float EnvironmentModifierForUrgency()
    {
        return 0f;//
    }
    public override float GetCost()
    {
        return 0f;//
    }
    public override float GetCostOfLeavingPurpose()
    {
        return 0f;//
    }
}
public class PurposeMedicalHelpMode : Purpose
{
    public PurposeMedicalHelpMode(NpcLogic logic) : base(logic)
    {
        _PurposeType = PurposeType.MedicalHelpMode;
    }
    protected override float PersonalModifierForUrgency()
    {
        //personal needs, characteristics, religion, wantedLevel, etc..
        return 0f;//
    }
    protected override float MemoryModifierForUrgency()
    {
        return 0f;//
    }
    protected override float KnowledgeModifierForUrgency()
    {
        return 0f;//
    }
    protected override float HabitModifierForUrgency()
    {
        return 0f;//
    }
    protected override float MoodModifierForUrgency()
    {
        return 0f;//
    }
    protected override float EnvironmentModifierForUrgency()
    {
        return 0f;//
    }
    public override float GetCost()
    {
        return 0f;//
    }
    public override float GetCostOfLeavingPurpose()
    {
        return 0f;//
    }
}
public class PurposeHelp : Purpose
{
    public PurposeHelp(NpcLogic logic) : base(logic)
    {
        _PurposeType = PurposeType.Help;
    }
    protected override float PersonalModifierForUrgency()
    {
        //personal needs, characteristics, religion, wantedLevel, etc..
        return 0f;//
    }
    protected override float MemoryModifierForUrgency()
    {
        return 0f;//
    }
    protected override float KnowledgeModifierForUrgency()
    {
        return 0f;//
    }
    protected override float HabitModifierForUrgency()
    {
        return 0f;//
    }
    protected override float MoodModifierForUrgency()
    {
        return 0f;//
    }
    protected override float EnvironmentModifierForUrgency()
    {
        return 0f;//
    }
    public override float GetCost()
    {
        return 0f;//
    }
    public override float GetCostOfLeavingPurpose()
    {
        return 0f;//
    }
}
public class PurposeRevenge : Purpose
{
    public PurposeRevenge(NpcLogic logic) : base(logic)
    {
        _PurposeType = PurposeType.Revenge;
    }
    protected override float PersonalModifierForUrgency()
    {
        //personal needs, characteristics, religion, wantedLevel, etc..
        return 0f;//
    }
    protected override float MemoryModifierForUrgency()
    {
        return 0f;//
    }
    protected override float KnowledgeModifierForUrgency()
    {
        return 0f;//
    }
    protected override float HabitModifierForUrgency()
    {
        return 0f;//
    }
    protected override float MoodModifierForUrgency()
    {
        return 0f;//
    }
    protected override float EnvironmentModifierForUrgency()
    {
        return 0f;//
    }
    public override float GetCost()
    {
        return 0f;//
    }
    public override float GetCostOfLeavingPurpose()
    {
        return 0f;//
    }
}
public class PurposeMigrate : Purpose
{
    public PurposeMigrate(NpcLogic logic) : base(logic)
    {
        _PurposeType = PurposeType.Migrate;
    }
    protected override float PersonalModifierForUrgency()
    {
        //personal needs, characteristics, religion, wantedLevel, etc..
        return 0f;//
    }
    protected override float MemoryModifierForUrgency()
    {
        return 0f;//
    }
    protected override float KnowledgeModifierForUrgency()
    {
        return 0f;//
    }
    protected override float HabitModifierForUrgency()
    {
        return 0f;//
    }
    protected override float MoodModifierForUrgency()
    {
        return 0f;//
    }
    protected override float EnvironmentModifierForUrgency()
    {
        return 0f;//
    }
    public override float GetCost()
    {
        return 0f;//
    }
    public override float GetCostOfLeavingPurpose()
    {
        return 0f;//
    }
}
public class PurposeSeekJusticeForOwn : Purpose
{
    public PurposeSeekJusticeForOwn(NpcLogic logic) : base(logic)
    {
        _PurposeType = PurposeType.SeekJusticeForOwn;
    }
    protected override float PersonalModifierForUrgency()
    {
        //personal needs, characteristics, religion, wantedLevel, etc..
        return 0f;//
    }
    protected override float MemoryModifierForUrgency()
    {
        return 0f;//
    }
    protected override float KnowledgeModifierForUrgency()
    {
        return 0f;//
    }
    protected override float HabitModifierForUrgency()
    {
        return 0f;//
    }
    protected override float MoodModifierForUrgency()
    {
        return 0f;//
    }
    protected override float EnvironmentModifierForUrgency()
    {
        return 0f;//
    }
    public override float GetCost()
    {
        return 0f;//
    }
    public override float GetCostOfLeavingPurpose()
    {
        return 0f;//
    }
}
public class PurposeWander : Purpose
{
    public PurposeWander(NpcLogic logic) : base(logic)
    {
        _PurposeType = PurposeType.Wander;
    }
    protected override float PersonalModifierForUrgency()
    {
        //personal needs, characteristics, religion, wantedLevel, etc..
        return 0f;//
    }
    protected override float MemoryModifierForUrgency()
    {
        return 0f;//
    }
    protected override float KnowledgeModifierForUrgency()
    {
        return 0f;//
    }
    protected override float HabitModifierForUrgency()
    {
        return 0f;//
    }
    protected override float MoodModifierForUrgency()
    {
        return 0f;//
    }
    protected override float EnvironmentModifierForUrgency()
    {
        return 0f;//
    }
    public override float GetCost()
    {
        return 0f;//
    }
    public override float GetCostOfLeavingPurpose()
    {
        return 0f;//
    }
}
public class PurposeSpreadReligion : Purpose
{
    public PurposeSpreadReligion(NpcLogic logic) : base(logic)
    {
        _PurposeType = PurposeType.SpreadReligion;
    }
    protected override float PersonalModifierForUrgency()
    {
        //personal needs, characteristics, religion, wantedLevel, etc..
        return 0f;//
    }
    protected override float MemoryModifierForUrgency()
    {
        return 0f;//
    }
    protected override float KnowledgeModifierForUrgency()
    {
        return 0f;//
    }
    protected override float HabitModifierForUrgency()
    {
        return 0f;//
    }
    protected override float MoodModifierForUrgency()
    {
        return 0f;//
    }
    protected override float EnvironmentModifierForUrgency()
    {
        return 0f;//
    }
    public override float GetCost()
    {
        return 0f;//
    }
    public override float GetCostOfLeavingPurpose()
    {
        return 0f;//
    }
}
public class PurposeGetFamous : Purpose
{
    public PurposeGetFamous(NpcLogic logic) : base(logic)
    {
        _PurposeType = PurposeType.GetFamous;
    }
    protected override float PersonalModifierForUrgency()
    {
        //personal needs, characteristics, religion, wantedLevel, etc..
        return 0f;//
    }
    protected override float MemoryModifierForUrgency()
    {
        return 0f;//
    }
    protected override float KnowledgeModifierForUrgency()
    {
        return 0f;//
    }
    protected override float HabitModifierForUrgency()
    {
        return 0f;//
    }
    protected override float MoodModifierForUrgency()
    {
        return 0f;//
    }
    protected override float EnvironmentModifierForUrgency()
    {
        return 0f;//
    }
    public override float GetCost()
    {
        return 0f;//
    }
    public override float GetCostOfLeavingPurpose()
    {
        return 0f;//
    }
}
public class PurposeGetRich : Purpose
{
    public PurposeGetRich(NpcLogic logic) : base(logic)
    {
        _PurposeType = PurposeType.GetRich;
    }
    protected override float PersonalModifierForUrgency()
    {
        //personal needs, characteristics, religion, wantedLevel, etc..
        return 0f;//
    }
    protected override float MemoryModifierForUrgency()
    {
        return 0f;//
    }
    protected override float KnowledgeModifierForUrgency()
    {
        return 0f;//
    }
    protected override float HabitModifierForUrgency()
    {
        return 0f;//
    }
    protected override float MoodModifierForUrgency()
    {
        return 0f;//
    }
    protected override float EnvironmentModifierForUrgency()
    {
        return 0f;//
    }
    public override float GetCost()
    {
        return 0f;//
    }
    public override float GetCostOfLeavingPurpose()
    {
        return 0f;//
    }
}
public class PurposeGetPower : Purpose
{
    public PurposeGetPower(NpcLogic logic) : base(logic)
    {
        _PurposeType = PurposeType.GetPower;
    }
    protected override float PersonalModifierForUrgency()
    {
        //personal needs, characteristics, religion, wantedLevel, etc..
        return 0f;//
    }
    protected override float MemoryModifierForUrgency()
    {
        return 0f;//
    }
    protected override float KnowledgeModifierForUrgency()
    {
        return 0f;//
    }
    protected override float HabitModifierForUrgency()
    {
        return 0f;//
    }
    protected override float MoodModifierForUrgency()
    {
        return 0f;//
    }
    protected override float EnvironmentModifierForUrgency()
    {
        return 0f;//
    }
    public override float GetCost()
    {
        return 0f;//
    }
    public override float GetCostOfLeavingPurpose()
    {
        return 0f;//
    }
}
#endregion
public abstract class Task
{
    public NpcLogic _NpcLogic { get; private set; }
    public Task(NpcLogic logic)
    {
        _NpcLogic = logic;
    }
    public abstract bool IsPossible();
    public abstract void TaskMethod();
    public abstract float GetImportance();
    public abstract float GetCost();
    public float GetWantedLevel() => _NpcLogic._WarrantLevel;
    public void SetNewMovementTarget(Vector3 pos) => _NpcLogic._Npc.ArrangeNewMovementTarget(pos);
}


#region Tasks
public class TaskEat : Task
{
    public TaskEat(NpcLogic logic) : base(logic) { }

    public Item _EatItem;
    public override bool IsPossible() { return _NpcLogic._Npc._Inventory.HasItemType(_EatItem._ItemDefinition); }
    public override float GetImportance() { return 0f; }
    public override float GetCost() { return 0f; }
    public override void TaskMethod() { Eat(); }
    public void Eat() { }
}
public class TaskDrink : Task
{
    public TaskDrink(NpcLogic logic) : base(logic) { }

    public Item _DrinkItem;
    public override bool IsPossible() { return _NpcLogic._Npc._Inventory.HasItemType(_DrinkItem._ItemDefinition); }
    public override float GetImportance() { return 0f; }
    public override float GetCost() { return 0f; }
    public override void TaskMethod() { Drink(); }
    public void Drink() { }
}
public class TaskPiss : Task
{
    public TaskPiss(NpcLogic logic) : base(logic) { }

    public Vector3 _PissPosition;
    public override bool IsPossible() { return true; }
    public override float GetImportance() { return 0f; }
    public override float GetCost() { return 0f; }
    public override void TaskMethod() { if ((_NpcLogic._Npc.transform.position - _PissPosition).sqrMagnitude < 0.5f) SetNewMovementTarget(_PissPosition); else Piss(); }
    public void Piss() { }
}

#endregion

public static class PredefinedNpcLogic
{
    public static Dictionary<PurposeType, byte> _PurposeTypeToBaseUrgency { get; private set; }
    public static Dictionary<PurposeType, sbyte[]> _PurposeTypeToCharacteristicModifiers { get; private set; }

    public static void ClearArray(this sbyte[] array) { for (int i = 0; i < array.Length; i++) { array[i] = 0; } }

    public static void Init()
    {
        SetPurposeTypeToBaseUrgency();
        SetPurposeTypeToCharacteristicModifiers();
    }

    public static void SetPurposeTypeToBaseUrgency()
    {
        _PurposeTypeToBaseUrgency = new Dictionary<PurposeType, byte>
        {
            { PurposeType.BasicFour, 175 },
            { PurposeType.Rest, 110 },
            { PurposeType.CleanYourself, 95 },
            { PurposeType.DoYourJob, 150 },
            { PurposeType.DefensiveStruggle, 200 },
            { PurposeType.AgressiveStruggle, 185 },
            { PurposeType.SurvivalMode, 225 },
            { PurposeType.MedicalHelpMode, 200 },
            { PurposeType.HobbyMode, 75 },
            { PurposeType.Revenge, 100 },
            { PurposeType.Wander, 95 },
            { PurposeType.SpreadReligion, 130 },
            { PurposeType.Help, 130 },
            { PurposeType.Migrate, 100 },
            { PurposeType.SeekJusticeForOwn, 160 },
            { PurposeType.GetFamous, 80 },
            { PurposeType.GetRich, 85 },
            { PurposeType.GetPower, 80 }
        };
    }
    public static void SetPurposeTypeToCharacteristicModifiers()
    {
        _PurposeTypeToCharacteristicModifiers = new Dictionary<PurposeType, sbyte[]>();
        sbyte[] temp = new sbyte[System.Enum.GetNames(typeof(Characteristic)).Length];

        temp[(int)Characteristic.Fortitude] = -60;
        temp[(int)Characteristic.Greed] = 40;
        temp[(int)Characteristic.Hedonizm] = 30;
        temp[(int)Characteristic.Obedience] = -20;
        temp[(int)Characteristic.Obession] = -100;
        temp[(int)Characteristic.Optimism] = 30;
        temp[(int)Characteristic.Piety] = -50;
        temp[(int)Characteristic.Selfishness] = 40;
        _PurposeTypeToCharacteristicModifiers.Add(PurposeType.BasicFour, temp);

        temp.ClearArray();
        temp[(int)Characteristic.Adventurousness] = -40;
        temp[(int)Characteristic.Diligence] = -90;
        temp[(int)Characteristic.Fortitude] = -70;
        temp[(int)Characteristic.Hedonizm] = 100;
        temp[(int)Characteristic.Innocence] = 40;
        temp[(int)Characteristic.Obession] = -80;
        temp[(int)Characteristic.Optimism] = 40;
        temp[(int)Characteristic.Piety] = -40;
        temp[(int)Characteristic.Selfishness] = 50;
        _PurposeTypeToCharacteristicModifiers.Add(PurposeType.Rest, temp);

        temp.ClearArray();
        temp[(int)Characteristic.Artistry] = 80;
        temp[(int)Characteristic.Diligence] = -60;
        temp[(int)Characteristic.Emotionality] = 20;
        temp[(int)Characteristic.Fortitude] = -80;
        temp[(int)Characteristic.Hedonizm] = -20;
        temp[(int)Characteristic.Innocence] = 90;
        temp[(int)Characteristic.Optimism] = 20;
        temp[(int)Characteristic.Piety] = -90;
        temp[(int)Characteristic.Politeness] = 50;
        temp[(int)Characteristic.SocialIntelligence] = 50;
        temp[(int)Characteristic.Swearing] = -60;
        _PurposeTypeToCharacteristicModifiers.Add(PurposeType.CleanYourself, temp);

        temp.ClearArray();
        temp[(int)Characteristic.Adventurousness] = -30;
        temp[(int)Characteristic.Arrogance] = -30;
        temp[(int)Characteristic.Artistry] = -30;
        temp[(int)Characteristic.Determination] = 50;
        temp[(int)Characteristic.Diligence] = 120;
        temp[(int)Characteristic.Empathy] = -10;
        temp[(int)Characteristic.Fortitude] = 40;
        temp[(int)Characteristic.Greed] = -10;
        temp[(int)Characteristic.Hedonizm] = -60;
        temp[(int)Characteristic.Honesty] = 30;
        temp[(int)Characteristic.Indecision] = -10;
        temp[(int)Characteristic.Obedience] = 30;
        temp[(int)Characteristic.Obession] = 70;
        temp[(int)Characteristic.Piety] = 50;
        temp[(int)Characteristic.Sarcasm] = -35;
        temp[(int)Characteristic.Selfishness] = -30;
        temp[(int)Characteristic.Stubbornness] = 15;
        _PurposeTypeToCharacteristicModifiers.Add(PurposeType.DoYourJob, temp);

        temp.ClearArray();
        temp[(int)Characteristic.Agression] = -120;
        temp[(int)Characteristic.Arrogance] = -40;
        temp[(int)Characteristic.AnalyticIntelligence] = 40;
        temp[(int)Characteristic.Artistry] = 80;
        temp[(int)Characteristic.Bravery] = -20;
        temp[(int)Characteristic.Composure] = 50;
        temp[(int)Characteristic.Determination] = 10;
        temp[(int)Characteristic.Emotionality] = 30;
        temp[(int)Characteristic.Empathy] = 100;
        temp[(int)Characteristic.Fortitude] = -30;
        temp[(int)Characteristic.Greed] = -20;
        temp[(int)Characteristic.Hedonizm] = 45;
        temp[(int)Characteristic.Indecision] = 40;
        temp[(int)Characteristic.Innocence] = 25;
        temp[(int)Characteristic.Jealousy] = -30;
        temp[(int)Characteristic.Obedience] = 20;
        temp[(int)Characteristic.Obession] = -35;
        temp[(int)Characteristic.Optimism] = 35;
        temp[(int)Characteristic.Piety] = 10;
        temp[(int)Characteristic.Politeness] = 100;
        temp[(int)Characteristic.Sarcasm] = 15;
        temp[(int)Characteristic.Sociability] = 20;
        temp[(int)Characteristic.SocialIntelligence] = 20;
        temp[(int)Characteristic.Stubbornness] = -40;
        temp[(int)Characteristic.Talkativeness] = 10;
        _PurposeTypeToCharacteristicModifiers.Add(PurposeType.DefensiveStruggle, temp);

        temp.ClearArray();
        temp[(int)Characteristic.Agression] = 120;
        temp[(int)Characteristic.Arrogance] = 40;
        temp[(int)Characteristic.AnalyticIntelligence] = -40;
        temp[(int)Characteristic.Artistry] = -80;
        temp[(int)Characteristic.Bravery] = 20;
        temp[(int)Characteristic.Composure] = -50;
        temp[(int)Characteristic.Determination] = -10;
        temp[(int)Characteristic.Emotionality] = -30;
        temp[(int)Characteristic.Empathy] = -100;
        temp[(int)Characteristic.Fortitude] = 30;
        temp[(int)Characteristic.Greed] = 20;
        temp[(int)Characteristic.Hedonizm] = -45;
        temp[(int)Characteristic.Indecision] = -40;
        temp[(int)Characteristic.Innocence] = -25;
        temp[(int)Characteristic.Jealousy] = 30;
        temp[(int)Characteristic.Obedience] = -20;
        temp[(int)Characteristic.Obession] = 35;
        temp[(int)Characteristic.Optimism] = -35;
        temp[(int)Characteristic.Piety] = -10;
        temp[(int)Characteristic.Politeness] = -100;
        temp[(int)Characteristic.Sarcasm] = -15;
        temp[(int)Characteristic.Sociability] = -20;
        temp[(int)Characteristic.SocialIntelligence] = -20;
        temp[(int)Characteristic.Stubbornness] = 40;
        temp[(int)Characteristic.Talkativeness] = -10;
        _PurposeTypeToCharacteristicModifiers.Add(PurposeType.AgressiveStruggle, temp);

        temp.ClearArray();
        temp[(int)Characteristic.Agression] = -50;
        temp[(int)Characteristic.Arrogance] = 70;
        temp[(int)Characteristic.Bravery] = -60;
        temp[(int)Characteristic.Composure] = -30;
        temp[(int)Characteristic.Determination] = -40;
        temp[(int)Characteristic.Diligence] = -10;
        temp[(int)Characteristic.Emotionality] = 30;
        temp[(int)Characteristic.Fortitude] = -50;
        temp[(int)Characteristic.Hedonizm] = 40;
        temp[(int)Characteristic.Indecision] = 20;
        temp[(int)Characteristic.Innocence] = 100;
        temp[(int)Characteristic.Loyalty] = -20;
        temp[(int)Characteristic.Obession] = -30;
        temp[(int)Characteristic.Piety] = -30;
        temp[(int)Characteristic.Politeness] = 20;
        temp[(int)Characteristic.Selfishness] = 120;
        temp[(int)Characteristic.Swearing] = -25;
        temp[(int)Characteristic.SocialIntelligence] = -50;
        _PurposeTypeToCharacteristicModifiers.Add(PurposeType.SurvivalMode, temp);

        temp.ClearArray();
        temp[(int)Characteristic.Agression] = -30;
        temp[(int)Characteristic.AnalyticIntelligence] = 30;
        temp[(int)Characteristic.Arrogance] = -50;
        temp[(int)Characteristic.Artistry] = 10;
        temp[(int)Characteristic.Bravery] = 20;
        temp[(int)Characteristic.Composure] = 70;
        temp[(int)Characteristic.Determination] = 40;
        temp[(int)Characteristic.Diligence] = 50;
        temp[(int)Characteristic.Emotionality] = 80;
        temp[(int)Characteristic.Empathy] = 120;
        temp[(int)Characteristic.Fortitude] = -40;
        temp[(int)Characteristic.Greed] = -30;
        temp[(int)Characteristic.Hedonizm] = -10;
        temp[(int)Characteristic.Honesty] = 40;
        temp[(int)Characteristic.Indecision] = -60;
        temp[(int)Characteristic.Innocence] = -10;
        temp[(int)Characteristic.Jealousy] = -20;
        temp[(int)Characteristic.Loyalty] = 20;
        temp[(int)Characteristic.Piety] = 20;
        temp[(int)Characteristic.Politeness] = 20;
        temp[(int)Characteristic.Selfishness] = -90;
        temp[(int)Characteristic.Sociability] = 30;
        temp[(int)Characteristic.SocialIntelligence] = 30;
        _PurposeTypeToCharacteristicModifiers.Add(PurposeType.MedicalHelpMode, temp);

        temp.ClearArray();
        temp[(int)Characteristic.Adventurousness] = 40;
        temp[(int)Characteristic.Agression] = -30;
        temp[(int)Characteristic.AnalyticIntelligence] = 20;
        temp[(int)Characteristic.Artistry] = 110;
        temp[(int)Characteristic.Composure] = -40;
        temp[(int)Characteristic.Diligence] = -30;
        temp[(int)Characteristic.Fortitude] = -20;
        temp[(int)Characteristic.Hedonizm] = 120;
        temp[(int)Characteristic.Innocence] = 80;
        temp[(int)Characteristic.Obession] = -50;
        temp[(int)Characteristic.Piety] = -70;
        temp[(int)Characteristic.Sarcasm] = 40;
        temp[(int)Characteristic.Selfishness] = 20;
        temp[(int)Characteristic.Sociability] = 40;
        temp[(int)Characteristic.SocialIntelligence] = 40;
        temp[(int)Characteristic.Talkativeness] = 25;
        _PurposeTypeToCharacteristicModifiers.Add(PurposeType.HobbyMode, temp);

        temp.ClearArray();
        temp[(int)Characteristic.Agression] = 120;
        temp[(int)Characteristic.AnalyticIntelligence] = -20;
        temp[(int)Characteristic.Artistry] = -20;
        temp[(int)Characteristic.Bravery] = 40;
        temp[(int)Characteristic.Determination] = 30;
        temp[(int)Characteristic.Emotionality] = 100;
        temp[(int)Characteristic.Empathy] = 50;
        temp[(int)Characteristic.Fortitude] = -30;
        temp[(int)Characteristic.Innocence] = 40;
        temp[(int)Characteristic.Loyalty] = 70;
        temp[(int)Characteristic.Obedience] = -50;
        temp[(int)Characteristic.Obession] = 100;
        temp[(int)Characteristic.Piety] = -20;
        temp[(int)Characteristic.Selfishness] = -40;
        temp[(int)Characteristic.Stubbornness] = 50;
        _PurposeTypeToCharacteristicModifiers.Add(PurposeType.Revenge, temp);

        temp.ClearArray();
        temp[(int)Characteristic.Adventurousness] = 120;
        temp[(int)Characteristic.Agression] = -40;
        temp[(int)Characteristic.AnalyticIntelligence] = 30;
        temp[(int)Characteristic.Artistry] = 70;
        temp[(int)Characteristic.Diligence] = -60;
        temp[(int)Characteristic.Greed] = 20;
        temp[(int)Characteristic.Hedonizm] = 40;
        temp[(int)Characteristic.Innocence] = 30;
        temp[(int)Characteristic.Obedience] = -40;
        temp[(int)Characteristic.Optimism] = 40;
        temp[(int)Characteristic.Piety] = 40;
        temp[(int)Characteristic.Sociability] = 80;
        temp[(int)Characteristic.SocialIntelligence] = 50;
        _PurposeTypeToCharacteristicModifiers.Add(PurposeType.Wander, temp);

        temp.ClearArray();
        temp[(int)Characteristic.Adventurousness] = 30;
        temp[(int)Characteristic.Agression] = 10;
        temp[(int)Characteristic.Arrogance] = 60;
        temp[(int)Characteristic.Artistry] = -60;
        temp[(int)Characteristic.Determination] = 30;
        temp[(int)Characteristic.Diligence] = 40;
        temp[(int)Characteristic.Emotionality] = 30;
        temp[(int)Characteristic.Empathy] = 30;
        temp[(int)Characteristic.Fortitude] = -60;
        temp[(int)Characteristic.Greed] = -30;
        temp[(int)Characteristic.Hedonizm] = -100;
        temp[(int)Characteristic.Honesty] = 40;
        temp[(int)Characteristic.Indecision] = -40;
        temp[(int)Characteristic.Loyalty] = 20;
        temp[(int)Characteristic.Obedience] = 60;
        temp[(int)Characteristic.Obession] = 90;
        temp[(int)Characteristic.Optimism] = -30;
        temp[(int)Characteristic.Piety] = 120;
        temp[(int)Characteristic.Politeness] = -60;
        temp[(int)Characteristic.Sarcasm] = -30;
        temp[(int)Characteristic.Selfishness] = -40;
        temp[(int)Characteristic.Sociability] = 40;
        temp[(int)Characteristic.Stubbornness] = 60;
        temp[(int)Characteristic.Swearing] = -50;
        temp[(int)Characteristic.Talkativeness] = 50;
        _PurposeTypeToCharacteristicModifiers.Add(PurposeType.SpreadReligion, temp);

        temp.ClearArray();
        temp[(int)Characteristic.Adventurousness] = 40;
        temp[(int)Characteristic.Agression] = -30;
        temp[(int)Characteristic.AnalyticIntelligence] = 20;
        temp[(int)Characteristic.Arrogance] = -40;
        temp[(int)Characteristic.Artistry] = 30;
        temp[(int)Characteristic.Bravery] = 50;
        temp[(int)Characteristic.Composure] = 20;
        temp[(int)Characteristic.Determination] = 40;
        temp[(int)Characteristic.Diligence] = 40;
        temp[(int)Characteristic.Emotionality] = 50;
        temp[(int)Characteristic.Empathy] = 120;
        _PurposeTypeToCharacteristicModifiers.Add(PurposeType.Help, temp);

        temp.ClearArray();
        temp[(int)Characteristic.Adventurousness] = 100;
        temp[(int)Characteristic.Artistry] = 30;
        temp[(int)Characteristic.Diligence] = -40;
        temp[(int)Characteristic.Greed] = 60;
        temp[(int)Characteristic.Indecision] = 60;
        temp[(int)Characteristic.Jealousy] = 40;
        temp[(int)Characteristic.Loyalty] = -50;
        temp[(int)Characteristic.Obedience] = -50;
        temp[(int)Characteristic.Obession] = 20;
        temp[(int)Characteristic.Sociability] = 20;
        temp[(int)Characteristic.Stubbornness] = -40;
        _PurposeTypeToCharacteristicModifiers.Add(PurposeType.Migrate, temp);

        temp.ClearArray();
        temp[(int)Characteristic.Adventurousness] = 20;
        temp[(int)Characteristic.Agression] = 70;
        temp[(int)Characteristic.AnalyticIntelligence] = 50;
        temp[(int)Characteristic.Bravery] = 70;
        temp[(int)Characteristic.Determination] = 100;
        temp[(int)Characteristic.Diligence] = -30;
        temp[(int)Characteristic.Fortitude] = -60;
        temp[(int)Characteristic.Greed] = 50;
        temp[(int)Characteristic.Hedonizm] = -50;
        temp[(int)Characteristic.Honesty] = 80;
        temp[(int)Characteristic.Indecision] = -100;
        temp[(int)Characteristic.Innocence] = -40;
        temp[(int)Characteristic.Obedience] = -120;
        temp[(int)Characteristic.Obession] = 40;
        temp[(int)Characteristic.Optimism] = -40;
        temp[(int)Characteristic.Piety] = -60;
        temp[(int)Characteristic.Politeness] = -30;
        temp[(int)Characteristic.Selfishness] = 60;
        temp[(int)Characteristic.SocialIntelligence] = 30;
        temp[(int)Characteristic.Stubbornness] = 120;
        _PurposeTypeToCharacteristicModifiers.Add(PurposeType.SeekJusticeForOwn, temp);

        temp.ClearArray();
        temp[(int)Characteristic.Adventurousness] = 100;
        temp[(int)Characteristic.Agression] = 20;
        temp[(int)Characteristic.AnalyticIntelligence] = -50;
        temp[(int)Characteristic.Arrogance] = 120;
        temp[(int)Characteristic.Artistry] = 100;
        temp[(int)Characteristic.Determination] = 30;
        temp[(int)Characteristic.Emotionality] = 30;
        temp[(int)Characteristic.Fortitude] = -90;
        temp[(int)Characteristic.Greed] = 40;
        temp[(int)Characteristic.Hedonizm] = 30;
        temp[(int)Characteristic.Honesty] = -50;
        temp[(int)Characteristic.Indecision] = -50;
        temp[(int)Characteristic.Innocence] = 30;
        temp[(int)Characteristic.Jealousy] = 30;
        temp[(int)Characteristic.Loyalty] = -50;
        temp[(int)Characteristic.Obedience] = -80;
        temp[(int)Characteristic.Obession] = 25;
        temp[(int)Characteristic.Optimism] = 25;
        temp[(int)Characteristic.Piety] = -120;
        temp[(int)Characteristic.Sarcasm] = -40;
        temp[(int)Characteristic.Sociability] = 40;
        temp[(int)Characteristic.Swearing] = -40;
        temp[(int)Characteristic.Talkativeness] = 30;
        _PurposeTypeToCharacteristicModifiers.Add(PurposeType.GetFamous, temp);

        temp.ClearArray();
        temp[(int)Characteristic.AnalyticIntelligence] = 30;
        temp[(int)Characteristic.Arrogance] = 100;
        temp[(int)Characteristic.Artistry] = -80;
        temp[(int)Characteristic.Determination] = 50;
        temp[(int)Characteristic.Diligence] = 50;
        temp[(int)Characteristic.Fortitude] = -70;
        temp[(int)Characteristic.Greed] = 120;
        temp[(int)Characteristic.Hedonizm] = -100;
        temp[(int)Characteristic.Indecision] = -40;
        temp[(int)Characteristic.Innocence] = -50;
        temp[(int)Characteristic.Jealousy] = 90;
        temp[(int)Characteristic.Obedience] = -120;
        temp[(int)Characteristic.Obession] = 30;
        temp[(int)Characteristic.Optimism] = -20;
        temp[(int)Characteristic.Piety] = -90;
        temp[(int)Characteristic.Selfishness] = 30;
        temp[(int)Characteristic.Swearing] = -30;
        _PurposeTypeToCharacteristicModifiers.Add(PurposeType.GetRich, temp);

        temp.ClearArray();
        temp[(int)Characteristic.Adventurousness] = 50;
        temp[(int)Characteristic.Agression] = 50;
        temp[(int)Characteristic.Arrogance] = 120;
        temp[(int)Characteristic.Artistry] = -120;
        temp[(int)Characteristic.Bravery] = 40;
        temp[(int)Characteristic.Determination] = 40;
        temp[(int)Characteristic.Diligence] = 50;
        temp[(int)Characteristic.Fortitude] = -60;
        temp[(int)Characteristic.Greed] = 30;
        temp[(int)Characteristic.Hedonizm] = -80;
        temp[(int)Characteristic.Honesty] = -30;
        temp[(int)Characteristic.Indecision] = -60;
        temp[(int)Characteristic.Innocence] = -70;
        temp[(int)Characteristic.Jealousy] = 60;
        temp[(int)Characteristic.Loyalty] = -80;
        temp[(int)Characteristic.Obedience] = -120;
        temp[(int)Characteristic.Obession] = 40;
        temp[(int)Characteristic.Optimism] = -40;
        temp[(int)Characteristic.Piety] = -80;
        temp[(int)Characteristic.Selfishness] = 60;
        _PurposeTypeToCharacteristicModifiers.Add(PurposeType.GetPower, temp);
    }
}



/*public enum TaskType : byte
{
    Eat, Drink, Piss, Defecate, Clean, Sit, LieDown, Sleep, Share, Craft, Repair, Build, Alchemy, Cleaning, Take, Drop, Steal, Lockpick, ToHorseBack, ToOnFoot, Equip, Unequip, GiveThing, RequestThing, Sell, Buy, Write, Pray, ExecuteOrder, RefuseAnyRequest,
    RequestFavor, Train, TeachTalk, MoveToRelaxed, MoveToFast, RunTo, Stand, StandAndWatch, PlayInstrument, Dance, PlayGame, PacingAround, CarryBigItem, Swim, GetOnShip, GetOffShip, CastMagic, Flee, Escort, Attack, SneakFollow, SetAmbush, SeekSomeone, SeekPlace, Kill, Capture,
    Assign, Deassign, GiveOrder, GiveJudgement, ArrangeFeast, ArrangeVisitors, ToTour, Rebel, SupressRebels, HuntRaiders, ArrangeTax, GiveTribute, AskTribute, PlanFor, Suspect, ArrangeLaws, DeclareWar, AskForPeace, MakePeace, ArrangeArmyPlans,
    AskForAlliance, AllyAnother, BreakAlliance
}*/

#region Memory

public class EventMemory
{
    public Event _Event;
    public AboutEvent _AboutEvent;
    public byte _MemoryLevel;
    public byte _ImportanceLevel;
}

public class Event
{
    public uint _When;
    public TypeOfEvent _TypeOfEvent;
    public ushort _Where;
    public ushort _Who;
    public ushort _ToWhom;
}

public struct AboutEvent
{
    public uint _HeardTime;
    public ushort _HeardAt;
    public ushort _HeardFrom;
    public byte _Belief;
}

public enum TypeOfEvent : ushort
{

}
public enum KnowledgeInformation : ushort
{

}

#endregion