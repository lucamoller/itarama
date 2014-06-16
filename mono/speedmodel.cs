using System;
using System.Collections.Generic;
using System.Linq;


public static class SpeedModel {
    //ds[t + 1] =  c_throttle * throttle[t] + c_ds * ds[t]
    public static double c_ds;
    public static double c_throttle;
    public static double c_collision_back = 0.8;
    public static double c_collision_front = 0.9;
    public static bool isEstimated = false;
    public static bool isFirstDataValid = false;

    private static double dsr1, dsr2, ds1, ds2, t1, t2, tf1, tf2;

    public static void SetFixedDefaultConstants() {
        c_ds = 0.98;
        c_throttle = 0.2;
    }

    public static void Increment() {
        if (isEstimated) {
            return;
        }

        if (
            Model.you.lastGameTick > 0 &&
            Model.you.states[Model.you.lastGameTick].isActive &&
            Model.you.states[Model.you.lastGameTick - 1].isActive &&
            Model.you.states[Model.you.lastGameTick].startLaneIndex == Model.you.states[Model.you.lastGameTick].endLaneIndex &&
            Model.you.states[Model.you.lastGameTick - 1].startLaneIndex == Model.you.states[Model.you.lastGameTick - 1].endLaneIndex &&
            Model.you.states[Model.you.lastGameTick].ds > 1e-6
            ) {
            ds2 = ds1;
            t2 = t1;
            dsr2 = dsr1;
            tf2 = tf1;

            dsr1 = Model.you.states[Model.you.lastGameTick].ds;
            t1 = Model.you.states[Model.you.lastGameTick - 1].throttle;
            ds1 = Model.you.states[Model.you.lastGameTick - 1].ds;
            tf1 = Model.you.states[Model.you.lastGameTick - 1].turboFactor;
        }
        else {
            return;
        }

        if (!isFirstDataValid) {
            isFirstDataValid = true;
            return;
        }
        else {
            Estimate();
        }
    }

    public static void Estimate() {
        if (Math.Abs(tf1 * t1 * ds2 - tf2 * t2 * ds1) > 1e-6) {
            c_ds = (tf1 * t1 * dsr2 - tf2 * t2 * dsr1) / (tf1 * t1 * ds2 - tf2 * t2 * ds1);
        }
        else {
            return;
        }

        if (Math.Abs(t2) > 1e-6) {
            c_throttle = (dsr2 - c_ds * ds2) / (tf2 * t2);
        }
        else if (Math.Abs(t1) > 1e-6) {
            c_throttle = (dsr1 - c_ds * ds1) / (tf1 * t1);
        }
        else {
            return;
        }

        isEstimated = true;
        Console.WriteLine("Speed model: c_thottle: " + c_throttle + ", c_ds: " + c_ds);
        return;
    }

    public static double GetNextDs(ref State state, bool useDsEqMax) {
        double ds = state.ds;
        if (state.collidedFront || state.collidedBack) {
            if (useDsEqMax) {
                ds = state.dsEqMax;
            } else {
                ds = state.dsEqMin;
            }
        }
        return c_ds * ds + c_throttle * state.throttle * state.turboFactor;
    }

    public static bool CheckModel() {
        double eps = 1e-6;
        double nextDs = GetNextDs(ref Model.you.states[Model.gameTick - 1], true);
        return Math.Abs(nextDs - Model.you.states[Model.gameTick].ds) < eps;
    }

    public static double GetDsEqBack(ref State stateOtherBefore, double otherThrottle) {
        return c_collision_back * (c_throttle * stateOtherBefore.turboFactor * otherThrottle + c_ds * stateOtherBefore.ds);
    }

    public static double GetDsEqFront(ref State stateOther) {
        return c_collision_front * stateOther.ds;
    }

    public static double GetDsEqBackSimulator(ref State stateOther) {
        return c_collision_back * stateOther.ds;
    }
}