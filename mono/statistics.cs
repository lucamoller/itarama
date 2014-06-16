using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using Newtonsoft.Json;
using System.Diagnostics;


public static class Statistics {

    public static List<int> lapTimes = new List<int>();
    public static List<int> crashes = new List<int>();

    public static int GetValueOrZero(Dictionary<int, int> dict, int key) {
        if (dict.ContainsKey(key)) {
            return dict[key];
        }
        return 0;
    }

    public static void PrintDistribution(Dictionary<int, int> dict, string name) {
        Console.WriteLine(" ==== Distribution ==== {0} ====:", name);
        List<int> sortedKeys = new List<int>(dict.Keys);
        sortedKeys.Sort();
        foreach (int key in sortedKeys) {
            Console.WriteLine("  {0}: {1}", key, dict[key]);
        }
    }

    public static void PrintFinalResults() {

        //PrintDistribution(Model.onesUsedToSave, "Ones Used to Save in WillCrash");
        //PrintDistribution(Model.diffsGoingBackToSave, "Diffs Going Back to Save in WillCrash");
        //PrintDistribution(Model.collisionTicks, "Ticks used in collision simulation");
        //PrintDistribution(Model.n1WpBeforeKamikaze, "Number of ticks using 1wp before kamikaze that succeed");

        Timer.waitingReceive.PrintResults();
        Timer.parseUpdate.PrintResults();
        Timer.operationsAfterSend.PrintResults();

        if (!Flags.local) {
            for (int i = 0; i < Model.lostTicks.Count; i++) {
                Console.WriteLine(" ==== Lost msg ====                 qualify: {1}, gameTick: {0} ", Model.lostTicks[i].tick, Model.lostTicks[i].qualify);
                Console.WriteLine("      ({0})[{1}]: {2}ms", Timer.parseUpdate.name, Model.lostTicks[i].tick - 1, Timer.parseUpdate.GetTimeFromTick(Model.lostTicks[i].tick - 1, Model.lostTicks[i].qualify));
                Console.WriteLine("      ({0})[{1}]: {2}ms", Timer.operationsAfterSend.name, Model.lostTicks[i].tick - 1, Timer.operationsAfterSend.GetTimeFromTick(Model.lostTicks[i].tick - 1, Model.lostTicks[i].qualify));
                Console.WriteLine("      ({0})[{1}]: {2}ms", Timer.waitingReceive.name, Model.lostTicks[i].tick - 1, Timer.waitingReceive.GetTimeFromTick(Model.lostTicks[i].tick - 1, Model.lostTicks[i].qualify));
                Console.WriteLine("      ({0})[{1}]: {2}ms", Timer.parseUpdate.name, Model.lostTicks[i].tick, Timer.parseUpdate.GetTimeFromTick(Model.lostTicks[i].tick, Model.lostTicks[i].qualify));
                Console.WriteLine("      ({0})[{1}]: {2}ms", Timer.operationsAfterSend.name, Model.lostTicks[i].tick, Timer.operationsAfterSend.GetTimeFromTick(Model.lostTicks[i].tick, Model.lostTicks[i].qualify));
                Console.WriteLine("      ({0})[{1}]: {2}ms", Timer.waitingReceive.name, Model.lostTicks[i].tick, Timer.waitingReceive.GetTimeFromTick(Model.lostTicks[i].tick, Model.lostTicks[i].qualify));
            }
            Console.WriteLine("Lost {0} ticks. Search novo", Model.lostTicks.Count);
        }

        Console.WriteLine("Lap times:");
        int totalTime = 0;
        for(int i = 0; i < lapTimes.Count; i++) {
            Console.WriteLine("  lap {0}: {1}ms", i, lapTimes[i]);
            totalTime += lapTimes[i];
        }
        Console.WriteLine("  total: {0}", totalTime);

        Console.WriteLine("Crashes: {0}", crashes.Count);
        for (int i = 0; i < crashes.Count; i++) {
            Console.WriteLine("  crashed on gameTick {0}", crashes[i]);
        }
    }
}


public class Timer {

    public static Timer parseUpdate = new Timer("Parse+Update+Send");
    public static Timer operationsAfterSend = new Timer("Operations After Send");
    public static Timer waitingReceive = new Timer("Waiting Receive");

    List<TimeMeasure> times = new List<TimeMeasure>();
    public string name;
    Stopwatch stopWatch = new Stopwatch();

    public Timer(string name) {
        this.name = name;
    }

    public void Start() {
        stopWatch.Reset();
        stopWatch.Start();
    }

    public void Stop() {
        stopWatch.Stop();
        if (Model.raceStarted) {
            double millisecondsTaken = stopWatch.Elapsed.TotalMilliseconds;
            times.Add(new TimeMeasure(millisecondsTaken, Model.gameTick, Model.qualify));
            //Console.WriteLine("Time elapsed ({0}): {1}", name, timeParseUpdateWatch.Elapsed);
        }
    }

    public void PrintResults() {
        double sum = 0;
        for (int i = 0; i < times.Count; i++) {
            sum += times[i].time;
        }
        Console.WriteLine(" ==== mean {0}: ({1})", name, sum / times.Count);

        times.Sort((x, y) => -x.time.CompareTo(y.time));
        for (int i = 0; i < 10 && i < times.Count; i++) {
            Console.WriteLine("      max_{0} ({1}): {2}, gameTick {3}, qualify: {4}", i, name, times[i].time, times[i].tick, times[i].qualify);
        }
    }

    public double GetTimeFromTick(int tick, bool qualify) {
        for (int i = 0; i < times.Count; i++) {
            if (times[i].tick == tick && times[i].qualify == qualify) {
                return times[i].time;
            }
        }
        return -1;
    }
}