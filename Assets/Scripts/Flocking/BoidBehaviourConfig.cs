using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class BoidBehaviourConfig : ScriptableObject 
{
    public float speed;
    public float attraction_force;
    public float attraction_range;
    public float repulsion_force;
    public float repulsion_range;

    public float neighbour_detection_range;
    public float align_force;

}

