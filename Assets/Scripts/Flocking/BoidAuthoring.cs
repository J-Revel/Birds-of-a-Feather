using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor.Rendering;
using UnityEngine;

public class BoidAuthoring : MonoBehaviour
{
    public BoidBehaviourConfigAsset config;
    public float start_direction_angle;
    public float display_scale = 1;

    public class Baker : Baker<BoidAuthoring>
    {
        public override void Bake(BoidAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            DependsOn(authoring.config);
            AddComponent<BoidState>(entity,
                new BoidState
                {
                    velocity = new float2(math.cos(authoring.start_direction_angle), math.sin(authoring.start_direction_angle))
                });
            AddComponent<BoidConfig>(entity, new BoidConfig
            {
                config = authoring.config.Bake(),
            });
            AddComponent<BoidNeighbourData>(entity);
            AddComponent<BoidDisplay>(entity, new BoidDisplay { display_scale = authoring.display_scale });
        }
    }
}

public struct BoidDisplay : IComponentData
{
    public float display_scale;
}

public struct BoidConfig: IComponentData
{
    public BoidBehaviourConfig config;
}

public struct BoidState : IComponentData
{
    public float2 velocity;
    public float2 acceleration;
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

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup)), UpdateAfter(typeof(BehaviourZoneSystem))]
public partial class BoidMovementSystem : SystemBase
{
    public class Singleton : IComponentData
    {
        public NativeParallelMultiHashMap<int2, Entity> partition_grid;

    }

    protected override void OnCreate()
    {
        base.OnCreate();
        EntityManager.CreateSingleton<Singleton>(new Singleton { 
            partition_grid = new NativeParallelMultiHashMap<int2, Entity>(4096 * 4, Allocator.Persistent),
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

        Entities.WithDeferredPlaybackSystem<EndFixedStepSimulationEntityCommandBufferSystem>()
            .WithNone<BoidPartitionCell>()
            .ForEach((EntityCommandBuffer command_buffer, Entity entity, in LocalTransform transform, in BoidState state, in BoidConfig config) =>
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

        Entities
            .WithName("Attraction_Repulsion")
            .WithReadOnly(partition)
            .WithReadOnly(wall_partition)
            .ForEach((Entity entity, ref BoidState state, in LocalTransform transform, in BoidPartitionCell partition_cell, in BoidConfig config, in BoidNeighbourData neighbour_data) =>
            {
                state.acceleration = new float2();
                float2 position = transform.Position.xz;

                float max_range = math.max(config.config.attraction_range, config.config.repulsion_range);
                int2 min_partition = (int2)((position - config.config.radius - max_range) / boids_partition_size);
                int2 max_partition = (int2)((position + config.config.radius + max_range) / boids_partition_size);
                NativeHashSet<Entity> handled_neighbours = new NativeHashSet<Entity>(64, Allocator.Temp);
                Entity closest_entity = entity;
                float mindistance = 10000;

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
                            float2 direction = math.all(neighbour_position == position) ? float2.zero : math.normalize(neighbour_position - position);
                            float distancesq = math.distancesq(position, neighbour_position);
                            float active_attraction_range = config.config.attraction_range + config.config.radius + neighbour_config.config.radius;
                            float active_repulsion_range = config.config.repulsion_range + config.config.radius + neighbour_config.config.radius;
                            if (distancesq < active_attraction_range * active_attraction_range)
                            {
                                state.acceleration += direction * config.config.attraction_force;
                            }
                            if (distancesq < active_repulsion_range * active_repulsion_range)
                            {
                                state.acceleration += -direction * config.config.repulsion_force;
                            }

                            if (distancesq < mindistance)
                            {
                                mindistance = distancesq;
                                closest_entity = neighbour_entity;
                            }
                        }
                    }
                }

                if (closest_entity != entity)
                {
                    var v1 = math.normalize(state.velocity);
                    var neighbour_position =
                        math.normalize(
                             SystemAPI.GetComponent<LocalTransform>(closest_entity).Position.xz);
                    var crossdot = v1.y * neighbour_position.x - v1.x * neighbour_position.y;
                    var scalar = math.dot(v1, neighbour_position);
                    if (scalar > 0)
                    {
                        var norm = new float2(-v1.y, v1.x);
                        state.acceleration += norm * crossdot * 0;
                        state.velocity -= state.velocity * math.abs(crossdot) * (float)0.01;
                    }
                }


                max_range = config.config.wall_repulsion_range;
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
                            float distance = segment.DistanceFromPointSq(new float3(position.x, 0, position.y));
                            if (distance < config.config.wall_repulsion_range)
                            {
                                state.acceleration -= segment.CollisionNormal(new float3(position.x, 0, position.y)).xz * config.config.wall_repulsion_force;
                            }


                        }
                    }
                }
                if (math.all(neighbour_data.average_velocity != float2.zero))
                    state.acceleration += math.normalize(neighbour_data.average_velocity) * config.config.align_force;
                float2 mouse_direction = mouse_position - position;
                float3 velocity_3D = new float3(state.velocity.x, 0, state.velocity.y);
                float3 cross = math.normalize(math.cross(velocity_3D, new float3(mouse_direction.x, 0, mouse_direction.y)));
                float3 force_direction = math.cross(cross, velocity_3D);
                state.velocity += force_direction.xz * config.config.mouse_attraction_force;
                state.velocity = state.velocity + state.acceleration * dt;
                state.velocity = math.normalize(state.velocity) * config.config.speed;
            }).ScheduleParallel();
    }
}
