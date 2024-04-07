using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class LevelConfigAuthoring: MonoBehaviour
{
    public LevelConfigAsset config_asset;

    public class Baker : Baker<LevelConfigAuthoring>
    {
        public override void Bake(LevelConfigAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            DependsOn(authoring.config_asset);
            DependsOn(authoring.config_asset.default_behaviour_config);
            DependsOn(authoring.config_asset.left_click_action.behaviour_config);
            DependsOn(authoring.config_asset.right_click_action.behaviour_config);
            AddComponent(entity, new LevelConfig
            {
                default_behaviour_config = authoring.config_asset.default_behaviour_config.Bake(),
                left_behaviour_config = authoring.config_asset.left_click_action.behaviour_config.Bake(),
                right_behaviour_config = authoring.config_asset.right_click_action.behaviour_config.Bake(),
                left_action_type = authoring.config_asset.left_click_action.action_type,
                right_action_type = authoring.config_asset.right_click_action.action_type,
                left_spawn_config = authoring.config_asset.left_click_action.spawn_config,
                right_spawn_config = authoring.config_asset.right_click_action.spawn_config,
            });
        }
    }
}

public enum PlayerActionType
{
    SwitchBehaviour,
    SpawnBoid,
}

[System.Serializable]
public struct SpawnBoidConfig
{
    public float spawn_interval;
    public int spawn_count;
    public float spawn_radius;
}

[System.Serializable]
public struct PlayerActionConfig
{
    public PlayerActionType action_type;
    public BoidBehaviourConfigAsset behaviour_config;
    public SpawnBoidConfig spawn_config;
}

public struct LevelConfig: IComponentData
{
    public BoidBehaviourConfig default_behaviour_config;
    public PlayerActionType left_action_type;
    public BoidBehaviourConfig left_behaviour_config;
    public SpawnBoidConfig left_spawn_config;
    public PlayerActionType right_action_type;
    public BoidBehaviourConfig right_behaviour_config;
    public SpawnBoidConfig right_spawn_config;
}

public partial class PlayerInputSystem: SystemBase
{
    protected override void OnUpdate()
    {
        LevelConfig level_config = SystemAPI.GetSingleton<LevelConfig>();
        EntityCommandBuffer command_buffer = new EntityCommandBuffer(Allocator.Temp);
        if(Input.GetMouseButtonDown(0))
        {
            foreach(Entity entity in Entities.WithAll<BoidConfig, ControllableBoidTag>().ToQuery().ToEntityArray(Allocator.Temp))
            {
                command_buffer.SetComponent<BoidConfig>(entity, new BoidConfig { config = level_config.left_behaviour_config });
            }
        }
        else if(Input.GetMouseButtonUp(0))
        {
            foreach(Entity entity in Entities.WithAll<BoidConfig, ControllableBoidTag>().ToQuery().ToEntityArray(Allocator.Temp))
            {
                command_buffer.SetComponent<BoidConfig>(entity, new BoidConfig { config = level_config.default_behaviour_config});
            }
        }

        if(Input.GetMouseButtonDown(1))
        {
            foreach(Entity entity in Entities.WithAll<BoidConfig, ControllableBoidTag>().ToQuery().ToEntityArray(Allocator.Temp))
            {
                command_buffer.SetComponent<BoidConfig>(entity, new BoidConfig { config = level_config.right_behaviour_config });
            }
        }
        else if(Input.GetMouseButtonUp(1))
        {
            foreach(Entity entity in Entities.WithAll<BoidConfig, ControllableBoidTag>().ToQuery().ToEntityArray(Allocator.Temp))
            {
                command_buffer.SetComponent<BoidConfig>(entity, new BoidConfig { config = level_config.default_behaviour_config});
            }
            
        }
        command_buffer.Playback(EntityManager);

    }
}
