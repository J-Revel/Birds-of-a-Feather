using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour
{
    public static LevelLoader instance;
    private World level_world;
    public EntitySceneReference[] levels;
    public SystemHandle sceneSystem;
    private Entity active_scene_entity;
    private int active_level_index = 0;
    private EntityManager entity_manager;

    private EntityQuery singleton_query;

    void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        entity_manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        singleton_query = new EntityQueryBuilder(Allocator.Temp).WithAll<LevelLoadSystem.Singleton>().Build(entity_manager);
    }

    void Update()
    {
        
    }

    public void LoadLevel(int level_index)
    {
        Entity singleton_entity = singleton_query.GetSingletonEntity();
        LevelLoadSystem.Singleton singleton = singleton_query.GetSingleton<LevelLoadSystem.Singleton>();
        singleton.scene_to_load = levels[level_index];
        entity_manager.SetComponentData<LevelLoadSystem.Singleton>(singleton_entity, singleton);
        active_level_index = level_index;
        /*
        StopAllCoroutines();
        UnloadScene();
        active_level_index = level_index;
        StartCoroutine(LoadLevelCoroutine(level_index));
        */
    }
    public void ReloadLevel()
    {
        Entity singleton_entity = singleton_query.GetSingletonEntity();
        LevelLoadSystem.Singleton singleton = singleton_query.GetSingleton<LevelLoadSystem.Singleton>();
        singleton.scene_to_load = levels[active_level_index];
        entity_manager.SetComponentData<LevelLoadSystem.Singleton>(singleton_entity, singleton);
        /*
        StopAllCoroutines();
        UnloadScene();
        StartCoroutine(LoadLevelCoroutine(active_level_index));
        */
    }

    /*
    public IEnumerator LoadLevelCoroutine(int level_index)
    {

        while (!SceneSystem.IsSceneLoaded(World.DefaultGameObjectInjectionWorld.Unmanaged, active_scene_entity))
            yield return null;
        while (!Input.GetKeyDown(KeyCode.Space))
            yield return null;
        UnloadScene();
    }
    */

    public void UnloadScene()
    {
        Entity singleton_entity = singleton_query.GetSingletonEntity();
        LevelLoadSystem.Singleton singleton = singleton_query.GetSingleton<LevelLoadSystem.Singleton>();
        singleton.unload = true;
        entity_manager.SetComponentData<LevelLoadSystem.Singleton>(singleton_entity, singleton);
        /*
        EntityManager entity_manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        EntityQuery query = new EntityQueryBuilder(Allocator.Temp).WithAll<SegmentCollisionSystem.Singleton>().Build(entity_manager);
        query.GetSingleton<SegmentCollisionSystem.Singleton>().partition_grid.Clear();
        if(active_scene_entity != Entity.Null)
            SceneSystem.UnloadScene(World.DefaultGameObjectInjectionWorld.Unmanaged, active_scene_entity);
        active_scene_entity = Entity.Null;
        */
    }
}

public struct LoadLevelRequest: IComponentData
{
    public EntitySceneReference scene;
}

public struct LoadedLevel: IComponentData
{
    public Entity scene_entity;
}

public partial class LevelLoadSystem: SystemBase
{
    public struct Singleton : IComponentData
    {
        public Entity active_scene_entity;
        public EntitySceneReference scene_to_load;
        public bool unload;
    }

    protected override void OnCreate()
    {
        base.OnCreate();
        EntityManager.CreateSingleton<Singleton>();
    }

    protected override void OnUpdate()
    {
        Singleton singleton = SystemAPI.GetSingleton<Singleton>();

        if(singleton.active_scene_entity != Entity.Null && (singleton.unload || singleton.scene_to_load.IsReferenceValid))
        {
            SceneSystem.UnloadScene(World.DefaultGameObjectInjectionWorld.Unmanaged, singleton.active_scene_entity, SceneSystem.UnloadParameters.Default);
            singleton.active_scene_entity = Entity.Null;
            singleton.unload = false;
        }
        if (singleton.scene_to_load.IsReferenceValid)
        {
            var loadParameters = new SceneSystem.LoadParameters() { Flags = SceneLoadFlags.LoadAdditive };
            Entity active_scene_entity = SceneSystem.LoadSceneAsync(World.DefaultGameObjectInjectionWorld.Unmanaged, singleton.scene_to_load, loadParameters);
            singleton.active_scene_entity = active_scene_entity;
            singleton.scene_to_load = default;
        }
        SystemAPI.SetSingleton<Singleton>(singleton);
    }
}
