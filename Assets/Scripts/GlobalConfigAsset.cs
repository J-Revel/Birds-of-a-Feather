using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class GlobalConfigAsset : MonoBehaviour
{
    public float boid_partition_size = 5;
    public float collider_partition_size = 30;
    
    public GlobalConfig Bake()
    {
        return new GlobalConfig
        {
            boid_partition_size = boid_partition_size,
            collider_partition_size = collider_partition_size,
        };
    }
}

public struct GlobalConfig: IComponentData
{
    public float boid_partition_size;
    public float collider_partition_size;
}
