using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class SkillSystem
{
    public Skill.Attribute Strength;
    public Skill.Attribute Dexterity;
    public Skill.Attribute Analytic;
    public Skill.Attribute Social;


    public Skill.Knowledge Mechanic;
    public Skill.Knowledge Tech;
    public Skill.Knowledge Medicine;
    public Skill.Knowledge Alchemy;
    public Skill.Knowledge Tactics;

    public Skill.Knowledge HistoryLaw;
    public Skill.Knowledge Religion;
    public Skill.Knowledge Economics;
    public Skill.Knowledge Psychology;
    public Skill.Knowledge Literature;


    //Combat
    public Skill.Ability MeleeFighting;
    public Skill.Ability Pistols;
    public Skill.Ability SMGs;
    public Skill.Ability Shotguns;
    public Skill.Ability Rifles;
    public Skill.Ability AutoRifles;
    public Skill.Ability SniperRifles;
    public Skill.Ability HeavyMachineGun;

    //Driving
    public Skill.Ability Motorcycles;
    public Skill.Ability Automobiles;
    public Skill.Ability Trucks;
    public Skill.Ability ArmoredCars;
    public Skill.Ability Helicopters;
    public Skill.Ability Planes;
    public Skill.Ability Ships;

    //Art, Sports and School
    public Skill.Ability Scout;
    public Skill.Ability Swimming;
    public Skill.Ability Music;
    public Skill.Ability Painting;
    public Skill.Ability Poetry;
    public Skill.Ability Literacy;
    public Skill.Ability Teaching;

    //Villager
    public Skill.Ability Farmer;
    public Skill.Ability Fishing;
    public Skill.Ability Miner;
    public Skill.Ability Woodcutting;
    public Skill.Ability Tailor;
    public Skill.Ability Smith;
    public Skill.Ability Carpenter;
    public Skill.Ability Cook;

    public abstract class Skill
    {
        public Humanoid Human;

        public class Attribute : Skill
        {
            public void LevelChanged()
            {
                Human.LevelChanged();
            }
        }
        public class Knowledge : Skill
        {
            public void LevelChanged()
            {
                Human.LevelChanged();
            }
        }
        public class Ability : Skill
        {
            public void LevelChanged()
            {
                Human.LevelChanged();
            }
        }
    }

}
