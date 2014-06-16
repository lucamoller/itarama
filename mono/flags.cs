


using System;
public static class Flags {

    public static bool noc = false;  // no collision simulation
    public static bool noe = false;  // no escape

    public static bool jem = false;  // just emergency escape
    public static bool af = false;   // agressive first
    public static bool td = false;   // ticks to consider the guy behind dangerous

    public static int tdvalue = 13;  // 


    public static bool local = false;
    public static bool server = false; // read server logs
    public static bool create = false;
    public static bool join = false;
    public static bool galera = false;
    public static string track = "keimola";
    public static bool debugovertake = false;

    public static bool logturbo = false;
    public static bool logtrack = false;
    public static bool logsimulador = false;
    public static bool logswitch = false;
    public static bool logtrackmodel = false;
    public static bool logcstopds = false;
    public static bool logt = false;
    public static bool logc = false;
    public static bool logdist = false;
    
    public static string botname = "";
    public static bool noturbo = false;

    public static bool noswitch = false;
    public static bool lane0 = false;
    public static bool lane1 = false;

    public static bool maxangle = false;
    public static double maxanglevalue = 60.0;

    public static bool fixedthrottle = false;
    public static double fixedthrottlevalue = 0.5;

    public static bool brakeonstart = false;
    public static int brakeonstartvalue = 0;

    public static bool stopontick = false;
    public static int stopontickvalue = 0;

    public static bool strategy1whenpossible = false;
    public static bool strategykamikaze = false;

    public static bool kamikazeafter = false;
    public static int kamikazeaftervalue = 0;

    public static bool skipqualify = false;

    public static bool prost = false;

    public static bool original = false;
    public static int originalvalue = 0;

    public static bool onewp = false;
    public static int onewpvalue = 0;

    public static bool nocol = false;
    public static int nocolvalue = 0;

    public static void Init(string[] args) {
        for (int i = 0; i < args.Length; i++) {
            if (args[i] == "local") local = true;
            if (args[i] == "server") server = true;
            if (args[i] == "create") create = true;
            if (args[i] == "join") join = true;
            if (args[i] == "galera") galera = true;
            if (args[i] == "keimola") track = "keimola";
            if (args[i] == "usa") track = "usa";
            if (args[i] == "germany") track = "germany";
            if (args[i] == "france") track = "france";
            if (args[i] == "elaeintarha") track = "elaeintarha";
            if (args[i] == "imola") track = "imola";
            if (args[i] == "england") track = "england";
            if (args[i] == "suzuka") track = "suzuka";
            if (args[i] == "pentag") track = "pentag";

            if (args[i] == "debugovertake") debugovertake = true;

            if (args[i] == "logturbo") logturbo = true;
            if (args[i] == "logtrack") logtrack = true;
            if (args[i] == "logsimulador") logsimulador = true;
            if (args[i] == "logswitch") logswitch = true;
            if (args[i] == "logtrackmodel") logtrackmodel = true;
            if (args[i] == "logcstopds") logcstopds = true;
            if (args[i] == "logt") logt = true;
            if (args[i] == "logc") logc = true;
            if (args[i] == "logdist") logdist = true;

            if (args[i] == "noturbo") noturbo = true;

            if (args[i] == "noswitch") noswitch = true;
            if (args[i] == "lane0") lane0 = true;
            if (args[i] == "lane1") lane1 = true;

            if (args[i].StartsWith("brakeonstart")) {
                brakeonstart = true;
                brakeonstartvalue = Convert.ToInt32(args[i].Split('=')[1]);
            }

            if (args[i].StartsWith("maxangle")) {
                maxangle = true;
                maxanglevalue = Convert.ToDouble(args[i].Split('=')[1]);
            }

            if (args[i].StartsWith("fixedthrottle")) {
                fixedthrottle = true;
                fixedthrottlevalue = Convert.ToDouble(args[i].Split('=')[1]);
            }

            if (args[i].StartsWith("stopontick")) {
                stopontick = true;
                stopontickvalue = Convert.ToInt32(args[i].Split('=')[1]);
            }

            if (args[i].StartsWith("kamikazeafter")) {
                kamikazeafter = true;
                kamikazeaftervalue = Convert.ToInt32(args[i].Split('=')[1]);
            }

            if (args[i] == "strategy1whenpossible") strategy1whenpossible = true;
            if (args[i] == "strategykamikaze") strategykamikaze = true;

            if (args[i] == "noc") noc = true;
            if (args[i] == "noe") noe = true;
            if (args[i] == "jem") jem = true;
            if (args[i] == "af") af = true;
            if (args[i].StartsWith("td")) {
                td = true;
                try {
                    tdvalue = Convert.ToInt32(args[i].Split('=')[1]);
                } catch {
                    tdvalue = 13;
                }
            }


            if (args[i] == "prost") prost = true;
            if (args[i] == "skipqualify") skipqualify = true;

            if (args[i].StartsWith("original")) {
                original = true;
                originalvalue = Convert.ToInt32(args[i].Split('=')[1]);
                //prost = true;
                skipqualify = true;
            }

            if (args[i].StartsWith("onewp")) {
                onewp = true;
                onewpvalue = Convert.ToInt32(args[i].Split('=')[1]);
                prost = true;
                noc = true;
                strategy1whenpossible = true;
                skipqualify = true;
            }

            if (args[i].StartsWith("nocol")) {
                nocol = true;
                nocolvalue = Convert.ToInt32(args[i].Split('=')[1]);
                prost = true;
                noc = true;
                skipqualify = true;
            }
        }
    }

    public static string GetBotName(string initialBotName) {
        botname = initialBotName;

        if (original) {
            botname = "original_" + originalvalue;
        }
        if (onewp) {
            botname = "onewp_" + onewpvalue;
        }
        if (nocol) {
            botname = "nocol_" + nocolvalue;
        }

        return botname;
    }
}
