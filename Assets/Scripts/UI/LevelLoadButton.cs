using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum LoadButtonMode
{
    Load,
    Reset,
    Unload,
}
public class LevelLoadButton : MonoBehaviour
{
    public LoadButtonMode mode;
    public int level_index;
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            switch(mode)
            {
                case LoadButtonMode.Load:
                    LevelLoader.instance.LoadLevel(level_index);
                    break;
                case LoadButtonMode.Reset:
                    LevelLoader.instance.ReloadLevel();
                    break;
                case LoadButtonMode.Unload:
                    LevelLoader.instance.UnloadScene();
                    break;
            }
        });
    }
}
