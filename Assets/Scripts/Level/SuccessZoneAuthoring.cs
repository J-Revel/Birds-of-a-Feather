using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Rendering;

public class SuccessZoneAuthoring : MonoBehaviour
{
    public float radius = 5;
    public int success_threshold = 1;
    public float trigger_duration = 3;

    public partial class Baker: Baker<SuccessZoneAuthoring>
    {
        public override void Bake(SuccessZoneAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<SuccessZoneConfig>(entity, new SuccessZoneConfig
            {
                radius = authoring.radius,
                success_threshold = authoring.success_threshold,
                trigger_duration = authoring.trigger_duration,
            });
            AddComponent<SuccessZoneState>(entity);
            AddComponent<SuccessRatioMaterialOverride>(entity);
        }
    }
}

public struct SuccessZoneConfig: IComponentData
{
    public float radius;
    public int success_threshold;
    public float trigger_duration;
}

public struct SuccessZoneState: IComponentData
{
    public float success_time;
}

[MaterialProperty("_Ratio")]
public struct SuccessRatioMaterialOverride: IComponentData
{
    public float Value;
}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup)), UpdateBefore(typeof(BoidMovementSystem))]
public partial class SuccessZoneSystem: SystemBase
{
    protected override void OnCreate()
    {
        base.OnCreate();
        RequireForUpdate<GlobalConfig>();
    }
    protected override void OnUpdate()
    {
        NativeParallelMultiHashMap<Unity.Mathematics.int2, Entity> partition_grid = SystemAPI.ManagedAPI.GetSingleton<BoidMovementSystem.Singleton>().partition_grid;
        float partition_size = SystemAPI.GetSingleton<GlobalConfig>().boid_partition_size;
        float dt = SystemAPI.Time.DeltaTime;
        Entities.WithReadOnly(partition_grid).ForEach((ref SuccessZoneState state, ref SuccessRatioMaterialOverride material_override, in SuccessZoneConfig zone, in LocalTransform transform) =>
        {
            float2 position = transform.Position.xz;

            float max_range = zone.radius;
            int2 min_partition = (int2)((position - max_range) / partition_size);
            int2 max_partition = (int2)((position + max_range) / partition_size);
            int in_range_count = 0;
            for(int i=min_partition.x; i <= max_partition.x; i++)
            {
                for (int j=min_partition.y; j<=max_partition.y; j++)
                {
                    foreach(Entity boid_entity in partition_grid.GetValuesForKey(new int2(i, j)))
                    {
                        LocalTransform boid_transform = SystemAPI.GetComponent<LocalTransform>(boid_entity);
                        if(math.distancesq(boid_transform.Position.xz, position) < zone.radius * zone.radius)
                        {
                            in_range_count++;
                        }
                    }
                }
            }
            if (in_range_count >= zone.success_threshold)
            {
                state.success_time += dt;
            }
            else state.success_time -= dt;
            state.success_time = math.clamp(state.success_time, 0, zone.trigger_duration);
            material_override.Value = math.saturate(state.success_time / zone.trigger_duration);
        }).Run();
        bool all_validated = true;
        bool any_validated = false;
        foreach((SuccessZoneState state, SuccessZoneConfig config) in SystemAPI.Query<SuccessZoneState, SuccessZoneConfig>())
        {
            if (state.success_time < config.trigger_duration)
            {
                all_validated = false;
            }
            else any_validated = true;
        }
        if(all_validated && any_validated)
        {
            LevelLoader.instance.LoadNextLevel();
        }
    }
}
