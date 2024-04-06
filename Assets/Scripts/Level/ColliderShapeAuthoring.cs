using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

public class ColliderShapeAuthoring : MonoBehaviour
{
    public Vector3[] points;
    public float thickness;
    public GlobalConfigAsset global_config;


    private void OnDrawGizmos()
    {
        Vector3[] positions = new Vector3[points.Length];
        for(int i=0; i<points.Length; i++)
        {
            positions[i] = transform.TransformPoint(points[i]);
        }
        Gizmos.DrawLineStrip(positions, true);
    }

    public class Baker : Baker<ColliderShapeAuthoring>
    {
        public override void Bake(ColliderShapeAuthoring authoring)
        {
            int2 min_partition = new int2(int.MaxValue, int.MaxValue);
            int2 max_partition = new int2(int.MinValue, int.MinValue);
            foreach(Vector3 point in authoring.points)
            {
                int2 partition = (int2)(((float3)authoring.transform.TransformPoint(point)).xz / authoring.global_config.collider_partition_size);
                min_partition.x = math.min(partition.x, min_partition.x);
                min_partition.y = math.min(partition.y, min_partition.y);
                max_partition.x = math.max(partition.x, max_partition.x);
                max_partition.y = math.max(partition.y, max_partition.y);
            }
            for(int i=0; i<authoring.points.Length; i++)
            {
                float2 A = ((float3)authoring.points[i]).xz;
                float2 B = ((float3)authoring.points[(i+1)%authoring.points.Length]).xz;
                List<float> splits = new List<float>();
                splits.Add(0);
                splits.Add(1);
                for(int j=min_partition.x; j <= max_partition.x; j++)
                {
                    float split_x = j * authoring.global_config.collider_partition_size;
                    float split_x_ratio = (split_x - A.x) / (B.x - A.x);
                    if(split_x_ratio > 0 && split_x_ratio < 1)
                    {
                        splits.Add(split_x_ratio);
                    }
                }
                for(int j=min_partition.y; j <= max_partition.y; j++)
                {
                    float split_y = j * authoring.global_config.collider_partition_size;
                    float split_y_ratio = (split_y - A.y) / (B.y - A.y);
                    if(split_y_ratio > 0 && split_y_ratio < 1)
                    {
                        splits.Add(split_y_ratio);
                    }
                }
                splits.Sort();
                for(int j=0; j< splits.Count - 1; j++)
                {
                    Entity child_entity = CreateAdditionalEntity(TransformUsageFlags.None, false, "Segment " + i + "-" + j);
                    float2 partition_center = (A + (B - A) * (splits[j] + splits[j + 1]) / 2);
                    AddComponent<ColliderSegment>(child_entity, new ColliderSegment { 
                        start = xz_to_float3(A + (B - A) * splits[j]), 
                        end = xz_to_float3(A + (B - A) * splits[j + 1]),
                        partition = (int2)(partition_center / authoring.global_config.collider_partition_size),
                    });
                }
            }
        }
    }

    private static float3 xz_to_float3(float2 xz)
    {
        return new float3(xz.x, 0, xz.y);
    }
}

public struct ColliderSegment: IComponentData
{
    public float3 start;
    public float3 end;
    public int2 partition;
    public float DistanceFromPointSq(float3 position)
    {
        float3 segment_direction = end - start;
        float3 cross = math.cross(math.normalize(segment_direction), start - position);
        if (math.dot(end - start, position - start) > 0 && math.dot(start - end, position - end) > 0)
        {
            return math.lengthsq(cross);
        }
        else return math.min(math.lengthsq(position - start), math.lengthsq(position - end));
    }

    public float3 CollisionNormal(float3 position){
        float3 vertical_vector = math.cross(end - start, position - start);
        
        return math.normalize(math.cross(end - start, vertical_vector)); 
    }
}

public struct SegmentPartitionTag: IComponentData { }

public partial class SegmentCollisionSystem: SystemBase
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

        Entities.WithDeferredPlaybackSystem<EndFixedStepSimulationEntityCommandBufferSystem>()
            .WithNone<SegmentPartitionTag>()
            .ForEach((EntityCommandBuffer command_buffer, Entity entity, in ColliderSegment segment) =>
        {
            command_buffer.AddComponent<SegmentPartitionTag>(entity);
            partition_grid.Add(segment.partition, entity);
        }).Run();
    }
}



