using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Scenes;
using UnityEditor.Search;
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

    void Awake()
    {
        instance = this;
    }

    void Update()
    {

    }

    public void LoadLevel(int level_index)
    {
        StopAllCoroutines();
        UnloadScene();
        active_level_index = level_index;
        StartCoroutine(LoadLevelCoroutine(level_index));
    }
    public void ReloadLevel()
    {
        StopAllCoroutines();
        UnloadScene();
        StartCoroutine(LoadLevelCoroutine(active_level_index));
    }

    public IEnumerator LoadLevelCoroutine(int level_index)
    {
        var loadParameters = new SceneSystem.LoadParameters()
        { Flags = SceneLoadFlags.LoadAdditive};
        active_scene_entity = SceneSystem.LoadSceneAsync(World.DefaultGameObjectInjectionWorld.Unmanaged, levels[level_index], loadParameters);

        while (!SceneSystem.IsSceneLoaded(World.DefaultGameObjectInjectionWorld.Unmanaged, active_scene_entity))
            yield return null;
        while (!Input.GetKeyDown(KeyCode.Space))
            yield return null;
        UnloadScene();
    }

    public void UnloadScene()
    {
        EntityManager entity_manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        EntityQuery query = new EntityQueryBuilder(Allocator.Temp).WithAll<SegmentCollisionSystem.Singleton>().Build(entity_manager);
        query.GetSingleton<SegmentCollisionSystem.Singleton>().partition_grid.Clear();
        if(active_scene_entity != Entity.Null)
            SceneSystem.UnloadScene(World.DefaultGameObjectInjectionWorld.Unmanaged, active_scene_entity);
        active_scene_entity = Entity.Null;
    }
}
