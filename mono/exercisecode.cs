using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using Newtonsoft.Json;
using System.Diagnostics;

public static class ExeciseCode {

    static string putTogether;

    public static void ExerciseCode() {
        List<TrackPieceData> trackPieces = new List<TrackPieceData>();

        TrackPieceData p1 = new TrackPieceData();
        p1.length = 103;
        p1.straight = true;
        p1.hasSwitch = false;

        TrackPieceData p2 = new TrackPieceData();
        p2.length = 103;
        p2.straight = true;
        p2.hasSwitch = false;

        TrackPieceData p3 = new TrackPieceData();
        p3.length = 103;
        p3.straight = true;
        p3.hasSwitch = true;

        TrackPieceData p4 = new TrackPieceData();
        p4.length = 103;
        p4.straight = true;
        p4.hasSwitch = false;

        TrackPieceData p5 = new TrackPieceData();
        p5.length = 103;
        p5.straight = true;
        p5.hasSwitch = false;

        TrackPieceData p6 = new TrackPieceData();
        p6.straight = false;
        p6.radius = 51;
        p6.angle = 47;
        p6.hasSwitch = true;

        TrackPieceData p7 = new TrackPieceData();
        p7.straight = false;
        p7.radius = 51;
        p7.angle = 47;
        p7.hasSwitch = false;

        for (int i = 0; i < 8; i++) {
            trackPieces.Add(p1);
            trackPieces.Add(p2);
            trackPieces.Add(p3);
            trackPieces.Add(p4);
            trackPieces.Add(p5);
            trackPieces.Add(p6);
            trackPieces.Add(p7);
        }
        // 56 pieces total

        List<LaneData> lanes = new List<LaneData>();

        LaneData lane1 = new LaneData();
        lane1.distanceFromCenter = -10;
        lane1.index = 0;
        lanes.Add(lane1);

        LaneData lane2 = new LaneData();
        lane2.distanceFromCenter = 10;
        lane2.index = 1;
        lanes.Add(lane2);

        RaceSessionData raceSessionData = new RaceSessionData();
        raceSessionData.maxLapTimeMs = 45678;
        raceSessionData.laps = 7;
        raceSessionData.quickRace = true;

        List<CarData> cars = new List<CarData>();
        CarData you = new CarData();
        you.color = "asdf";
        you.name = "name12345";
        cars.Add(you);

        CarData other = new CarData();
        other.color = "zxcv";
        other.name = "name54321";
        cars.Add(other);

        YourCarData yourCarData = new YourCarData();
        yourCarData.color = you.color;
        yourCarData.name = you.name;

        List<CarPositionData> carPositions = new List<CarPositionData>();
        CarPositionData yourPosition = new CarPositionData();
        yourPosition.color = you.color;
        yourPosition.name = you.name;
        yourPosition.inPieceDistance = 0;
        yourPosition.pieceIndex = 0;
        yourPosition.startLaneIndex = 0;
        yourPosition.endLaneIndex = 0;
        yourPosition.angle = 0;
        yourPosition.gameTick = 0;
        yourPosition.previousGameTick = -1;
        yourPosition.lap = 0;
        carPositions.Add(yourPosition);

        CarPositionData otherPosition = new CarPositionData();
        otherPosition.color = other.color;
        otherPosition.name = other.name;
        otherPosition.inPieceDistance = 0;
        otherPosition.pieceIndex = 0;
        otherPosition.startLaneIndex = 1;
        otherPosition.endLaneIndex = 1;
        otherPosition.angle = 0;
        otherPosition.gameTick = 0;
        otherPosition.previousGameTick = -1;
        otherPosition.lap = 0;
        carPositions.Add(otherPosition);

        Model.UpdateYourCar(yourCarData);
        Model.GameInit("warmup", "warmup_ci", trackPieces, lanes, raceSessionData, cars);
        Model.UpdateCarPositions(carPositions);
        Controller.Update();
        string exerciseThrotle = new Throttle(Controller.GetThrottle(), 0).Serialize();
        string exerciseSwitch = new SwitchLane(1, 0).Serialize();
        string exerciseTurbo = new Turbo(0).Serialize();
        putTogether = exerciseThrotle + exerciseSwitch + exerciseTurbo;
        TurboSimulator.Simulate();

        Model.qualify = true;
        Model.raceStarted = false;
        Model.timesGameInitCalled = 0;
        Model.you = null;
        Model.others = new Dictionary<string, Car>();
        TurboSimulator.bestPieceForTurbo = 0;
        TurboSimulator.hasStateAfterFirstLap = false;
        TurboSimulator.hasSimulatedGoodLap = false;

        TrackModel.firstTime = true;
        SpeedModel.isEstimated = false;
        SpeedModel.isFirstDataValid = false;
        SpeedModel.SetFixedDefaultConstants();

        AngleModel.c_stop_ds_on_piece_estimated = false;
        AngleModel.isEstimated = false;
        AngleModel.isEstimated1 = false;
        AngleModel.isEstimated2 = false;
        AngleModel.isFirstDataValid1 = false;
        AngleModel.isFirstDataValid2 = false;
        AngleModel.inList2 = true;
        AngleModel.gameticks2 = new List<int>();
        AngleModel.gameticksRadius = new List<int>();
        AngleModel.minTrackRadius = 2000000000.0;
        AngleModel.SetFixedDefaultConstants();
    }
}