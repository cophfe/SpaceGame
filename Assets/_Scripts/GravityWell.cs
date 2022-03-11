using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GravityWell
{
	float gravitationalConstant = 0.00000000000000001f;
	float radius = 10.0f;
	float density = 3;
	float regularMass;
	public Vector2 Centre { get; set; }

	public GravityWell(float radius, float density, float constant, Vector2 centre)
	{
		regularMass = CalculateMass(radius);
		this.radius = radius;
		this.density = density;
		gravitationalConstant = constant;
		Centre = centre;
	}

	public void Start()
	{
		regularMass = CalculateMass(radius);
		GameManager.Instance.RegisterWell(this);
	}

	public virtual float GetGravityAcceleration(Vector2 position)
	{
		return CalculateGravity(position);
	}

	float CalculateGravity(Vector2 position)
	{
		//IRL: f = GMm/R^2
		//in this game: f = GMm/R (we are using linear gravity, like outer wilds, becuz planets are smaller)

		//according to a minute physics video, when inside a spherical mass, the outer layers' mass cancel out.
		//So, while inside a uniformly dense shape, all I have to do is set calculate mass with a radius based on the distance from position to the centre of the planet.
		
		float dist = Vector2.Distance(position, Centre);
		float mass = dist < radius ? CalculateMass(dist) : regularMass;

		float gravitationalForce = gravitationalConstant * mass / dist;
		return gravitationalForce;

	}

	float CalculateMass(float radius)
	{
		return Mathf.PI * radius * radius * density;
	}

	public virtual Vector2 GetUpDirection(Vector2 position)
	{
		return (position - Centre).normalized;
	}

	public float GetSquareDistance(Vector2 position)
	{
		return (position - Centre).sqrMagnitude;
	}

	public static implicit operator bool(GravityWell well) => well != null;
}
