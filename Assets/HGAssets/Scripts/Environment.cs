using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Environment : Singleton<Environment>
{
	#region Environment

	[Range(1f, 20f)]
	[Tooltip("How often (in seconds) hunters will regen health when out of combat.")]
	public float RegenInterval = 2f;

	[Range(5f, 20f)]
	[Tooltip("The delay (in seconds) before hunters start regaining health.")]
	public float InitialRegenDelay = 5f;

	[Range(0f, 10f)]
	[Tooltip("The amount of available resources (food) on the map.")]
	public float ResourceAbundance = 0f;

	[Tooltip("Resources will only respawn if this is true. Fairly self explanitory.")]
	public bool DoResourcesRespawn = false;

	[Range(10f, 100f)]
	[Tooltip("The rate (in seconds) that resources will respawn at.")]
	public float ResourceRespawnInterval = 100f;

	#endregion

}