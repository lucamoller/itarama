using System;
using System.Collections.Generic;

public static class TrackModel {
    public static bool foundBestLanes = false;
    public static bool mustRefresh = false;

    public static int[] idSwitches;
    public static double[,] switchLanes;
    public static double[,,] pd;
    public static int[,,] pai;

    public static int[] bestLaneSwitches;
    public static int[] bestLanes;

    static double unused;

    static public int activePiece;
    static public int activeLane;
    static public int countSwitches;
    static public bool firstTime = true;

    public static int bestLanesModified(int piece, bool useActiveLane) {
        if (useActiveLane && piece == activePiece) {
            return activeLane;
        }
        return bestLanes[piece];
    }

    public static int FindNextSwitch(int pieceIndex) {
        for (int i = (pieceIndex + 1) % Model.trackPieces.Length;
             i != pieceIndex;
             i = (i + 1) % Model.trackPieces.Length) {
            if (Model.trackPieces[i].hasSwitch) return i;
        }
        return -1;
    }

    public static int FindNextBestLane(int pieceIndex, int currentLane) {
        if (!foundBestLanes) return 0;
        int nextSwitch = FindNextSwitch(pieceIndex);
        if (nextSwitch == -1) return 0;

        int targetLane = bestLanes[nextSwitch];
        if (targetLane > currentLane) return 1;
        if (targetLane < currentLane) return -1;
        return 0;
    }

    public static void FindBestLanes() {
        foundBestLanes = false;

        int firstSwitch;
        int currentSwitch;

        if (firstTime) {
            countSwitches = 0;
            for (int i = 0; i < Model.trackPieces.Length; i++) {
                if (Model.trackPieces[i].hasSwitch) {
                    countSwitches++;
                }
            }

            if (countSwitches == 0) {
                return;
            }

            idSwitches = new int[countSwitches];
            switchLanes = new double[countSwitches, Model.lanes.Length];
            firstSwitch = FindNextSwitch(0);
            currentSwitch = -1;

            for (int i = firstSwitch; ; i = (i + 1) % Model.trackPieces.Length) {
                if (Model.trackPieces[i].hasSwitch) {
                    currentSwitch++;
                    if (currentSwitch == countSwitches) break;
                    idSwitches[currentSwitch] = i;
                    for (int j = 0; j < Model.lanes.Length; j++) {
                        switchLanes[currentSwitch, j] = 0.0;
                    }
                }
                for (int j = 0; j < Model.lanes.Length; j++) {
                    switchLanes[currentSwitch, j] += Model.lanePieces[i, j].totalLength;
                }
            }

            pd = new double[Model.lanes.Length, countSwitches, Model.lanes.Length];
            pai = new int[Model.lanes.Length, countSwitches, Model.lanes.Length];
            bestLaneSwitches = new int[countSwitches];
            bestLanes = new int[Model.trackPieces.Length];
        }

        if (countSwitches == 0) {
            return;
        }

        for (int k = 0; k < Model.lanes.Length; k++) {
            for (int i = 0; i < countSwitches; i++) {
                for (int j = 0; j < Model.lanes.Length; j++) {
                    pd[k, i, j] = 2000000000.0;
                    pai[k, i, j] = -1;
                }
            }
        }

        double adicional;

        for (int k = 0; k < Model.lanes.Length; k++) {
            pd[k, 0, k] = switchLanes[0, k];

            for (int i = 1; i < countSwitches; i++) {
                for (int j = 0; j < Model.lanes.Length; j++) {
                    for (int h = 0; h < Model.lanes.Length; h++) {
                        if (Model.trackPieces[idSwitches[i]].straight && h!=j) {
                            adicional = Model.GetLinearSwitchSize(
                                Model.LinearSwitchIds[i,j,h],
                                ref unused
                                ) - Model.trackPieces[idSwitches[i]].length;
                        }
                        else if (h != j) {
                            adicional = Model.GetCurveSwitchSize(
                                Model.CurveSwitchIds[idSwitches[i],h,j],
                                ref unused
                                ) - Model.lanePieces[idSwitches[i], j].totalLength;
                        }
                        else {
                            adicional = 0.0;
                        }
                        if (Math.Abs(j - h) <= 1 && pd[k, i, j] >= pd[k, i - 1, h] + switchLanes[i, j] + adicional) {
                            pd[k, i, j] = pd[k, i - 1, h] + switchLanes[i, j] + adicional;
                            pai[k, i, j] = h;
                        }
                    }
                }
            }
        }


        int resp_k = -1;
        int resp_j = -1;
        double resp = 2000000000.0;
        for (int k = 0; k < Model.lanes.Length; k++) {
            for (int j = 0; j < Model.lanes.Length; j++) {
                if (Model.trackPieces[idSwitches[0]].straight && k != j) {
                    adicional = Model.GetLinearSwitchSize(
                        Model.LinearSwitchIds[idSwitches[0],j,k],
                        ref unused
                        ) - Model.trackPieces[idSwitches[0]].length;
                }
                else if (k != j) {
                    adicional = Model.GetCurveSwitchSize(
                        Model.CurveSwitchIds[idSwitches[0], j, k],
                        ref unused
                        ) - Model.lanePieces[idSwitches[0], k].totalLength;
                }
                else {
                    adicional = 0.0;
                }
                if (Math.Abs(j - k) <= 1 && pd[k, countSwitches - 1, j] + adicional < resp) {
                    resp = pd[k, countSwitches - 1, j] + adicional;
                    resp_k = k;
                    resp_j = j;
                }
            }
        }

        for (int i = countSwitches - 1; i >= 0; i--) {
            bestLaneSwitches[i] = resp_j;
            resp_j = pai[resp_k, i, resp_j];
        }

        firstSwitch = FindNextSwitch(0);
        currentSwitch = -1;

        for (int i = firstSwitch; ; i = (i + 1) % Model.trackPieces.Length) {
            if (Model.trackPieces[i].hasSwitch) {
                currentSwitch++;
                if (currentSwitch == countSwitches) break;
            }
            bestLanes[i] = bestLaneSwitches[currentSwitch];
        }

        if (Flags.logtrackmodel) {
            for (int i = 0; i < Model.trackPieces.Length; i++) {
                Console.WriteLine(i + "->" + bestLanes[i] + (Model.trackPieces[i].hasSwitch ? "*" : ""));
            }
        }

        foundBestLanes = true;
        firstTime = false;
    }
}
