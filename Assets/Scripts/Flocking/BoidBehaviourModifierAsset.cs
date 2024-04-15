using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu()]
public class BoidBehaviourModifierAsset : ScriptableObject 
{
    public float speed_change = 0;
    public float random_turn_force_multiplier = 1;
    public float turn_variation_speed_multiplier = 1;
    public float attraction_force_offset = 0;
    public float attraction_range_multiplier = 1;
    public float repulsion_force_offset = 0;
    public float repulsion_range_multiplier = 1;

    public float neighbour_detection_range_multiplier = 1;
    public float align_force_offset = 0;

    public float mouse_attraction_force_offset = 0;

    public float wall_repulsion_range_multiplier = 1;
    public float wall_repulsion_force_offset = 0;

    public Color color;

    public BoidBehaviourModifier Bake()
    {
        return new BoidBehaviourModifier
        {
            speed_change = speed_change,
            random_turn_force_multiplier = random_turn_force_multiplier,
            turn_variation_speed_multiplier = turn_variation_speed_multiplier,
            attraction_force_offset = attraction_force_offset,
            attraction_range_multiplier = attraction_range_multiplier,
            repulsion_force_offset = repulsion_force_offset,
            repulsion_range_multiplier = repulsion_range_multiplier,
            neighbour_detection_range_multiplier = neighbour_detection_range_multiplier,
            align_force_offset = align_force_offset,
            mouse_attraction_force_offset  = mouse_attraction_force_offset,
            wall_repulsion_force_offset  = wall_repulsion_force_offset,
            wall_repulsion_range_multiplier  = wall_repulsion_range_multiplier,
            color = new float4(color.r, color.g, color.b, color.a),
        };
    }
}

public struct BoidBehaviourModifier: IComponentData
{
    public float radius;
    public float speed_change;

    public float random_turn_force_multiplier;
    public float turn_variation_speed_multiplier;

    public float attraction_force_offset;
    public float attraction_range_multiplier;
    public float repulsion_force_offset;
    public float repulsion_range_multiplier;

    public float neighbour_detection_range_multiplier;
    public float align_force_offset;

    public float mouse_attraction_force_offset;

    public float wall_repulsion_range_multiplier;
    public float wall_repulsion_force_offset;

    public float4 color;

}
