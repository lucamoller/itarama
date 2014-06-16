using System;
using System.Collections.Generic;
using System.Linq;


public static class Simulator {

    public static void SimulateSingleNextState(double throttle, ref State currentState, ref State newState,
                                               bool useDirtyMargin, bool useActiveLane, ref bool crashed, double max_angle, bool useMaxDsEq=true) {
        double lanePieceLength = Model.GetLanePieceTotalLength(ref currentState);
        crashed = false;

        newState.da = AngleModel.GetNextDa(ref currentState, useMaxDsEq);
        newState.a = currentState.a + newState.da;

        currentState.throttle = throttle;
        newState.ds = SpeedModel.GetNextDs(ref currentState, useMaxDsEq);
        newState.inPieceDistance = currentState.inPieceDistance + newState.ds;

        newState.pieceIndex = currentState.pieceIndex;
        newState.startLaneIndex = currentState.startLaneIndex;
        newState.endLaneIndex = currentState.endLaneIndex;

        if (newState.inPieceDistance > lanePieceLength) {
            SimulateChangePiece(ref newState, lanePieceLength, useActiveLane);
        }

        if (currentState.turboTicksRemaining > 1) {
            newState.turboTicksRemaining = currentState.turboTicksRemaining - 1;
            newState.turboFactor = currentState.turboFactor;
        }
        else {
            newState.turboTicksRemaining = 0;
            newState.turboFactor = 1.0;
        }

        newState.collidedBack = false;
        newState.collidedFront = false;
        newState.dsEqMax = newState.ds;
        newState.dsEqMin = newState.ds;
        newState.dirtyUnknownSwitchSafetyMargin = currentState.dirtyUnknownSwitchSafetyMargin;

        if (Math.Abs(newState.a) + (useDirtyMargin ? currentState.dirtyUnknownSwitchSafetyMargin : 0.0)
            > max_angle) {
            crashed = true;
        }
        else {
            crashed = false;
        }
    }

    public static void SimulateChangePiece(ref State newState, double lanePieceLength, bool useActiveLane) {
        newState.inPieceDistance -= lanePieceLength;
        newState.pieceIndex = (newState.pieceIndex + 1) % Model.trackPieces.Length;
        newState.startLaneIndex = newState.endLaneIndex;

        int bestNextLane = TrackModel.bestLanesModified(newState.pieceIndex, useActiveLane);

        if (bestNextLane > newState.startLaneIndex && Model.trackPieces[newState.pieceIndex].hasSwitch) {
            newState.endLaneIndex = newState.startLaneIndex + 1;
        }
        else if (bestNextLane < newState.startLaneIndex && Model.trackPieces[newState.pieceIndex].hasSwitch) {
            newState.endLaneIndex = newState.startLaneIndex - 1;
        }
        else {
            newState.endLaneIndex = newState.startLaneIndex;
        }
    }
    
}