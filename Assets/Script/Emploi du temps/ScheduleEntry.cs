using System;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class ScheduleEntry
{
    public int day;
    public DayTime timeOfDay;
    public string activityName;
    public string description;
    public Sprite icon;
    public ScheduleActivityType activityType;

    [Header("Effets planifiés")]
    public EffectSO effectToApply;

    public ScheduleEntry(int day, DayTime time, string name, ScheduleActivityType type)
    {
        this.day = day;
        this.timeOfDay = time;
        this.activityName = name;
        this.activityType = type;
    }
}

public enum ScheduleActivityType
{
    Village,
    MiniJeu,
    Relation,
    Event,
    Custom
}

