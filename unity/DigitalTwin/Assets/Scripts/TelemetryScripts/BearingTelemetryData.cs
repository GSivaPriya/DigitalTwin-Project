
using System;

[Serializable]
public class BearingTelemetryData
{
    public string conveyorId;
    public float loadKg;
    public float speedMps;
    public float motorCurrentA;

    public RollerTelemetryData[] rollers;
}

[Serializable]
public class RollerTelemetryData
{
    public string rollerId;
    public BearingTelemetryItem[] bearings;
}

[Serializable]
public class BearingTelemetryItem
{
    public string bearingId;
    public float temperatureC;
    public float vibrationMmS;
}
