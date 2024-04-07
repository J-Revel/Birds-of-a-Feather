using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class IngameTextAuthoring : MonoBehaviour
{
    [TextArea(2, 2)]
    public string text;
    public float size;

    public class Baker : Baker<IngameTextAuthoring>
    {
        public override void Bake(IngameTextAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponentObject<IngameTextAuthoring>(entity, new IngameTextAuthoring { text = authoring.text, size = authoring.size });
        }
    }
}

public class IngameTextComponent: IComponentData
{
    public string text;
    public float size;
}
