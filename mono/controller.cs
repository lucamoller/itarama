using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public static class Controller {
    
    static bool shouldSendTurbo = false;
    static double throttleToSend = 1.0;

    static bool updateMaxSlideAngle = false;
   
    public static bool runSearchNextBestThrotle = false;

    public static Car closestFront;
    public static Car closestBack;
    public static Car closestFrontAnyLane;
    public static Car closestBackAnyLane;
    public static int countFrontSameLane;
    public static int countFrontOtherLane;
    public static int countBackSameLane;
    public static int countBackOtherLane;

    public static void Update() {
        runSearchNextBestThrotle = true;

        Setup();

        CheckModels();

        UpdateCarsNearby();

        SwitchController.Update();

        throttleToSend = ThrottleController.CalculateThrottle(ref shouldSendTurbo);

        if (Flags.logt) 
        {
            Console.WriteLine("throttleToSend {0}, gametick {1}, angle {2}, ds {3}, piece {4} ({5}, {6}), inPiece {7}, pieceLength{8}, maxAngle {9}", throttleToSend, Model.gameTick,
                Model.you.states[Model.gameTick].a, Model.you.states[Model.gameTick].ds,
                Model.you.states[Model.gameTick].pieceIndex, Model.you.states[Model.gameTick].startLaneIndex, Model.you.states[Model.gameTick].endLaneIndex,
                Model.you.states[Model.gameTick].inPieceDistance, Model.GetLanePieceTotalLength(ref Model.you.states[Model.gameTick]), AngleModel.max_slide_angle_global);

            //Console.WriteLine("turboFactor {0}, turboTicksRemaning {1}, gameTick {2}", Model.you.states[Model.gameTick].turboFactor, Model.you.states[Model.gameTick].turboTicksRemaining, Model.gameTick);
        }

        SwitchController.DecideIfShouldSwitch(ref throttleToSend);

        if (!SwitchController.shouldSendSwitch) {
            shouldSendTurbo = CalculateTurbo();
        }
    }

    public static void Setup() {
        if (Flags.maxangle) {
            AngleModel.max_slide_angle_global = Flags.maxanglevalue;
        }

        if ((Model.qualify || Flags.skipqualify) && Model.gameTick == 0) {
            AngleModel.UpdateSimulationStopConstants();
            TrackModel.FindBestLanes();
        }
        if (Model.gameTick == 0) {
            SwitchController.consideringSendSwitch = false;
            SwitchController.forceSendSwitchNextTime = false;
            SwitchController.shouldSendSwitch = false;
            SwitchController.waitingForSwitchToHappen = 0;
            shouldSendTurbo = false;
            throttleToSend = 1.0;
            TrackModel.activePiece = TrackModel.FindNextSwitch(Model.you.currentPieceIndex);
            TrackModel.activeLane = TrackModel.bestLanes[TrackModel.activePiece];
        }
    }

    public static void CheckModels() {
        if (Model.gameTick > 2) {
            if (SpeedModel.isEstimated && !SpeedModel.CheckModel() && Model.you.isActive) {
                Console.WriteLine("SpeedModel check failed! observed: ds: " + Model.you.states[Model.gameTick].ds
                                  + ", expected: ds: " + SpeedModel.GetNextDs(ref Model.you.states[Model.gameTick - 1], true)
                                  + ", invRadius: " + Model.GetLanePieceInvRadius(ref Model.you.states[Model.gameTick - 1])
                                  + ", gameTick: " + Model.gameTick);
            }

            if (!Model.you.states[Model.gameTick].isActive && Model.you.gameTickCrashed == Model.gameTick) {
                double expeted_a = Math.Abs(Model.you.states[Model.gameTick -1].a + AngleModel.GetNextDa(ref Model.you.states[Model.gameTick -1], true));
                if (expeted_a < AngleModel.after_estimation_max_slide_angle) {
                    Console.WriteLine("CRASHED expecting angle {0}, smaller than max {1}", expeted_a, AngleModel.after_estimation_max_slide_angle);
                    updateMaxSlideAngle = true;
                }
            }
            if (updateMaxSlideAngle) {
                AngleModel.max_slide_angle_global = AngleModel.maxAngleSeen;
            }

            if (AngleModel.isEstimated && !AngleModel.CheckModel() && Model.you.isActive) {
                Console.WriteLine("AngleModel check failed! observed: da: " + Model.you.states[Model.gameTick].da
                                  + ", expected: da: " + AngleModel.GetNextDa(ref Model.you.states[Model.gameTick - 1], true)
                                  + ", invRadius: " + Model.GetLanePieceInvRadius(ref Model.you.states[Model.gameTick - 1])
                                  + ", gameTick: " + Model.gameTick);
                Console.WriteLine(Model.you.states[Model.gameTick].da + ";" + AngleModel.GetNextDa(ref Model.you.states[Model.gameTick - 1], true) + ";" + Model.you.states[Model.gameTick - 1].da + ";" + Model.you.states[Model.gameTick - 1].ds + ";" + Model.you.states[Model.gameTick - 1].a + ";" + Model.GetLanePieceInvRadius(ref Model.you.states[Model.gameTick - 1]) + ";" + Model.you.states[Model.gameTick - 1].pieceIndex + ";" + Model.you.states[Model.gameTick - 1].startLaneIndex + ";" + Model.you.states[Model.gameTick - 1].endLaneIndex + ";" + Model.you.states[Model.gameTick - 1].inPieceDistance);
            }
        }
    }

    public static void UpdateCarsNearby() {
        bool hasSwitchNearby = CollisionSimulator.HasSwitchNearby();
        closestFront = null;
        closestBack = null;
        closestFrontAnyLane = null;
        closestBackAnyLane = null;
        countFrontSameLane = 0;
        countFrontOtherLane = 0;
        countBackSameLane = 0;
        countBackOtherLane = 0;

        CollisionSimulator.FindInRange(15, true, hasSwitchNearby, ref closestFront, ref closestFrontAnyLane, ref countFrontSameLane, ref countFrontOtherLane);
        CollisionSimulator.FindInRange(25, false, hasSwitchNearby, ref closestBack, ref closestBackAnyLane, ref countBackSameLane, ref countBackOtherLane);

        if (Flags.logc) Console.WriteLine("  countFrontSameLane {0},  countFrontOtherLane {1}, countBackSameLane {2}, countBackOtherLane {3}",
                                    countFrontSameLane, countFrontOtherLane, countBackSameLane, countBackOtherLane);
    }
    
    public static bool CalculateTurbo() {
        if (Flags.noturbo) {
            return false;
        }


        if (Model.you.HasTurboAvailable() && !Model.you.isTurboActive
            && Model.you.lastGametickPassedHere[TurboSimulator.bestPieceForTurbo].Count > 0 
            && Model.you.lastGametickPassedHere[TurboSimulator.bestPieceForTurbo].Last() + 30 > Model.gameTick) 
        {
            State newState = new State();
            bool crashed = false;
            Simulator.SimulateSingleNextState(Model.you.GetLastThrottle(), ref Model.you.states[Model.gameTick], ref newState, true, true, ref crashed, AngleModel.max_slide_angle_global);
            newState.turboTicksRemaining = Model.turboDuration + 1;
            newState.turboFactor = Model.turboFactor;
            if (SurvivalSimulator.CanSurviveUsing(newState, 1.0, 5, true, true, true, null, AngleModel.max_slide_angle_global)) {
                ThrottleController.tickMemoryFoundUsed = -1000;

                if (Flags.logturbo) {
                    Console.WriteLine("**************************** Decided to use turbo on piece {0} (bestPiece was {1})", Model.you.states[Model.gameTick].pieceIndex, TurboSimulator.bestPieceForTurbo);
                }

                return true;
            }
        }
        return false;
    }

    public static double GetThrottle() {
        double value = throttleToSend;

        if (value > 1.0) value = 1.0;
        if (value < 0.0) value = 0.0;
        Model.you.states[Model.gameTick].throttle = value;

        return value;
    }

    public static int GetSwitch() {
        if (!SwitchController.shouldSendSwitch || !SpeedModel.isEstimated) return 0;

        Model.you.RepeatThrottle();
        SwitchController.shouldSendSwitch = false;
        SwitchController.waitingForSwitchToHappen = SwitchController.switchToSend;
        SwitchController.sentSwitchMsgOnTick = Model.gameTick;
        SwitchController.switchToSend = 0;
        return SwitchController.waitingForSwitchToHappen;
    }

    public static bool GetTurbo() {
        if (!shouldSendTurbo || !SpeedModel.isEstimated) return false;

        Model.you.RepeatThrottle();
        shouldSendTurbo = false;
        Model.you.UseTurbo();
        return true;
    }
}
