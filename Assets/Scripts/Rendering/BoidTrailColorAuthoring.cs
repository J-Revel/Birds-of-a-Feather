using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

public class BoidTrailColorAuthoring : MonoBehaviour
{
    public float transition_duration = 0.5f;
    public partial class Baker: Baker<BoidTrailColorAuthoring>
    {
        public override void Bake(BoidTrailColorAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent<TrailColorComponent>(entity);
            AddComponent<TrailColorTransitionState>(entity, new TrailColorTransitionState { transition_duration = authoring.transition_duration});
        }
    }
}

[MaterialProperty("_Tint")]
public struct TrailColorComponent: IComponentData
{
    public Color Value;
}

public struct TrailColorTransitionState: IComponentData
{
    public Color target_color;
    public Color start_color;
    public float transition_time;
    public float transition_duration;
}


public partial class TrailColorUpdateSystem: SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<LevelState>();
    }
    protected override void OnUpdate()
    {
        float dt = SystemAPI.Time.DeltaTime;
        Color modifier_color = SystemAPI.GetSingleton<LevelState>().current_modifier.color;
        
        Entities.ForEach((ref TrailColorTransitionState state, ref TrailColorComponent target_color) =>
        {
            if(state.target_color != modifier_color)
            {
                state.start_color = target_color.Value;
                state.target_color = modifier_color;
                state.transition_time = 0;
            }
            state.transition_time += dt;
            target_color.Value = Color.Lerp(state.start_color, state.target_color, math.saturate(state.transition_time / state.transition_duration));
        }).Schedule();
    }
}
