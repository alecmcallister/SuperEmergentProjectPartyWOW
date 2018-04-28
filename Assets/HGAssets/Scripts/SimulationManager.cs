using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulationManager : Singleton<SimulationManager>
{
	#region Simulation Properties

	public bool ViewAOERings = true;

	[Range(0f, 30f)]
	[Tooltip("How far away the hunters can be spawned from the center of the spawn point.")]
	public float SpawnPointRadius = 15f;

	[Range(1, 10)]
	[Tooltip("How many solo hunters to start the simulation with.")]
	public int SoloHunterAmount = 2;

	[Range(1, 50)]
	[Tooltip("How many group hunters to start the simulation with.")]
	public int GroupHunterAmount = 10;

	[Header("Solo Hunter Properties")]
	public HunterStats SoloStats;

	[Header("Group Hunter Properties")]
	public HunterStats GroupStats;

	[Space]
	public Gradient SoloDamageGradient;
	public Gradient GroupDamageGradient;

	GameObject groupHunterPrefab;
	GameObject soloHunterPrefab;

	#endregion

	Transform SpawnParent;
	List<Transform> SpawnPoints = new List<Transform>();

	void Awake()
	{
		groupHunterPrefab = Resources.Load<GameObject>("Prefabs/GroupHunter");
		soloHunterPrefab = Resources.Load<GameObject>("Prefabs/SoloHunter");

		SpawnParent = GameObject.Find("SpawnParent").transform;

		for (int i = 0; i < SpawnParent.childCount; i++)
		{
			Transform child = SpawnParent.GetChild(i);
			if (child.name == "HunterSpawnPoint")
				SpawnPoints.Add(child);
		}
	}

	void Start()
	{
		SpawnRandom(SoloHunterAmount, new HunterStats(SoloStats));
		SpawnRandom(GroupHunterAmount, new HunterStats(GroupStats));
	}

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.S))
			SpawnRandom(1, SoloStats);

		if (Input.GetKeyDown(KeyCode.G))
			SpawnRandom(1, GroupStats);
	}

	public Hunter SpawnHunter(HunterStats stats, Vector3 position, List<Hunter> parents = null)
	{
		Hunter hunter = Instantiate(stats.HunterType == HunterType.Solo ? soloHunterPrefab : groupHunterPrefab, transform).GetComponent<Hunter>();
		if (parents != null)
		{
			stats.Attack = Mathf.Min(Mathf.Max(parents[0].Attack, parents[1].Attack) + Random.Range(-0.5f, 0.25f), stats.Attack + 5f);
		}
		hunter.Init(stats, position, parents);
		hunter.HunterEvent += HunterEvent;
		UIManager.Instance.UpdateHunterText();
		return hunter;
	}

	public void HunterEvent(Hunter hunter, HunterEventType type)
	{
		if (type == HunterEventType.Died)
		{
			UIManager.Instance.UpdateHunterText();
		}
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
		Hunter baby = SpawnHunter((a.HunterType == HunterType.Group) ? GroupStats : SoloStats, pos, new List<Hunter>() { a, b });
		float health = Mathf.Max(a.Health, b.Health);
		baby.TakeDamage(baby.Health - health, true);
		baby.CurrentHeading = a.CurrentHeading;
	}
}
