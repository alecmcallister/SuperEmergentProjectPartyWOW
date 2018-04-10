using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulationManager : Singleton<SimulationManager>
{
	GameObject groupHunterPrefab;
	GameObject soloHunterPrefab;

	#region Simulation Properties

	public bool ViewAOERings = true;

	[Tooltip("The list of all available spawn points (to be filled in by the user). Will " +
			 "select a spawn point at random when instantiating hunters in the scene.")]
	public List<Transform> SpawnPoints;

	[Range(0f, 30f)]
	[Tooltip("How far away the hunters can be spawned from the center of the spawn point.")]
	public float SpawnPointRadius = 15f;

	[Range(1, 10)]
	[Tooltip("How many solo hunters to start the simulation with.")]
	public int SoloHunterAmount = 2;

	[Range(1, 50)]
	[Tooltip("How many group hunters to start the simulation with.")]
	public int GroupHunterAmount = 10;

	[Space]
	[Header("Solo Hunter Properties")]
	public HunterStats SoloStats;

	[Space]
	[Header("Group Hunter Properties")]
	public HunterStats GroupStats;

	#endregion

	public Gradient SoloDamageGradient;
	public Gradient GroupDamageGradient;

	void Awake()
	{
		groupHunterPrefab = Resources.Load<GameObject>("Prefabs/GroupHunter");
		soloHunterPrefab = Resources.Load<GameObject>("Prefabs/SoloHunter");
	}

	void Start()
	{
		SpawnRandom(SoloHunterAmount, new HunterStats(SoloStats));
		SpawnRandom(GroupHunterAmount, new HunterStats(GroupStats));
	}

	public Hunter SpawnHunter(HunterStats stats, Vector3 position, List<Hunter> parents = null)
	{
		Hunter hunter = Instantiate(stats.HunterType == HunterType.Solo ? soloHunterPrefab : groupHunterPrefab, transform).GetComponent<Hunter>();
		hunter.Init(stats, position, parents);
		return hunter;
	}

	public List<Hunter> SpawnRandom(int amount, HunterStats stats, List<Hunter> parents = null)
	{
		List<Hunter> spawned = new List<Hunter>();

		for (int i = 0; i < amount; i++)
		{
			Transform spawnPoint = SpawnPoints.SelectRandom();
			Vector3 pos = spawnPoint.position.WithinRandomRadius(SpawnPointRadius);
			spawned.Add(SpawnHunter(stats, pos, parents));
		}

		return spawned;
	}

	public void MakeABaby(Hunter a, Hunter b)
	{
		Vector3 pos = a.transform.position + (b.transform.position - a.transform.position) * 0.5f;
		SpawnHunter((a.HunterType == HunterType.Group) ? GroupStats : SoloStats, pos, new List<Hunter>() { a, b });
	}
}
