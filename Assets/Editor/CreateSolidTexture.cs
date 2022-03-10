using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class CreateSolidTexture : EditorWindow
{
	static Vector2Int resolution = new Vector2Int(512, 512);
	static Color textureColor = Color.white;
	static string fileName = "Colour";
	string path;

	[MenuItem("Assets/Create/Solid Texture")]
	private static void CreateTexture()
	{

		CreateSolidTexture window = ScriptableObject.CreateInstance<CreateSolidTexture>();
		Resolution r = Screen.currentResolution;
		Vector2 size = new Vector2(250, 140);
		window.position = new Rect((new Vector2(r.width, r.height) - size) * 0.5f , size);
		window.minSize = size - Vector2.one * 5;
		window.maxSize = size;
		window.name = "Texture Settings";
		window.path = AssetDatabase.GetAssetPath(Selection.activeObject);
		window.Show();
	}

	[MenuItem("Assets/Create/Solid Texture", true)]
	private static bool ValidateCreateTexture()
	{
		return AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(Selection.activeObject));
	}

	void OnGUI()
	{
		EditorGUILayout.LabelField("Texture Settings:", EditorStyles.boldLabel);
		GUILayout.Space(5);
		resolution = EditorGUILayout.Vector2IntField("Resolution: ", resolution);
		textureColor = EditorGUILayout.ColorField("Colour:", textureColor);
		fileName = EditorGUILayout.TextField("Name:", fileName);
		GUILayout.Space(5);
		if (GUILayout.Button("Generate"))
		{
			Texture2D texture = new Texture2D(resolution.x, resolution.y, TextureFormat.RGB24, false, false);
			for (int x = 0; x < resolution.x; x++)
			{
				for (int y = 0; y < resolution.y; y++)
				{
					texture.SetPixel(x, y, textureColor);
				}
			}
			texture.Apply();
			byte[] data = texture.EncodeToPNG();

			if (fileName == "")
				path = $"{path}/#{ColorUtility.ToHtmlStringRGB(textureColor)}";
			else
				path = $"{path}/{fileName}";
			
			int num = 0;
			string numPath = path + ".png";
			while (File.Exists(numPath))
			{
				numPath = path + num + ".png";
				num++;
			}
			
			File.WriteAllBytes(numPath, data);
			AssetDatabase.Refresh();
			Close();
		}
	}

}
