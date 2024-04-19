using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
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
            DependsOn(authoring.config_asset.left_click_action.behaviour_modifier);
            DependsOn(authoring.config_asset.right_click_action.behaviour_modifier);
            AddComponent<LevelConfig>(entity, new LevelConfig
            {
                default_behaviour_config = authoring.config_asset.default_behaviour_config.Bake(),
                left_behaviour_modifier = authoring.config_asset.left_click_action.behaviour_modifier.Bake(),
                right_behaviour_modifier = authoring.config_asset.right_click_action.behaviour_modifier.Bake(),
                left_action_type = authoring.config_asset.left_click_action.action_type,
                right_action_type = authoring.config_asset.right_click_action.action_type,
                left_use_duration = authoring.config_asset.left_click_action.max_use_duration,
                right_use_duration = authoring.config_asset.right_click_action.max_use_duration,
                camera_size = authoring.config_asset.camera_size,
            });
            AddComponent<LevelState>(entity);
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
    public BoidBehaviourModifierAsset behaviour_modifier;
    public float max_use_duration;
    public float reload_delay;
    public float reload_speed;
}

public struct LevelConfig: IComponentData
{
    public BoidBehaviourConfig default_behaviour_config;
    public PlayerActionType left_action_type;
    public BoidBehaviourModifier left_behaviour_modifier;
    public PlayerActionType right_action_type;
    public BoidBehaviourModifier right_behaviour_modifier;
    public float left_use_duration;
    public float right_use_duration;
    public float left_reload_delay;
    public float right_reload_delay;
    public float camera_size;
}

public struct LevelState: IComponentData
{
    public float left_use_time;
    public float right_use_time;
    public float left_reload_delay;
    public float right_reload_delay;
    public bool using_left;
    public bool using_right;
}

public partial class PlayerInputSystem: SystemBase
{
    protected override void OnCreate()
    {
        base.OnCreate();
        RequireForUpdate<LevelConfig>();
    }
    protected override void OnUpdate()
    {
        LevelConfig level_config = SystemAPI.GetSingleton<LevelConfig>();
        EntityCommandBuffer command_buffer = new EntityCommandBuffer(Allocator.Temp);
        LevelState state = SystemAPI.GetSingleton<LevelState>();
        if (Input.GetMouseButtonDown(0) && state.left_use_time < level_config.left_use_duration)
        {
            state.using_left = true;
            foreach (Entity entity in Entities.WithAll<BoidConfig, ControllableBoidTag>().ToQuery().ToEntityArray(Allocator.Temp))
            {
                command_buffer.SetComponent<BoidConfig>(entity,
                    new BoidConfig
                    {
                        config = BoidBehaviourConfigAsset.ConvertFromModifier(
                            level_config.default_behaviour_config,
                            level_config.left_behaviour_modifier),
                    });
            }
        }
        if(state.using_left)
        {
            state.left_use_time += SystemAPI.Time.DeltaTime;
            state.left_reload_delay = 0;
            if (!Input.GetMouseButton(0) || (state.using_left && state.left_use_time >= level_config.left_use_duration))
            {
                state.using_left = false;
                foreach (Entity entity in Entities.WithAll<BoidConfig, ControllableBoidTag>().ToQuery().ToEntityArray(Allocator.Temp))
                {
                    command_buffer.SetComponent<BoidBehaviourModifier>(entity, BoidBehaviourModifier.default_modifier);
                }
            }
        }

        if (Input.GetMouseButtonDown(1) && state.right_use_time < level_config.right_use_duration)
        {
            state.using_right = true;
            foreach (Entity entity in Entities.WithAll<BoidConfig, ControllableBoidTag>().ToQuery().ToEntityArray(Allocator.Temp))
            {
                command_buffer.SetComponent<BoidConfig>(
                    entity,
                    new BoidConfig
                    {
                        config = BoidBehaviourConfigAsset.ConvertFromModifier(
                            level_config.default_behaviour_config,
                            level_config.right_behaviour_modifier),
                    });
            }
        }
        if(state.using_right)
        {
            state.right_use_time += SystemAPI.Time.DeltaTime;
            state.right_reload_delay = 0;
            if (Input.GetMouseButtonUp(1))
            {
                state.using_right = false;
                foreach (Entity entity in Entities.WithAll<BoidConfig, ControllableBoidTag>().ToQuery().ToEntityArray(Allocator.Temp))
                {
                    command_buffer.SetComponent<BoidBehaviourModifier>(entity, BoidBehaviourModifier.default_modifier);
                }
            }
        }
        state.left_reload_delay += SystemAPI.Time.DeltaTime;
        state.right_reload_delay += SystemAPI.Time.DeltaTime;
        if (state.left_reload_delay > level_config.left_reload_delay && !state.using_left)
            state.left_use_time = math.max(0, state.left_use_time - SystemAPI.Time.DeltaTime);
        if (state.right_reload_delay > level_config.right_reload_delay && !state.using_right)
            state.right_use_time -= math.max(0, state.right_use_time - SystemAPI.Time.DeltaTime);
        SystemAPI.SetSingleton<LevelState>(state);
        command_buffer.Playback(EntityManager);
    }
}
