using System;
using System.Collections.Generic;
using System.Linq;

public static class CollisionSimulator {

    public static int c_collision_ticks = 50;

    public static bool HasSwitchNearby() {
        double distAhead = 100;
        double distBefore = 150;
        double unused = 0;
        int currentPieceIndex = Model.you.states[Model.gameTick].pieceIndex;

        if (Model.trackPieces[currentPieceIndex].hasSwitch) return true;

        double distFront = Model.GetLanePieceTotalLength(ref Model.you.states[Model.gameTick]) - Model.you.states[Model.gameTick].inPieceDistance;
        while (distFront < distAhead) {
            currentPieceIndex = Model.GetNextPieceIndex(currentPieceIndex);
            distFront += Model.GetLanePieceTotalLength(currentPieceIndex, Model.you.states[Model.gameTick].endLaneIndex, Model.you.states[Model.gameTick].endLaneIndex, ref unused);
            if (Model.trackPieces[currentPieceIndex].hasSwitch) return true;
        }

        currentPieceIndex = Model.you.states[Model.gameTick].pieceIndex;
        double distBack = Model.you.states[Model.gameTick].inPieceDistance;
        while (distBack < distBefore) {
            currentPieceIndex = Model.GetPieceIndexBefore(currentPieceIndex);
            distBack += Model.GetLanePieceTotalLength(currentPieceIndex, Model.you.states[Model.gameTick].endLaneIndex, Model.you.states[Model.gameTick].endLaneIndex, ref unused);
            if (Model.trackPieces[currentPieceIndex].hasSwitch) return true;
        }

        return false;
    }

    public static void FindInRange(int maxTimeDiff, bool inFront, bool acceptClosestOtherLanes, ref Car closestCar, ref Car closestCarAnyLane, ref int carsInRangeSameLane, ref int carsInRangeOtherLane) {
        closestCar = null;
        closestCarAnyLane = null;
        carsInRangeSameLane = 0;
        carsInRangeOtherLane = 0;

        if (Flags.noc) return;
        if (Model.qualify) return;
     
        double closestDist = 2000000000;
        double closestDistAnyLane = 2000000000;
        Car back;
        Car front;

        foreach (Car car in Model.others.Values) {
            if (!car.isActive) continue;

            if (inFront) {
                back = Model.you;
                front = car;
            }
            else {
                back = car;
                front = Model.you;
            }

            double dist = Model.GetDistanceBetween(back.states[Model.gameTick].pieceIndex, back.states[Model.gameTick].inPieceDistance,
                                                   back.states[Model.gameTick].startLaneIndex, back.states[Model.gameTick].endLaneIndex,
                                                   front.states[Model.gameTick].pieceIndex, front.states[Model.gameTick].inPieceDistance);
            if (dist >= 0 && dist < 500) {
                bool noData = false;
                if ((inFront && car.TimeAheadYou(ref noData) < maxTimeDiff)
                     || (!inFront && car.TimeBehindYou(ref noData) < maxTimeDiff)
                     || (noData && dist < 150))
                {
                    if (dist < closestDistAnyLane) {
                        closestCarAnyLane = car;
                        closestDistAnyLane = dist;
                    }

                    if (car.states[Model.gameTick].startLaneIndex != Model.you.states[Model.gameTick].startLaneIndex ||
                        car.states[Model.gameTick].endLaneIndex != Model.you.states[Model.gameTick].endLaneIndex) 
                    {
                        if (!acceptClosestOtherLanes) continue;
                        carsInRangeOtherLane++;
                    }
                    else {
                        carsInRangeSameLane++;
                    
                    }

                    if (dist < closestDist) {
                        closestCar = car;
                        closestDist = dist;
                    }
                }
            }
        }
    }

    public static bool NoWayToEscapeOther(Strategy myStrategy, Car otherCar, bool otherInBack) {
        if (otherInBack) {
            return NoWayToEscapeOtherBack(myStrategy, otherCar);
        }
        return NoWayToEscapeOtherFront(myStrategy, otherCar);
    }

    public static bool DestroyOtherFront(Strategy myStrategy, Car otherCar, bool tryTurbo, double my_max_angle) {
        if (Flags.noc) return false;
        if (Model.qualify) return false;
        if (Flags.logc) Console.WriteLine("testing destroy front...");

        if (!CheckDestroyStrategies(Model.you.states[Model.gameTick], myStrategy, my_max_angle, tryTurbo,
                                    otherCar.states[Model.gameTick], otherCar.GetCurrentS1WhenPossible(), AngleModel.max_slide_angle_global)) {
            //Console.WriteLine("  FALHOU DESTROY FRONT, OTHER 1WHENPOSSIBLE");
            return false;
        }


        if (!CheckDestroyStrategies(Model.you.states[Model.gameTick], myStrategy, my_max_angle, tryTurbo,
                                    otherCar.states[Model.gameTick], new S0(), AngleModel.max_slide_angle_global)) {
            //Console.WriteLine("  FALHOU DESTROY FRONT, OTHER FREIANDO");
            return false;
        }
        return true;
    }

    public static bool NoWayToEscapeOtherFront(Strategy myStrategy, Car otherCar) {
        if (Flags.noc) return false;
        if (Model.qualify) return false;
        if (Flags.logc) Console.WriteLine("testing front...");

        if (CheckWillCrashStrategies(Model.you.states[Model.gameTick], myStrategy, AngleModel.max_slide_angle_global,
                                    otherCar.states[Model.gameTick], otherCar.GetCurrentS1WhenPossible(), AngleModel.max_slide_angle_global)) 
        {
            Console.WriteLine("  FALHOU FRONT, OTHER 1WHENPOSSIBLE");
            return true;
        }


        if (CheckWillCrashStrategies(Model.you.states[Model.gameTick], myStrategy, AngleModel.max_slide_angle_global,
                                    otherCar.states[Model.gameTick], new S0(), AngleModel.max_slide_angle_global)) 
        {
            Console.WriteLine("  FALHOU FRONT, OTHER FREIANDO");
            return true;
        }
        return false;
    }


    public static bool NoWayToEscapeOtherBack(Strategy myStrategy, Car otherCar) {
        if (Flags.noc) return false;
        if (Model.qualify) return false;
        if (Flags.logc) Console.WriteLine("testing back...");

        if (CheckWillCrashStrategies(Model.you.states[Model.gameTick], myStrategy, AngleModel.max_slide_angle_global,
                                    otherCar.states[Model.gameTick], otherCar.GetCurrentS1WhenPossible(), AngleModel.max_slide_angle_global)) 
        {
            Console.WriteLine("  FALHOU BACK, OTHER 1WHENPOSSIBLE");
            return true;
        }

        //for (int i = 0; i < 10; i++) {
        //    if (CheckWillCrashStrategies(Model.you.states[Model.gameTick], myStrategy, AngleModel.max_slide_angle_global,
        //                                    otherCar.states[Model.gameTick], new SMixed(otherCar.GetCurrentS1WhenPossible(), new S1(), i), AngleModel.max_slide_angle_global)) {
        //        Console.WriteLine("  FALHOU BACK, OTHER 1WHENPOSSIBLE for {0} ticks, then KAMIKAZE", i);
        //        Model.n1WpBeforeKamikaze[i] = 1 + Statistics.GetValueOrZero(Model.n1WpBeforeKamikaze, i);
        //        return true;
        //    }
        //}

        for (int i = 0; i < 2; i++) {
            if (CheckWillCrashStrategies(Model.you.states[Model.gameTick], myStrategy, AngleModel.max_slide_angle_global,
                                            otherCar.states[Model.gameTick], new SMixed(new S0(), new S1(), i), AngleModel.max_slide_angle_global)) {
                Console.WriteLine("  FALHOU BACK, OTHER S0 for {0} ticks, then KAMIKAZE", i);
                //Model.n1WpBeforeKamikaze[i] = 1 + Statistics.GetValueOrZero(Model.n1WpBeforeKamikaze, i);
                return true;
            }
        }

        return false;
    }

    public static bool CheckDestroyStrategies(State myState, Strategy myStrategy, double my_max_angle, bool try_turbo,
                                              State otherState, Strategy otherStrategy, double other_max_angle) {
        myStrategy.Reset();
        otherStrategy.Reset();
        bool myCrash = false;
        bool otherCrash = false;

        State myNewState = new State();
        State otherNewState = new State();

        for (int i = 0; i < c_collision_ticks; i++) {
            if (i == 1 && try_turbo) {
                myState.turboFactor = Model.turboFactor;
                myState.turboTicksRemaining = Model.turboDuration + 1;
            }

            SimulateMultipleNextState(ref myState, ref myNewState, myStrategy, ref myCrash, my_max_angle,
                                      ref otherState, ref otherNewState, otherStrategy, ref otherCrash, other_max_angle);

            //Console.WriteLine("cs, crash[{0}] {2}, cf[{0}] {3}, cb[{0}] {4}, ds[{0}] {5}, a[{0}] {6}, t[{1}] {7}, t_other[{1}] {8}",
            //    i + Model.gameTick + 1, i + Model.gameTick, myCrash, myNewState.collidedFront, myNewState.collidedBack,
            //    myNewState.ds, myNewState.a, myState.throttle, otherState.throttle);
            if (myCrash) {
                return false;
            }

            if (otherCrash) {
                //ja vai morrer sozinho mesmo...
                return false;
            }

            if (myNewState.collidedBack || myNewState.collidedFront) {
                bool myFinalResult = SurvivalSimulator.WillCrash(myNewState, true, true, true, my_max_angle);
                bool otherFinalResult = SurvivalSimulator.WillCrash(otherNewState, true, true, false, other_max_angle);
                //Model.collisionTicks[i] = 1 + Statistics.GetValueOrZero(Model.collisionTicks, i);
                return (otherFinalResult && !myFinalResult);
            }

            myState = myNewState;
            otherState = otherNewState;
        }

        return false;
    }

    public static bool CheckWillCrashStrategies(State myState, Strategy myStrategy, double my_max_angle,
                                                State otherState, Strategy otherStrategy, double other_max_angle) {
        myStrategy.Reset();
        otherStrategy.Reset();
        bool myCrash = false;
        bool otherCrash = false;

        State myNewState = new State();
        State otherNewState = new State();

        for (int i = 0; i < c_collision_ticks; i++) {
            SimulateMultipleNextState(ref myState, ref myNewState, myStrategy, ref myCrash, my_max_angle,
                                      ref otherState, ref otherNewState, otherStrategy, ref otherCrash, other_max_angle);

            //Console.WriteLine("cs, crash[{0}] {2}, cf[{0}] {3}, cb[{0}] {4}, ds[{0}] {5}, a[{0}] {6}, t[{1}] {7}, t_other[{1}] {8}",
            //    i + Model.gameTick + 1, i + Model.gameTick, myCrash, myNewState.collidedFront, myNewState.collidedBack,
            //    myNewState.ds, myNewState.a, myState.throttle, otherState.throttle);
            if (myCrash) {
                //Console.WriteLine("        ======== ERROR: CRASH INESPERADO ANTES DA COLISAO ========");
                //Model.collisionTicks[i] = 1 + Statistics.GetValueOrZero(Model.collisionTicks, i);
                return false;
            }

            if (myNewState.collidedFront)
            {
                bool finalResult = SurvivalSimulator.WillCrash(myNewState, true, true, true, my_max_angle);
                //Console.WriteLine("  ==  Final Result {0}", finalResult);
                //Model.collisionTicks[i] = 1 + Statistics.GetValueOrZero(Model.collisionTicks, i);
                return finalResult;
            }

            if (myNewState.collidedBack) {
                bool finalResult = (SurvivalSimulator.WillCrash(myNewState, true, true, true, my_max_angle) ||
                                    SurvivalSimulator.WillCrash(myNewState, true, true, true, my_max_angle, false));
                //Console.WriteLine("  ==  Final Result {0}", finalResult);
                //Model.collisionTicks[i] = 1 + Statistics.GetValueOrZero(Model.collisionTicks, i);
                return finalResult;
            }

            if (otherCrash) {
                //Model.collisionTicks[i] = 1 + Statistics.GetValueOrZero(Model.collisionTicks, i);
                return false;
            }

            myState = myNewState;
            otherState = otherNewState;
        }

       return false;
    }

    public static void SimulateMultipleNextState(
          ref State myCurrentState, ref State myNewState, Strategy myStrategy, ref bool myCrash, double my_max_angle,
          ref State otherCurrentState, ref State otherNewState, Strategy otherStrategy, ref bool otherCrash, double other_max_angle) {

        double myThrottle = myStrategy.GetNextThrottle(ref myCurrentState, my_max_angle);
        Simulator.SimulateSingleNextState(myThrottle, ref myCurrentState, ref myNewState, true, true, ref myCrash, my_max_angle);

        if (!otherCrash) {
            double otherThrottle = otherStrategy.GetNextThrottle(ref otherCurrentState, other_max_angle);
            Simulator.SimulateSingleNextState(otherThrottle, ref otherCurrentState, ref otherNewState, false, true, ref otherCrash, other_max_angle);
        }

        if (!otherCrash && !myCrash) {
            if (myNewState.startLaneIndex == otherNewState.startLaneIndex && myNewState.endLaneIndex == otherNewState.endLaneIndex) {
                if (myNewState.pieceIndex == otherNewState.pieceIndex) {
                    if (myNewState.inPieceDistance < otherNewState.inPieceDistance) {

                        TestCollisionSamePiece(ref myNewState, ref otherNewState);
                    }
                    else {
                        TestCollisionSamePiece(ref otherNewState, ref myNewState);
                    }
                }

                int currentPiece = Model.GetNextPieceIndex(myNewState.pieceIndex);
                for (int i = 0; i < 5; i++) {
                    if (myNewState.startLaneIndex != myNewState.endLaneIndex) break;

                    if (currentPiece == otherNewState.pieceIndex) {
                        TestCollisionDifferentPiece(ref myNewState, ref otherNewState);
                        return;
                    }
                    currentPiece = Model.GetNextPieceIndex(currentPiece);
                }

                currentPiece = Model.GetNextPieceIndex(otherNewState.pieceIndex);
                for (int i = 0; i < 5; i++) {
                    if (otherNewState.startLaneIndex != otherNewState.endLaneIndex) break;

                    if (currentPiece == myNewState.pieceIndex) {
                        TestCollisionDifferentPiece(ref otherNewState, ref myNewState);
                        return;
                    }
                    currentPiece = Model.GetNextPieceIndex(currentPiece);
                }
            }
        }
    }

    public static void TestCollisionSamePiece(ref State stateBack, ref State stateFront) {
        if (stateFront.inPieceDistance - stateBack.inPieceDistance < Model.you.carData.length) {
            stateBack.collidedBack = true;
            stateFront.collidedFront = true;

            stateBack.dsEqMin = SpeedModel.GetDsEqBackSimulator(ref stateFront);
            stateBack.dsEqMax = SpeedModel.GetDsEqBackSimulator(ref stateFront);
            stateFront.dsEqMin = SpeedModel.GetDsEqFront(ref stateBack);
            stateFront.dsEqMax = SpeedModel.GetDsEqFront(ref stateBack);

            double lanePieceLength = Model.GetLanePieceTotalLength(ref stateFront);
            stateFront.inPieceDistance = stateBack.inPieceDistance + Model.you.carData.length;
            if (stateFront.inPieceDistance > lanePieceLength) {
                Simulator.SimulateChangePiece(ref stateFront, lanePieceLength, true);
            }
        }
    }

    public static void TestCollisionDifferentPiece(ref State stateBack, ref State stateFront) {
        double unused = 0;
        double backExtraLength = 0;
        int cursorPiece = stateBack.pieceIndex;
        while (cursorPiece != stateFront.pieceIndex) {
            backExtraLength += Model.GetLanePieceTotalLength(cursorPiece, stateBack.startLaneIndex, stateBack.endLaneIndex, ref unused);
            cursorPiece = Model.GetNextPieceIndex(cursorPiece);
        }

        if (stateFront.inPieceDistance + backExtraLength - stateBack.inPieceDistance < Model.you.carData.length) {
            stateBack.collidedBack = true;
            stateFront.collidedFront = true;

            stateBack.dsEqMin = SpeedModel.GetDsEqBackSimulator(ref stateFront);
            stateBack.dsEqMax = SpeedModel.GetDsEqBackSimulator(ref stateFront);
            stateFront.dsEqMin = SpeedModel.GetDsEqFront(ref stateBack);
            stateFront.dsEqMax = SpeedModel.GetDsEqFront(ref stateBack);

            double lanePieceLength = Model.GetLanePieceTotalLength(ref stateFront);
            stateFront.inPieceDistance = stateBack.inPieceDistance + Model.you.carData.length - backExtraLength;
            if (stateFront.inPieceDistance > lanePieceLength) {
                Simulator.SimulateChangePiece(ref stateFront, lanePieceLength, true);
            }
        }
    }


}
