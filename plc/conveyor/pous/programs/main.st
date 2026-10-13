PROGRAM main
  VAR
    StartCommand : BOOL AT %MX0.0 := FALSE; (* Start command from external controller *)
    StopCommand : BOOL AT %MX0.1 := FALSE; (* Stop command from external controller *)
    EntrySensor : BOOL AT %MX0.4 := FALSE; (* Entry sensor detects a box entering the conveyor *)
    ExitSensor : BOOL AT %MX0.5 := FALSE; (* Exit sensor detects a box leaving the conveyor *)
    MotorRun : BOOL AT %MX0.6 := FALSE; (* PLC-controlled motor run state *)
    BoxCount : INT AT %MW1 := 0; (* Number of boxes counted by the PLC from EntrySensor rising edges *)
    PreviousEntrySensor : BOOL := FALSE; (* Internal memory of EntrySensor from the previous PLC scan *)
    BearingHealth : ARRAY [1..4] OF REAL AT %MD0; (* Bearing health Array *)
    BearingLubrication : ARRAY [1..4] OF REAL AT %MD4;
    BearingTemperature : ARRAY [1..4] OF REAL AT %MD8;
    BearingVibration : ARRAY [1..4] OF REAL AT %MD12;
    BearingResistance : ARRAY [1..4] OF REAL AT %MD16;
    BearingTemperatureWarning : ARRAY [1..4] OF BOOL;
    BearingVibrationWarning : ARRAY [1..4] OF BOOL;
    BearingTrip : ARRAY [1..4] OF BOOL;
    AnyBearingTrip : BOOL := FALSE;
    ResetFault : BOOL := FALSE;
    MotorRunPermissive : BOOL := TRUE;
    i : INT := 0;
  END_VAR

(* =========================================================
   BEARING WARNING AND TRIP MONITORING
   ========================================================= *)

AnyBearingTrip := FALSE;

FOR i := 1 TO 4 DO

    (* Temperature warning: 70 degrees C *)
    BearingTemperatureWarning[i] :=
        BearingTemperature[i] >= 70.0;

    (* Vibration warning: 4.5 mm/s *)
    BearingVibrationWarning[i] :=
        BearingVibration[i] >= 4.5;

    (* Latch a trip if either limit is exceeded *)
    IF (BearingTemperature[i] >= 85.0) OR
       (BearingVibration[i] >= 7.1) THEN
        BearingTrip[i] := TRUE;
    END_IF;

    (* Reset trip only when both readings are below
       their trip thresholds *)
    IF ResetFault AND
       (BearingTemperature[i] < 85.0) AND
       (BearingVibration[i] < 7.1) THEN
        BearingTrip[i] := FALSE;
    END_IF;

    (* Aggregate the individual bearing trips *)
    IF BearingTrip[i] THEN
        AnyBearingTrip := TRUE;
    END_IF;

END_FOR;


(* =========================================================
   MOTOR RUN PERMISSIVE
   ========================================================= *)

MotorRunPermissive := NOT AnyBearingTrip;


(* =========================================================
   MOTOR CONTROL
   Stop has priority over Start
   ========================================================= *)

IF StopCommand THEN
    MotorRun := FALSE;

ELSIF NOT MotorRunPermissive THEN
    MotorRun := FALSE;

ELSIF StartCommand THEN
    MotorRun := TRUE;
END_IF;


(* =========================================================
   BOX COUNTING
   Count on EntrySensor rising edge
   FALSE -> TRUE
   ========================================================= *)

IF EntrySensor AND NOT PreviousEntrySensor THEN
    BoxCount := BoxCount + 1;
END_IF;

(* Remember entry sensor state for the next scan *)
PreviousEntrySensor := EntrySensor;

END_PROGRAM