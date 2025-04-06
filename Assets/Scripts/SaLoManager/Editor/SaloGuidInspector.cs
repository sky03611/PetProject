using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SaloGUID))]
class SaloGuidInspector : Editor
{
    void OnEnable()
    {
        
    }
    Vector2 scrollPos;
    public override void OnInspectorGUI()
    {
        SaloGUID guid = (SaloGUID)target;
        if (guid.IsDisabled())
        {
            EditorGUILayout.LabelField("SaLo GUID not working in prefab edit mode.");
        }
        else
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandWidth(true), GUILayout.MaxHeight(256));
            foreach (var item in guid.ComponentsGUID)
            {

                // EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(item.Value.GetType().Name);
                EditorGUILayout.SelectableLabel(item.Key.ToString());
                //EditorGUILayout.EndHorizontal();

            }
            EditorGUILayout.EndScrollView();
        }
    }
}