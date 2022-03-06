using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlanetGenerator", menuName = "Pixel Generators/Planet Generator", order = 1)]
public class PixelGeneratorPlanet : PixelGenerators
{
	public float planetRadius = 50; //radius of the actual planet in cells
	public float planetAtmosphereSize = 30; //height of the atmosphere in cells

	protected override sealed void OnEnable()
	{
		chunkResolution = new Vector2Int(2, 2);
		base.OnEnable();
	}

	public override sealed Pixel[,] GenerateChunkData(PixelWorld map, Vector2Int chunkCoordinate)
	{
		return null;
	}

	public override sealed GeneratorType Type { get => GeneratorType.Noise; }
}
