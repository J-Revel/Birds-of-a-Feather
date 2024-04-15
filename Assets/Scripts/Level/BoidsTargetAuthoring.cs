using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class BoidsTargetAuthoring : MonoBehaviour
{
    public Vector3[] points;
    public float thickness;
    public GlobalConfigAsset global_config;

    private void OnDrawGizmos()
    {
        Vector3[] positions = new Vector3[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            positions[i] = transform.TransformPoint(points[i]);
        }
        Gizmos.DrawLineStrip(positions, true);
    }


    public class Baker : Baker<BoidsTargetAuthoring>
    {
        public override void Bake(BoidsTargetAuthoring authoring)
        {
            int2 min_partition = new int2(int.MaxValue, int.MaxValue);
            int2 max_partition = new int2(int.MinValue, int.MinValue);
            foreach (Vector3 point in authoring.points)
            {
                int2 partition = (int2)(((float3)authoring.transform.TransformPoint(point)).xz / authoring.global_config.collider_partition_size);
                min_partition.x = math.min(partition.x, min_partition.x);
                min_partition.y = math.min(partition.y, min_partition.y);
                max_partition.x = math.max(partition.x, max_partition.x);
                max_partition.y = math.max(partition.y, max_partition.y);
            }
            for (int i = 0; i < authoring.points.Length; i++)
            {
                float3 point = authoring.transform.TransformPoint(authoring.points[i]);
                int next_point_index = (i + 1) % authoring.points.Length;
                float3 next_point = authoring.transform.TransformPoint(authoring.points[next_point_index]);
                float2 A = point.xz;
                float2 B = next_point.xz;
                List<float> splits = new List<float>();
                splits.Add(0);
                splits.Add(1);
                for (int j = min_partition.x; j <= max_partition.x; j++)
                {
                    float split_x = j * authoring.global_config.collider_partition_size;
                    float split_x_ratio = (split_x - A.x) / (B.x - A.x);
                    if (split_x_ratio > 0 && split_x_ratio < 1)
                    {
                        splits.Add(split_x_ratio);
                    }
                }
                for (int j = min_partition.y; j <= max_partition.y; j++)
                {
                    float split_y = j * authoring.global_config.collider_partition_size;
                    float split_y_ratio = (split_y - A.y) / (B.y - A.y);
                    if (split_y_ratio > 0 && split_y_ratio < 1)
                    {
                        splits.Add(split_y_ratio);
                    }
                }
                splits.Sort();
                for (int j = 0; j < splits.Count - 1; j++)
                {
                    Entity child_entity = CreateAdditionalEntity(TransformUsageFlags.None, false, "Segment " + i + "-" + j);
                    float2 partition_center = (A + (B - A) * (splits[j] + splits[j + 1]) / 2);
                    AddComponent<ColliderSegment>(child_entity, new ColliderSegment
                    {
                        start = ColliderShapeAuthoring.xz_to_float3(A + (B - A) * splits[j]),
                        end = ColliderShapeAuthoring.xz_to_float3(A + (B - A) * splits[j + 1]),
                        partition = (int2)(partition_center / authoring.global_config.collider_partition_size),
                        isTarget = true,
                        partitionSize = 1.0f / (float)(splits.Count - 1) / (float)authoring.points.Length,
                    });
                }
            }
        }
    }
}