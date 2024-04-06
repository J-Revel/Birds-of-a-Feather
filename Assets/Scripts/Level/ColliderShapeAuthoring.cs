using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
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
                for(int j=min_partition.x; j <= max_partition.x; j++)
                {
                    float split_x = j * authoring.global_config.collider_partition_size;
                    float split_x_ratio = (split_x - A.x) / (B.x / A.x);
                }
            }
        }
    }
}

public struct ColliderSegment: IComponentData
{
    public float3 start;
    public float3 end;
    public float DistanceFromPoint(float3 position)
    {
        float3 segment_direction = end - start;
        float3 cross = math.cross(math.normalize(segment_direction), start - position);
        return math.length(cross);
    }

    public float3 collision_normal { get { return math.cross(end - start, new float3(0, 1, 0)); } }
}

