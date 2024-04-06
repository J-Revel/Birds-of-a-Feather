using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class GlobalConfigAuthoring : MonoBehaviour
{
    GlobalConfigAsset config;

    public class Baker: Baker<GlobalConfigAuthoring>
    {
        public override void Bake(GlobalConfigAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent<GlobalConfig>(entity, authoring.config.Bake());
        }
    }
}

