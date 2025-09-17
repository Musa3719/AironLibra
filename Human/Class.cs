using System.Collections.Generic;
using UnityEngine;

public abstract class Class
{
    public List<Job> _Jobs { get; protected set; }
    public Job _ActiveJob { get; protected set; }
    public Class()
    {
        _Jobs = new List<Job>();
    }
    public void SelectActiveJob()
    {
        if (_Jobs == null || _Jobs.Count == 0) return;
        foreach (var job in _Jobs)
        {
            bool condition = true;
            if (condition)
            {
                if (_ActiveJob != null)
                    _ActiveJob.StopJob();
                _ActiveJob = job;
                _ActiveJob.StartJob();
            }
        }
    }
    public void GainJob(Job job)
    {
        _Jobs.Add(job);
    }
    public void LoseJob(Job job)
    {
        if (!_Jobs.Contains(job)) return;

        _Jobs.Remove(job);
        if (_ActiveJob == job)
            SelectActiveJob();
    }
}
public class Peasant : Class
{
    public Peasant() : base()
    {
        _Jobs.Add(new FarmerJob());
        _Jobs.Add(new LivestockJob());
        _Jobs.Add(new HuntingJob());
        _Jobs.Add(new FishingJob());
        _Jobs.Add(new TimberJob());
        _Jobs.Add(new MinerJob());
        _Jobs.Add(new CarpenterJob());
        _Jobs.Add(new TailorJob());
        _Jobs.Add(new BlacksmithJob());
        _Jobs.Add(new ShopkeepingJob());
    }
}
public class Lord : Class
{
    public Lord() : base()
    {
        _Jobs.Add(new RuleJob());
    }
}
public class King : Class
{
    public King() : base()
    {
        _Jobs.Add(new RuleJob());
    }
}
public class Priest : Class
{
    public Priest() : base()
    {
        _Jobs.Add(new PreachJob());
        _Jobs.Add(new WorshipJob());
    }
}
public class Soldier : Class
{
    public Soldier() : base()
    {
        _Jobs.Add(new PatrollingJob());
        _Jobs.Add(new InquiryJob());
        _Jobs.Add(new AttackJob());
    }
}
public class Artist : Class
{
    public Artist() : base()
    {
        _Jobs.Add(new BardJob());
        _Jobs.Add(new PainterJob());
        _Jobs.Add(new WriterJob());
    }
}
public class Merchant : Class
{
    public Merchant() : base()
    {
        _Jobs.Add(new MerchantJob());
        _Jobs.Add(new ShopkeepingJob());
    }
}
public class Physician : Class
{
    public Physician() : base()
    {
        _Jobs.Add(new MedicalJob());
    }
}


public abstract class Job
{
    public WeeklyRoutine _WorkRoutine;
    public string _Name { get; protected set; }
    public float _Experience { get; protected set; }
    public abstract void SetWeeklyRoutine();
    public abstract void StartJob();
    public abstract void DoJob();
    public abstract void StopJob();
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

public class FarmerJob : Job
{
    public override void SetWeeklyRoutine()
    {

    }

    public override void StartJob()
    {

    }
    public override void DoJob()
    {

    }
    public override void StopJob()
    {

    }
}
public class ForagerJob : Job
{
    public override void SetWeeklyRoutine()
    {

    }

    public override void StartJob()
    {

    }
    public override void DoJob()
    {

    }
    public override void StopJob()
    {

    }
}
public class LivestockJob : Job
{
    public override void SetWeeklyRoutine()
    {

    }

    public override void StartJob()
    {

    }
    public override void DoJob()
    {

    }
    public override void StopJob()
    {

    }
}
public class HuntingJob : Job
{
    public override void SetWeeklyRoutine()
    {

    }

    public override void StartJob()
    {

    }
    public override void DoJob()
    {

    }
    public override void StopJob()
    {

    }
}
public class FishingJob : Job
{
    public override void SetWeeklyRoutine()
    {

    }

    public override void StartJob()
    {

    }
    public override void DoJob()
    {

    }
    public override void StopJob()
    {

    }
}
public class TimberJob : Job
{
    public override void SetWeeklyRoutine()
    {

    }

    public override void StartJob()
    {

    }
    public override void DoJob()
    {

    }
    public override void StopJob()
    {

    }
}
public class MinerJob : Job
{
    public override void SetWeeklyRoutine()
    {

    }

    public override void StartJob()
    {

    }
    public override void DoJob()
    {

    }
    public override void StopJob()
    {

    }
}
public class CarpenterJob : Job
{
    public override void SetWeeklyRoutine()
    {

    }

    public override void StartJob()
    {

    }
    public override void DoJob()
    {

    }
    public override void StopJob()
    {

    }
}
public class TailorJob : Job
{
    public override void SetWeeklyRoutine()
    {

    }

    public override void StartJob()
    {

    }
    public override void DoJob()
    {

    }
    public override void StopJob()
    {

    }
}
public class BlacksmithJob : Job
{
    public override void SetWeeklyRoutine()
    {

    }

    public override void StartJob()
    {

    }
    public override void DoJob()
    {

    }
    public override void StopJob()
    {

    }
}
public class ShopkeepingJob : Job
{
    public override void SetWeeklyRoutine()
    {

    }

    public override void StartJob()
    {

    }
    public override void DoJob()
    {

    }
    public override void StopJob()
    {

    }
}
public class PatrollingJob : Job
{
    public override void SetWeeklyRoutine()
    {

    }

    public override void StartJob()
    {

    }
    public override void DoJob()
    {

    }
    public override void StopJob()
    {

    }
}
public class InquiryJob : Job//murder etc
{
    public override void SetWeeklyRoutine()
    {

    }

    public override void StartJob()
    {

    }
    public override void DoJob()
    {

    }
    public override void StopJob()
    {

    }
}
public class AttackJob : Job//for battles and small ambushes etc
{
    public override void SetWeeklyRoutine()
    {

    }

    public override void StartJob()
    {

    }
    public override void DoJob()
    {

    }
    public override void StopJob()
    {

    }
}
public class PreachJob : Job
{
    public override void SetWeeklyRoutine()
    {

    }

    public override void StartJob()
    {

    }
    public override void DoJob()
    {

    }
    public override void StopJob()
    {

    }
}
public class WorshipJob : Job
{
    public override void SetWeeklyRoutine()
    {

    }

    public override void StartJob()
    {

    }
    public override void DoJob()
    {

    }
    public override void StopJob()
    {

    }
}
public class MerchantJob : Job
{
    public override void SetWeeklyRoutine()
    {

    }

    public override void StartJob()
    {

    }
    public override void DoJob()
    {

    }
    public override void StopJob()
    {

    }
}
public class MedicalJob : Job
{
    public override void SetWeeklyRoutine()
    {

    }

    public override void StartJob()
    {

    }
    public override void DoJob()
    {

    }
    public override void StopJob()
    {

    }
}
public class TeachJob : Job
{
    public override void SetWeeklyRoutine()
    {

    }

    public override void StartJob()
    {

    }
    public override void DoJob()
    {

    }
    public override void StopJob()
    {

    }
}
public class TrainJob : Job
{
    public override void SetWeeklyRoutine()
    {

    }

    public override void StartJob()
    {

    }
    public override void DoJob()
    {

    }
    public override void StopJob()
    {

    }
}
public class BardJob : Job
{
    public override void SetWeeklyRoutine()
    {

    }

    public override void StartJob()
    {

    }
    public override void DoJob()
    {

    }
    public override void StopJob()
    {

    }
}
public class PainterJob : Job
{
    public override void SetWeeklyRoutine()
    {

    }

    public override void StartJob()
    {

    }
    public override void DoJob()
    {

    }
    public override void StopJob()
    {

    }
}
public class WriterJob : Job
{
    public override void SetWeeklyRoutine()
    {

    }

    public override void StartJob()
    {

    }
    public override void DoJob()
    {

    }
    public override void StopJob()
    {

    }
}
public class MessengerJob : Job
{
    public override void SetWeeklyRoutine()
    {

    }

    public override void StartJob()
    {

    }
    public override void DoJob()
    {

    }
    public override void StopJob()
    {

    }
}
public class RuleJob : Job
{
    public override void SetWeeklyRoutine()
    {

    }

    public override void StartJob()
    {

    }
    public override void DoJob()
    {

    }
    public override void StopJob()
    {

    }
}