PROGRAM main
  VAR
    StartCommand : BOOL AT %MX0.0 := FALSE; (* Start command from external controller *)
    StopCommand : BOOL AT %MX0.1 := FALSE; (* Stop command from external controller *)
    EntrySensor : BOOL AT %MX0.4 := FALSE; (* Entry sensor detects a box entering the conveyor *)
    ExitSensor : BOOL AT %MX0.5 := FALSE; (* Exit sensor detects a box leaving the conveyor *)
    MotorRun : BOOL AT %MX0.6 := FALSE; (* PLC-controlled motor run state *)
    BoxCount : INT AT %MW1 := 0; (* Number of boxes counted by the PLC from EntrySensor rising edges *)
    PreviousEntrySensor : BOOL := FALSE; (* Internal memory of EntrySensor from the previous PLC scan *)
    BearingTemperature : ARRAY [1..4] OF REAL AT %MD0;
    BearingVibration : ARRAY [1..4] OF REAL AT %MD8;
    BearingTemperatureWarning : ARRAY [1..4] OF BOOL;
    BearingVibrationWarning : ARRAY [1..4] OF BOOL;
    BearingTrip : ARRAY [1..4] OF BOOL;
    AnyBearingTrip : BOOL := FALSE;
    ResetFault : BOOL := FALSE;
    MotorRunPermissive : BOOL := TRUE;
    i : INT := 0;
    CompletedBoxCount : DINT := 0;
    PreviousExitSensor : BOOL := FALSE;
    EntryTimeMs : ARRAY [1..100] OF ULINT;
    QueueHead : INT := 0;
    QueueTail : INT := 0;
    QueueCount : INT := 0;
    CycleTimeMs : ULINT := 0;
    CycleTimeValid : BOOL := 0;
    CycleTimeError : BOOL := 0;
    SpeedSetPoint : REAL AT %MD20 := 0.45;
    CommandedSpeed : REAL AT %MD22 := 0;
    MaxAllowedSpeed : REAL := 1.33;
    SpeedRequestValid : BOOL := TRUE;
    ResetAccepted : BOOL := FALSE;
    MotorCurrent : REAL AT %MD32;
    ConveyorLoad : REAL AT %MD36;
  END_VAR

SpeedSetPoint := 0.45;

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

    (* Reset only when both readings are below trip thresholds *)
    IF ResetFault AND
       (BearingTemperature[i] < 85.0) AND
       (BearingVibration[i] < 7.1) THEN
        BearingTrip[i] := FALSE;
    END_IF;

    (* Aggregate bearing trips *)
    IF BearingTrip[i] THEN
        AnyBearingTrip := TRUE;
    END_IF;

END_FOR;


(* =========================================================
   MOTOR RUN PERMISSIVE
   ========================================================= *)

MotorRunPermissive := NOT AnyBearingTrip;


(* =========================================================
   SPEED SETPOINT VALIDATION
   ========================================================= *)

SpeedRequestValid :=
    (SpeedSetpoint >= 0.0) AND
    (SpeedSetpoint <= MaxAllowedSpeed);

IF SpeedRequestValid THEN
    CommandedSpeed := SpeedSetpoint;
ELSE
    (* Invalid requests are rejected *)
    CommandedSpeed := 0.0;
END_IF;


(* =========================================================
   MOTOR CONTROL
   Stop and bearing trips take priority over Start
   ========================================================= *)

IF StopCommand THEN
    MotorRun := FALSE;

ELSIF NOT MotorRunPermissive THEN
    MotorRun := FALSE;

ELSIF StartCommand THEN
    MotorRun := TRUE;
END_IF;


(* Ensure speed is zero when the motor is not permitted to run *)
IF NOT MotorRun OR NOT MotorRunPermissive THEN
    CommandedSpeed := 0.0;
END_IF;


(* =========================================================
   RESET STATUS
   ========================================================= *)

ResetAccepted :=
    ResetFault AND NOT AnyBearingTrip;


(* =========================================================
   ENTRY BOX COUNTING
   Count on rising edge: FALSE -> TRUE
   ========================================================= *)

IF EntrySensor AND NOT PreviousEntrySensor THEN
    BoxCount := BoxCount + 1;
END_IF;

PreviousEntrySensor := EntrySensor;


(* =========================================================
   EXIT BOX COUNTING
   ========================================================= *)

IF ExitSensor AND NOT PreviousExitSensor THEN
    CompletedBoxCount := CompletedBoxCount + 1;
END_IF;

PreviousExitSensor := ExitSensor;

END_PROGRAM