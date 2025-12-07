using System;
using UnityEngine;

[Serializable]
public class GhostData
{
    public int id;
    public string name;
    public string personality;
    public GhostLocation location;
    public float visibility_radius_m;
    public GhostInteraction interaction;
    public string created_at;
}

[Serializable]
public class GhostLocation
{
    public double lat;
    public double lng;
}

[Serializable]
public class GhostInteraction
{
    public string type;
    public string riddle;
    public string correct_answer;
    public GhostReward reward;
}

[Serializable]
public class GhostReward
{
    public string type;
    public string value;
}

[Serializable]
public class GhostListResponse
{
    public GhostData[] ghosts;
}
