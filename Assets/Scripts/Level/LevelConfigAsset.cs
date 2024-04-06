using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class LevelConfigAsset : ScriptableObject 
{
    public BoidBehaviourConfigAsset default_behaviour_config;
    public PlayerActionConfig left_click_action;
    public PlayerActionConfig right_click_action;
}

