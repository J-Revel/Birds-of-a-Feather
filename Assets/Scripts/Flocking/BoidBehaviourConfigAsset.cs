using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu()]
public class BoidBehaviourConfigAsset : ScriptableObject 
{
    public float radius;
    public float speed;
    public float random_turn_force;
    public float turn_variation_speed;
    public float attraction_force;
    public float attraction_range;
    public float repulsion_force;
    public float repulsion_range;

    public float neighbour_detection_range;
    public float align_force;

    public float mouse_attraction_force;

    public float wall_repulsion_range;
    public float wall_repulsion_force;
    public Color color;
    public BoidBehaviourConfig Bake()
    {
        return new BoidBehaviourConfig
        {
            radius = radius,
            speed = speed,
            random_turn_force = random_turn_force,
            turn_variation_speed = turn_variation_speed,
            attraction_force = attraction_force,
            attraction_range = attraction_range,
            repulsion_force = repulsion_force,
            repulsion_range = repulsion_range,
            neighbour_detection_range = neighbour_detection_range,
            align_force = align_force,
            mouse_attraction_force = mouse_attraction_force,
            wall_repulsion_force = wall_repulsion_force,
            wall_repulsion_range = wall_repulsion_range,
            color = new float4(color.r, color.g, color.b, color.a),
        };
    }


    public static BoidBehaviourConfig ConvertFromModifier(BoidBehaviourConfig config, BoidBehaviourModifier modifier)
    {
        return new BoidBehaviourConfig
        {
            radius = config.radius,
            speed = config.speed,
            random_turn_force = config.random_turn_force * modifier.random_turn_force_multiplier,
            turn_variation_speed = config.turn_variation_speed * modifier.turn_variation_speed_multiplier,
            attraction_force = config.attraction_force,
            attraction_range = config.attraction_range,
            repulsion_force = config.repulsion_force,
            repulsion_range = config.repulsion_range,
            neighbour_detection_range = config.neighbour_detection_range,
            align_force = config.align_force,
            mouse_attraction_force = config.mouse_attraction_force,
            wall_repulsion_force = config.wall_repulsion_force,
            wall_repulsion_range = config.wall_repulsion_range,
            color = config.color,
        };
    }
}

public struct BoidBehaviourConfig
{
    public float radius;
    public float speed;

    public float random_turn_force;
    public float turn_variation_speed;

    public float attraction_force;
    public float attraction_range;
    public float repulsion_force;
    public float repulsion_range;

    public float neighbour_detection_range;
    public float align_force;

    public float mouse_attraction_force;

    public float wall_repulsion_range;
    public float wall_repulsion_force;

    public float4 color;

}
