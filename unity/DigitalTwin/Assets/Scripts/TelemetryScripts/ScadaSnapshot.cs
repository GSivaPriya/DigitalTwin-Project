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
}

[Serializable]
public class Bearing
{
    public float temperature;
    public float vibration;
}