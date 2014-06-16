using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public static class ThrottleController {

    public static double[] surviveMemory0 = new double[100];
    public static int tickMemoryFound0 = 0;
    public static double[] surviveMemory1 = new double[100];
    public static int tickMemoryFound1 = 0;

    public static double[] surviveMemoryUsed;
    public static int tickMemoryFoundUsed = 0;

    public static bool debugsim = false;

    public static double nextBestThrottle = 1.0;
    public static int nextBestThrottleForTick = -1000;

    public static double CalculateThrottle(ref bool shouldSendTurbo) {
        double result = CalculateThrottleValue(ref shouldSendTurbo);

        // Update memory used
        if (result == 1.0 && tickMemoryFound1 == Model.gameTick) {
            surviveMemoryUsed = surviveMemory1;
            tickMemoryFoundUsed = tickMemoryFound1;
        }
        if (result == 0.0 && tickMemoryFound0 == Model.gameTick) {
            surviveMemoryUsed = surviveMemory0;
            tickMemoryFoundUsed = tickMemoryFound0;
        }

        return result;
    }

    private static double GetThrottleMemorized() {
        int memoryTick = Model.gameTick - tickMemoryFoundUsed - 1;
        double result = 0.0;
        if (memoryTick >= 0 && memoryTick < surviveMemoryUsed.Length) result = surviveMemoryUsed[memoryTick];
        Console.WriteLine("     ==== USING TICK MEMORIZED, throttle {0}, gametick {1} ====", result, Model.gameTick);
        return result;
    }

    private static double CalculateThrottleValue(ref bool shouldSendTurbo) {
        if (!Model.you.isActive) {
            return 1.0;
        }

        if (Flags.fixedthrottle) {
            return Flags.fixedthrottlevalue;
        }
        if (Flags.brakeonstart) {
            if (Model.gameTick < Flags.brakeonstartvalue) return 0.0;
        }
        if (Flags.strategykamikaze) {
            return 1.0;
        }
        if (Flags.kamikazeafter) {
            if (Model.gameTick > Flags.kamikazeaftervalue) return 1.0;
        }

        //if (Model.gameTick == 3098 || Model.gameTick == 3099) debugsim = true;
        bool achou1 = SurvivalSimulator.CanSurviveUsing(Model.you.states[Model.gameTick], 1.0, 1, true, true, true, surviveMemory1, AngleModel.max_slide_angle_global);
        bool achou0 = SurvivalSimulator.CanSurviveUsing(Model.you.states[Model.gameTick], 0.0, 1, true, true, true, surviveMemory0, AngleModel.max_slide_angle_global);
        if (achou1) tickMemoryFound1 = Model.gameTick;
        if (achou0) tickMemoryFound0 = Model.gameTick;

        if (debugsim) Console.WriteLine("achou1 {0}, achou0 {1}, gameTick {2}", achou1, achou0, Model.gameTick);
        debugsim = false;

        if (!achou0 && !achou1) {
            if (Flags.logsimulador) Console.WriteLine(" ==== ACHOU0 == FALSE && ACHOU1 == FALSE, gameTick {0}", Model.gameTick);
            return GetThrottleMemorized();
        }

        if (!achou1) {
            if (Flags.logsimulador) Console.WriteLine(" ==== ACHOU1 == FALSE usando 0, gameTick {0}", Model.gameTick);
            return 0.0;
        }
        if (!achou0) {
            if (Flags.logsimulador) Console.WriteLine(" ==== ACHOU0 == FALSE usando 1, gameTick {0}", Model.gameTick);
            return 1.0;
        }

        if (Model.gameTick > 0 && Model.you.states[Model.gameTick - 1].gametickLost) {
            return 1.0;  // Tentando re-sincronizar
        }
        if (Flags.strategy1whenpossible) {
            return 1.0;  // Para testes
        }


        if (Controller.closestBack != null || Controller.closestFront != null) {
            bool achou0collision = false;
            bool achou1collision = false;

            Car otherTested = Controller.closestBack;
            bool testingBack = true;
            if (Controller.closestBack == null) {
                otherTested = Controller.closestFront;
                testingBack = false;
            }

            Strategy send1Then1WP = new SMixed(new S1(), new S1WhenPossibleWithMemory(), 1);
            Strategy send0Then1WP = new SMixed(new S0(), new S1WhenPossibleWithMemory(), 1);

            achou0collision = !CollisionSimulator.NoWayToEscapeOther(new S0(), otherTested, testingBack);
            if (!achou0collision) {
                achou0collision = !CollisionSimulator.NoWayToEscapeOther(send0Then1WP, otherTested, testingBack);
                if (achou0collision) Console.WriteLine("                    ==== SALVOU 0 achou0collision COM 1WP", Model.gameTick);
            }

            achou1collision = !CollisionSimulator.NoWayToEscapeOther(new SMixed(new S1(), new S0(), 1), otherTested, testingBack);
            if (!achou1collision) {
                achou1collision = !CollisionSimulator.NoWayToEscapeOther(send1Then1WP, otherTested, testingBack);
                if (achou1collision) Console.WriteLine("                    ==== SALVOU 1 achou1collision COM 1WP", Model.gameTick);
            }

            //filtrar pelo front se deu tudo certo pro back
            if (achou0collision && achou1collision && Controller.closestBack != null && Controller.closestFront != null) {
                otherTested = Controller.closestFront;
                testingBack = false;
                achou0collision = !CollisionSimulator.NoWayToEscapeOther(new S0(), otherTested, testingBack);
                if (!achou0collision) {
                    achou0collision = !CollisionSimulator.NoWayToEscapeOther(send0Then1WP, otherTested, testingBack);
                    if (achou0collision) Console.WriteLine("                    ==== SALVOU 0 achou0collision COM 1WP", Model.gameTick);
                }

                achou1collision = !CollisionSimulator.NoWayToEscapeOther(new SMixed(new S1(), new S0(), 1), otherTested, testingBack);
                if (!achou1collision) {
                    achou1collision = !CollisionSimulator.NoWayToEscapeOther(send1Then1WP, otherTested, testingBack);
                    if (achou1collision) Console.WriteLine("                    ==== SALVOU 1 achou1collision COM 1WP", Model.gameTick);
                }
            }

            if (achou0collision && achou1collision && Controller.closestFront != null) {
                otherTested = Controller.closestFront;
                bool achouDestroy = CollisionSimulator.DestroyOtherFront(send1Then1WP, otherTested, false, AngleModel.max_slide_angle_global);
                if (achouDestroy) {
                    Console.WriteLine("SEEK AND DESTROY!!!!");
                    return 1.0;
                }

                if (Model.you.HasTurboAvailable() && IsInBetterRankingThanYou(otherTested)) {
                    Console.WriteLine("      Testando seek and destroy turbo...");
                    Strategy strategyTurbo;
                    //Strategy strategyTurboKamikaze;
                    if (Model.you.GetLastThrottle() == 1.0) {
                        strategyTurbo = new SMixed(new S1(), new S1WhenPossibleWithMemory(), 1);
                        //strategyTurboKamikaze = new S1();
                    }
                    else {
                        strategyTurbo = new SMixed(new S0(), new S1WhenPossibleWithMemory(), 1);
                        //strategyTurboKamikaze = new SMixed(new S0(), new S1(), 1);
                    }

                    achouDestroy = CollisionSimulator.DestroyOtherFront(strategyTurbo, otherTested, true, AngleModel.max_slide_angle_global);
                    //if (!achouDestroy) CollisionSimulator.DestroyOtherFront(strategyTurboKamikaze, otherTested, true, 60);
                    if (achouDestroy) {
                        Console.WriteLine("SEEK AND DESTROY WITH TURBO!!!!");
                        shouldSendTurbo = true;
                        return Model.you.GetLastThrottle();
                    }
                }
            }

            if (!achou0collision && !achou1collision) {
                Console.WriteLine("  ==== ACHOU_0_COLLISION == FALSE, ACHOU_1_COLLISION == FALSE gametick {0}", Model.gameTick);
            }

            if (!achou0collision && achou1collision) {
                Console.WriteLine("    ACHOU_0_COLLISION == FALSE, gametick {0}", Model.gameTick);
                return 1.0;
            }
            if (!achou1collision && achou0collision) {
                Console.WriteLine("    ACHOU_1_COLLISION == FALSE, gametick {0}", Model.gameTick);
                return 0.0;
            }
        }
        
        if (nextBestThrottleForTick == Model.gameTick) {
            return nextBestThrottle;
        }
        return 1.0;
    }

    public static void SearchNextBestThrottle() {
        State currentState = Model.you.states[Model.gameTick];
        State nextState = new State();
        bool crashed = false;
        nextBestThrottle = 1.0;
        nextBestThrottleForTick = Model.gameTick + 1;

        Simulator.SimulateSingleNextState(Model.you.states[Model.gameTick].throttle, ref currentState, ref nextState, true, true, ref crashed, AngleModel.max_slide_angle_global);
        if (crashed) return;
 
        int bestTime = 2000000000;
        double bestDistance = 0;
        double bestThrottle = 1.0;

        bool achou1 = SurvivalSimulator.CanSurviveUsing(nextState, 1.0, 1, true, true, true, null, AngleModel.max_slide_angle_global);
        bool achou0 = SurvivalSimulator.CanSurviveUsing(nextState, 0.0, 1, true, true, true, null, AngleModel.max_slide_angle_global);

        if (achou1 && achou0) {
            SearchUsingThrottle(nextState, 1.0, 0.0, ref bestTime, ref bestDistance, ref bestThrottle);
            SearchUsingThrottle(nextState, 0.0, 1.0, ref bestTime, ref bestDistance, ref bestThrottle);
        }

        nextBestThrottle = bestThrottle;
    }

    private static void SearchUsingThrottle(State currentState, double primaryThrottle, double secondaryThrottle, ref int bestTime, ref double bestDistance, ref double bestThrottle) {
        State newState = new State();
        bool crashed = false;

        int piecesAhead = 4;
        if (Model.trackId == "pentag") piecesAhead = 3;
        if (Model.trackId == "france") piecesAhead = 3;
        if (Model.trackId == "germany") piecesAhead = 5;
        if (Model.trackId == "imola") piecesAhead = 5;
        if (Model.trackId == "england") piecesAhead = 5;


        int targetPiece = Model.GetNextNPiecesAhead(Model.you.currentPieceIndex, piecesAhead);
        int numThrottles = 5;
        for (int i = 1; i <= numThrottles; i++) {
            Simulator.SimulateSingleNextState(primaryThrottle, ref currentState, ref newState, true, true, ref crashed, AngleModel.max_slide_angle_global);
            if (crashed) break;
            currentState = newState;

            double distanceInPiece;
            int time;
            if (i != numThrottles) {
                Simulator.SimulateSingleNextState(secondaryThrottle, ref currentState, ref newState, true, true, ref crashed, AngleModel.max_slide_angle_global);
                if (crashed) continue;
                time = i + 1 + SurvivalSimulator.SimulateGameTicksUntilPiece(newState, targetPiece, out distanceInPiece, AngleModel.max_slide_angle_global);
            }
            else {
                time = i + SurvivalSimulator.SimulateGameTicksUntilPiece(currentState, targetPiece, out distanceInPiece, AngleModel.max_slide_angle_global);
            }

            if (time < bestTime || (time == bestTime && distanceInPiece > bestDistance)) {
                bestTime = time;
                bestDistance = distanceInPiece;
                bestThrottle = primaryThrottle;
            }
        }
    }

    public static bool IsInBetterRankingThanYou(Car otherCar) {
        if (otherCar.currentLap > Model.you.currentLap) {
            return true;
        }

        if (otherCar.currentLap == Model.you.currentLap) {
            if (otherCar.states[Model.gameTick].pieceIndex > Model.you.states[Model.gameTick].pieceIndex) {
                return true;
            }

            if (otherCar.states[Model.gameTick].pieceIndex == Model.you.states[Model.gameTick].pieceIndex) {
                if (otherCar.states[Model.gameTick].inPieceDistance > Model.you.states[Model.gameTick].inPieceDistance) {
                    return true;
                }
            }
        }

        return false;
    }
}