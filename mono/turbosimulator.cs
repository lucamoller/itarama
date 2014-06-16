using System;
using System.Collections.Generic;
using System.Linq;

public static class TurboSimulator {

    private static List<StreakAfterPosition>[] streaks = null;
    private static State[] state = new State[50000];

    private static State stateAfterFirstLap;
    public static bool hasStateAfterFirstLap = false;
    public static bool hasSimulatedGoodLap = false;
    public static int bestPieceForTurbo = 0;

    public static void Simulate() {
        if (!hasStateAfterFirstLap || SurvivalSimulator.WillCrash(stateAfterFirstLap, false, true, true, AngleModel.max_slide_angle_global)) {
            SimulateFirstLap();
            return;
        }

        SimulateGoodLap();
    }

    private static void SimulateFirstLap() {
        int firstStateAfter1Lap = Simulate1Lap(State.GetZeroState());
        stateAfterFirstLap = state[firstStateAfter1Lap];
        hasStateAfterFirstLap = true;
        return;
    }

    public static void SimulateGoodLap() {
        int finalSt = Simulate1Lap(stateAfterFirstLap) - 1;

        int currentThrottleStreak = 0;
        for (int i = 0; i <= finalSt; i++) {
            if (state[i].throttle == 1.0) {
                currentThrottleStreak++;
            }
            else {
                break;
            }
        }

        int longuestStreak = 0;
        int longuestSt = 0;
        streaks = new List<StreakAfterPosition>[Model.trackPieces.Length];
        for (int i = 0; i < Model.trackPieces.Length; i++) { streaks[i] = new List<StreakAfterPosition>(); }

        for (int i = finalSt - 1; i >= 0; i--) {
            if (state[i].throttle == 1.0) {
                currentThrottleStreak++;
            }
            else {
                currentThrottleStreak = 0;
            }

            //Console.WriteLine("pieceIndex {0}, currentStreak {1}", state[i].pieceIndex, currentThrottleStreak);

            streaks[state[i].pieceIndex].Add(new StreakAfterPosition(state[i].inPieceDistance, currentThrottleStreak, state[i].ds));
            if (currentThrottleStreak > longuestStreak) {
                longuestStreak = currentThrottleStreak;
                longuestSt = i;
            }
        }
        for (int i = 0; i < Model.trackPieces.Length; i++) { streaks[i].Reverse(); }


        bestPieceForTurbo = state[longuestSt].pieceIndex;
        Console.WriteLine(">>>>>> BestPieceToStartUsingTurbo: " + bestPieceForTurbo + ", totalPieces: " + Model.trackPieces.Length);
        hasSimulatedGoodLap = true;
    }

    private static int Simulate1Lap(State initialState) {
        int st = 0;
        state[0] = initialState;
        int initialPiece = state[st].pieceIndex;
        bool crashed = false;
        bool finished = false;
        while (!finished) {
            crashed = false;
            Simulator.SimulateSingleNextState(1.0, ref state[st], ref state[st + 1], true, false, ref crashed, AngleModel.max_slide_angle_global);
            if (crashed || SurvivalSimulator.WillCrash(state[st + 1], false, true, true, AngleModel.max_slide_angle_global)) {
                Simulator.SimulateSingleNextState(0.0, ref state[st], ref state[st + 1], true, false, ref crashed, AngleModel.max_slide_angle_global);
            }
            st++;

            if (state[st].pieceIndex != state[st - 1].pieceIndex) {
                if (state[st].pieceIndex == initialPiece) {
                    finished = true;
                }
            }
        }
        return st;
    }

    public static int GetStreakAfter(ref State state) { //Return the streak of the next point recorded
        if (streaks == null) return 0;

        List<StreakAfterPosition> pieceStreaks = streaks[state.pieceIndex];
        if (pieceStreaks.Count == 0) return 0;

        if (state.inPieceDistance > pieceStreaks[pieceStreaks.Count - 1].inPieceDistance) {
            List<StreakAfterPosition> pieceStreaksAfter = streaks[Model.GetNextPieceIndex(state.pieceIndex)];
            if (pieceStreaksAfter.Count == 0) return 0;
            return pieceStreaksAfter[0].streak;
        }

        int streak = 0;
        for (int i = 0; i < pieceStreaks.Count; i++) {
            if (pieceStreaks[i].inPieceDistance > state.inPieceDistance) {
                streak = pieceStreaks[i].streak;
                break;
            }
        }
        return streak;
    }

    public static double GetMaxSpeedOnPiece(int pieceIndex) {
        double max_possible = SpeedModel.c_throttle / (1 - SpeedModel.c_ds);
        if (streaks == null) {
            return max_possible;
        }

        List<StreakAfterPosition> pieceStreaks = streaks[pieceIndex];
        if (pieceStreaks.Count == 0) return max_possible;

        double ds = 0;
        for (int i = 0; i < pieceStreaks.Count; i++) {
            if (pieceStreaks[i].ds > ds) {
                ds = pieceStreaks[i].ds;
            } 
        }
        return ds;
    }
}


public struct StreakAfterPosition {
    public double inPieceDistance;
    public int streak;
    public double ds;

    public StreakAfterPosition(double _inPieceDistance, int _streak, double ds) {
        this.inPieceDistance = _inPieceDistance;
        this.streak = _streak;
        this.ds = ds;
    }
}