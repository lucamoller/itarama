using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Diagnostics;

public class Replay {
    public static void Main(string[] args) {
        string logname = args[0];

        Flags.Init(args);

        new Replay(logname);
    }

    private Parser parser;

    List<string> inputLines = new List<string>();
    List<string> outputLines = new List<string>();
    int outputIndex = 0;

    public void Run() {
        if (Model.gameTick % 100 == 0) {
            Console.WriteLine("gameTick " + Model.gameTick);
        }

        if (!Model.you.HasFinished()) {
            //Console.WriteLine("{0}, {1}, {2}",
            //    Model.you.states[Model.you.lastGameTick].startLaneIndex,
            //    Model.you.states[Model.you.lastGameTick].endLaneIndex,
            //    Model.you.states[Model.you.lastGameTick].a);
            Controller.Update();

            if (Controller.GetTurbo()) {
                Console.WriteLine("REPLAY TRYING TO: " + new Turbo(Model.gameTick).Serialize());
            } else {
                int switchLane = Controller.GetSwitch();
                if (switchLane == 1 || switchLane == -1) {
                    SwitchLane switchLaneMsg = new SwitchLane(switchLane, Model.gameTick);
                    Console.WriteLine("REPLAY TRYING TO: " + switchLaneMsg.Serialize());
                }
            }

            JObject json = JObject.Parse(outputLines[outputIndex]);
            string msgType = json["msgType"].ToString();
            while ((msgType == "join" ||
                    msgType == "createRace" ||
                    msgType == "joinRace")
                    && outputIndex < outputLines.Count) {
                outputIndex++;
                json = JObject.Parse(outputLines[outputIndex]);
                msgType = json["msgType"].ToString();
            }

            if (msgType == "throttle") {
                Model.you.states[Model.gameTick].throttle = json["data"].ToObject<Double>();
            }
            if (msgType == "switchLane") {
                Console.WriteLine("SWITCHINLOG: SwitchLane: " + outputLines[outputIndex]);
                Model.you.RepeatThrottle();
            }
            if (msgType == "turbo") {
                Console.WriteLine("TURBOINLOG Turbo: " + outputLines[outputIndex]);
                Model.you.RepeatThrottle();
            }
        }

        if (AngleModel.isEstimated && !TurboSimulator.hasStateAfterFirstLap && Model.gameTick > AngleModel.estimatedTick + 2) {
            TurboSimulator.Simulate();
        }
        else if (TurboSimulator.hasStateAfterFirstLap && !TurboSimulator.hasSimulatedGoodLap) {
            TurboSimulator.Simulate();
        }
        else if (TurboSimulator.hasSimulatedGoodLap && !AngleModel.c_stop_ds_on_piece_estimated) {
            AngleModel.EstimateCStopDs();
        }
        else if (TrackModel.mustRefresh) {
            TrackModel.FindBestLanes();
            TrackModel.mustRefresh = false;
        }

        outputIndex++;
    }

    Replay(string logname) {
        Console.WriteLine("Reading from log: " + logname);

        parser = new Parser();
        SpeedModel.SetFixedDefaultConstants();
        AngleModel.SetFixedDefaultConstants();

        string line;

        System.IO.StreamReader file = new System.IO.StreamReader(logname);

        if (Flags.server) {
            while ((line = file.ReadLine()) != null) {
                if (line.StartsWith(Logger.INPUT_PREFIX)) {
                    inputLines.Add(line.Substring(Logger.INPUT_PREFIX.Length));
                }
                if (line.StartsWith(Logger.OUTPUT_PREFIX)) {
                    outputLines.Add(line.Substring(Logger.OUTPUT_PREFIX.Length));
                }
            }
        }
        else {
            while ((line = file.ReadLine()) != null) { // Lendo inputs
                if (line == Logger.SEPARATOR) {
                    break;
                }

                inputLines.Add(line);
            }
            while ((line = file.ReadLine()) != null) { // Lendo outpus
                outputLines.Add(line);
            }
        }

        file.Close();

        Console.WriteLine("Read " + inputLines.Count + " input lines and " + outputLines.Count + " output lines.");

        // Mudar para adicionar revezando
        foreach (string record in inputLines) {
            //Stopwatch stopWatch = new Stopwatch();
            //stopWatch.Start();

            string msgType = parser.Read(record);
            if (msgType == "carPositions") {
                if (Model.raceStarted) {
                    Run();
                }
            }
            if (msgType == "gameStart") {
                Model.SetRaceStarted();
                Run();
            }

            //stopWatch.Stop();
            //Console.WriteLine("Time elapsed: {0}", stopWatch.Elapsed);
        }

        //Console.WriteLine("gt: " + Model.gameTick);
        //for (int i = 0; i < Model.gameTick; i++) {
        //    Console.WriteLine(
        //        Model.gameTick + ";" +
        //        Model.you.states[i].pieceIndex + ";" +
        //        Model.you.states[i].startLaneIndex + ";" +
        //        Model.you.states[i].endLaneIndex + ";" +
        //        Model.you.states[i].inPieceDistance + ";" +

        //        Model.others.First().Value.states[i].pieceIndex + ";" +
        //        Model.others.First().Value.states[i].startLaneIndex + ";" +
        //        Model.others.First().Value.states[i].endLaneIndex + ";" +
        //        Model.others.First().Value.states[i].inPieceDistance
        //        );
        //}

        //Console.WriteLine(
        //    "total: " + (throttleOutput.Count + switchLaneOutput.Count) + ", "
        //    + throttleOutput.Count + " throttle outputs, " + switchLaneOutput.Count + " switchLane outputs.");

        // Imprimir algo sobre o modelo
        //model.PrintPositionHistory();
        //Model.PrintPieceRealSize();

        //int lastGameTick = 0;
        //double lastThrottle = 0;
        //foreach (string record in outputLines) {
        //    JObject json = JObject.Parse(record);
        //    string msgType = json["msgType"].ToString();

        //    lastGameTick = json["gameTick"].ToObject<int>();
        //    Console.WriteLine(lastGameTick);
        //    if (msgType == "throttle") {
        //        lastThrottle = json["data"].ToObject<Double>();
        //        Model.you.states[lastGameTick].throttle = lastThrottle;
        //    }
        //    else {
        //        Model.you.states[lastGameTick].throttle = lastThrottle;
        //    }
        //}

        //Console.WriteLine("pieceIndex;inPieceDistance;ds(t);GetNextDs(t-1);a(t);a(t-1)+GetNextDa(t-1);da(t);GetNextDa(t-1);invRadius(t);throttle(t-1);turboFactor(t-1);gameTick");
        //for (int i = 1; i < Model.gameTick; i++) {
        //    Console.WriteLine("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11}",
        //        Model.you.states[i].pieceIndex, Model.you.states[i].inPieceDistance,
        //        Model.you.states[i].ds, SpeedModel.GetNextDs(ref Model.you.states[i - 1], true),
        //        Model.you.states[i].a, Model.you.states[i - 1].a + AngleModel.GetNextDa(ref Model.you.states[i - 1], true),
        //        Model.you.states[i].da, AngleModel.GetNextDa(ref Model.you.states[i - 1], true),
        //        Model.GetLanePieceInvRadius(ref Model.you.states[i]),
        //        Model.you.states[i - 1].throttle, Model.you.states[i - 1].turboFactor, i);

        //    //Console.WriteLine("{0};{1};{2}", Model.you.states[i].startLaneIndex, Model.you.states[i].endLaneIndex, i);
        //}
    }

}