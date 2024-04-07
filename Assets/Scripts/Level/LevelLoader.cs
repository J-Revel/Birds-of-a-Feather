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
    public EntitySceneReference level;
    public SystemHandle sceneSystem;
    private Entity active_scene_entity;

    void Awake()
    {
        instance = this;
    }

    private IEnumerator Start()
    {
        yield return LoadLevel(0);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            StopAllCoroutines();
            UnloadScene();
            StartCoroutine(LoadLevel(0));
        }
    }

    public void UnloadLevel()
    {
        if(level_world.IsCreated)
            level_world.Dispose();
    }

    public IEnumerator LoadLevel(int level_index)
    {
        var loadParameters = new SceneSystem.LoadParameters()
        { Flags = SceneLoadFlags.LoadAdditive};
        active_scene_entity = SceneSystem.LoadSceneAsync(World.DefaultGameObjectInjectionWorld.Unmanaged, level, loadParameters);

        while (!SceneSystem.IsSceneLoaded(World.DefaultGameObjectInjectionWorld.Unmanaged, active_scene_entity))
            yield return null;
        while (!Input.GetKeyDown(KeyCode.Space))
            yield return null;
        UnloadScene();
        /*var ecb = new EntityCommandBuffer(Allocator.Persistent,
            PlaybackPolicy.MultiPlayback);
        var postLoadEntity = ecb.CreateEntity();

        var postLoadCommandBuffer = new PostLoadCommandBuffer()
        {
            CommandBuffer = ecb
        };
        level_world.EntityManager.AddComponentData(sceneEntity, postLoadCommandBuffer);*/
    }

    public void UnloadScene()
    {
        if(active_scene_entity != Entity.Null)
            SceneSystem.UnloadScene(World.DefaultGameObjectInjectionWorld.Unmanaged, active_scene_entity);
        active_scene_entity = Entity.Null;

    }
}
