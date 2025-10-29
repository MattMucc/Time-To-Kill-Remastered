using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PersistenceID))]
public class PersistenceIDEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        PersistenceID id = (PersistenceID)target;
        GUILayout.Space(10);

        // Add a button to regenerate the unique ID
        if (GUILayout.Button("Regenerate ID"))
        {
            Undo.RecordObject(id, "Regenerate Persistence ID");
            id.RegenerateID();
            EditorUtility.SetDirty(id);
        }

        // Add a small label reminder
        EditorGUILayout.HelpBox("Each object with a PersistenceID should have a unique ID.\n" + "Use 'Regenerate ID' if you duplicated this prefab or object.", MessageType.Info);
    }
}