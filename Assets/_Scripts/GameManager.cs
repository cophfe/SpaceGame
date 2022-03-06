using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
	//there could be multiple maps in the scene
	public List<PixelWorld> Worlds { get; private set;}
	public Camera MainCamera { get; private set; }
	public PlayerController Player { get; private set; }

	new private void Awake()
	{
		base.Awake();
		Worlds = new List<PixelWorld>();
		MainCamera = Camera.main;
	}

	public void RegisterPlayerController(PlayerController controller)
	{
		Player = controller;
	}

	//Maps should register themselves on Start()
	public bool RegisterMap(PixelWorld map)
	{
		if (map != null)
		{
			Worlds.Add(map);
			return true;
		}
		return false;
	}
}
