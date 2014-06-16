using System;
using System.Collections.Generic;
using System.Linq;


public static class Model {
    public static List<TimeMeasure> lostTicks = new List<TimeMeasure>();

    public static double minDirtySafetyAngleMargin = 0.5;
    public static double maxDirtySafetyAngleMargin = 5.0;

    //public static Dictionary<int, int> onesUsedToSave = new Dictionary<int, int>();
    //public static Dictionary<int, int> diffsGoingBackToSave = new Dictionary<int, int>();

    //public static Dictionary<int, int> collisionTicks = new Dictionary<int, int>();
    //public static Dictionary<int, int> n1WpBeforeKamikaze = new Dictionary<int, int>();

    public static int gameTick = 0;
    public static bool raceStarted = false;
    public static bool qualify = true;
    public static int timesGameInitCalled = 0;
    public static bool currentMsgHasGametick = false;

    public static int spawnTime = 400;
  
    public static YourCarData yourCar;

    public static Car you;
    public static Dictionary<string /*color*/, Car> others = new Dictionary<string, Car>();

    public static string trackId;
    public static string trackName;

    public static RaceSessionData raceSessionData;
    public static TrackPieceData[] trackPieces;
    public static LaneData[] lanes;
    public static LanePiece[,] lanePieces;
    public static Double turboFactor;
    public static int turboDuration;

    public static int[, ,] LinearSwitchIds;
    public static double propLinear = 1.0;
    public static LinearSwitchSize[] linearSwitchSizes;

    public static int[, ,] CurveSwitchIds;
    public static double propCurve = 1.0;
    public static CurveSwitchSize[] curveSwitchSizes;

    public static CurveSwitchRadius[] curveSwitchRadius;

    public static List<LapFinishedData> laps = new List<LapFinishedData>();

    public static void SetRaceStarted() {
        raceStarted = true;
    }

    public static void GameInit(string _trackId, string _trackName, List<TrackPieceData> trackPieceList,
                         List<LaneData> laneList, RaceSessionData _raceSessionData, List<CarData> carList) {
        timesGameInitCalled++;
        if (timesGameInitCalled == 2) {
            qualify = false;
        }

        if (Flags.skipqualify) {
            qualify = false;
        }

        gameTick = 0;
        raceStarted = false;
        trackId = _trackId;
        trackName = _trackName;
        raceSessionData = _raceSessionData;

        if (qualify || Flags.skipqualify) {
            trackPieces = new TrackPieceData[trackPieceList.Count];
            lanes = new LaneData[laneList.Count];
            lanePieces = new LanePiece[trackPieceList.Count, laneList.Count];
            LinearSwitchIds = new int[trackPieceList.Count, laneList.Count, laneList.Count];
            CurveSwitchIds = new int[trackPieceList.Count, laneList.Count, laneList.Count];

            for (int j = 0; j < laneList.Count; j++) {
                lanes[j] = laneList[j];
            }

            for (int i = 0; i < trackPieceList.Count; i++) {
                trackPieces[i] = trackPieceList[i];
                for (int j = 0; j < laneList.Count; j++) {
                    lanePieces[i, j] = new LanePiece(trackPieces[i], lanes[j]);
                }

                if (Flags.logtrack) { Console.WriteLine("pieceIndex: {0}, straight: {1}, switch: {2}", i, trackPieces[i].straight, trackPieces[i].hasSwitch); }
            }

            double length = 0;
            double width = 0;
            int id = -1;

            List<LinearSwitchSize> linearSwitchSizesAux = new List<LinearSwitchSize>();
            for (int i = 0; i < trackPieceList.Count; i++) {
                if (trackPieces[i].straight && trackPieces[i].hasSwitch) {
                    for (int j = 0; j < laneList.Count - 1; j++) {
                        length = Math.Abs(trackPieces[i].length);
                        width = Math.Abs(lanes[j].distanceFromCenter - lanes[j + 1].distanceFromCenter);
                        id = -1;
                        for (int k = 0; k < linearSwitchSizesAux.Count; k++) {
                            if (Math.Abs(linearSwitchSizesAux[k].length - length) < 1e-6 && Math.Abs(linearSwitchSizesAux[k].width - width) < 1e-6) {
                                id = k;
                                break;
                            }
                        }
                        if (id < 0) {
                            linearSwitchSizesAux.Add(new LinearSwitchSize(length, width, -Math.Sqrt(length * length + width * width)));
                            LinearSwitchIds[i, j, j + 1] = linearSwitchSizesAux.Count - 1;
                            LinearSwitchIds[i, j + 1, j] = linearSwitchSizesAux.Count - 1;
                        }
                        else {
                            LinearSwitchIds[i, j, j + 1] = id;
                            LinearSwitchIds[i, j + 1, j] = id;
                        }
                    }
                }
            }

            linearSwitchSizes = new LinearSwitchSize[linearSwitchSizesAux.Count];
            for (int i = 0; i < linearSwitchSizesAux.Count; i++) {
                linearSwitchSizes[i] = linearSwitchSizesAux[i];
            }


            double absRIni = 0;
            double absRFim = 0;
            double absAng = 0;
            id = -1;

            List<CurveSwitchSize> curveSwitchSizesAux = new List<CurveSwitchSize>();
            for (int i = 0; i < trackPieceList.Count; i++) {
                if (!trackPieces[i].straight && trackPieces[i].hasSwitch) {
                    for (int j = 0; j < laneList.Count - 1; j++) {
                        absRIni = Math.Abs(lanePieces[i, j].radius);
                        absRFim = Math.Abs(lanePieces[i, j + 1].radius);
                        absAng = Math.Abs(trackPieces[i].angle);
                        id = -1;
                        for (int k = 0; k < curveSwitchSizesAux.Count; k++) {
                            if (Math.Abs(curveSwitchSizesAux[k].absRIni - absRIni) < 1e-6 && Math.Abs(curveSwitchSizesAux[k].absRFim - absRFim) < 1e-6 && Math.Abs(curveSwitchSizesAux[k].absAng - absAng) < 1e-6) {
                                id = k;
                                break;
                            }
                        }
                        if (id < 0) {
                            curveSwitchSizesAux.Add(new CurveSwitchSize(absRIni, absRFim, absAng, -(absAng / 180.0 * Math.PI * (absRIni + absRFim) / 2.0)));
                            CurveSwitchIds[i, j, j + 1] = curveSwitchSizesAux.Count - 1;
                        }
                        else {
                            CurveSwitchIds[i, j, j + 1] = id;
                        }
                    }

                    for (int j = 1; j < laneList.Count; j++) {
                        absRIni = Math.Abs(lanePieces[i, j].radius);
                        absRFim = Math.Abs(lanePieces[i, j - 1].radius);
                        absAng = Math.Abs(trackPieces[i].angle);
                        id = -1;
                        for (int k = 0; k < curveSwitchSizesAux.Count; k++) {
                            if (Math.Abs(curveSwitchSizesAux[k].absRIni - absRIni) < 1e-6 && Math.Abs(curveSwitchSizesAux[k].absRFim - absRFim) < 1e-6 && Math.Abs(curveSwitchSizesAux[k].absAng - absAng) < 1e-6) {
                                id = k;
                                break;
                            }
                        }
                        if (id < 0) {
                            curveSwitchSizesAux.Add(new CurveSwitchSize(absRIni, absRFim, absAng, -(absAng / 180.0 * Math.PI * (absRIni + absRFim) / 2.0)));
                            CurveSwitchIds[i, j, j - 1] = curveSwitchSizesAux.Count - 1;
                        }
                        else {
                            CurveSwitchIds[i, j, j - 1] = id;
                        }
                    }
                }
            }

            curveSwitchSizes = new CurveSwitchSize[curveSwitchSizesAux.Count];
            curveSwitchRadius = new CurveSwitchRadius[curveSwitchSizesAux.Count];
            for (int i = 0; i < curveSwitchSizesAux.Count; i++) {
                curveSwitchSizes[i] = curveSwitchSizesAux[i];
                curveSwitchRadius[i] = new CurveSwitchRadius(curveSwitchSizes[i].absRIni, curveSwitchSizes[i].absRFim, curveSwitchSizes[i].absAng);
                curveSwitchRadius[i].radius.Add(new CurveSwitchRadiusData(0, curveSwitchSizes[i].absRIni, false));
                curveSwitchRadius[i].radius.Add(new CurveSwitchRadiusData(Math.Abs(curveSwitchSizesAux[i].size), curveSwitchSizes[i].absRFim, true));
            }
        }

        foreach (CarData carData in carList) {
            if (carData.color == yourCar.color) {
                if (you == null) {
                    you = new Car(carData, trackPieceList.Count, null);
                }
                else {
                    you = new Car(carData, trackPieceList.Count, you.lastGametickPassedHere);
                }
                
            }
            else {
                if (!others.ContainsKey(carData.color)) {
                    others[carData.color] = new Car(carData, trackPieceList.Count, null);
                }
                else {
                    others[carData.color] = new Car(carData, trackPieceList.Count, others[carData.color].lastGametickPassedHere);
                }
                
            }
        }
    }

    public static void UpdateLinearSwitchSize(int id, double size) {
        size = Math.Abs(size);

        if (linearSwitchSizes[id].size >= 0) {
            if (Math.Abs(linearSwitchSizes[id].size - size) > 1e-6) {
                Console.WriteLine("Erro em linearSwitchSize: " + linearSwitchSizes[id].length + ";" + linearSwitchSizes[id].width + ";" + size + ";" + linearSwitchSizes[id].size);
            }
        }
        else {
            linearSwitchSizes[id].size = size;
            Console.WriteLine("Novo linearSwitchSize: " + linearSwitchSizes[id].length + ";" + linearSwitchSizes[id].width + ";" + size);
            int cont = 0;
            propLinear = 0;
            for (int i = 0; i < linearSwitchSizes.Length; i++) {
                if (linearSwitchSizes[i].size >= 0) {
                    propLinear += linearSwitchSizes[i].size / Math.Sqrt(linearSwitchSizes[i].length * linearSwitchSizes[i].length + linearSwitchSizes[i].width * linearSwitchSizes[i].width);
                    cont++;
                }
            }
            propLinear /= cont;
            TrackModel.mustRefresh = true;
        }
    }

    public static void UpdateCurveSwitchSize(int id, double size) {
        size = Math.Abs(size);

        if (curveSwitchSizes[id].size >= 0) {
            if (Math.Abs(curveSwitchSizes[id].size - size) > 1e-6) {
                Console.WriteLine("Erro em curveSwitchSize: " + curveSwitchSizes[id].absRIni + ";" + curveSwitchSizes[id].absRFim + ";" + curveSwitchSizes[id].absAng + ";" + size + ";" + curveSwitchSizes[id].size);
            }
        }
        else {
            curveSwitchSizes[id].size = size;
            Console.WriteLine("Novo curveSwitchSize: " + curveSwitchSizes[id].absRIni + ";" + curveSwitchSizes[id].absRFim + ";" + curveSwitchSizes[id].absAng + ";" + size);
            int cont = 0;
            propCurve = 0;
            for (int i = 0; i < curveSwitchSizes.Length; i++) {
                if (curveSwitchSizes[i].size >= 0) {
                    propCurve += curveSwitchSizes[i].size / (curveSwitchSizes[i].absAng / 180.0 * Math.PI * (curveSwitchSizes[i].absRIni + curveSwitchSizes[i].absRFim) / 2.0);
                    cont++;
                }
            }
            propCurve /= cont;
            Model.UpdateCurveSwitchRadius(id, curveSwitchSizes[id].size, curveSwitchSizes[id].absRFim, true);
            TrackModel.mustRefresh = true;
        }
    }

    public static void UpdateCurveSwitchRadius(int id, double inPieceDistace, double radius, bool deleteTemp) {
        radius = Math.Abs(radius);
        inPieceDistace = Math.Abs(inPieceDistace);
        
        if (deleteTemp) {
            bool achou = false;
            CurveSwitchRadiusData csrd = new CurveSwitchRadiusData();

            for (int j = curveSwitchRadius[id].radius.Count - 1; j >= 0 ; j--) {
                if (curveSwitchRadius[id].radius[j].temporario) {
                    csrd = curveSwitchRadius[id].radius[j];
                    achou = true;
                    break;
                }
            }
            if (achou) {
                curveSwitchRadius[id].radius.Remove(csrd);
            }
        }

        CurveSwitchRadiusData ncsrd = new CurveSwitchRadiusData(inPieceDistace,radius,false);
        int pos = curveSwitchRadius[id].radius.BinarySearch(ncsrd);

        if (pos >= 0) return;
        pos = ~pos;

        if (pos < curveSwitchRadius[id].radius.Count && Math.Abs(curveSwitchRadius[id].radius[pos].inPieceDistance - inPieceDistace) < 1e-6) {
            return;
        }
        if (pos > 0 && Math.Abs(curveSwitchRadius[id].radius[pos - 1].inPieceDistance - inPieceDistace) < 1e-6) {
            return;
        }

        curveSwitchRadius[id].radius.Insert(pos, ncsrd);

        if (pos == 1 && (curveSwitchRadius[id].radius[1].inPieceDistance - curveSwitchRadius[id].radius[0].inPieceDistance) < 0.05 * curveSwitchRadius[id].radius.Last().inPieceDistance) {
            curveSwitchRadius[id].radius[0] = new CurveSwitchRadiusData(
                curveSwitchRadius[id].radius[0].inPieceDistance,
                curveSwitchRadius[id].radius[1].radius,
                curveSwitchRadius[id].radius[0].temporario);
        }
        if (pos == curveSwitchRadius[id].radius.Count - 2 && (curveSwitchRadius[id].radius.Last().inPieceDistance - curveSwitchRadius[id].radius[pos].inPieceDistance) < 0.05 * curveSwitchRadius[id].radius.Last().inPieceDistance) {
            curveSwitchRadius[id].radius[pos + 1] = new CurveSwitchRadiusData(
                curveSwitchRadius[id].radius[pos + 1].inPieceDistance,
                curveSwitchRadius[id].radius[pos].radius,
                curveSwitchRadius[id].radius[pos + 1].temporario);
        }
    }

    public static void PrintCurveSwitchRadius(int id) {
        Console.WriteLine("* " + curveSwitchRadius[id].absRIni + ";" + curveSwitchRadius[id].absRFim + ";" + curveSwitchRadius[id].absAng);
        for (int j = 0; j < curveSwitchRadius[id].radius.Count; j++) {
            Console.WriteLine(curveSwitchRadius[id].radius[j].inPieceDistance + ";" + curveSwitchRadius[id].radius[j].radius);
        }
    }

    //public static void TestCollisionSamePiece(CarPositionData back, CarPositionData front) {
    //    double eps = 1e-6;
    //    if (back.startLaneIndex == front.startLaneIndex && back.endLaneIndex == front.endLaneIndex) {
    //        if (Math.Abs((front.inPieceDistance - back.inPieceDistance) - you.carData.length) < eps) {
    //            back.collidedBack = true;
    //            back.colorOther = front.color;
    //            front.collidedFront = true;
    //            front.colorOther = back.color;
    //        }
    //    }
    //}

    //public static void TestCollisionDifferentPiece(CarPositionData back, CarPositionData front) {
    //    double unused = 0;
        
    //    double backExtraLength = 0;
    //    int cursorPiece = back.pieceIndex;
    //    while (cursorPiece != front.pieceIndex) {
    //        backExtraLength += GetLanePieceTotalLength(cursorPiece, back.startLaneIndex, back.endLaneIndex, ref unused);
    //        cursorPiece = GetNextPieceIndex(cursorPiece);
    //    }
       
        
    //}

    public static double GetDistanceBetween(int pieceIndexBack, double inPieceDistanceBack, int startLaneBack, int endLaneBack,
                                            int pieceIndexFront, double inPieceDistanceFront) {
        double unused = 0;
        double backExtraLength = 0;

        while (pieceIndexBack != pieceIndexFront) {
            backExtraLength += GetLanePieceTotalLength(pieceIndexBack, startLaneBack, endLaneBack, ref unused);
            startLaneBack = endLaneBack;
            pieceIndexBack = GetNextPieceIndex(pieceIndexBack);
            if (backExtraLength - inPieceDistanceBack > 300) return 2000000000;
        }

        return inPieceDistanceFront + backExtraLength - inPieceDistanceBack;
    }

    public static void FindCollisions(List<CarPositionData> carList) {
        double eps = 1e-6;

        for (int i = 0; i < carList.Count; i++) {
            for (int j = i + 1; j < carList.Count; j++) {
                CarPositionData car1 = carList[i];
                CarPositionData car2 = carList[j];

                if (car1.startLaneIndex != car2.startLaneIndex || car1.endLaneIndex != car2.endLaneIndex) {
                   continue;
                }

                if (car1.startLaneIndex != car1.endLaneIndex && car1.pieceIndex != car2.pieceIndex) {
                    continue;
                }

                if (Math.Abs(GetDistanceBetween(car1.pieceIndex, car1.inPieceDistance, car1.startLaneIndex, car1.endLaneIndex, 
                                                car2.pieceIndex, car2.inPieceDistance) - you.carData.length) < eps) {
                    car1.collidedBack = true;
                    car1.colorOther = car2.color;
                    car2.collidedFront = true;
                    car2.colorOther = car1.color;
                }

                if (Math.Abs(GetDistanceBetween(car2.pieceIndex, car2.inPieceDistance, car2.startLaneIndex, car2.endLaneIndex,
                                                car1.pieceIndex, car1.inPieceDistance) - you.carData.length) < eps) {
                    car2.collidedBack = true;
                    car2.colorOther = car1.color;
                    car1.collidedFront = true;
                    car1.colorOther = car2.color;
                }
            }
        }
    }

    public static void UpdateCarPositions(List<CarPositionData> carPositionDataList) {
        FindCollisions(carPositionDataList);

        foreach (CarPositionData carPosition in carPositionDataList) {
            if (carPosition.color == yourCar.color) {
                if (carPosition.collidedBack) Console.WriteLine("CollisionBack on tick {0}", Model.gameTick);
                if (carPosition.collidedFront) Console.WriteLine("CollisionFront on tick {0}", Model.gameTick);

                you.AddPosition(carPosition);
                if (Model.gameTick > 0 
                    && !you.states[Model.gameTick].collidedBack
                    && !you.states[Model.gameTick].collidedFront
                    && !you.states[Model.gameTick - 1].collidedBack
                    && !you.states[Model.gameTick - 1].collidedFront
                    && !you.states[Model.gameTick - 1].gametickLost) {
                    SpeedModel.Increment();
                    AngleModel.Increment();
                }
            }
            else {
                others[carPosition.color].AddPosition(carPosition);
            }
        }

        foreach (CarPositionData carPosition in carPositionDataList) {
            if (carPosition.color == yourCar.color) {
                you.UpdateCollisionEqDs(carPosition);
            }
            else {
                others[carPosition.color].UpdateCollisionEqDs(carPosition);
            }
        }
    }

    public static void UpdateTurboAvailable(TurboAvailableData turboAvailableData) {
        turboFactor = turboAvailableData.turboFactor;
        turboDuration = turboAvailableData.turboDurationTicks;

        if (Flags.logturbo) { Console.WriteLine("TurboAvailable! gametick: " + gameTick); }

        you.addTurbo(turboAvailableData);
        foreach (Car car in others.Values) {
            car.addTurbo(turboAvailableData);
        }
    }

    public static void UpdateTurboStart(TurboStartData turboStartData) {
        if (turboStartData.color == yourCar.color) {
            you.AddTurboStartData();
            if (Flags.logturbo) { Console.WriteLine("TurboStart! gametick: " + gameTick); }
        }
        else {
            others[turboStartData.color].AddTurboStartData();
        }
    }

    public static void UpdateTurboEnd(TurboEndData turboEndData) {
        if (turboEndData.color == yourCar.color) {
            you.AddTurboEndData();
            if (Flags.logturbo) { Console.WriteLine("TurboEnd! gametick: " + gameTick); }
        }
        else {
            others[turboEndData.color].AddTurboEndData();
        }
    }

    public static void UpdateCrash(CrashData crashData) {
        if (crashData.color == yourCar.color) {
            Console.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>> You crashed! gameTick: " + gameTick + " <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<");
            //Console.WriteLine("REALIZADOS!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            //PrintCurveSwitchRadius();
            //Console.WriteLine("PREVISTOS!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            //PrintCurveSwitchPredicition();
            you.AddCrash();
            Statistics.crashes.Add(Model.gameTick);
        }
        else {
            others[crashData.color].AddCrash();
        }
    }

    public static void UpdateSpawn(SpawnData spawnData) {
        if (spawnData.color == yourCar.color) {
            you.AddSpawn();
            Console.WriteLine("Spawn! gametick: " + gameTick);
        }
        else {
            others[spawnData.color].AddSpawn();
        }
    }

    public static void UpdateFinish(FinishData finishData) {
        if (finishData.color == yourCar.color) {
            you.AddFinish(finishData);
        }
        else {
            others[finishData.color].AddFinish(finishData);
        }
    }

    public static void UpdateGameTick(int _gameTick) {
        gameTick = _gameTick;
    }

    public static void UpdateYourCar(YourCarData _yourCar) {
        yourCar = _yourCar;
    }

    public static void UpdateLapFinished(LapFinishedData lapFinished) {
        if (lapFinished.color == yourCar.color) {
            Console.WriteLine("LapFinished: " + lapFinished.lapTimeLap + ", timeMillis: " + lapFinished.lapTimeMillis);
            Statistics.lapTimes.Add(lapFinished.lapTimeMillis);
        }
        else {

        }
        laps.Add(lapFinished);
    }

    public static double GetLanePieceInvRadius(ref State state) {
        return GetLanePieceInvRadius(state.pieceIndex, state.inPieceDistance, state.startLaneIndex, state.endLaneIndex, ref state.dirtyUnknownSwitchSafetyMargin);
    }

    public static double GetLanePieceInvRadius(int pieceIndex, double inPieceDistance, int startLaneIndex, int endLaneIndex, ref double dirtyUnknownSwitchSafetyMargin) {
        if (Model.trackPieces[pieceIndex].straight) {
            return 0;
        }
        else if (startLaneIndex == endLaneIndex) {
            return Model.lanePieces[pieceIndex, endLaneIndex].invRadius;
        }
        else {
            int sign = Math.Sign(Model.lanePieces[pieceIndex, endLaneIndex].invRadius);

            return 1.0 / Model.GetCurveSwitchRadius(
                Model.CurveSwitchIds[pieceIndex, startLaneIndex, endLaneIndex],
                inPieceDistance,
                sign,
                ref dirtyUnknownSwitchSafetyMargin
                );
        }
    }

    public static double GetLanePieceSqrtAbsInvRadius(ref State state) {
        if (state.startLaneIndex == state.endLaneIndex) {
            return Model.lanePieces[state.pieceIndex, state.endLaneIndex].sqrtAbsInvRadius;
        }
        else {
            return Math.Sqrt(Math.Abs(Model.GetLanePieceInvRadius(ref state)));
        }
    }

    public static double GetLanePieceTotalLength(ref State state) {
        return GetLanePieceTotalLength(state.pieceIndex, state.startLaneIndex, state.endLaneIndex, ref state.dirtyUnknownSwitchSafetyMargin);
    }

    public static double GetLanePieceTotalLength(int pieceIndex, int startLaneIndex, int endLaneIndex, ref double dirtyUnknownSwitchSafetyMargin) {
        if (startLaneIndex == endLaneIndex) {
            return Model.lanePieces[pieceIndex, endLaneIndex].totalLength;
        }
        else if (Model.trackPieces[pieceIndex].straight) {
            return Model.GetLinearSwitchSize(
                Model.LinearSwitchIds[pieceIndex, startLaneIndex, endLaneIndex],
                ref dirtyUnknownSwitchSafetyMargin
                );
        }
        else {
            return Model.GetCurveSwitchSize(
                CurveSwitchIds[pieceIndex, startLaneIndex, endLaneIndex],
                ref dirtyUnknownSwitchSafetyMargin
                );
        }
    }

    public static double GetLinearSwitchSize(int id, ref double dirty) {
        if (linearSwitchSizes[id].size >= 0) {
            return linearSwitchSizes[id].size;
        }
        else {
            if (dirty < maxDirtySafetyAngleMargin) {
                dirty = maxDirtySafetyAngleMargin;
            }
            return propLinear * Math.Abs(linearSwitchSizes[id].size);
        }
    }

    public static double GetCurveSwitchSize(int id, ref double dirty) {
        if (curveSwitchSizes[id].size >= 0) {
            return curveSwitchSizes[id].size;
        }
        else {
            if (dirty < maxDirtySafetyAngleMargin) {
                dirty = maxDirtySafetyAngleMargin;
            }
            return propCurve * Math.Abs(curveSwitchSizes[id].size);
        }
    }

    public static double GetCurveSwitchRadius(int id, double inPieceDistace, int sign, ref double dirty) {
        inPieceDistace = Math.Abs(inPieceDistace);

        CurveSwitchRadiusData ncsrd = new CurveSwitchRadiusData(inPieceDistace, 0, false);
        int pos = curveSwitchRadius[id].radius.BinarySearch(ncsrd);

        if (pos >= 0) {
            if (dirty < minDirtySafetyAngleMargin) {
                dirty = minDirtySafetyAngleMargin;
            }
            return sign * curveSwitchRadius[id].radius[pos].radius;
        }
        pos = ~pos;

        if (pos == 0) return sign * curveSwitchRadius[id].absRIni;

        if (pos < curveSwitchRadius[id].radius.Count) {
            double aux = minDirtySafetyAngleMargin + (maxDirtySafetyAngleMargin - minDirtySafetyAngleMargin) * (curveSwitchRadius[id].radius[pos].inPieceDistance - curveSwitchRadius[id].radius[pos - 1].inPieceDistance) / curveSwitchRadius[id].radius.Last().inPieceDistance;
            if (dirty < aux) {
                dirty = aux;
            }
            if (pos < curveSwitchRadius[id].radius.Count - 1) {
                return sign * (curveSwitchRadius[id].radius[pos - 1].radius + (inPieceDistace - curveSwitchRadius[id].radius[pos - 1].inPieceDistance) / (curveSwitchRadius[id].radius[pos].inPieceDistance - curveSwitchRadius[id].radius[pos - 1].inPieceDistance) * (curveSwitchRadius[id].radius[pos].radius - curveSwitchRadius[id].radius[pos - 1].radius));
            }
            else {
                return sign * Math.Min(curveSwitchRadius[id].radius[pos - 1].radius, curveSwitchRadius[id].radius[pos].radius);
            }
        }

        if (dirty < minDirtySafetyAngleMargin) {
            dirty = minDirtySafetyAngleMargin;
        }
        return sign * curveSwitchRadius[id].absRFim;
    }

    public static int GetNextPieceIndex(int pieceIndex) {
        return (pieceIndex + 1) % trackPieces.Length;
    }

    public static int GetPieceIndexBefore(int pieceIndex) {
        return (pieceIndex - 1 + trackPieces.Length) % trackPieces.Length;
    }

    public static int GetNextNPiecesAhead(int pieceIndex, int n) {
        return (pieceIndex + n) % trackPieces.Length;
    }

    public static bool IsPieceBetween(int piece, int startPiece, int endPiece) {
        if (endPiece < startPiece) {
            if (piece > endPiece || piece < startPiece) {
                return true;
            }
        }
        else {
            if ((piece > startPiece && piece < endPiece)) {
                return true;
            }
        }
        return false;
    }

    public static double minRadiusNextPieces(int piece, int numPieces) {
        double resp = 1000000000;

        for (int i = 0; i <= numPieces; i++) {
            for (int j = 0; j < Model.lanes.Length; j++) {
                if (Math.Abs(Model.lanePieces[Model.GetNextNPiecesAhead(piece, i), j].radius) < resp) {
                    resp = Math.Abs(Model.lanePieces[Model.GetNextNPiecesAhead(piece, i), j].radius);
                }
            }
        }

        return resp;
    }

    public static Car GetCarByColor(string color) {
        if (color == yourCar.color) return you;
        return others[color];
    }
}

public class LanePiece {
    public double totalLength;
    public double radius;
    public double invRadius;
    public double sqrtAbsInvRadius;

    public LanePiece(TrackPieceData trackPiece, LaneData laneData) {
        if (trackPiece.straight) {
            this.totalLength = trackPiece.length;
            this.radius = Double.PositiveInfinity;
            this.invRadius = 0;
            this.sqrtAbsInvRadius = 0;
        }
        else {
            this.radius = trackPiece.radius - laneData.distanceFromCenter;
            this.totalLength = Math.Abs(Math.PI * radius * trackPiece.angle / 180.0);
            this.invRadius = 1.0 / this.radius;
            this.sqrtAbsInvRadius = Math.Sqrt(Math.Abs(invRadius));
        }

        if (Math.Abs(this.radius) < AngleModel.minTrackRadius) {
            AngleModel.minTrackRadius = Math.Abs(this.radius);
        }
    }
}

public class Car {
    public CarData carData;
    public FinishData finishData = null;

    public bool isActive;
    public int gameTickCrashed = -1;

    public bool isTurboActive;
    public int lastGameTick = -1;

    public List<int> turbosGameTickUsed = new List<int>();
    public bool iSturboAvailable;

    public int previousPieceIndex;
    public int currentPieceIndex;
    public int currentTotalPieceIndex;
    public List<double> sTotalPreviousPieceIndex = new List<double>(); // total distance up to the end of the previous piece.

    public int currentLap;

    public int tmpTimeToNextSwitchCalculated;

    public List<int>[] lastGametickPassedHere;
    public List<int>[] observationsFromQualify;

    public State[] states = new State[100000];

    public int sMemorized1WhenPossibleGameTick = -1000;
    public S1WhenPossibleWithMemory sMemorized1WhenPossible;


    public Car(CarData carData, int numberOfPieces, List<int>[] observationsFromQualify) {
        this.carData = carData;
        this.isActive = true;
        this.isTurboActive = false;
        iSturboAvailable = false;
        this.observationsFromQualify = observationsFromQualify;
        sMemorized1WhenPossibleGameTick = -100;

        lastGametickPassedHere = new List<int>[numberOfPieces];
        for (int i = 0; i < numberOfPieces; i++) {
            lastGametickPassedHere[i] = new List<int>();
            //lastGametickPassedHere[i].Add(-1);
        }
    }

    public S1WhenPossibleWithMemory GetCurrentS1WhenPossible() {
        if (sMemorized1WhenPossibleGameTick != Model.gameTick) {
            sMemorized1WhenPossible = new S1WhenPossibleWithMemory();
            sMemorized1WhenPossibleGameTick = Model.gameTick;
        }
        return sMemorized1WhenPossible;
    }

    public void UpdateCollisionEqDs(CarPositionData position) {
        int gt = position.gameTick;
        if (gt > 0) {

            if (position.collidedFront) {
                Car other = Model.GetCarByColor(position.colorOther);
                states[gt].dsEqMin = SpeedModel.GetDsEqFront(ref other.states[gt]);
                states[gt].dsEqMax = SpeedModel.GetDsEqFront(ref other.states[gt]);

                if (position.color == Model.yourCar.color) {
                    states[gt].throttle = 0.0;
                    Console.WriteLine("Collision Prediction NextDs with Throttle 0: {0}, DsOther: {1}, DsEq: {2}",
                        SpeedModel.GetNextDs(ref states[gt], true),
                        other.states[gt].ds,
                        states[gt].dsEqMin
                        );

                    states[gt].throttle = 1.0;
                    Console.WriteLine("Collision Prediction NextDs with Throttle 1: {0}, DsOther: {1}, DsEq: {2}",
                        SpeedModel.GetNextDs(ref states[gt], true),
                        other.states[gt].ds,
                        states[gt].dsEqMin
                        );
                }
            }
            
            if (position.collidedBack) {
                Car other = Model.GetCarByColor(position.colorOther);
                states[gt].dsEqMin = SpeedModel.GetDsEqBack(ref other.states[gt - 1], 0.0);
                states[gt].dsEqMax = SpeedModel.GetDsEqBack(ref other.states[gt - 1], 1.0);

                if (position.color == Model.yourCar.color) {
                    states[gt].throttle = 0.0;
                    Console.WriteLine("Collision Prediction NextDs with Throttle 0: {0}, DsOther(t-1): {1}, DsEqMin: {2}",
                        SpeedModel.GetNextDs(ref states[gt], false),
                        other.states[gt - 1].ds,
                        states[gt].dsEqMin
                        );

                    states[gt].throttle = 1.0;
                    Console.WriteLine("Collision Prediction NextDs with Throttle 1: {0}, DsOther(t-1): {1}, DsEqMin: {2}",
                        SpeedModel.GetNextDs(ref states[gt], true),
                        other.states[gt - 1].ds,
                        states[gt].dsEqMin
                        );

                    states[gt].throttle = 0.0;
                    Console.WriteLine("Collision Prediction NextDs with Throttle 0: {0}, DsOther(t-1): {1}, DsEqMax: {2}",
                        SpeedModel.GetNextDs(ref states[gt], false),
                        other.states[gt - 1].ds,
                        states[gt].dsEqMax
                        );

                    states[gt].throttle = 1.0;
                    Console.WriteLine("Collision Prediction NextDs with Throttle 1: {0}, DsOther(t-1): {1}, DsEqMax: {2}",
                        SpeedModel.GetNextDs(ref states[gt], true),
                        other.states[gt - 1].ds,
                        states[gt].dsEqMax
                        );
                }
            }

            if (position.color == Model.yourCar.color) {
                if (states[gt - 1].collidedFront || states[gt - 1].collidedBack) {
                    Console.WriteLine("Ds after collision: {0}", states[gt].ds);
                }
            }
        }
    }

    public void AddPosition(CarPositionData position) {
        int gt = position.gameTick;
        lastGameTick = gt;

        currentLap = position.lap;

        if (gt >= states.Length) {
            Array.Resize(ref states, states.Length + 10000);
        }

        if (Math.Abs(position.angle) > AngleModel.maxAngleSeen) {
            AngleModel.maxAngleSeen = Math.Abs(position.angle);
        }

        if (position.color == Model.yourCar.color) {
            if (Model.raceStarted && Model.gameTick > 0 && position.previousGameTick != Model.gameTick - 1 && isActive && Model.currentMsgHasGametick) {
                Model.lostTicks.Add(new TimeMeasure(0, Model.gameTick - 1, Model.qualify));
                states[Model.gameTick - 1].gametickLost = true;

                Console.WriteLine("LOST GAMETICK: Previous gameTick {0}", position.previousGameTick);

                if (SwitchController.sentSwitchMsgOnTick == Model.gameTick - 1) {
                    Console.WriteLine("LOST SWITCH MSG!");
                    SwitchController.waitingForSwitchToHappen = 0;
                }
                //Controller.forceSendSwitchNextTime = false;
            }
            else if (Model.gameTick > 0) {
                states[Model.gameTick - 1].gametickLost = false;
            }
        }

        states[gt].isActive = isActive;
        states[gt].collidedBack = position.collidedBack;
        states[gt].collidedFront = position.collidedFront;

        if (gt > 0) {
            if (currentPieceIndex != position.pieceIndex) {
                currentPieceIndex = position.pieceIndex;
                currentTotalPieceIndex++;

                if (position.color == Model.yourCar.color) {
                    if (Model.trackPieces[currentPieceIndex].hasSwitch) {
                        SwitchController.waitingForSwitchToHappen = 0;
                    }
                }

                lastGametickPassedHere[currentPieceIndex].Add(Model.gameTick);

                //Console.WriteLine("states[gt].collidedFront {0}, states[gt - 1].collidedBack {1}", states[gt].collidedFront, states[gt - 1].collidedBack);
                //Console.WriteLine("states[gt - 1].startLaneIndex {0}, states[gt - 1].endLaneIndex {1}", states[gt - 1].startLaneIndex, states[gt - 1].endLaneIndex);
                if (states[gt - 1].startLaneIndex == states[gt - 1].endLaneIndex) {
                    sTotalPreviousPieceIndex.Add(
                        sTotalPreviousPieceIndex.Last() +
                        Model.lanePieces[states[gt - 1].pieceIndex, states[gt - 1].startLaneIndex].totalLength);
                }
                else {
                    
                    if (states[gt].collidedFront 
                        || states[gt - 1].collidedBack) {
                        double unused = 0;
                        sTotalPreviousPieceIndex.Add(
                            sTotalPreviousPieceIndex.Last() +
                            Model.GetLanePieceTotalLength(states[gt - 1].pieceIndex, states[gt - 1].startLaneIndex, states[gt - 1].endLaneIndex, ref unused));
                    }
                    else {
                        double tamanhoSemSwitch = sTotalPreviousPieceIndex.Last();
                        double tamanhoComSwitch;

                        tamanhoComSwitch = sTotalPreviousPieceIndex.Last() + states[gt - 1].inPieceDistance
                            + SpeedModel.GetNextDs(ref states[gt - 1], true) - position.inPieceDistance;

                        sTotalPreviousPieceIndex.Add(tamanhoComSwitch);

                        if (position.color == Model.yourCar.color) {
                            if (Model.trackPieces[states[gt - 1].pieceIndex].straight && states[gt - 1].isActive && states[gt].isActive && !states[gt - 1].gametickLost) {
                                Model.UpdateLinearSwitchSize(
                                    Model.LinearSwitchIds[states[gt - 1].pieceIndex, states[gt - 1].startLaneIndex, states[gt - 1].endLaneIndex],
                                    Math.Abs(tamanhoComSwitch - tamanhoSemSwitch)
                                    );
                            }
                            else if (!Model.trackPieces[states[gt - 1].pieceIndex].straight && states[gt - 1].isActive && states[gt].isActive && !states[gt - 1].gametickLost) {
                                Model.UpdateCurveSwitchSize(
                                    Model.CurveSwitchIds[states[gt - 1].pieceIndex, states[gt - 1].startLaneIndex, states[gt - 1].endLaneIndex],
                                    Math.Abs(tamanhoComSwitch - tamanhoSemSwitch)
                                    );
                            }
                        }
                    }
                }
            }
            states[gt].s = position.inPieceDistance + sTotalPreviousPieceIndex.Last();
            states[gt].ds = states[gt].s - states[gt - 1].s;
            states[gt].a = position.angle;
            states[gt].da = states[gt].a - states[gt - 1].a;
            if (isTurboActive && states[gt - 1].turboTicksRemaining == 0) {
                states[gt].turboTicksRemaining = Model.turboDuration + 1;
            }
            else if (isTurboActive && states[gt - 1].turboTicksRemaining > 1) {
                states[gt].turboTicksRemaining = states[gt - 1].turboTicksRemaining - 1;
            }
            else if (isTurboActive) {
                states[gt].turboTicksRemaining = 1;
            }
            else {
                states[gt].turboTicksRemaining = 0;
            }

            if (gt > 0
                && !states[gt].collidedFront && !states[gt].collidedBack
                && !states[gt - 1].collidedFront && !states[gt - 1].collidedBack
                && (position.color == Model.yourCar.color || (gt > 1 && (states[gt - 2].pieceIndex == states[gt - 1].pieceIndex || !Model.trackPieces[states[gt - 2].pieceIndex].hasSwitch)))
                && (!Model.trackPieces[states[gt - 1].pieceIndex].straight) 
                && states[gt - 1].startLaneIndex != states[gt - 1].endLaneIndex
                && states[gt - 1].isActive 
                && states[gt].isActive) 
            {
                if (AngleModel.isEstimated) {
                    double estimatedRadius = AngleModel.GetEquivalentRadius(ref states[gt - 1], states[gt].da);
                    if (estimatedRadius > 0) {
                        Model.UpdateCurveSwitchRadius(
                            Model.CurveSwitchIds[states[gt - 1].pieceIndex, states[gt - 1].startLaneIndex, states[gt - 1].endLaneIndex],
                            Math.Abs(states[gt - 1].inPieceDistance),
                            estimatedRadius,
                            false
                            );
                    }
                }
                else {
                    if (position.color == Model.yourCar.color) {
                        AngleModel.gameticksRadius.Add(gt);
                    }
                }
            }
        }
        else {
            currentPieceIndex = position.pieceIndex;
            currentTotalPieceIndex = 0;

            sTotalPreviousPieceIndex.Add(0.0);

            states[gt].s = position.inPieceDistance;
            states[gt].ds = 0.0;
            states[gt].a = position.angle;
            states[gt].da = 0.0;
            states[gt].turboTicksRemaining = 0;
        }

        states[gt].pieceIndex = position.pieceIndex;
        states[gt].inPieceDistance = position.inPieceDistance;
        states[gt].startLaneIndex = position.startLaneIndex;
        states[gt].endLaneIndex = position.endLaneIndex;
        states[gt].turboFactor = (isTurboActive ? Model.turboFactor : 1.0);
        states[gt].dirtyUnknownSwitchSafetyMargin = 0;
    }

    public int EstimateNextGameTickInPiece(int targetPiece, int referencePiece, bool useBestTime) {

        int medianToPiece = ExtractMedianObservarion(currentPieceIndex, targetPiece, useBestTime);
        int medianToNext = ExtractMedianObservarion(currentPieceIndex, (currentPieceIndex + 1) % Model.trackPieces.Length, useBestTime);

        if (Flags.debugovertake) {
            if (Model.gameTick > 950 && Model.gameTick < 1000)
            Console.WriteLine("estimateNextGameTick, color: {0}, medianToPiece {1}, medianToNext {2}, gametick {3}", carData.color, medianToPiece, medianToNext, Model.gameTick);
        }

        if (medianToPiece != -1) {
            int lateToNext = 0;
            if (lastGametickPassedHere[currentPieceIndex].Count > 0 && medianToNext != -1) { // o quanto esta atrasando para chegar na proxima peca
                if (lastGameTick - lastGametickPassedHere[currentPieceIndex].Last() > medianToNext) {
                    lateToNext = lastGameTick - lastGametickPassedHere[currentPieceIndex].Last() - medianToNext;
                }
                if (lateToNext < 0) lateToNext = 0;
            }

            int timeCompletedInCurrent = (int) (medianToNext * states[lastGameTick].inPieceDistance / Model.lanePieces[states[lastGameTick].pieceIndex, states[lastGameTick].endLaneIndex].totalLength);

            //Console.WriteLine("medianToPiece {0}, diffFromBegging {1}, lateToNext {2}", medianToPiece, diffFromBegging, lateToNext);
            return medianToPiece + lateToNext - timeCompletedInCurrent;
        }

        
        double distanceRemaining = 0;
        for (int i = currentPieceIndex; i != targetPiece; i = (i + 1) % Model.trackPieces.Length) {
            distanceRemaining += Model.lanePieces[i, states[lastGameTick].endLaneIndex].totalLength;
        }
        if (states[lastGameTick].ds > 0) return (int)(distanceRemaining / (0.8 * states[lastGameTick].ds));
        return 2000000000;
    }

    public int ExtractMedianObservarion(int initalPiece, int finalPiece, bool useBestTime) {
        List<int> observations = new List<int>();
        for (int i = 0; i < lastGametickPassedHere[initalPiece].Count; i++) {
            for (int j = 0; j < lastGametickPassedHere[finalPiece].Count; j++) {
                if (lastGametickPassedHere[finalPiece][j] > lastGametickPassedHere[initalPiece][i]) {
                    observations.Add(lastGametickPassedHere[finalPiece][j] - lastGametickPassedHere[initalPiece][i]);
                    break;
                }
            }
        }

        if (observationsFromQualify != null) {
            for (int i = 0; i < observationsFromQualify[initalPiece].Count; i++) {
                for (int j = 0; j < observationsFromQualify[finalPiece].Count; j++) {
                    if (observationsFromQualify[finalPiece][j] > observationsFromQualify[initalPiece][i]) {
                        observations.Add(observationsFromQualify[finalPiece][j] - observationsFromQualify[initalPiece][i]);
                        break;
                    }
                }
            }
        }

        if (observations.Count > 0) {
            observations.Sort();

            if (useBestTime) {
                return observations[0];
            }

            return observations[observations.Count / 2];
        }
        return -1;
    }

    public double GetLastThrottle() {
        if (lastGameTick > 0) {
            return states[lastGameTick - 1].throttle;
        }
        else {
            return 0.0;
        }
    }

    public void RepeatThrottle() {
        states[lastGameTick].throttle = GetLastThrottle();
    }

    public void AddCrash() {
        this.isActive = false;
        this.iSturboAvailable = false;
        this.isTurboActive = false;
        states[Model.gameTick].turboTicksRemaining = 0;
        this.gameTickCrashed = Model.gameTick;
    }

    public void AddTurboStartData() {
        this.isTurboActive = true;
        this.iSturboAvailable = false;
    }

    public void AddTurboEndData() {
        this.isTurboActive = false;
    }

    public void AddSpawn() {
        this.isActive = true;
        Model.spawnTime = Model.gameTick - gameTickCrashed;
    }

    public int ExpectedTicksToSpawn() {
        if (isActive) return 0;
        return gameTickCrashed + Model.spawnTime - Model.gameTick;
    }

    public void addTurbo(TurboAvailableData turboAvailable) {
        if (this.isActive) {
            this.iSturboAvailable = true;
        }
    }

    public void UseTurbo() {
        this.iSturboAvailable = false;
    }

    public bool HasTurboAvailable() {
        return this.iSturboAvailable;
    }

    public void AddFinish(FinishData finishData) {
        this.finishData = finishData;
        isActive = false;
        gameTickCrashed = 1000000000;
    }

    public bool HasFinished() {
        return finishData != null;
    }

    public bool IsDangerous() {
        if (!isActive) return false;
        bool unused = false;

        if (TimeBehindYou(ref unused) < Flags.tdvalue) return true;
        
        return false;
    }

    public int TimeBehindYou(ref bool noData) {
        if (lastGametickPassedHere[states[Model.gameTick].pieceIndex].Count > 0 &&
            Model.you.lastGametickPassedHere[states[Model.gameTick].pieceIndex].Count > 0) 
        {

            int lastTickInPiece = lastGametickPassedHere[states[Model.gameTick].pieceIndex].Last();
            int lastTickInPieceYou = Model.you.lastGametickPassedHere[states[Model.gameTick].pieceIndex].Last();
            if (Flags.logdist) Console.WriteLine("TimeBehindYou {0}", lastTickInPiece - lastTickInPieceYou);
            noData = false;
            return lastTickInPiece - lastTickInPieceYou;            
        }
        noData = true;
        return 2000000000;
    }

    public bool IsCompetitive() {
        if (!isActive) return false;
        bool unused = false; ;

        if (!ThrottleController.IsInBetterRankingThanYou(this)) return false;

        if (TimeAheadYou(ref unused) < 13) return true;

        return false;
    }

    public int TimeAheadYou(ref bool noData) {
        if (Model.you.lastGametickPassedHere[Model.you.states[Model.gameTick].pieceIndex].Count > 0 &&
            lastGametickPassedHere[Model.you.states[Model.gameTick].pieceIndex].Count > 0) {
            int lastTickInPieceYou = Model.you.lastGametickPassedHere[Model.you.states[Model.gameTick].pieceIndex].Last();
            int lastTickInPiece = lastGametickPassedHere[Model.you.states[Model.gameTick].pieceIndex].Last();

            if (Flags.logdist) Console.WriteLine("TimeAheadYou {0}", lastTickInPieceYou - lastTickInPiece);
            noData = false;
            return lastTickInPieceYou - lastTickInPiece;
        }
        noData = true;
        return 2000000000;
    }
}

public struct State {
    public double inPieceDistance;
    public double s;
    public double ds;
    public double a;
    public double da;
    public int pieceIndex;
    public int startLaneIndex;
    public int endLaneIndex;
    public bool isActive;
    public double throttle;
    public double turboFactor;
    public int turboTicksRemaining;
    public bool collidedBack;
    public bool collidedFront;
    public double dsEqMin;
    public double dsEqMax;
    public double dirtyUnknownSwitchSafetyMargin;
    public bool gametickLost;

    public static State GetZeroState() {
        State result = new State();

        result.inPieceDistance = 0;
        result.s = 0;
        result.ds = 0;
        result.a = 0;
        result.da = 0;
        result.pieceIndex = 0;
        result.startLaneIndex = 0;
        result.endLaneIndex = 0;
        result.isActive = true;
        result.throttle = 0.0;
        result.turboFactor = 1.0;
        result.turboTicksRemaining = 0;
        result.collidedBack = false;
        result.collidedFront = false;
        result.dsEqMax = 0.0;
        result.dsEqMin = 0.0;
        result.dirtyUnknownSwitchSafetyMargin = 0;
        result.gametickLost = false;

        return result;
    }
}

public struct LinearSwitchSize {
    public double length;
    public double width;
    public double size;

    public LinearSwitchSize(double _lenght, double _width, double _size) {
        length = _lenght;
        width = _width;
        size = _size;
    }
};

public struct CurveSwitchSize {
    public double absRIni;
    public double absRFim;
    public double absAng;
    public double size;

    public CurveSwitchSize(double _absRIni, double _absRFim, double _absAng, double _size) {
        absRIni = _absRIni;
        absRFim = _absRFim;
        absAng = _absAng;
        size = _size;
    }
};

public struct CurveSwitchRadius {
    public double absRIni;
    public double absRFim;
    public double absAng;
    public List<CurveSwitchRadiusData> radius;

    public CurveSwitchRadius(double _absRIni, double _absRFim, double _absAng) {
        absRIni = _absRIni;
        absRFim = _absRFim;
        absAng = _absAng;
        radius = new List<CurveSwitchRadiusData>();
    }
};

public struct CurveSwitchRadiusData : IComparable<CurveSwitchRadiusData> {
    public double inPieceDistance;
    public double radius;
    public bool temporario;

    public CurveSwitchRadiusData(double _inPieceDistance, double _radius, bool _temporario){
        inPieceDistance = _inPieceDistance;
        radius = _radius;
        temporario = _temporario;
    }

    public int CompareTo(CurveSwitchRadiusData other) {
        return inPieceDistance.CompareTo(other.inPieceDistance);
    }
};

public class TimeMeasure {
    public double time;
    public int tick;
    public bool qualify;

    public TimeMeasure(double time, int tick, bool qualify) {
        this.time = time;
        this.tick = tick;
        this.qualify = qualify;
    }
}