using UnityEngine;
using UnityEditor;
using System.IO;

public class TextureArrayMaker
{
	[MenuItem("Assets/Create Texture Array From Folder")]
	private static void CreateTextureArray()
	{
		string path = AssetDatabase.GetAssetPath(Selection.activeObject);
		//Object[] children = findallatpath;

		/*
		if (children.Length > 0)
		{
			Vector2 size = children[0].texelSize;
			TextureFormat format = children[0].format;

			for (int i = 1; i < children.Length; i++)
			{
				if (children[i].texelSize != size)
				{
					Debug.LogWarning("Failed to create texture array: Textures were not the same size");
					return;
				}
				else if (children[i].format != format)
				{
					Debug.LogWarning("Failed to create texture array: Textures were not the same format");
					return;
				}
			}

			Texture2DArray texArray = new Texture2DArray((int)size.x, (int)size.y, children.Length, format, false);
			texArray.filterMode = FilterMode.Bilinear;
			texArray.wrapMode = TextureWrapMode.Repeat;

			for (int i = 0; i < children.Length; i++)
			{
				texArray.SetPixels(children[i].GetPixels(),
					i);
			}

			texArray.Apply();

			string newPath = path + "textureArray";
			string newPathNum = newPath;
			int j = 0;
			while (File.Exists(newPathNum))
			{
				newPathNum = newPath + j;
				j++;
			}
			AssetDatabase.CreateAsset(texArray, newPathNum);
			Debug.Log("Succesfully created asset!");
		}
		else
			Debug.LogWarning("Failed to create texture array: no textures in directory");
		*/
	}

	[MenuItem("Assets/Create Texture Array From Folder", true)]
	private static bool ValidateTextureArray()
	{
		return AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(Selection.activeObject));
	}
}
