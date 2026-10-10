PROGRAM main
  VAR
    StartCommand : BOOL AT %MX0.0 := FALSE; (* Start command from external controller *)
    StopCommand : BOOL AT %MX0.1 := FALSE; (* Stop command from external controller *)
    ForwardCommand : BOOL AT %MX0.2 := FALSE; (* Command requesting forward conveyor movement *)
    ReverseCommand : BOOL AT %MX0.3 := FALSE; (* Command requesting reverse conveyor movement *)
    EntrySensor : BOOL AT %MX0.4 := FALSE; (* Entry sensor detects a box entering the conveyor *)
    ExitSensor : BOOL AT %MX0.5 := FALSE; (* Exit sensor detects a box leaving the conveyor *)
    MotorRun : BOOL AT %MX0.6 := FALSE; (* PLC-controlled motor run state *)
    MotorDirection : INT AT %MW0 := 0; (* PLC motor direction: 1=Forward, -1=Reverse, 0=Stopped *)
    BoxCount : INT AT %MW1 := 0; (* Number of boxes counted by the PLC from EntrySensor rising edges *)
    PreviousEntrySensor : BOOL := FALSE; (* Internal memory of EntrySensor from the previous PLC scan *)
  END_VAR

(* =========================================================
       MOTOR CONTROL
       ========================================================= *)

    (* Stop has highest priority *)
    IF StopCommand THEN
        MotorRun := FALSE;

    (* Start only if exactly one direction is selected *)
    (*ELSIF StartCommand AND (ForwardCommand XOR ReverseCommand) THEN
        MotorRun := TRUE;
    END_IF;*)

    ELSIF StartCommand THEN
        MotorRun := TRUE;
    END_IF;

    (* =========================================================
       MOTOR DIRECTION
       
       1  = Forward
       -1 = Reverse
       0  = No direction
       ========================================================= *)

    (* Conflicting direction commands *)
    (*IF ForwardCommand AND ReverseCommand THEN
        MotorRun := FALSE;
        MotorDirection := 0;*)

    (*ELSIF ForwardCommand THEN
        MotorDirection := 1;

    ELSIF ReverseCommand THEN
        MotorDirection := -1;
    END_IF;*)


    (* =========================================================
       BOX COUNTING
       
       Count only when EntrySensor changes:
       FALSE -> TRUE
       ========================================================= *)

    IF EntrySensor AND NOT PreviousEntrySensor THEN
        BoxCount := BoxCount + 1;
    END_IF;


    (* Remember EntrySensor state for the next PLC scan *)
    PreviousEntrySensor := EntrySensor;

END_PROGRAM