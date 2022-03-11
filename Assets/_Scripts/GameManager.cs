using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
	[field: SerializeField]
	public FloorItem FloorItemPrefab { get; private set; }

	[field: SerializeField]
	public PixelMaterialObject PixelMaterialData { get; private set; }

	//there could be multiple maps in the scene
	public List<PixelWorld> Worlds { get; private set;}
	public List<GravityWell> GravityWells{ get; private set;}
	public Camera MainCamera { get; private set; }
	public PlayerController Player { get; private set; }


	new private void OnEnable()
	{
		base.OnEnable();
	}

	 private void Awake()
	{
		//Construct 
		Worlds = new List<PixelWorld>();
		GravityWells = new List<GravityWell>();
		MainCamera = Camera.main;
	}

	public void RegisterPlayerController(PlayerController controller)
	{
		Player = controller;
	}

	public void RegisterWell(GravityWell well)
	{
		GravityWells.Add(well);
	}

	//is here for future management reasons
	public FloorItem CreateDrop(GameItem item, float amount, Vector2 position)
	{
		FloorItem drop = FloorItem.CreateFloorItem(item, amount);
		if (drop)
			drop.transform.position = position;

		return drop;
	}

	public GravityWell GetClosestWell(Vector2 position)
	{
		float dist = Mathf.Infinity;
		GravityWell closest = null;

		for (int i = 0; i < GravityWells.Count; i++)
		{
			float newDist = GravityWells[i].GetSquareDistance(position);
			
			if (newDist < dist)
			{
				dist = newDist;
				closest = GravityWells[i];
			}
		}

		return closest;
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
