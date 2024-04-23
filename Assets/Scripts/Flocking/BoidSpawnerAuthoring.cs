using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class BoidSpawnerAuthoring : MonoBehaviour
{
    public float interval = 0.3f;
    public float random_position = 1;
    public int flock_count = 10;
    public int flock_size = 1;
    public float angle_range = 30;
    public BoidAuthoring prefab;

    public class Baker : Baker<BoidSpawnerAuthoring>
    {
        public override void Bake(BoidSpawnerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<BoidSpawner>(entity, new BoidSpawner
            {
                angle_range = authoring.angle_range,
                interval = authoring.interval,
                prefab = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic),
                time = 0,
                random = new Unity.Mathematics.Random(10),
                flock_count = authoring.flock_count,
                flock_size = authoring.flock_size,
            });
        }
    }
}

public struct BoidSpawner : IComponentData
{
    public float interval;
    public float random_position;
    public float angle_range;
    public Entity prefab;
    public float time;
    public Unity.Mathematics.Random random;
    public int flock_count;
    public int flock_index;
    public int flock_size;
}

public partial class BoidSpawnerUpdateSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float dt = SystemAPI.Time.DeltaTime;
        if (!SystemAPI.HasSingleton<LevelConfig>())
            return;
        LevelConfig level_config = SystemAPI.GetSingleton<LevelConfig>();
        Entities.WithDeferredPlaybackSystem<EndSimulationEntityCommandBufferSystem>()
            .ForEach((EntityCommandBuffer command_buffer, ref BoidSpawner spawner, in LocalTransform transform) =>
            {
                spawner.time += dt;
                if (spawner.time > spawner.interval && spawner.flock_index < spawner.flock_count)
                {
                    spawner.flock_index++;
                    spawner.time -= spawner.interval;
                    for(int i=0; i<spawner.flock_size; i++)
                    {
                        Entity boid = command_buffer.Instantiate(spawner.prefab);
                        float2 random_offset = spawner.random.NextFloat2();
                        command_buffer.SetComponent<LocalTransform>(boid, new LocalTransform
                        {
                            Position = transform.Position + new float3(random_offset.x, 0, random_offset.y) * spawner.random_position,
                            Rotation = transform.Rotation,
                            Scale = 1,
                        });

                        command_buffer.SetComponent<BoidState>(boid, new BoidState
                        {
                            velocity = spawner.random.NextFloat2(-10.0f, 10.0f),
                        });
                        command_buffer.SetComponent<BoidConfig>(boid, new BoidConfig
                        {
                            config = level_config.default_behaviour_config
                        });
                    }
                }

            }).Schedule();
    }
}
