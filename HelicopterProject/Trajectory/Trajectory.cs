using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Trajectory 
{
    public string name; 

    public GameObject trajectoryFinger;

    public GameObject trajectoryFlight;

    public Sprite icon;

    public Vector3 offset = Vector3.zero;

    public TrickItem trickItem;

}
