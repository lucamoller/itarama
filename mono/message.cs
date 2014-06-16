using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public interface OutputMessage {
    string Serialize();
}

public class CreateRace : OutputMessage {
    public string botName;
    public string botKey;
    public string trackId;
    public string password;
    public int carCount;

    public CreateRace(string botName, string botKey, string trackId, string password, int carCount) {
        this.botName = botName;
        this.botKey = botKey;
        this.trackId = trackId;
        this.password = password;
        this.carCount = carCount;
    }

    public string Serialize() {
        JObject result = new JObject();
        result["msgType"] = "createRace";
        result["data"] = new JObject();
        result["data"]["botId"] = new JObject();
        result["data"]["botId"]["name"] = botName;
        result["data"]["botId"]["key"] = botKey;
        result["data"]["trackName"] = trackId;
        result["data"]["password"] = password;
        result["data"]["carCount"] = carCount;

        return result.ToString(Formatting.None, new JsonConverter[0]);
    }
}

public class JoinRace : OutputMessage {
    public string botName;
    public string botKey;
    public string trackId;
    public string password;
    public int carCount;

    public JoinRace(string botName, string botKey, string trackId, string password, int carCount) {
        this.botName = botName;
        this.botKey = botKey;
        this.trackId = trackId;
        this.password = password;
        this.carCount = carCount;
    }

    public string Serialize() {
        JObject result = new JObject();
        result["msgType"] = "joinRace";
        result["data"] = new JObject();
        result["data"]["botId"] = new JObject();
        result["data"]["botId"]["name"] = botName;
        result["data"]["botId"]["key"] = botKey;
        result["data"]["trackName"] = trackId;
        result["data"]["password"] = password;
        result["data"]["carCount"] = carCount;

        return result.ToString(Formatting.None, new JsonConverter[0]);
    }
}

public class Join : OutputMessage {
    public string name;
    public string key;

    public Join(string name, string key) {
        this.name = name;
        this.key = key;
    }

    public string Serialize() {
        JObject result = new JObject();
        result["msgType"] = "join";
        result["data"] = new JObject();
        result["data"]["name"] = name;
        result["data"]["key"] = key;

        return result.ToString(Formatting.None, new JsonConverter[0]);
    }
}

public class Ping : OutputMessage {
    //public int gameTick;

    public Ping() {
        //this.gameTick = gameTick;
    }

    public string Serialize() {
        JObject result = new JObject();
        result["msgType"] = "ping";

        return result.ToString(Formatting.None, new JsonConverter[0]);
    }
}

public class Throttle : OutputMessage {
    public double value;
    public int gameTick;

    public Throttle(double value, int gameTick) {
        this.value = value;
        this.gameTick = gameTick;
    }

    public string Serialize() {
        JObject result = new JObject();
        result["msgType"] = "throttle";
        result["data"] = value;
        result["gameTick"] = gameTick;

        return result.ToString(Formatting.None, new JsonConverter[0]);
    }
}

public class SwitchLane : OutputMessage {
    public string value;
    public int gameTick;

    public SwitchLane(int leftOrRight, int gameTick) {
        this.gameTick = gameTick;
        if (leftOrRight == -1) {
            value = "Left";
        }
        else if (leftOrRight == 1) {
            value = "Right";
        }
        else {
            value = "null";
        }
    }

    public string Serialize() {
        JObject result = new JObject();
        result["msgType"] = "switchLane";
        result["data"] = value;
        result["gameTick"] = gameTick;

        return result.ToString(Formatting.None, new JsonConverter[0]);
    }
}

public class Turbo : OutputMessage {
    public string value;
    public int gameTick;

    public Turbo(int gameTick) {
        this.value = "Go go go!";
        this.gameTick = gameTick;
    }

    public string Serialize() {
        JObject result = new JObject();
        result["msgType"] = "turbo";
        result["data"] = value;
        result["gameTick"] = gameTick;

        return result.ToString(Formatting.None, new JsonConverter[0]);
    }
}