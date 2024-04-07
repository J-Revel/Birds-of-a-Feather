using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public enum LevelAction { Left, Right }
public class ActionStockBar : MonoBehaviour
{
    private RectTransform rect_transform;
    private EntityManager entity_manager;
    private EntityQuery query;
    public LevelAction action;
    void Start()
    {
        entity_manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        query = new EntityQueryBuilder(Allocator.Temp).WithAll<LevelConfig, LevelState>().Build(entity_manager);
        rect_transform = GetComponent<RectTransform>();
    }

    void Update()
    {
        LevelState level_state = query.GetSingleton<LevelState>();
        LevelConfig level_config = query.GetSingleton<LevelConfig>();
        float ratio = 0;
        switch (action)
        {
            case LevelAction.Left:
                ratio = level_state.left_use_time / level_config.left_use_duration;
                break;
            case LevelAction.Right:
                ratio = level_state.right_use_time / level_config.right_use_duration;
                break;
        }
        rect_transform.anchorMax = new float2(1, 1 - ratio);
    }
}
