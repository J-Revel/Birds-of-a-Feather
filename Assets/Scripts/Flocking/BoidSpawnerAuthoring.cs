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
    public int maxspawn = 10;
    public int countspawn = 0;
    public float angle_range = 30;
    public BoidAuthoring prefab;
    public int levelcompleteboids = 1;

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
                maxspawn = authoring.maxspawn,
                levelcompleteboids = authoring.levelcompleteboids,
            });
        }
    }
}

public struct BoidSpawner : IComponentData
{
    public float interval;
    public float angle_range;
    public Entity prefab;
    public float time;
    public Unity.Mathematics.Random random;
    public int maxspawn;
    public int countspawn;
    public int levelcompleteboids;
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
                if (spawner.time > spawner.interval && spawner.countspawn <= spawner.maxspawn)
                {
                    spawner.countspawn++;
                    spawner.time -= spawner.interval;
                    Entity boid = command_buffer.Instantiate(spawner.prefab);
                    command_buffer.SetComponent<LocalTransform>(boid, new LocalTransform
                    {
                        Position = transform.Position,
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

            }).Schedule();
    }
}
