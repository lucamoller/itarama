using System;
using System.IO;
using System.Collections.Generic;

public class Logger {

    public static string SEPARATOR = "---outputs---";
    public static string INPUT_PREFIX = "INPUT:";
    public static string OUTPUT_PREFIX = "OUTPUT:";

    private List<string> inputLines = new List<string>();
    private List<string> outputLines = new List<string>();
    private string trackId;

    public Logger() {
        trackId = "";
    }

    public void SetTrackId(string trackId) {
        this.trackId = trackId;
    }

    public void LogInput(string line) {
        inputLines.Add(line);
        if (!Flags.local) {
            //Console.WriteLine(INPUT_PREFIX + line);
        }
    }

    public void LogOuput(string line) {
        outputLines.Add(line);
        if (!Flags.local) {
            //Console.WriteLine(OUTPUT_PREFIX + line);
        }
    }

    public void Flush() {
        inputLines.Add(SEPARATOR);
        inputLines.AddRange(outputLines);
        System.IO.Directory.CreateDirectory("logs");

        string file = "logs/" + Flags.botname + trackId + "_" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".txt";
        File.WriteAllLines(file, inputLines.ToArray());
        Console.WriteLine("Wrote logs to " + file);
    }

}