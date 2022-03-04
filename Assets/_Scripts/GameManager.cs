using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
	//there could be multiple maps in the scene
	public List<Map> Maps { get; private set;}
	public Camera MainCamera { get; private set; }
	public PlayerController Player { get; private set; }

	new private void Awake()
	{
		base.Awake();
		Maps = new List<Map>();
		MainCamera = Camera.main;
	}

	public void RegisterPlayerController(PlayerController controller)
	{
		Player = controller;
	}

	//Maps should register themselves on Start()
	public bool RegisterMap(Map map)
	{
		if (map != null)
		{
			Maps.Add(map);
			return true;
		}
		return false;
	}
}
