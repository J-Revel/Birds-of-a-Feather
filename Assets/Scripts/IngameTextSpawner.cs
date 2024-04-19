using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class IngameTextSpawner : MonoBehaviour
{
    public TMPro.TextMeshPro prefab;

    public List<TMPro.TextMeshPro> instances;
    public List<Entity> instance_entities;

    public EntityQuery query;


    void Start()
    {
        query = new EntityQueryBuilder().WithAll<IngameTextComponent, LocalTransform>().Build(World.DefaultGameObjectInjectionWorld.EntityManager);
    }

    void Update()
    {
        return;
        var entities = query.ToEntityArray(Allocator.Temp);
        var transforms = query.ToComponentDataArray<LocalTransform>(Allocator.Temp);
        var ingame_text = query.ToComponentArray<IngameTextComponent>();
        for (int i = 0; i < entities.Length; i++)
        {
            if (!entities.Contains(entities[i]))
            {
                TMPro.TextMeshPro instance = Instantiate(prefab, transforms[i].Position, transforms[i].Rotation);
                instance.fontSize = ingame_text[i].size;
                instance.text = ingame_text[i].text;
                instances.Add(instance);
                instance_entities.Add(entities[i]);
            }
        }
        for(int i = instance_entities.Count-1; i>=0; i--)
        {
            if (!entities.Contains(instance_entities[i]))
            {
                Destroy(instances[i].gameObject);
                instances.RemoveAt(i);
                instance_entities.RemoveAt(i);
            }

        }
    }
}
