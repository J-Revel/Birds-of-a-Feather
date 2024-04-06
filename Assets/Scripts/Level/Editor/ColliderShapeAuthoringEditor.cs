using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ColliderShapeAuthoring))]
public class ColliderShapeAuthoringEditor : Editor
{
    protected virtual void OnSceneGUI()
    {
        ColliderShapeAuthoring collider = (ColliderShapeAuthoring)target;
        for (int i = 0; i < collider.points.Length; i++)
        {
            EditorGUI.BeginChangeCheck();
            Vector3 newPosition = Handles.PositionHandle(collider.points[i], Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                collider.points[i] = newPosition;
            }
        }
    }
}
