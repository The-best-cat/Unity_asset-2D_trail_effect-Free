using UnityEditor;

#if UNITY_EDITOR
namespace BlackCatTrail
{
    [CustomEditor(typeof(TrailManager))]
    public class TrailManagerDrawer : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            var serializedObj = new SerializedObject(target);

            var property = serializedObj.FindProperty("autoCleanUp");
            EditorGUILayout.PropertyField(property, true, null);

            if (property.boolValue)
            {
                property = serializedObj.FindProperty("cleanUpAfterSeconds");
                EditorGUILayout.PropertyField(property, true, null);

                property = serializedObj.FindProperty("numberOfTrailRemains");
                EditorGUILayout.PropertyField(property, true, null);
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObj.ApplyModifiedProperties();
            }
        }
    }
}
#endif