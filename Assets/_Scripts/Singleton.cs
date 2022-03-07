using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//forgive me Father, for I have sinned
public class Singleton<T> : MonoBehaviour where T : Component
{
	public static T Instance { get; protected set; } = null;

	protected void OnEnable()
	{
		if (Instance != null && Instance != this)
		{
			Debug.LogError($"ERROR: More than one instance of {this.name} in the scene.");
			Destroy(this);
		}
		else
		{
			Instance = this as T;
		}
	}

}
