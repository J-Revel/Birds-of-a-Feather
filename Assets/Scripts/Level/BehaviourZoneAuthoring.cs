using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

public class BehaviourZoneAuthoring : MonoBehaviour
{

}

public struct BehaviourZone: IComponentData
{
    public float radius;
    public BoidConfig config;
}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup)), UpdateBefore(typeof(BoidMovementSystem))]
public partial class BehaviourZoneSystem: SystemBase
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
        Entities.WithReadOnly(partition_grid).ForEach((in BehaviourZone zone, in LocalTransform transform) =>
        {
            float2 position = transform.Position.xz;

            float max_range = zone.radius;
            int2 min_partition = (int2)((position - max_range) / partition_size);
            int2 max_partition = (int2)((position + max_range) / partition_size);
            for(int i=min_partition.x; i <= max_partition.x; i++)
            {
                for (int j=min_partition.y; j<=max_partition.y; j++)
                {
                    foreach(Entity boid_entity in partition_grid.GetValuesForKey(new int2(i, j)))
                    {
                        SystemAPI.SetComponent<BoidConfig>(boid_entity, zone.config);
                    }
                }
            }
        }).Run();
    }
}
