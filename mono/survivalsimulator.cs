using System;
using System.Collections.Generic;
using System.Linq;

public static class SurvivalSimulator {

    public static bool CanSurviveUsing(State currentState, double throttle, int repeatThrottle, bool useActiveLane, bool useOnesToSurvive, bool useDirtyMargin, double[] throttlesMemorized, double max_angle) {
        bool crashed = false;
        State newState = new State();
        for (int i = 0; i < repeatThrottle; i++) {
            Simulator.SimulateSingleNextState(throttle, ref currentState, ref newState, true, useActiveLane, ref crashed, max_angle);
            if (crashed) {
                return false;
            }
            currentState = newState;
        }

        if (throttlesMemorized == null) {
            return !WillCrash(currentState, useActiveLane, useOnesToSurvive, useDirtyMargin, max_angle);
        }
        return !WillCrashMemory(currentState, useActiveLane, useOnesToSurvive, useDirtyMargin, throttlesMemorized, max_angle);
    }

    public static bool WillCrashZeroThrottle(State currentState, bool useActiveLane, bool useDirtyMargin, double max_angle, bool useMaxDsEq=true)
    {
        State newState = new State();
        int ticks = 0;
        bool crashed = false;

        while (ticks < 300) {
            Simulator.SimulateSingleNextState(0.0, ref currentState, ref newState, useDirtyMargin, useActiveLane, ref crashed, max_angle, useMaxDsEq);
            if (crashed) return true;

            if (ThrottleController.debugsim) Console.WriteLine("WillCrashZeroThrottle ticks {0}, ds {1}, a {2}, stopDs {3}, nextDa {4}", ticks, currentState.ds, currentState.a, AngleModel.GetCStopDs(ref newState), AngleModel.GetNextDa(ref states[ticks + 1], true));
            
            if (newState.ds < AngleModel.GetCStopDs(ref newState)) {
                double angle_pg_sum = Math.Abs(newState.a + AngleModel.c_stop_simulation_da * AngleModel.GetNextDa(ref newState, true));
                if (angle_pg_sum < max_angle - 5 && Math.Abs(newState.a) < max_angle - 5) {
                    return false;
                }
                if (newState.ds < 1.0 && angle_pg_sum < max_angle) {
                    return false;
                }
            }
            
            currentState = newState;
            ticks++;
        }
        return false;
    }

    static State[] states = new State[10000];
    static int ticks;

    public static bool WillCrashMemory(State currentState, bool useActiveLane, bool useOnesToSurvive, bool useDirtyMargin, double[] throttlesMemorized, double max_angle) {
        bool result = WillCrash(currentState, useActiveLane, useOnesToSurvive, useDirtyMargin, max_angle);
        if (!result) {
            for (int i = 0; i < throttlesMemorized.Length; i++) {
                if (i < ticks - 1) {
                    throttlesMemorized[i] = states[i].throttle;
                } else {
                    throttlesMemorized[i] = 0.0;
                }
            }
        }
        return result;
    }

    public static bool WillCrash(State currentState, bool useActiveLane, bool useOnesToSurvive, bool useDirtyMargin, double max_angle, bool useMaxDsEq=true) {
        states[0] = currentState;
        ticks = 0;
        bool crashed = false;

        while (ticks < 300) {
            Simulator.SimulateSingleNextState(0.0, ref states[ticks], ref states[ticks + 1], useDirtyMargin, useActiveLane, ref crashed, max_angle, useMaxDsEq);
            if (crashed) break;

            if (ThrottleController.debugsim) LogState(ref states[ticks], ticks, states[ticks].throttle);

            
            if (states[ticks + 1].ds < AngleModel.GetCStopDs(ref states[ticks + 1])) {
                double angle_pg_sum = Math.Abs(states[ticks + 1].a + AngleModel.c_stop_simulation_da * AngleModel.GetNextDa(ref states[ticks + 1], true));
                if(angle_pg_sum < max_angle - 5 && Math.Abs(states[ticks + 1].a) < max_angle - 5) {
                    return false;
                }
                if (states[ticks + 1].ds < 1.0 && angle_pg_sum < max_angle) {
                    return false;
                }
            }
            
            ticks++;
        }
        if (ticks == 300) {
            Console.WriteLine("    ==== ERROR: 300 no WillCrash! ==== ds {0}, a {1}, da {2}", states[ticks].ds, states[ticks].a, states[ticks].da);
            return true;
        }

        if (!useOnesToSurvive) return true;

        int crashTick = ticks;
        while (ticks >= 0) { //&& ticks > crashTick - 5
            Simulator.SimulateSingleNextState(0.0, ref states[ticks], ref states[ticks + 1], useDirtyMargin, useActiveLane, ref crashed, max_angle, useMaxDsEq);
            double signedNextDa0 = AngleModel.GetNextDa(ref states[ticks + 1], true) * Math.Sign(states[ticks + 1].a);
            Simulator.SimulateSingleNextState(1.0, ref states[ticks], ref states[ticks + 1], useDirtyMargin, useActiveLane, ref crashed, max_angle, useMaxDsEq);
            double signedNextDa1 = AngleModel.GetNextDa(ref states[ticks + 1], true) * Math.Sign(states[ticks + 1].a);
            if (signedNextDa0 < signedNextDa1) break;
            ticks--;
        }

        int initialTick = ticks;

        if (crashTick == initialTick) return true;
        if (ThrottleController.debugsim) Console.WriteLine("crashTick {0}, initialTick {1}, diff {2}", crashTick, initialTick, crashTick - initialTick);

        //Model.diffsGoingBackToSave[crashTick - initialTick] = 1 + Statistics.GetValueOrZero(Model.diffsGoingBackToSave, crashTick - initialTick);

        initialTick++; // volta para o tick que eh bom usar 1;
        int initialTickBefore = initialTick;
        for (int i = 1; i <= 10; i++) {

            ticks = initialTick;
            int onesRemaining = 1;
            bool crashed0 = false;
            State nextState0 = new State();
            while (true) {
                bool usedOne = false;

                Simulator.SimulateSingleNextState(0.0, ref states[ticks], ref nextState0, useDirtyMargin, useActiveLane, ref crashed0, max_angle, useMaxDsEq);
                double signedNextDa0 = AngleModel.GetNextDa(ref nextState0, true) * Math.Sign(nextState0.a);

                if (onesRemaining > 0) {
                    Simulator.SimulateSingleNextState(1.0, ref states[ticks], ref states[ticks + 1], useDirtyMargin, useActiveLane, ref crashed, max_angle, useMaxDsEq);
                    double signedNextDa1 = AngleModel.GetNextDa(ref states[ticks + 1], true) * Math.Sign(states[ticks + 1].a);
                    if (!crashed && signedNextDa0 > signedNextDa1) {
                        if (ThrottleController.debugsim) LogState(ref states[ticks], ticks, states[ticks].throttle);
                        usedOne = true;
                        ticks++;
                        initialTick = ticks;
                        onesRemaining--;
                    }
                }

                if (!usedOne) {
                    if (ThrottleController.debugsim) LogState(ref states[ticks], ticks, 0.0);
                    if (crashed0) break;
                    states[ticks].throttle = 0.0;
                    states[ticks + 1] = nextState0;
                    ticks++;
                }

                if (ticks >= crashTick + 5) {
                    bool finalResult = WillCrashZeroThrottle(states[ticks], useActiveLane, useDirtyMargin, max_angle, useMaxDsEq);
                    if (!finalResult) {
                        //Model.onesUsedToSave[i] = 1 + Statistics.GetValueOrZero(Model.onesUsedToSave, i);
                        if (ThrottleController.debugsim) Console.WriteLine("USED {0} 1's TO SURVIVE, after ticks {1}, gameTick {2}", i, initialTickBefore, Model.gameTick);
                    }
                    return finalResult;
                }
            }

            if (onesRemaining != 0) break;
        }

        return true;
    }

    private static void LogState(ref State state, int ticks, double throttle) {
        Console.WriteLine("WillCrash used throttle {0} , on ticks {1}, eqgt {2}, ds {3}, a {4}, turboFactor{5}, turboRemaining{6}, gameTick {7}",
                           throttle, ticks, ticks + Model.gameTick + 1, state.ds, state.a, state.turboFactor, state.turboTicksRemaining, Model.gameTick);
    }


    public static int SimulateGameTicksUntilPiece(State currentState, int targetPiece, out double distanceInPiece, double max_angle) {
        State newState = new State();
        bool crashed = false;
        int ticks = 0;
        distanceInPiece = 0;

        int tickMemory = -2000000000;
        double[] memory = new double[10];

        while (ticks < 1000) {
            bool achou = false;
            if (currentState.pieceIndex == targetPiece) {
                distanceInPiece = currentState.inPieceDistance;
                return ticks;
            }

            Simulator.SimulateSingleNextState(1.0, ref currentState, ref newState, true, true, ref crashed, max_angle);
            if (!crashed && !WillCrashMemory(newState, true, true, true, memory, max_angle)) {
                achou = true;
                tickMemory = ticks;
            }

            if (!achou) {
                Simulator.SimulateSingleNextState(0.0, ref currentState, ref newState, true, true, ref crashed, max_angle);
                if (!crashed && !WillCrashMemory(newState, true, true, true, memory, max_angle)) {
                    achou = true;
                    tickMemory = ticks;
                }
            }

            if (!achou) {
                double throttle = 1.0; //caso esteja no inicio sem memory
                int tickToUse = ticks - tickMemory - 1;
                bool usedMemory = false;
                if (tickToUse >= 0 && tickToUse < 10) {
                    usedMemory = true;
                    throttle = memory[tickToUse];
                }
                Simulator.SimulateSingleNextState(throttle, ref currentState, ref newState, true, true, ref crashed, max_angle);
                if (crashed) {
                    if (usedMemory) Console.WriteLine("    ==== ERROR: Unexpected crash using memory in SimulateGameTicksUntilPiece! ====");
                    return 1000000000;
                }
            }

            ticks++;
            currentState = newState;
        }
        Console.WriteLine("    ==== ERROR: 1000 in SimulateGameTicksUntilPiece! ====");
        return 1000000000;
    }

}