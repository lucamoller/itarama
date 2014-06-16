using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class Parser {
    private Logger logger;

    public Parser() {
        this.logger = null;
    }

    public string Read(string line) {
        JObject json = JObject.Parse(line);

        string msgType = json["msgType"].ToString();

        int gameTick = Model.gameTick;
        if (json["gameTick"] != null) {
            gameTick = json["gameTick"].ToObject<int>();
            Model.UpdateGameTick(gameTick);
            Model.currentMsgHasGametick = true;
        } else {
            Model.currentMsgHasGametick = false;
        }

        if (msgType == "yourCar") {
            YourCarData yourCar = new YourCarData(json["data"]);
            Model.UpdateYourCar(yourCar);
        }

        if (msgType == "gameInit") {
            JToken race = json["data"]["race"];

            JToken track = race["track"];
            string trackId = track["id"].ToString();
            string trackName = track["name"].ToString();
            if (logger != null) {
                logger.SetTrackId(trackId);
            }

            List<LaneData> laneList = new List<LaneData>();
            foreach (JToken lane in track["lanes"]) {
                laneList.Add(new LaneData(lane));
            }
            List<TrackPieceData> trackPieceList = new List<TrackPieceData>();
            foreach (JToken piece in track["pieces"]) {
                trackPieceList.Add(new TrackPieceData(piece));
            }

            RaceSessionData raceSessionData = new RaceSessionData(race["raceSession"]);

            List<CarData> carList = new List<CarData>();
            foreach (JToken car in race["cars"]) {
                carList.Add(new CarData(car));
            }

            Model.GameInit(trackId, trackName, trackPieceList, laneList, raceSessionData, carList);
        }

        if (msgType == "gameStart") {
            // Model.GameStart()
        }

        if (msgType == "carPositions") {
            List<CarPositionData> carPositionDataList = new List<CarPositionData>();
            foreach (JToken car in json["data"]) {
                CarPositionData carPosition = new CarPositionData(car, gameTick);
                carPositionDataList.Add(carPosition);
            }

            Model.UpdateCarPositions(carPositionDataList);
        }

        if (msgType == "crash") {
            CrashData crashData = new CrashData(json["data"], gameTick);
            Model.UpdateCrash(crashData);
        }

        if (msgType == "spawn") {
            SpawnData spawnData = new SpawnData(json["data"], gameTick);
            Model.UpdateSpawn(spawnData);
        }

        if (msgType == "finish") {
            FinishData finishData = new FinishData(json["data"], gameTick);
            Model.UpdateFinish(finishData);
        }

        if (msgType == "lapFinished") {
            LapFinishedData lapFinished = new LapFinishedData(json["data"]);
            Model.UpdateLapFinished(lapFinished);
        }

        if (msgType == "turboAvailable") {
            TurboAvailableData turboAvailableData = new TurboAvailableData(json["data"], gameTick);
            Model.UpdateTurboAvailable(turboAvailableData);
        }

        if (msgType == "turboStart") {
            TurboStartData turboStart = new TurboStartData(json["data"], gameTick);
            Model.UpdateTurboStart(turboStart);
        }

        if (msgType == "turboEnd") {
            TurboEndData turboEnd = new TurboEndData(json["data"], gameTick);
            Model.UpdateTurboEnd(turboEnd);
        }


        return msgType;
    }

    public void SetLogger(Logger logger) {
        this.logger = logger;
    }
}

public class YourCarData {
    public string name;
    public string color;

    public YourCarData(JToken data) {
        this.name = data["name"].ToString();
        this.color = data["color"].ToString();
    }

    public YourCarData() { }
}

public class TrackPieceData {
    public bool straight;

    public double length;
    public double radius;
    public double angle;
    public bool hasSwitch;

    public TrackPieceData(JToken piece) {
        if (piece["radius"] != null) {
            this.straight = false;
            this.radius = piece["radius"].ToObject<Double>();
            this.angle = piece["angle"].ToObject<Double>();

            if (this.angle < 0) {
                this.radius = -this.radius;
                this.angle = -this.angle;
            }
        }
        else {
            this.straight = true;
            this.length = piece["length"].ToObject<Double>();
        }

        if (piece["switch"] != null) {
            this.hasSwitch = piece["switch"].ToObject<bool>();
        }
        else {
            this.hasSwitch = false;
        }
    }

    public TrackPieceData() { }
}

public class LaneData {
    public double distanceFromCenter;
    public int index;

    public LaneData(JToken lane) {
        this.distanceFromCenter = lane["distanceFromCenter"].ToObject<Double>();
        this.index = lane["index"].ToObject<int>();
    }

    public LaneData() { }
}

public class RaceSessionData {
    public bool qualify;
    public int durationMs;
    public int laps;
    public int maxLapTimeMs;
    public bool quickRace;

    public RaceSessionData(JToken data) {
        laps = -1;
        if (data["durationMs"] != null) {
            qualify = true;
            durationMs = data["durationMs"].ToObject<int>();
        }
        else {
            qualify = false;
            laps = data["laps"].ToObject<int>();
            maxLapTimeMs = data["maxLapTimeMs"].ToObject<int>();
            quickRace = data["quickRace"].ToObject<bool>();
        }
    }

    public RaceSessionData() { }
}

public class CarData {
    public string name;
    public string color;
    public double length;
    public double width;
    public double guideFlagPosition;

    public CarData(JToken data) {
        this.name = data["id"]["name"].ToString();
        this.color = data["id"]["color"].ToString();
        this.length = data["dimensions"]["length"].ToObject<Double>();
        this.width = data["dimensions"]["width"].ToObject<Double>();
        this.guideFlagPosition = data["dimensions"]["guideFlagPosition"].ToObject<Double>();
    }

    public CarData() { }
}

public class CarPositionData {
    public string name;
    public string color;
    public double angle;
    public int pieceIndex;
    public double inPieceDistance;
    public int startLaneIndex;
    public int endLaneIndex;
    public int lap;
    public int gameTick;
    public int previousGameTick;

    public bool collidedBack;
    public bool collidedFront;
    public string colorOther;

    public CarPositionData(JToken car, int gameTick) {
        this.name = car["id"]["name"].ToString();
        this.color = car["id"]["color"].ToString();
        this.angle = car["angle"].ToObject<Double>();
        this.pieceIndex = car["piecePosition"]["pieceIndex"].ToObject<int>();
        this.inPieceDistance = car["piecePosition"]["inPieceDistance"].ToObject<Double>();
        this.startLaneIndex = car["piecePosition"]["lane"]["startLaneIndex"].ToObject<int>();
        this.endLaneIndex = car["piecePosition"]["lane"]["endLaneIndex"].ToObject<int>();
        this.lap = car["piecePosition"]["lap"].ToObject<int>();
        this.gameTick = gameTick;

        if (car["prevCommandTick"] != null) {
            this.previousGameTick = car["prevCommandTick"].ToObject<int>();
            //if (previousGameTick != gameTick -1) {
            //  Console.WriteLine("Previous gameTick {0}", previousGameTick);
            //}
        } else {
            this.previousGameTick = -1; //gameTick - 1;
        }

        this.collidedBack = false;
        this.collidedFront = false;
        this.colorOther = "";
    }

    public CarPositionData() { }
}

public class CrashData {
    public string name;
    public string color;
    public int gameTick;

    public CrashData(JToken data, int gameTick) {
        this.name = data["name"].ToString();
        this.color = data["color"].ToString();
        this.gameTick = gameTick;
    }
}

public class SpawnData {
    public string name;
    public string color;
    public int gameTick;

    public SpawnData(JToken data, int gameTick) {
        this.name = data["name"].ToString();
        this.color = data["color"].ToString();
        this.gameTick = gameTick;
    }
}

public class TurboAvailableData {
    public double turboDurationMilliseconds;
    public int turboDurationTicks;
    public double turboFactor;
    public int gameTick;


    public TurboAvailableData(JToken data, int gameTick) {
        this.turboDurationMilliseconds = data["turboDurationMilliseconds"].ToObject<double>();
        this.turboDurationTicks = data["turboDurationTicks"].ToObject<int>();
        this.turboFactor = data["turboFactor"].ToObject<double>();
        this.gameTick = gameTick;
    }
}

public class LapFinishedData {
    public string name;
    public string color;

    public int lapTimeLap;
    public int lapTimeTicks;
    public int lapTimeMillis;

    public int raceTimeLaps;
    public int raceTimeTicks;
    public int raceTimeMillis;

    public int rankingOverral;
    public int rankingFastestLap;

    public LapFinishedData(JToken data) {
        this.name = data["car"]["name"].ToString();
        this.color = data["car"]["color"].ToString();

        this.lapTimeLap = data["lapTime"]["lap"].ToObject<int>();
        this.lapTimeTicks = data["lapTime"]["ticks"].ToObject<int>();
        this.lapTimeMillis = data["lapTime"]["millis"].ToObject<int>();

        this.raceTimeLaps = data["raceTime"]["laps"].ToObject<int>();
        this.raceTimeTicks = data["raceTime"]["ticks"].ToObject<int>();
        this.raceTimeMillis = data["raceTime"]["millis"].ToObject<int>();

        this.rankingOverral = data["ranking"]["overall"].ToObject<int>();
        this.rankingFastestLap = data["ranking"]["fastestLap"].ToObject<int>();
    }
}

public class FinishData {
    public string name;
    public string color;
    public int gameTick;

    public FinishData(JToken data, int gameTick) {
        this.name = data["name"].ToString();
        this.color = data["color"].ToString();
        this.gameTick = gameTick;
    }
}

public class TurboStartData {
    public string name;
    public string color;
    public int gameTick;

    public TurboStartData(JToken data, int gameTick) {
        this.name = data["name"].ToString();
        this.color = data["color"].ToString();
        this.gameTick = gameTick;
    }
}

public class TurboEndData {
    public string name;
    public string color;
    public int gameTick;

    public TurboEndData(JToken data, int gameTick) {
        this.name = data["name"].ToString();
        this.color = data["color"].ToString();
        this.gameTick = gameTick;
    }
}
