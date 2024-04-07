using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    private new Camera camera;
    private EntityManager entity_manager;
    private EntityQuery query;
    
    private void Start()
    {
        camera = GetComponent<Camera>();
        entity_manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        query = new EntityQueryBuilder(Allocator.Temp).WithAll<LevelConfig, LevelState>().Build(entity_manager);
    }
    public void Update()
    {
        //camera.orthographicSize = query.GetSingleton<LevelConfig>().camera_size;
    }
}
