using System;
using System.Collections.Generic;
using System.Linq;


public abstract class Strategy {

    public abstract double GetNextThrottle(ref State state, double max_angle);

    public virtual void Reset() { }

}

public class S1WhenPossible : Strategy {

    public override double GetNextThrottle(ref State state, double max_angle) {
        State newState = new State();
        bool crashed = false;
        Simulator.SimulateSingleNextState(1.0, ref state, ref newState, true, true, ref crashed, max_angle);
        //if (crashed || SurvivalSimulator.WillCrashZeroThrottle(newState, true, max_angle)) {
        if (crashed || SurvivalSimulator.WillCrash(newState, true, true, true, max_angle)) {
            return 0.0;
        }
        return 1.0;
    }
}

public class S1WhenPossibleWithMemory : Strategy {
    List<double> memory = new List<double>();
    int ticks = 0;
    S1WhenPossible s1WhenPossible = new S1WhenPossible();

    public override void Reset() {
        ticks = 0;
    }

    public override double GetNextThrottle(ref State state, double max_angle) {
        ticks++;

        if (ticks - 1 < memory.Count) {
            return memory[ticks - 1];
        }

        double newMemory = s1WhenPossible.GetNextThrottle(ref state, max_angle);
        memory.Add(newMemory);
        return newMemory;
    }
}

public class S0 : Strategy {

    public override double GetNextThrottle(ref State state, double max_angle) {
        return 0.0;
    }
}

public class S1 : Strategy {

    public override double GetNextThrottle(ref State state, double max_angle) {
        return 1.0;
    }
}


public class SMixed : Strategy {
    public Strategy firstStrategy;
    public Strategy secondStrategy;
    public int useFirstDuringTicks;
    public int ticksUsed;

    public SMixed(Strategy firstStrategy, Strategy secondStrategy, int useFirstDuringTicks) {
        this.firstStrategy = firstStrategy;
        this.secondStrategy = secondStrategy;
        this.useFirstDuringTicks = useFirstDuringTicks;
    }

    public override void Reset() {
        base.Reset();
        firstStrategy.Reset();
        secondStrategy.Reset();
        ticksUsed = 0;
    }

    public override double GetNextThrottle(ref State state, double max_angle) {
        if (ticksUsed < useFirstDuringTicks) {
            ticksUsed++;
            return firstStrategy.GetNextThrottle(ref state, max_angle);
        }
        return secondStrategy.GetNextThrottle(ref state, max_angle);
    }
}

