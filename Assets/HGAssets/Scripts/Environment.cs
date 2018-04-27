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

	[Tooltip("Resources will only respawn if this is true. Fairly self explanitory.")]
	public bool DoResourcesRespawn = false;

	[Range(1f, 10f)]
	[Tooltip("The amount of HP that food will restore on pickup.")]
	public float ResourceHP = 1f;

	[Range(0, 50)]
	[Tooltip("The amount of available resources (food) on the map.")]
	public int MaxResourceAbundance = 5;

	[Range(1f, 100f)]
	[Tooltip("The rate (in seconds) that resources will respawn at.")]
	public float ResourceRespawnInterval = 10f;

	[Range(0f, 50f)]
	[Tooltip("How far away the food can be spawned from the center of the spawn point.")]
	public float FoodSpawnPointRadius = 15f;

	[Range(0f, 10f)]
	[Tooltip("The amount of HP taken off each hunters health each decay tick.")]
	public float HunterDecayAmount = 1f;

	[Range(1f, 50f)]
	[Tooltip("The time (in seconds) between each decay tick.")]
	public float HunterDecayInterval = 10f;

	Transform SpawnParent;
	List<Transform> FoodSpawnPoints = new List<Transform>();

	#endregion

	int currentFoodAmount = 0;

	GameObject foodPrefab;

	void Awake()
	{
		foodPrefab = Resources.Load<GameObject>("Prefabs/Food");

		SpawnParent = GameObject.Find("SpawnParent").transform;

		for (int i = 0; i < SpawnParent.childCount; i++)
		{
			Transform child = SpawnParent.GetChild(i);
			if (child.name == "FoodSpawnPoint")
				FoodSpawnPoints.Add(child);
		}
	}

	void Start()
	{
		if (DoResourcesRespawn)
			StartCoroutine(SpawnFood());

		StartCoroutine(Decay());
	}

	public void MakeFood(Vector3 pos)
	{
		Food food = Instantiate(foodPrefab, transform).GetComponent<Food>();
		food.transform.position = pos;
		food.InitFood(ResourceHP, () => { currentFoodAmount -= 1; });
	}

	IEnumerator SpawnFood()
	{
		while (DoResourcesRespawn)
		{
			if (currentFoodAmount < MaxResourceAbundance)
			{
				Transform spawnPoint = FoodSpawnPoints.SelectRandom();
				Vector3 pos = spawnPoint.position.WithinRandomRadius(FoodSpawnPointRadius);
				MakeFood(pos);
				currentFoodAmount += 1;
			}
			yield return new WaitForSeconds(ResourceRespawnInterval);
		}
	}

	IEnumerator Decay()
	{
		while (true)
		{
			yield return new WaitForSeconds(HunterDecayInterval);

			foreach (Hunter hunter in Hunter.Hunters)
			{
				hunter.TakeDamage(HunterDecayAmount, true);
			}
		}
	}
}