using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Collections.Generic;

public class TextureArrayMaker
{
	[MenuItem("Assets/Create Texture Array From Folder")]
	private static void CreateTextureArray()
	{
		string path = AssetDatabase.GetAssetPath(Selection.activeObject);
		string[] children = Directory.GetFiles(path);
		List<Texture2D> textures = new List<Texture2D>();

		for (int i = 0; i < children.Length; i++)
		{
			Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(children[i]);
			if (tex != null)
			{
				if(!tex.isReadable)
				{
					Debug.LogWarning($"Texture {tex.name} was skipped: Cannot be read by script");
					continue;
				}
				textures.Add(tex);
			}
		}

		if (textures.Count > 0)
		{
			Vector2Int size = new Vector2Int(textures[0].width, textures[0].height);
			TextureFormat format = textures[0].format;

			for (int i = 1; i < textures.Count; i++)
			{
				if (new Vector2Int(textures[i].width, textures[i].height) != size)
				{
					Debug.LogWarning("Failed to create texture array: Textures were not the same size");
					return;
				}
				else if (textures[i].format != format)
				{
					Debug.LogWarning("Failed to create texture array: Textures were not the same format");
					return;
				}
			}

			Texture2DArray texArray = new Texture2DArray(size.x, size.y, textures.Count, format, false);
			if (texArray == null)
			{
				Debug.LogWarning("Failed to create texture array: reason unknown");
				return;
			}
			texArray.filterMode = FilterMode.Bilinear;
			texArray.wrapMode = TextureWrapMode.Repeat;

			for (int i = 0; i < textures.Count; i++)
			{
				Graphics.CopyTexture(textures[i], 0, 0, texArray, i, 0);
			}

			texArray.Apply();

			string newPath = $"{path}/{Selection.activeObject.name}Array";
			string newPathNum = newPath + ".asset";
			int j = 0;
			while (File.Exists(newPathNum))
			{
				newPathNum = newPath + j + ".asset";
				j++;
			}
			AssetDatabase.CreateAsset(texArray, newPathNum);
			Debug.Log("Succesfully created asset!");
		}
		else
			Debug.LogWarning("Failed to create texture array: no textures in directory");
	}

	[MenuItem("Assets/Create Texture Array From Folder", true)]
	private static bool ValidateTextureArray()
	{
		return AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(Selection.activeObject));
	}
}
