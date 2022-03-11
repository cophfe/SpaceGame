using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PixelMaterialObject))]
public class MaterialDataEditor : Editor
{
	SerializedProperty materialDataArray;
	SerializedProperty offset;
	SerializedProperty sprite;

	private void OnEnable()
	{
		materialDataArray = serializedObject.FindProperty("materialData");
		offset = serializedObject.FindProperty("offset");
		sprite = serializedObject.FindProperty("sprite");
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();
		EditorGUILayout.PropertyField(sprite);
		EditorGUILayout.PropertyField(offset);

		EditorGUILayout.Space(10);
		EditorGUILayout.LabelField("Materials: ", EditorStyles.boldLabel);

		int size = materialDataArray.arraySize;
		for (int i = 0; i < size; i++)
		{
			string label = ((Pixel.MaterialType)i).ToString();
			//EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(materialDataArray.GetArrayElementAtIndex(i), new GUIContent(label));
		}
		serializedObject.ApplyModifiedProperties();
	}
}
