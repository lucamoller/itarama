using System;
using System.Collections.Generic;
using System.Linq;

public static class SwitchController {
    public static bool consideringSendSwitch = false;
    public static bool forceSendSwitchNextTime = false;
    public static bool shouldSendSwitch = false;
    public static int switchToSend;
    public static int waitingForSwitchToHappen = 0;
    public static int sentSwitchMsgOnTick = -1000;

    static List<Car> mightNeedToOvertake;
    static int yourTimeToNextSwitch = 0;

    public static void Update() {

        if (TrackModel.activePiece != TrackModel.FindNextSwitch(Model.you.currentPieceIndex)) {
            TrackModel.activePiece = TrackModel.FindNextSwitch(Model.you.currentPieceIndex);
            TrackModel.activeLane = TrackModel.bestLanes[TrackModel.activePiece];
        }

        if (waitingForSwitchToHappen == 0) {
            int activeLaneAnterior = TrackModel.activeLane;

            consideringSendSwitch = CalculateSwitch();
            if (consideringSendSwitch) {
                TrackModel.activeLane = Model.you.states[Model.you.lastGameTick].endLaneIndex + switchToSend;
            }
            else {
                TrackModel.activeLane = Model.you.states[Model.you.lastGameTick].endLaneIndex;
            }

            if (Flags.logswitch) {
                Console.WriteLine("consideringSendSwitch: {0}, switchToSend {1}, piece {2}({3}, {4}), gameTick {5}", consideringSendSwitch, switchToSend, Model.you.states[Model.gameTick].pieceIndex, Model.you.states[Model.gameTick].startLaneIndex, Model.you.states[Model.gameTick].endLaneIndex, Model.gameTick);
            }

            if (TrackModel.activeLane != activeLaneAnterior) {
                bool achou1 = SurvivalSimulator.CanSurviveUsing(Model.you.states[Model.gameTick], 1.0, 1, true, true, true, null, AngleModel.max_slide_angle_global);
                bool achou0 = SurvivalSimulator.CanSurviveUsing(Model.you.states[Model.gameTick], 0.0, 1, true, true, true, null, AngleModel.max_slide_angle_global);

                if (!achou0 && !achou1) {
                    if (activeLaneAnterior > Model.you.states[Model.you.lastGameTick].endLaneIndex) {
                        switchToSend = 1;
                        consideringSendSwitch = true;
                    }
                    else if (activeLaneAnterior < Model.you.states[Model.you.lastGameTick].endLaneIndex) {
                        switchToSend = -1;
                        consideringSendSwitch = true;
                    }
                    else {
                        switchToSend = 0;
                        consideringSendSwitch = false;
                    }
                    TrackModel.activeLane = activeLaneAnterior;
                }
            }
        }
    }

    public static void DecideIfShouldSwitch(ref double throttleToSend) {
        if (IsInPieceBeforeSwitch()) {
            if (consideringSendSwitch) {
                //Console.WriteLine("Trying to send switch");
                if (throttleToSend == Model.you.GetLastThrottle() || forceSendSwitchNextTime) {
                    //Console.WriteLine("Sending switch, shouldOvertake {0}, waitingForSwitchToHappen {1}, pieceIndex {2}", shouldOvertake, waitingForSwitchToHappen, Model.you.currentPieceIndex);
                    shouldSendSwitch = true;
                    forceSendSwitchNextTime = false;
                    consideringSendSwitch = false;
                    ThrottleController.tickMemoryFoundUsed = -1000;
                }
                else {
                    if (SurvivalSimulator.CanSurviveUsing(Model.you.states[Model.gameTick], throttleToSend, 2, true, true, true, null, AngleModel.max_slide_angle_global)) {
                        //Console.WriteLine("Trying to send switch, will force switch");
                        forceSendSwitchNextTime = true;
                    }
                    else if (SurvivalSimulator.CanSurviveUsing(Model.you.states[Model.gameTick], Model.you.GetLastThrottle(), 1, true, true, true, null, AngleModel.max_slide_angle_global)) {
                        shouldSendSwitch = true;
                        forceSendSwitchNextTime = false;
                        consideringSendSwitch = false;
                        ThrottleController.tickMemoryFoundUsed = -1000;
                    }
                    else if (SurvivalSimulator.CanSurviveUsing(Model.you.states[Model.gameTick], 1.0 - throttleToSend, 2, true, true, true, null, AngleModel.max_slide_angle_global)) {
                        //Console.WriteLine("Trying to send switch, will force switch");
                        throttleToSend = 1.0 - throttleToSend;
                        forceSendSwitchNextTime = true;
                        ThrottleController.tickMemoryFoundUsed = -1000;
                    }
                }
            }
            else {
                forceSendSwitchNextTime = false;
            }
        }
    }

    public static bool CalculateSwitch() {
        if (Flags.noswitch) {
            return false;
        }
        if (Flags.lane1) {
            if (Model.you.states[Model.gameTick].endLaneIndex == 0) {
                switchToSend = 1;
                return true;
            }
            return false;
        }
        if (Flags.lane0) {
            if (Model.you.states[Model.gameTick].endLaneIndex == 1) {
                switchToSend = -1;
                return true;
            }
            return false;
        }

        int emergencyStartSwitch = 0;
        bool shouldEscapeEmergency = CalculateEmergencyStartEscape(ref emergencyStartSwitch);
        if (shouldEscapeEmergency) {
            if (emergencyStartSwitch == 0) {
                return false;
            }
            else {
                switchToSend = emergencyStartSwitch;
                return true;
            }
        }

        int overtakeSwitch = 0;
        bool shouldOvertake = CalculateOvertake(ref overtakeSwitch);
        if (shouldOvertake) {
            //Console.WriteLine("Apply overtake!");
            switchToSend = overtakeSwitch;
            return true;
        }

        int escapeSwitch = 0;
        bool shouldEscape = CalculateEscape(ref escapeSwitch);
        if (shouldEscape) {
            if (escapeSwitch == 0) {
                return false;
            }
            else {
                switchToSend = escapeSwitch;
                return true;
            }
        }

        if (TrackModel.foundBestLanes) {
            int nextBestSwitch = TrackModel.FindNextBestLane(Model.you.states[Model.gameTick].pieceIndex, Model.you.states[Model.gameTick].endLaneIndex);
            if (nextBestSwitch != 0) {
                if (!CheckGoodLaneToGo(Model.you.states[Model.gameTick].endLaneIndex + nextBestSwitch, mightNeedToOvertake, yourTimeToNextSwitch - 3)) {
                    //Console.WriteLine("Bad lane to go, from {0} to {1}, gametick: {2}", Model.you.states[Model.gameTick].endLaneIndex, Model.you.states[Model.gameTick].endLaneIndex + result, Model.gameTick);
                    return false;
                }
                switchToSend = nextBestSwitch;
                return true;
            }
        }

        return false;
    }

    public static bool IsInPieceBeforeSwitch() {
        int nextSwitch = TrackModel.FindNextSwitch(Model.you.currentPieceIndex);
        return ((Model.you.currentPieceIndex + 1) % Model.trackPieces.Length == nextSwitch);
    }

    public static bool CalculateOvertake(ref int overtakeSwitch) {
        mightNeedToOvertake = new List<Car>();
        overtakeSwitch = 0;

        int nextSwitch1 = TrackModel.FindNextSwitch(Model.you.currentPieceIndex);
        int nextSwitch2 = TrackModel.FindNextSwitch(nextSwitch1);

        if (nextSwitch1 == -1) return false; // pista sem switch ou vc ta nele
        if (nextSwitch2 == -1 && Model.trackPieces[Model.you.currentPieceIndex].hasSwitch) return false; // pista com 2 switchs, espera pra decidir

        if (nextSwitch2 == -1) { //pista com um switch soh
            // caguei :P
            return false;
        }
        else {
            foreach (Car car in Model.others.Values) {
                if (car.HasFinished()) continue;
                if (nextSwitch2 < Model.you.currentPieceIndex) {
                    if (car.currentPieceIndex > Model.you.currentPieceIndex || car.currentPieceIndex < nextSwitch2 ||
                        (car.currentPieceIndex == Model.you.currentPieceIndex &&
                         car.states[Model.gameTick].inPieceDistance > Model.you.states[Model.gameTick].inPieceDistance
                        )
                       ) {
                        mightNeedToOvertake.Add(car);
                    }
                }
                else {
                    if ((car.currentPieceIndex > Model.you.currentPieceIndex && car.currentPieceIndex < nextSwitch2) ||
                        (car.currentPieceIndex == Model.you.currentPieceIndex &&
                         car.states[Model.gameTick].inPieceDistance > Model.you.states[Model.gameTick].inPieceDistance
                        )
                       ) {
                        mightNeedToOvertake.Add(car);
                    }
                }
            }
        }

        yourTimeToNextSwitch = Model.you.EstimateNextGameTickInPiece(nextSwitch2, Model.you.currentPieceIndex, false);
        int yourTimeToNextSwitchBest = Model.you.EstimateNextGameTickInPiece(nextSwitch2, Model.you.currentPieceIndex, true);
        if (Model.you.isTurboActive || (Model.you.iSturboAvailable && Model.IsPieceBetween(TurboSimulator.bestPieceForTurbo, Model.you.currentPieceIndex, nextSwitch2))) {
            if (Flags.debugovertake && IsInPieceBeforeSwitch()) Console.WriteLine("yourTimeToNextSwitch {0}, yourTimeToNextSwitchBest {1}", yourTimeToNextSwitch, yourTimeToNextSwitchBest);
            yourTimeToNextSwitch = yourTimeToNextSwitchBest;// -10;
        }

        Car bestToOvertake = null;
        int longestTimeToNextSwitch = 0;
        foreach (Car car in mightNeedToOvertake) {
            car.tmpTimeToNextSwitchCalculated = car.EstimateNextGameTickInPiece(nextSwitch2, Model.you.currentPieceIndex, car.isTurboActive || car.HasTurboAvailable());
            if (Flags.debugovertake && IsInPieceBeforeSwitch()) Console.WriteLine("mightNeedToOvertake: {0}, tmpTimeToNextSwitchCalculated {1}, isActive {2}, hisPiece: {3}, hisDist: {4}, yourPiece: {5}, yourDist: {6}, gametick: {7}", car.carData.color, car.tmpTimeToNextSwitchCalculated, car.isActive, car.currentPieceIndex, car.states[Model.gameTick].inPieceDistance, Model.you.currentPieceIndex, Model.you.states[Model.gameTick].inPieceDistance, Model.gameTick);

            if (car.isActive == false) {
                if (Flags.debugovertake && IsInPieceBeforeSwitch()) Console.WriteLine("yourTimeToNextSwitch + 10: {0}, ExpectedTicksToSpawn: {1}", yourTimeToNextSwitch + 10, car.ExpectedTicksToSpawn());
                if (yourTimeToNextSwitch + 10 < car.ExpectedTicksToSpawn()) continue;
            }

            if (car.tmpTimeToNextSwitchCalculated > longestTimeToNextSwitch &&
                car.states[Model.gameTick].endLaneIndex == Model.you.states[Model.gameTick].endLaneIndex) {
                longestTimeToNextSwitch = car.tmpTimeToNextSwitchCalculated;
                bestToOvertake = car;
            }
        }

        if (bestToOvertake == null) return false;
        if (Flags.debugovertake && IsInPieceBeforeSwitch()) Console.WriteLine("bestToOvertake: {0}, longestTimeToNextSwitch {1}", bestToOvertake.carData.color, longestTimeToNextSwitch);



        if (Flags.debugovertake && IsInPieceBeforeSwitch()) Console.WriteLine("yourTimeToNextSwitch {0}, gameTick {1}", yourTimeToNextSwitch, Model.gameTick);
        if (longestTimeToNextSwitch > yourTimeToNextSwitch) {
            int rightLane = Model.you.states[Model.gameTick].endLaneIndex + 1;
            bool laneRightGood = CheckGoodLaneToGo(rightLane, mightNeedToOvertake, longestTimeToNextSwitch);

            int leftLane = Model.you.states[Model.gameTick].endLaneIndex - 1;
            bool laneLeftGood = CheckGoodLaneToGo(leftLane, mightNeedToOvertake, longestTimeToNextSwitch);

            if (Flags.debugovertake && IsInPieceBeforeSwitch()) Console.WriteLine("Good to overtake! lane {0}: {1}, lane {2}: {3}, gameTick: {4}", rightLane, laneRightGood, leftLane, laneLeftGood, Model.gameTick);

            if (laneLeftGood && laneRightGood) {
                int bestLane = TrackModel.bestLanes[nextSwitch1];
                if (Math.Abs(Model.you.states[Model.gameTick].endLaneIndex + 1 - bestLane) <
                    Math.Abs(Model.you.states[Model.gameTick].endLaneIndex - 1 - bestLane)
                    ) {
                    overtakeSwitch = 1;
                }
                else {
                    overtakeSwitch = -1;
                }

                return true;
            }
            else if (laneRightGood) {
                overtakeSwitch = 1;
                return true;
            }
            else if (laneLeftGood) {
                overtakeSwitch = -1;
                return true;
            }
            return false;
        }

        return false;
    }

    private static bool CheckGoodLaneToGo(int tryLane, List<Car> carsToConsider, int longestTimeCarInYourLane) {
        int longestTimeOtherLane = 0;

        //Console.WriteLine("Trying lane {0}, Cars to consider: {1}", tryLane, carsToConsider.Count);
        if (tryLane >= 0 && tryLane < Model.lanes.Length) {
            foreach (Car car in carsToConsider) {
                if (Flags.debugovertake && IsInPieceBeforeSwitch()) Console.WriteLine("Other lane car: {0}, tmpTimeToNextSwitchCalculated: {1}, yourTime: {2}, gameTick: {3}", car.carData.color, car.tmpTimeToNextSwitchCalculated, yourTimeToNextSwitch, Model.gameTick);

                if (car.isActive == false) {
                    if (yourTimeToNextSwitch + 10 < car.ExpectedTicksToSpawn()) continue;
                }

                if (car.states[Model.gameTick].endLaneIndex == tryLane) {
                    if (car.tmpTimeToNextSwitchCalculated > longestTimeOtherLane) {
                        longestTimeOtherLane = car.tmpTimeToNextSwitchCalculated;
                    }
                }
            }
            if (Flags.debugovertake && IsInPieceBeforeSwitch()) Console.WriteLine("longestTimeInLane: {0}, longestTimeToNextSwitch: {1}", longestTimeOtherLane, longestTimeCarInYourLane);
            if (longestTimeOtherLane >= longestTimeCarInYourLane) { // no caso de ser igual, eh melhor nao fazer switch
                return false;
            }
        }
        else {
            return false;
        }
        return true;
    }

    public static bool CalculateEmergencyStartEscape(ref int escapeSwitch) {
        bool result = false;
        if (Flags.noe) return false;
        if (Model.qualify) return false;
        if (CloseToRaceFinish()) return false;

        // Largada
        if (Controller.countBackSameLane + Controller.countBackOtherLane > 2) {
            Console.WriteLine("                        *** EMERGENCY START ESCAPE ***, gametick {0}", Model.gameTick);
            result = true;
        }

        if (result) escapeSwitch = FindEscapeSwitch(true);
        return result;
    }

    public static bool CalculateEscape(ref int escapeSwitch) {
        bool result = false;
        if (Flags.noe) return false;
        if (Flags.jem) return false;
        if (Model.qualify) return false;
        if (CloseToRaceFinish()) return false;

        // Dois atras eh perigoso
        if (Controller.countBackSameLane > 1 || Controller.countBackOtherLane > 1) {
            Console.WriteLine("                        *** ESCAPE 2 SAME LANE***, gametick {0}", Model.gameTick);
            result = true;
        }

        // Cara atras eh perigoso, e cara da frente nao eh competitivo
        if (!result && (Flags.af || (Controller.closestBackAnyLane != null && Controller.closestBackAnyLane.IsDangerous()))) {
            if (Controller.closestFrontAnyLane == null || !Controller.closestFrontAnyLane.IsCompetitive()) {
                Console.WriteLine("                        *** ESCAPE DANGEROUS CAR BEHIND***, gametick {0}", Model.gameTick);
                result = true;
            } 
        }

        if (result) escapeSwitch = FindEscapeSwitch(false);
        //Console.WriteLine("escapeSwitch {0}", escapeSwitch);
        return result;
    }

    public static int FindEscapeSwitch(bool forcedEmergency) {
        int nextBestSwitch = TrackModel.FindNextBestLane(Model.you.states[Model.gameTick].pieceIndex, Model.you.states[Model.gameTick].endLaneIndex);
        int nextBestSwitchAfter = TrackModel.FindNextBestLane(TrackModel.FindNextSwitch(Model.you.states[Model.gameTick].pieceIndex), Model.you.states[Model.gameTick].endLaneIndex);

        //Console.WriteLine("nextBestSwitch {0}", nextBestSwitch);
        if (!TrackModel.foundBestLanes) return 0;
        int nextSwitch = TrackModel.FindNextSwitch(Model.you.states[Model.gameTick].pieceIndex);
        if (nextSwitch == -1) return 0;
        int nextBestLane = TrackModel.bestLanes[nextSwitch];

         
        int option1 = -1;
        bool option1Good = (Model.you.states[Model.gameTick].endLaneIndex + option1) != nextBestLane;
        if (Model.you.states[Model.gameTick].endLaneIndex + option1 < 0 || Model.you.states[Model.gameTick].endLaneIndex + option1 >= Model.lanes.Length) option1Good = false;
        int option2 = 0;
        bool option2Good = (Model.you.states[Model.gameTick].endLaneIndex + option2) != nextBestLane;
        if (Model.you.states[Model.gameTick].endLaneIndex + option2 < 0 || Model.you.states[Model.gameTick].endLaneIndex + option2 >= Model.lanes.Length) option2Good = false;
        int option3 = 1;
        bool option3Good = (Model.you.states[Model.gameTick].endLaneIndex + option3) != nextBestLane;
        if (Model.you.states[Model.gameTick].endLaneIndex + option3 < 0 || Model.you.states[Model.gameTick].endLaneIndex + option3 >= Model.lanes.Length) option3Good = false;

        if (!forcedEmergency) {
            if (!CheckGoodLaneToGo(Model.you.states[Model.gameTick].endLaneIndex + option1, mightNeedToOvertake, yourTimeToNextSwitch - 3)) option1Good = false;
            if (!CheckGoodLaneToGo(Model.you.states[Model.gameTick].endLaneIndex + option2, mightNeedToOvertake, yourTimeToNextSwitch - 3)) option2Good = false;
            if (!CheckGoodLaneToGo(Model.you.states[Model.gameTick].endLaneIndex + option3, mightNeedToOvertake, yourTimeToNextSwitch - 3)) option3Good = false;
        }

        //if (Controller.closestBack != null && Controller.closestBack.IsDangerous())
        //{
        //    int closestBackEndLane = Controller.closestBack.states[Model.gameTick].endLaneIndex;
        //    int ourEndLane = Model.you.states[Model.gameTick].endLaneIndex;

        //    if (ourEndLane + option1 == closestBackEndLane) option1Good = false;
        //    //if (ourEndLane + option2 == closestBackEndLane) option2Good = false;
        //    if (ourEndLane + option3 == closestBackEndLane) option3Good = false;
        //}

        if (option2Good && Math.Abs(option2 - nextBestSwitch - nextBestSwitchAfter) == 0) return option2;
        if (option1Good && Math.Abs(option1 - nextBestSwitch - nextBestSwitchAfter) == 0) return option1;
        if (option3Good && Math.Abs(option3 - nextBestSwitch - nextBestSwitchAfter) == 0) return option3;

        if (option2Good && Math.Abs(option2 - nextBestSwitch - nextBestSwitchAfter) == 1) return option2;
        if (option1Good && Math.Abs(option1 - nextBestSwitch - nextBestSwitchAfter) == 1) return option1;
        if (option3Good && Math.Abs(option3 - nextBestSwitch - nextBestSwitchAfter) == 1) return option3;

        if (option2Good) return option2;
        if (option1Good) return option1;
        if (option3Good) return option3;

        return 0;
    }

    public static bool CloseToRaceFinish() {
        if (Model.raceSessionData.laps > 0) {
            if (Model.you.currentLap < Model.raceSessionData.laps - 2) {
                return false;
            }
            else {
                return true;
            }
        }
        Console.WriteLine("CloseToRaceFinish false");
        return false;
    }
}