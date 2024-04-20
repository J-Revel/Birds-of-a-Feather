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
    public bool to_render_texture = false;
    public Material[] render_texture_materials;
    public string render_texture_shader_param;
    private RenderTexture render_texture;
    
    private void Start()
    {
        camera = GetComponent<Camera>();
        entity_manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        query = new EntityQueryBuilder(Allocator.Temp).WithAll<LevelConfig, LevelState>().Build(entity_manager);
        if(to_render_texture)
        {
            render_texture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.R8);
            foreach(Material material in render_texture_materials)
            {
                material.SetTexture(render_texture_shader_param, render_texture);
            }
            camera.targetTexture = render_texture;
        }
    }
    public void Update()
    {
        if (to_render_texture && (render_texture.width != Screen.width || render_texture.height != Screen.height))
        {
            render_texture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.R8);
            foreach(Material material in render_texture_materials)
            {
                material.SetTexture(render_texture_shader_param, render_texture);
            }
            camera.targetTexture = render_texture;
        }
        if(query.HasSingleton<LevelConfig>())
            camera.orthographicSize = query.GetSingleton<LevelConfig>().camera_size;
    }
}
