using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Planet : MonoBehaviour
{
	[SerializeField] PixelGeneratorPlanet planetData = null;
	[SerializeField] PixelWorld worldPrefab;
	[SerializeField] public float density = 5;
	[SerializeField] public float gravitationalConstant = 5;

	PixelWorld world;
	GravityWell well;

	private void Awake()
	{
		world = Instantiate(worldPrefab, transform, false);
		world.SetGenerator(planetData);

		well = new GravityWell(planetData.planetRadius, density, gravitationalConstant, transform.position);
	}

	private void Start()
	{
		well.Start();
	}

	private void Update()
	{
		well.Centre = transform.position;
	}
}
