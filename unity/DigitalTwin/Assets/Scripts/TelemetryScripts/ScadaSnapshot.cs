using System;
using UnityEngine;

[System.Serializable]
public class ScadaSnapshot
{
    public string timestamp;
    public Conveyor conveyor;
    public Bearings bearings;
}

[System.Serializable]
public class Conveyor
{
    public float commandedSpeed;
    public float conveyorLoad;
    public bool isMotorRunning;
    public float motorCurrent;

}

[System.Serializable]
public class Bearings
{
    public Bearing roller_1_bearing_1;
    public Bearing roller_1_bearing_2;
    public Bearing roller_2_bearing_1;
    public Bearing roller_2_bearing_2;

    public Bearing GetBearingById(string id)
    {
        if(roller_1_bearing_1!=null && roller_1_bearing_1.bearingId==id) return roller_1_bearing_1;
        if(roller_1_bearing_2!=null && roller_1_bearing_2.bearingId==id) return roller_1_bearing_2;
        if(roller_2_bearing_1!=null && roller_2_bearing_1.bearingId==id) return roller_2_bearing_1;
        if(roller_2_bearing_2!=null && roller_2_bearing_2.bearingId==id) return roller_2_bearing_2;
        return null;
    }
}

[Serializable]
public class Bearing
{
    public string bearingId;
    public float temperature;
    public float vibration;
}