using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEditor.Rendering;
using UnityEngine;
using static Unity.Entities.SystemBaseDelegates;
using static UnityEngine.GraphicsBuffer;

//[MaterialProperty("_Color")]
public struct BoidColor : IComponentData
{
    public float4 Value;
}

public struct BoidColorState: IComponentData
{
    public float left_time;
    public float right_time;
}

public class BoidAuthoring : MonoBehaviour
{
    public BoidBehaviourConfigAsset config;
    public float start_direction_angle;
    public float display_scale = 1;
    public bool controllable = true;
    public float color_transition_duration = 0.5f;
    public Color default_color;
    public Color left_action_color;
    public Color right_action_color;

    public class Baker : Baker<BoidAuthoring>
    {
        public override void Bake(BoidAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            DependsOn(authoring.config);
            AddComponent<BoidState>(entity,
                new BoidState
                {
                    velocity = new float2(math.cos(authoring.start_direction_angle), math.sin(authoring.start_direction_angle)),
                    start_color = new float4(authoring.default_color.r, authoring.default_color.g, authoring.default_color.b, authoring.default_color.a),
                    target_color = new float4(authoring.default_color.r, authoring.default_color.g, authoring.default_color.b, authoring.default_color.a),
                });
            AddComponent<BoidConfig>(entity, new BoidConfig
            {
                config = authoring.config.Bake(),
                color_transition_duration = authoring.color_transition_duration,
            });
            AddComponent<BoidBehaviourModifier>(entity, new BoidBehaviourModifier
            {
                attraction_range_multiplier = 1,
                neighbour_detection_range_multiplier = 1,
                random_turn_force_multiplier = 1,
                repulsion_range_multiplier = 1,
                turn_variation_speed_multiplier = 1,
                wall_repulsion_range_multiplier = 1,
            });
            AddComponent<BoidNeighbourData>(entity);
            AddComponent<BoidDisplay>(entity, new BoidDisplay { display_scale = authoring.display_scale });
            if(authoring.controllable)
            {
                AddComponent<ControllableBoidTag>(entity);
            }
            AddComponent<BoidColor>(entity, new BoidColor { Value = new float4(authoring.default_color.r, authoring.default_color.g, authoring.default_color.b, authoring.default_color.a)});
        }
    }
}

public struct ControllableBoidTag: IComponentData
{

}

public struct BoidDisplay : IComponentData
{
    public float display_scale;
}

public struct BoidConfig: IComponentData
{
    public float color_transition_duration;
    public BoidBehaviourConfig config;
}

public struct BoidActiveBehaviour: IComponentData
{
    public BoidBehaviourModifier modifier;
}

public struct BoidState : IComponentData
{
    public float2 velocity;
    public float2 acceleration;
    public float4 start_color;
    public float4 target_color;
    public float color_transition_time;
    public float turn_position;
}

public struct BoidRandom: IComponentData
{
    public Unity.Mathematics.Random random;
}

public struct BoidPartitionCell : IComponentData
{
    public int2 min_partition;
    public int2 max_partition;
}

public struct BoidNeighbourData : IComponentData
{
    public float2 average_velocity;
    public int neighbour_count;
}


[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class BoidDisplaySystem : SystemBase
{
    protected override void OnUpdate()
    {
        float dt = SystemAPI.Time.DeltaTime;
        Entities.ForEach((ref BoidColor out_color, ref BoidState state, in BoidConfig config) =>
        {
            out_color.Value = math.lerp(state.start_color, state.target_color, state.color_transition_time / state.color_transition_time);
            state.color_transition_time += dt;
            if (state.color_transition_time >= config.color_transition_duration)
                state.color_transition_time = config.color_transition_duration;
            if(math.any(config.config.color != state.target_color))
            {
                state.start_color = out_color.Value;
                state.target_color = config.config.color;
                state.color_transition_time = 0;
            }
        }).Schedule();
    }
}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup)), UpdateAfter(typeof(BehaviourZoneSystem))]
public partial class BoidMovementSystem : SystemBase
{
    public class Singleton : IComponentData
    {
        public NativeParallelMultiHashMap<int2, Entity> partition_grid;
        public Unity.Mathematics.Random random;
    }

    protected override void OnCreate()
    {
        base.OnCreate();
        RequireForUpdate<GlobalConfig>();
        EntityManager.CreateSingleton<Singleton>(new Singleton
        {
            partition_grid = new NativeParallelMultiHashMap<int2, Entity>(4096 * 4, Allocator.Persistent),
            random = new Unity.Mathematics.Random((uint)(System.DateTime.Now.TimeOfDay.TotalSeconds * 100 + SystemAPI.Time.ElapsedTime * 100)),
        });
    }

    protected override void OnUpdate()
    {
        float dt = SystemAPI.Time.DeltaTime;

        NativeParallelMultiHashMap<int2, Entity> partition_grid = SystemAPI.ManagedAPI.GetSingleton<Singleton>().partition_grid;
        float boids_partition_size = SystemAPI.GetSingleton<GlobalConfig>().boid_partition_size;
        float collider_partition_size = SystemAPI.GetSingleton<GlobalConfig>().collider_partition_size;
        partition_grid.Clear();
        NativeParallelMultiHashMap<int2, Entity> partition = partition_grid;
        NativeParallelMultiHashMap<int2, Entity> wall_partition = SystemAPI.ManagedAPI.GetSingleton<SegmentCollisionSystem.Singleton>().partition_grid;
        EntityQuery query = Entities.WithNone<BoidRandom>().WithAll<BoidState>().ToQuery();


        EntityCommandBuffer command_buffer = new EntityCommandBuffer(Allocator.Temp);
        foreach(Entity entity in query.ToEntityArray(Allocator.Temp))
        {
            command_buffer.AddComponent<BoidRandom>(entity, new BoidRandom { 
                random = new Unity.Mathematics.Random((uint)(System.DateTime.Now.TimeOfDay.TotalSeconds * 100 + SystemAPI.Time.ElapsedTime * 100 + entity.Index * 128)),
            });
        }
        command_buffer.Playback(EntityManager);

        Entities.WithDeferredPlaybackSystem<EndFixedStepSimulationEntityCommandBufferSystem>()
            .WithNone<BoidPartitionCell>()
            .ForEach((EntityCommandBuffer command_buffer, Entity entity, 
                in LocalTransform transform, in BoidState state, in BoidConfig config) =>
            {
                int2 cell_min = (int2)((transform.Position.xz + config.config.radius) / boids_partition_size);
                int2 cell_max = (int2)((transform.Position.xz + config.config.radius) / boids_partition_size);

                command_buffer.AddComponent<BoidPartitionCell>(entity, new BoidPartitionCell {
                    min_partition = cell_min,
                    max_partition = cell_max,
                });

                for(int i=cell_min.x; i<=cell_max.x; i++)
                {
                    for(int j=cell_min.y; j<=cell_max.y; j++)
                    {
                        partition.Add(new int2(i, j), entity);
                    }
                }
            }).Schedule();
        Entities
            .WithName("Position_Update")
            .ForEach((Entity entity, ref LocalTransform transform, ref BoidState state, ref BoidPartitionCell partition_cell, in BoidConfig config, in BoidDisplay display) =>
            {
                float3 velocity = new float3(state.velocity.x, 0, state.velocity.y);
                transform.Position += velocity * dt;
                transform.Scale = display.display_scale;
                if(math.any(velocity != 0))
                    transform.Rotation = quaternion.LookRotation(velocity, new float3(0, 1, 0));
                int2 new_partition_cell_min = (int2)((transform.Position.xz - config.config.radius) / boids_partition_size);
                int2 new_partition_cell_max = (int2)((transform.Position.xz + config.config.radius) / boids_partition_size);
                for(int i=partition_cell.min_partition.x; i<=partition_cell.max_partition.x; i++)
                {
                    for (int j = partition_cell.min_partition.y; j <= partition_cell.max_partition.y; j++)
                    {
                        if(i < new_partition_cell_min.x || i > new_partition_cell_max.x || j < new_partition_cell_min.y || j > new_partition_cell_max.y)
                            partition.Remove(new int2(i, j), entity);
                    }
                }
                for(int i= new_partition_cell_min.x; i<=new_partition_cell_max.x; i++)
                {
                    for (int j = new_partition_cell_min.y; j <= new_partition_cell_max.y; j++)
                    {
                        if(i < partition_cell.min_partition.x || i > partition_cell.max_partition.x || j < partition_cell.min_partition.y || j > partition_cell.max_partition.y)
                            partition.Add(new int2(i, j), entity);
                    }
                }
            }).Schedule();
        var writer = partition.AsParallelWriter();
        Entities.WithName("Partition_Update").ForEach((Entity entity, in LocalTransform transform, in BoidState state) =>
        {
            int2 cell = (int2)(transform.Position.xz / boids_partition_size);
            writer.Add(cell, entity);
        }).ScheduleParallel();
        Entities
            .WithName("Neighbour_Data_Compute")
            .WithReadOnly(partition)
            .ForEach((Entity entity, ref BoidNeighbourData neighbour_data, in BoidState state, in LocalTransform transform, in BoidPartitionCell partition_cell, in BoidConfig config) =>
            {
                float2 position = transform.Position.xz;

                float max_range = config.config.neighbour_detection_range;
                int2 min_partition = (int2)((position - max_range) / boids_partition_size);
                int2 max_partition = (int2)((position + max_range) / boids_partition_size);

                float2 neighbour_velocity_sum = float2.zero;
                int neighbour_count = 0;
                NativeHashSet<Entity> handled_neighbours = new NativeHashSet<Entity>(64, Allocator.Temp);
                for (int i = min_partition.x; i <= max_partition.x; i++)
                {
                    for (int j = min_partition.y; j <= max_partition.y; j++)
                    {
                        foreach (Entity neighbour_entity in partition.GetValuesForKey(new int2(i, j)))
                        {
                            if (neighbour_entity == entity || handled_neighbours.Contains(neighbour_entity))
                                continue;
                            handled_neighbours.Add(neighbour_entity);
                            neighbour_velocity_sum += SystemAPI.GetComponent<BoidState>(neighbour_entity).velocity;
                            neighbour_count++;
                        }
                    }
                }
                if (neighbour_count > 0)
                {
                    neighbour_data.average_velocity = neighbour_velocity_sum / neighbour_count;
                    neighbour_data.neighbour_count = neighbour_count;

            }
        }).ScheduleParallel();
        float3 mouse_pos_screen = (float3)Input.mousePosition;
        mouse_pos_screen.z = 10;
        float2 mouse_position = ((float3)Camera.main.ScreenToWorldPoint(mouse_pos_screen)).xz;

        Singleton singleton = SystemAPI.ManagedAPI.GetSingleton<Singleton>();
        Entities
            .WithName("Attraction_Repulsion")
            .WithReadOnly(partition)
            .WithReadOnly(wall_partition)
            .ForEach((Entity entity, int entityInQueryIndex, ref BoidState state, ref BoidRandom random_component,
                in LocalTransform transform, in BoidBehaviourModifier behaviour_modifier, in BoidConfig config, in BoidNeighbourData neighbour_data) =>
            {
                state.acceleration = new float2();
                float2 position = transform.Position.xz;


                float attraction_range = config.config.attraction_range * behaviour_modifier.attraction_range_multiplier;
                float repulsion_range = config.config.repulsion_range * behaviour_modifier.repulsion_range_multiplier;
                float wall_repulsion_range = config.config.wall_repulsion_range * behaviour_modifier.wall_repulsion_range_multiplier;
                float neighbour_detection_range = config.config.neighbour_detection_range * behaviour_modifier.neighbour_detection_range_multiplier;
                float attraction_force = config.config.attraction_force + behaviour_modifier.attraction_force_offset;
                float repulsion_force = config.config.repulsion_force + behaviour_modifier.repulsion_force_offset;
                float wall_repulsion_force = config.config.wall_repulsion_force + behaviour_modifier.wall_repulsion_force_offset;
                float align_force = config.config.align_force + behaviour_modifier.align_force_offset;
                float mouse_attraction_force = config.config.mouse_attraction_force + behaviour_modifier.mouse_attraction_force_offset;
                float turn_variation_speed = config.config.turn_variation_speed * behaviour_modifier.turn_variation_speed_multiplier;
                float speed = config.config.speed * behaviour_modifier.speed_multiplier;
                float random_turn_force = config.config.random_turn_force * behaviour_modifier.random_turn_force_multiplier;

                float max_range = math.max(attraction_range, repulsion_range);
                int2 min_partition = (int2)((position - config.config.radius - max_range) / boids_partition_size);
                int2 max_partition = (int2)((position + config.config.radius + max_range) / boids_partition_size);
                NativeHashSet<Entity> handled_neighbours = new NativeHashSet<Entity>(64, Allocator.Temp);
                
                
                for (int i = min_partition.x; i <= max_partition.x; i++)
                {
                    for (int j = min_partition.y; j <= max_partition.y; j++)
                    {
                        foreach (Entity neighbour_entity in partition.GetValuesForKey(new int2(i, j)))
                        {
                            BoidConfig neighbour_config = SystemAPI.GetComponent<BoidConfig>(neighbour_entity);
                            if (neighbour_entity == entity || handled_neighbours.Contains(neighbour_entity))
                                continue;
                            handled_neighbours.Add(neighbour_entity);
                            float2 neighbour_position = SystemAPI.GetComponent<LocalTransform>(neighbour_entity).Position.xz;
                            float2 direction = math.normalizesafe(neighbour_position - position);
                            float distancesq = math.distancesq(position, neighbour_position);
                            float active_attraction_range = attraction_range + config.config.radius + neighbour_config.config.radius;
                            float active_repulsion_range = repulsion_range + config.config.radius + neighbour_config.config.radius;
                            if (distancesq < active_attraction_range * active_attraction_range)
                            {
                                state.acceleration += direction * attraction_force;
                            }
                            if (distancesq < active_repulsion_range * active_repulsion_range)
                            {
                                float ratio = 1 - (math.sqrt(distancesq / active_repulsion_range / active_repulsion_range));
                                state.acceleration += -direction * repulsion_force * ratio * ratio;
                            }
                        }
                    }
                }
                max_range = wall_repulsion_range;
                min_partition = (int2)((position - max_range) / collider_partition_size);
                max_partition = (int2)((position + max_range) / collider_partition_size);
                
                for (int i = min_partition.x; i <= max_partition.x; i++)
                {
                    for (int j = min_partition.y; j <= max_partition.y; j++)
                    {
                        foreach (Entity wall_entity in wall_partition.GetValuesForKey(new int2(i, j)))
                        {
                            if (wall_entity == entity)
                                continue;
                            ColliderSegment segment = SystemAPI.GetComponent<ColliderSegment>(wall_entity);
                            float distancesq = segment.DistanceFromPointSq(new float3(position.x, 0, position.y));
                            if (distancesq < wall_repulsion_range)
                            {
                                float ratio = 1 - (math.sqrt(distancesq / wall_repulsion_range/ wall_repulsion_range));
                                state.acceleration -= segment.CollisionNormal(new float3(position.x, 0, position.y)).xz * wall_repulsion_force * ratio;
                            }
                        }
                    }
                }
                state.acceleration += math.normalizesafe(neighbour_data.average_velocity) * align_force;
                float2 mouse_direction = mouse_position - position;
                float3 velocity_3D = new float3(state.velocity.x, 0, state.velocity.y);
                float3 cross = math.normalizesafe(math.cross(velocity_3D, new float3(mouse_direction.x, 0, mouse_direction.y)));
                float3 force_direction = math.cross(cross, velocity_3D);
                state.velocity += force_direction.xz * mouse_attraction_force;
                state.turn_position += random_component.random.NextFloat(-1, 1) * turn_variation_speed;
                state.turn_position = math.clamp(state.turn_position, -1, 1);
                state.velocity += force_direction.xz * state.turn_position * random_turn_force;

                state.velocity = state.velocity + state.acceleration * dt;
                var targetvelocity = math.normalize(state.velocity) * speed;
                state.velocity = (new float2(0.9) * state.velocity + new float2(0.1) * targetvelocity);
            }).ScheduleParallel();
    }
}
