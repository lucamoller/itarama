using System;
using System.Collections.Generic;
using System.Linq;


public static class AngleModel {
    //da[t + 1] =  c_ds * ds[t] * a[t] + c_da * da[t]
    public static double minTrackRadius = 2000000000.0;
    public static double c_stop_simulation_ds_max;
    public static double c_stop_simulation_da;

    public static bool c_stop_ds_on_piece_estimated = false;
    public static double[] c_stop_ds_on_piece_1;
    public static double[] c_stop_ds_on_piece_1_5;
    public static double[] c_stop_ds_on_piece_2;
    public static double[] c_stop_ds_on_piece_3;
    public static double[] c_stop_max_ds_used;

    public static double maxAngleSeen = 1.0;

    public static double c_ds;
    public static double c_da;
    public static double c_extra;
    public static double c_tracao;
    public static double sqrt_inv_c_tracao;

    public static double max_slide_angle_global = 30;
    public static double after_estimation_max_slide_angle = 59.9998;

    public static bool isEstimated = false;
    public static int estimatedTick = 0;
    public static bool isEstimated1 = false;
    public static bool isEstimated2 = false;

    public static bool isFirstDataValid1 = false;
    public static bool isFirstDataValid2 = false;
    public static bool inList2 = true;

    public static List<int> gameticks2 = new List<int>();

    public static List<int> gameticksRadius = new List<int>();

    private static double dar1, dar2, dsa1, dsa2, da1, da2;
    private static double diff1, diff2, invR1, invR2, ds1, ds2;

    public static void UpdateSimulationStopConstants() {
        c_stop_simulation_da = 1.0 / (1.0 - c_da);
        c_stop_simulation_ds_max = Math.Sqrt(minTrackRadius * c_tracao);
        Console.WriteLine("c_stop_simulation_ds_max: {0}, c_stop_simulation_da: {1}", c_stop_simulation_ds_max, c_stop_simulation_da);
    }

    public static void SetFixedDefaultConstants() {
        c_da = 0.9;
        c_ds = -0.00125;
        c_extra = 0.3;
        //c_tracao = 0.32;
        c_tracao = 0.25;
        if (Flags.prost) {
            c_tracao = 0.32;
            max_slide_angle_global = after_estimation_max_slide_angle;
        }
        sqrt_inv_c_tracao = Math.Sqrt(1 / c_tracao);
    }

    public static void Increment() {
        if (isEstimated) return;

        if (
            !isEstimated2 &&
            Model.you.lastGameTick > 0 &&
            Model.you.states[Model.you.lastGameTick].isActive &&
            Model.you.states[Model.you.lastGameTick - 1].isActive &&
            Model.you.states[Model.you.lastGameTick].startLaneIndex == Model.you.states[Model.you.lastGameTick].endLaneIndex &&
            Model.you.states[Model.you.lastGameTick - 1].startLaneIndex == Model.you.states[Model.you.lastGameTick - 1].endLaneIndex &&
            Model.you.states[Model.you.lastGameTick].a == 0 &&
            !Model.trackPieces[Model.you.states[Model.you.lastGameTick - 1].pieceIndex].straight
            ) {
                double implicit_c_tracao = Model.you.states[Model.you.lastGameTick - 1].ds * Model.you.states[Model.you.lastGameTick - 1].ds * Math.Abs(Model.lanePieces[Model.you.states[Model.you.lastGameTick - 1].pieceIndex, Model.you.states[Model.you.lastGameTick - 1].startLaneIndex].invRadius);
            if (implicit_c_tracao > c_tracao) {
                c_tracao = implicit_c_tracao;
                Console.WriteLine("Mudando c_tracao: " + c_tracao.ToString());
            }
        }

        Increment1();
        Increment2();

        isEstimated = (isEstimated1 && isEstimated2);
        estimatedTick = Model.gameTick;
    }

    public static void Increment1() {
        if (isEstimated1) {
            return;
        }
        if (
            Model.you.lastGameTick > 0 &&
            Model.you.states[Model.you.lastGameTick].isActive &&
            Model.you.states[Model.you.lastGameTick - 1].isActive &&
            Model.trackPieces[Model.you.states[Model.you.lastGameTick - 1].pieceIndex].straight &&
            Math.Abs(Model.you.states[Model.you.lastGameTick].da) > 1e-6
            ) {
            dar2 = dar1;
            dsa2 = dsa1;
            da2 = da1;

            dar1 = Model.you.states[Model.you.lastGameTick].da;
            dsa1 = Model.you.states[Model.you.lastGameTick - 1].ds * Model.you.states[Model.you.lastGameTick - 1].a;
            da1 = Model.you.states[Model.you.lastGameTick - 1].da;
        }
        else {
            return;
        }

        if (!isFirstDataValid1) {
            isFirstDataValid1 = true;
            return;
        }
        else {
            Estimate1();
        }
    }

    public static void Increment2() {
        if (isEstimated2) {
            return;
        }

        if (inList2) {
            if (
                Model.you.lastGameTick > 0 &&
                Model.you.states[Model.you.lastGameTick].isActive &&
                Model.you.states[Model.you.lastGameTick - 1].isActive &&
                Model.you.states[Model.you.lastGameTick].startLaneIndex == Model.you.states[Model.you.lastGameTick].endLaneIndex &&
                Model.you.states[Model.you.lastGameTick - 1].startLaneIndex == Model.you.states[Model.you.lastGameTick - 1].endLaneIndex &&
                Math.Abs(Model.you.states[Model.you.lastGameTick].da) > 1e-6
                ) {
                gameticks2.Add(Model.you.lastGameTick);
            }

            if (isEstimated1) {
                int cont = 0;
                for (int i = 0; i < gameticks2.Count && !isEstimated2; i++) {
                    if (Math.Abs(c_da * Model.you.states[gameticks2[i] - 1].da + c_ds * Model.you.states[gameticks2[i] - 1].ds * Model.you.states[gameticks2[i] - 1].a - Model.you.states[gameticks2[i]].da) > 1e-6) {
                        cont++;
                        diff2 = diff1;
                        invR2 = invR1;
                        ds2 = ds1;

                        diff1 = Model.you.states[gameticks2[i]].da - (c_da * Model.you.states[gameticks2[i] - 1].da + c_ds * Model.you.states[gameticks2[i] - 1].ds * Model.you.states[gameticks2[i] - 1].a);
                        invR1 = Model.lanePieces[Model.you.states[gameticks2[i] - 1].pieceIndex, Model.you.states[gameticks2[i] - 1].startLaneIndex].invRadius;
                        ds1 = Model.you.states[gameticks2[i] - 1].ds;

                        cont++;
                        isFirstDataValid2 = true;

                        if (cont >= 2) {
                            Estimate2();
                        }
                    }
                }
                inList2 = false;
            }
        }
        else {
            if (
            Model.you.lastGameTick > 0 &&
            Model.you.states[Model.you.lastGameTick].isActive &&
            Model.you.states[Model.you.lastGameTick - 1].isActive &&
            Model.you.states[Model.you.lastGameTick].startLaneIndex == Model.you.states[Model.you.lastGameTick].endLaneIndex &&
            Model.you.states[Model.you.lastGameTick - 1].startLaneIndex == Model.you.states[Model.you.lastGameTick - 1].endLaneIndex &&
            Math.Abs(Model.you.states[Model.you.lastGameTick].da) > 1e-6 &&
            Math.Abs(c_da * Model.you.states[Model.you.lastGameTick - 1].da + c_ds * Model.you.states[Model.you.lastGameTick - 1].ds * Model.you.states[Model.you.lastGameTick - 1].a - Model.you.states[Model.you.lastGameTick].da) > 1e-6
            ) {
                diff2 = diff1;
                invR2 = invR1;
                ds2 = ds1;

                diff1 = Model.you.states[Model.you.lastGameTick].da - (c_da * Model.you.states[Model.you.lastGameTick - 1].da + c_ds * Model.you.states[Model.you.lastGameTick - 1].ds * Model.you.states[Model.you.lastGameTick - 1].a);
                invR1 = Model.lanePieces[Model.you.states[Model.you.lastGameTick - 1].pieceIndex, Model.you.states[Model.you.lastGameTick - 1].startLaneIndex].invRadius;
                ds1 = Model.you.states[Model.you.lastGameTick - 1].ds;
            }
            else {
                return;
            }

            if (!isFirstDataValid2) {
                isFirstDataValid2 = true;
                return;
            }
            else {
                Estimate2();
            }
        }

    }

    public static void Estimate1() {
        //dar1 =  c_ds * dsa1 + c_da * da1
        //dar2 =  c_ds * dsa2 + c_da * da2

        if (Math.Abs(dsa1 * da2 - dsa2 * da1) > 1e-6) {
            c_da = (dsa1 * dar2 - dsa2 * dar1) / (dsa1 * da2 - dsa2 * da1);
        }
        else {
            return;
        }

        if (Math.Abs(dsa2) > 1e-6) {
            c_ds = (dar2 - c_da * da2) / dsa2;
        }
        else if (Math.Abs(dsa1) > 1e-6) {
            c_ds = (dar1 - c_da * da1) / dsa1;
        }
        else {
            return;
        }

        isEstimated1 = true;
        Console.WriteLine("Angle model: c_da: " + c_da + ", c_ds: " + c_ds);
        return;
    }

    public static void Estimate2() {
        //aux1 = c_extra * sing(invR1) * ds1 * sqrt(ds1*ds1*abs(invR1)) * x - c_extra * sing(invR1) * ds1
        //aux2 = c_extra * sing(invR2) * ds2 * sqrt(ds1*ds1*abs(invR2)) * x - c_extra * sing(invR2) * ds2

        double prod;

        if (ds1 * ds2 * (Math.Sqrt(ds1 * ds1 * Math.Abs(invR1)) - Math.Sqrt(ds2 * ds2 * Math.Abs(invR2))) > 0.0001) {
            prod = Math.Sign(invR1) * Math.Sign(invR2) * (diff1 * Math.Sign(invR2) * ds2 - diff2 * Math.Sign(invR1) * ds1) / (ds1 * ds2 * (Math.Sqrt(ds1 * ds1 * Math.Abs(invR1)) - Math.Sqrt(ds2 * ds2 * Math.Abs(invR2))));
        }
        else {
            return;
        }

        if (Math.Abs(ds1) > 1e-6) {
            c_extra = (prod * Math.Sign(invR1) * ds1 * Math.Sqrt(ds1 * ds1 * Math.Abs(invR1)) - diff1) * Math.Sign(invR1) / ds1;
        }
        else if (Math.Abs(ds2) > 1e-6) {
            c_extra = (prod * Math.Sign(invR2) * ds2 * Math.Sqrt(ds2 * ds2 * Math.Abs(invR2)) - diff2) * Math.Sign(invR2) / ds2;
        }
        else {
            return;
        }
        c_tracao = 1.0 / Math.Pow(prod / c_extra, 2);
        sqrt_inv_c_tracao = Math.Sqrt(1 / c_tracao);

        isEstimated2 = true;
        max_slide_angle_global = after_estimation_max_slide_angle;
        Console.WriteLine("Angle model: c_extra: " + c_extra + ", c_tracao: " + c_tracao);
        ProccessRadius();
        UpdateSimulationStopConstants();
        return;
    }

    public static void ProccessRadius() {
        for (int i = 0; i < gameticksRadius.Count; i++) {
            int gt = gameticksRadius[i];
            double estimatedRadius = AngleModel.GetEquivalentRadius(ref Model.you.states[gt - 1], Model.you.states[gt].da);
            if (estimatedRadius > 0) {
                Model.UpdateCurveSwitchRadius(
                    Model.CurveSwitchIds[Model.you.states[gt - 1].pieceIndex, Model.you.states[gt - 1].startLaneIndex, Model.you.states[gt - 1].endLaneIndex],
                    Math.Abs(Model.you.states[gt - 1].inPieceDistance),
                    estimatedRadius,
                    false
                    );
            }
        }
    }

    public static double nextDa;
    public static double extraDa;
    public static double invRadius;
    public static double sign;
    public static double sqrtAbsInvRadius;

    public static double GetNextDa(ref State state, bool useDsEqMax) {
        double ds = state.ds;
        if (state.collidedFront || state.collidedBack) {
            if (useDsEqMax) {
                ds = state.dsEqMax;
            }
            else {
                ds = state.dsEqMin;
            }
        }

        nextDa = c_da * state.da + c_ds * ds * state.a;

        invRadius = Model.GetLanePieceInvRadius(ref state);
        sqrtAbsInvRadius = Model.GetLanePieceSqrtAbsInvRadius(ref state);
  
        if (invRadius != 0) {
            sign = 1.0;
            if (invRadius < 0) {
                sign = -1.0;
            }

            extraDa = c_extra * ds * (ds * sqrtAbsInvRadius * sqrt_inv_c_tracao - 1.0);
            if (extraDa > 0) {
                nextDa += sign * extraDa;
            }
        }

        return nextDa;
    }

    public static bool CheckModel() {
        double eps = 1e-6;
        double nextDa = GetNextDa(ref Model.you.states[Model.gameTick - 1], true);
        return Math.Abs(nextDa - Model.you.states[Model.gameTick].da) < eps;
    }

    //Retorno o modulo do raio ou -1 se nao for possivel encontrar
    public static double GetEquivalentRadius(ref State state, double observedDa) {
        double simpleDa = c_da * state.da + c_ds * state.ds * state.a;
        double extraDa = observedDa - simpleDa;

        if (Math.Abs(extraDa) < 1e-6 || Math.Abs(state.ds) < 1e-6) {
            return -1;
        }

        return state.ds * state.ds/Math.Pow((Math.Abs(extraDa/(c_extra*state.ds))+1.0),2)/c_tracao;
    }

    public static double GetCStopDs(ref State state) {
        if (!c_stop_ds_on_piece_estimated) return c_stop_simulation_ds_max;

        if (state.ds <= c_stop_max_ds_used[state.pieceIndex]) {
            return c_stop_ds_on_piece_1[state.pieceIndex];
        }

        // turbo e simulacoes diferentes
        if (state.ds <= 1.5 * c_stop_max_ds_used[state.pieceIndex]) {
            return c_stop_ds_on_piece_1_5[state.pieceIndex];
        }

        if (state.ds <= 2.0 * c_stop_max_ds_used[state.pieceIndex]) {
            return c_stop_ds_on_piece_2[state.pieceIndex];
        }

        if (state.ds <= 3.0 * c_stop_max_ds_used[state.pieceIndex]) {
            return c_stop_ds_on_piece_3[state.pieceIndex];
        }

        return c_stop_simulation_ds_max;
    }

    public static void EstimateCStopDs() {
        if (!TurboSimulator.hasSimulatedGoodLap) return;

        c_stop_ds_on_piece_1 = new double[Model.trackPieces.Length];
        c_stop_ds_on_piece_1_5 = new double[Model.trackPieces.Length];
        c_stop_ds_on_piece_2 = new double[Model.trackPieces.Length];
        c_stop_ds_on_piece_3 = new double[Model.trackPieces.Length];
        c_stop_max_ds_used = new double[Model.trackPieces.Length];

        for (int i = 0; i < Model.trackPieces.Length; i++) {
            c_stop_max_ds_used[i] = TurboSimulator.GetMaxSpeedOnPiece(i);
            c_stop_ds_on_piece_1[i] = EstimateSpecificCStopDs(i, c_stop_max_ds_used[i]);
            if (Flags.logcstopds) Console.WriteLine("c_stop_ds_on_piece_1[{0}]: {1}", i, c_stop_ds_on_piece_1[i]);

            c_stop_ds_on_piece_1_5[i] = EstimateSpecificCStopDs(i, 1.5 * c_stop_max_ds_used[i]);
            c_stop_ds_on_piece_2[i] = EstimateSpecificCStopDs(i, 2.0 * c_stop_max_ds_used[i]);
            c_stop_ds_on_piece_3[i] = EstimateSpecificCStopDs(i, 3.0 * c_stop_max_ds_used[i]);
        }

        c_stop_ds_on_piece_estimated = true;
    }

    private static double EstimateSpecificCStopDs(int pieceIndex, double ds) {
        double max_s = ds / (1 - SpeedModel.c_ds);
        max_s += GetMinPieceLength(pieceIndex); // folga por nao saber exatamente onde esta da peca

        if (Flags.logcstopds) Console.WriteLine("startPiece: {0}, ds: {1}", pieceIndex, ds);
        double max_abs_inv_radius = 0.0;
        while (max_s > 0) {
            max_s -= GetMinPieceLength(pieceIndex);
            max_abs_inv_radius = Math.Max(max_abs_inv_radius, GetMaxPieceAbsInvRadius(pieceIndex));
            pieceIndex = Model.GetNextPieceIndex(pieceIndex);
        }
        if (Flags.logcstopds) Console.WriteLine("endPiece: {0}", pieceIndex);

        if (max_abs_inv_radius == 0) return 2000000000.0;

        return Math.Sqrt(c_tracao / max_abs_inv_radius);
    }

    private static double GetMinPieceLength(int pieceIndex) {
        double unused = 0;
        double result = 2000000000;
        for (int i = 0; i < Model.lanes.Length; i++) {
            for (int j = 0; j < Model.lanes.Length; j++) {
                if (Math.Abs(i - j) > 1) continue;
                if (i != j && !Model.trackPieces[pieceIndex].hasSwitch) continue;
                result = Math.Min(result, Model.GetLanePieceTotalLength(pieceIndex, i, j, ref unused));
            }
        }
        return result;
    }

    private static double GetMaxPieceAbsInvRadius(int pieceIndex) {
        double unused = 0;
        double result = 0;
        for (int i = 0; i < Model.lanes.Length; i++) {
            for (int j = 0; j < Model.lanes.Length; j++) {
                if (Math.Abs(i - j) > 1) continue;
                if (i != j && !Model.trackPieces[pieceIndex].hasSwitch) continue;
                result = Math.Max(result, Math.Abs(Model.GetLanePieceInvRadius(pieceIndex, 0.0, i, j, ref unused)));
            }
        }
        return result;
    }
}