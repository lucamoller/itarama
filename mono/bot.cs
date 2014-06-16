using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Reflection;

public class Bot {

    public static void Main(string[] args) {
        PreJIT();
        string host = args[0];
        int port = int.Parse(args[1]);
        string botName = args[2];
        string botKey = args[3];

        Flags.Init(args);
        botName = Flags.GetBotName(botName);

        Console.WriteLine("Connecting to " + host + ":" + port + " as " + botName + "/" + botKey);

        using (TcpClient client = new TcpClient(host, port)) {
            client.NoDelay = true;
            NetworkStream stream = client.GetStream();
            StreamReader reader = new StreamReader(stream);
            StreamWriter writer = new StreamWriter(stream);
            writer.AutoFlush = true;

            if (Flags.galera) {
                CreateRace startMsg = new CreateRace(botName, botKey, Flags.track, "xupadawan", 4);
                Console.WriteLine(startMsg.Serialize());
                new Bot(reader, writer, startMsg);
            }
            if (Flags.create) {
                CreateRace startMsg = new CreateRace(botName, botKey, Flags.track, "testepass", 6);
                Console.WriteLine(startMsg.Serialize());
                new Bot(reader, writer, startMsg);
            }
            else if (Flags.join) {
                JoinRace startMsg = new JoinRace(botName, botKey, Flags.track, "testepass", 6);
                Console.WriteLine(startMsg.Serialize());
                new Bot(reader, writer, startMsg);
            }
            else if (Flags.local) {
                CreateRace startMsg = new CreateRace(botName, botKey, Flags.track, "testepass", 1);
                Console.WriteLine(startMsg.Serialize());
                new Bot(reader, writer, startMsg);
            }
            else {
                Join startMsg = new Join(botName, botKey);
                Console.WriteLine(startMsg.Serialize());
                new Bot(reader, writer, startMsg);
            }
        }
    }

    static void PreJIT() {
        foreach (var type in Assembly.GetExecutingAssembly().GetTypes()) {
            foreach (var method in type.GetMethods(BindingFlags.DeclaredOnly |
                                BindingFlags.NonPublic |
                                BindingFlags.Public | BindingFlags.Instance |
                                BindingFlags.Static)) {
                try {
                    System.Runtime.CompilerServices.RuntimeHelpers.PrepareMethod(method.MethodHandle);
                } catch { }
            }
        }
    }

    private StreamWriter writer;

    private Parser parser;
    private Logger logger;

    public void Run() {
        if (Model.gameTick % 100 == 0) {
            Console.WriteLine("gameTick " + Model.gameTick);
        }
        //send(new Throttle(0.5, Model.gameTick));
        //return;

        if (!Model.you.HasFinished()) {

            Controller.Update();

            if (Controller.GetTurbo()) {
                Turbo turbo = new Turbo(Model.gameTick);
                send(turbo);
                Console.WriteLine("Turbo!!! " + turbo.Serialize());
            }
            else {
                int switchLane = Controller.GetSwitch();
                if (switchLane == 1 || switchLane == -1) {
                    SwitchLane switchLaneMsg = new SwitchLane(switchLane, Model.gameTick);
                    Console.WriteLine("SwitchLane: " + switchLaneMsg.Serialize());
                    send(switchLaneMsg);
                }
                else {
                    send(new Throttle(Controller.GetThrottle(), Model.gameTick));
                }
            }
        }
    }

    Bot(StreamReader reader, StreamWriter writer, OutputMessage join) {
        this.writer = writer;
        string line;

        logger = new Logger();
        parser = new Parser();
        parser.SetLogger(logger);
        SpeedModel.SetFixedDefaultConstants();
        AngleModel.SetFixedDefaultConstants();

        ExeciseCode.ExerciseCode();

        send(join);


        Timer.waitingReceive.Start();
        
        while ((line = reader.ReadLine()) != null) {
            Timer.waitingReceive.Stop();
            Timer.parseUpdate.Start();

            string msgType = parser.Read(line);
            logger.LogInput(line);
            
            switch (msgType) {
                case "carPositions":
                    if (Model.raceStarted && Model.currentMsgHasGametick) {
                        Run();
                    }
                    break;
                case "join":
                    break;
                case "crash":
                    break;
                case "gameInit":
                    Console.WriteLine("Race init");
                    break;
                case "gameEnd":
                    Console.WriteLine("Race ended");
                    break;
                case "gameStart":
                    Console.WriteLine("Race starts");
                    Model.SetRaceStarted();
                    Run();
                    break;
                default:
                    break;
            }

            if (Flags.stopontick) {
                if (Model.gameTick == Flags.stopontickvalue) break;
            }

            Timer.parseUpdate.Stop();
            Timer.operationsAfterSend.Start();

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

            if (Controller.runSearchNextBestThrotle && Model.raceStarted && !Model.you.HasFinished() && Model.you.isActive && 
                (Model.gameTick == 0 || (Model.gameTick > 0 && !Model.you.states[Model.gameTick - 1].gametickLost))) {
                ThrottleController.SearchNextBestThrottle();
                Controller.runSearchNextBestThrotle = false;
            }

            Timer.operationsAfterSend.Stop();
            Timer.waitingReceive.Start();
        }

        Statistics.PrintFinalResults();

        logger.Flush();
    }

    private void send(OutputMessage msg) {
        string line = msg.Serialize();
        logger.LogOuput(line);
        writer.WriteLine(line);
        // writer.Flush();
    }

   
}


