using UnityEditor;
using UnityEditor.PackageManager.UI;
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
            Vector3 new_position = Handles.PositionHandle(collider.transform.TransformPoint(collider.points[i]), Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(collider, "Change Look At Target Position");
                collider.points[i] = collider.transform.InverseTransformPoint(new_position);
            }
        }
    }
}
