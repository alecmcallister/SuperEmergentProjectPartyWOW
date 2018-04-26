using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public class Hunter : MonoBehaviour
{
	public static List<Hunter> Hunters;

	public event Action<Hunter, HunterEventType> HunterEvent = new Action<Hunter, HunterEventType>((h, e) => { });

	public List<Hunter> Parents = new List<Hunter>();

	#region Here we go

	[Tooltip("The type of the hunter (Group or Solo).")]
	public HunterType HunterType;

	#region Combat

	[Range(1f, 10f)]
	[Tooltip("How much damage the hunter does when attacking (taken off opponents HP)")]
	public float Attack;

	[Range(10f, 100f)]
	[Tooltip("The hunters HP. Health reaching 0 means dead.")]
	public float Health;

	[Range(1f, 20f)]
	[Tooltip("If HP falls below this, the hunter will run away from combat.")]
	public float LowHealthThreshold;

	[Range(1f, 10f)]
	[Tooltip("How much HP is regenerated each tick (governed by the environment).")]
	public float RegenAmount;

	#endregion

	#region Movement/ Sight

	[Range(1f, 100f)]
	[Tooltip("How fast the hunter accelerates.")]
	public float Acceleration;

	[Range(1f, 100f)]
	[Tooltip("The maximum speed the hunter can go.")]
	public float MaxSpeed;

	[Range(1f, 20f)]
	[Tooltip("How far the hunter can see around it (in a circle)")]
	public float SightDistance;

	#endregion

	#region Procreation

	[Range(0f, 1f)]
	[Tooltip("The chance of procreating with another hunter (of the same type).")]
	public float ProcreationProbability;

	[Range(5f, 25f)]
	[Tooltip("The time (in seconds) needed to be around another hunter (of the same type) in order to procreate.")]
	public float ProcreationTime;

	#endregion

	#endregion

	Rigidbody rb;
	SphereCollider sp;

	public bool InCombat { get; private set; }

	float sizeMultiplier = 0.2f;
	float maxHealth;

	public Vector3 CurrentHeading = Vector3.forward;
	int terrainLayerMask;
	int hunterLayerMask;

	Coroutine regenCoroutine;

	Transform areaOfEffectRing;
	SpriteRenderer ringRenderer;

	Vector3 areaOfEffectBaseScale = Vector3.one * 0.4f;

	Vector3 minSize = Vector3.one * 0.2f;

	MeshRenderer mr;
	Color originalColor;

	void Awake()
	{
		if (Hunters == null)
			Hunters = new List<Hunter>();

		Hunters.Add(this);

		rb = GetComponent<Rigidbody>();
		sp = GetComponent<SphereCollider>();
		mr = GetComponent<MeshRenderer>();
		originalColor = mr.material.GetColor("_Color");

		areaOfEffectRing = transform.Find("Area of Effect");
		areaOfEffectRing.SetParent(SimulationManager.Instance.transform, true);

		ringRenderer = areaOfEffectRing.GetComponentInChildren<SpriteRenderer>();

		terrainLayerMask = 1 << LayerMask.NameToLayer("Terrain");
		hunterLayerMask = 1 << LayerMask.NameToLayer("Hunter");

		ringRenderer.color = SimulationManager.Instance.ViewAOERings ? ringRenderer.color : Color.clear;
	}

	public void Init(HunterStats stats, Vector3 position, List<Hunter> parents = null)
	{
		HunterType = stats.HunterType;
		Attack = stats.Attack;
		Health = stats.Health;
		LowHealthThreshold = stats.LowHealthThreshold;
		RegenAmount = stats.RegenAmount;
		Acceleration = stats.Acceleration;
		MaxSpeed = stats.MaxSpeed;
		SightDistance = stats.SightDistance;
		ProcreationProbability = stats.ProcreationProbability;
		ProcreationTime = stats.ProcreationTime;

		transform.position = position;

		if (parents != null)
			Parents = parents;

		maxHealth = stats.Health;

		InitialSize();
	}

	void OnDestroy()
	{
		StopAllCoroutines();

		if (areaOfEffectRing != null)
			Destroy(areaOfEffectRing.gameObject);

		Hunters.Remove(this);
	}

	void Update()
	{
		Look();
		Move();

		Color current = GetCurrentColor();
		ringRenderer.color = SimulationManager.Instance.ViewAOERings ? current : Color.clear;

		if (!iframe)
			mr.material.color = current;
	}

	float speedMultiplier = 1f;

	public void Look()
	{
		List<Collider> seen = Physics.OverlapSphere(transform.position, SightDistance, hunterLayerMask).ToList();

		bool inCombat = false;
		Hunter closest = this;

		Vector3 newHeading = CurrentHeading;

		foreach (Collider other in seen)
		{
			Hunter otherHunter = other.GetComponent<Hunter>();

			if (closest == this)
				closest = otherHunter;

			else
				if ((otherHunter.transform.position - transform.position).magnitude < (closest.transform.position - transform.position).magnitude)
				closest = otherHunter;

			if (otherHunter.HunterType != HunterType)
			{
				closest = otherHunter;
				inCombat = true;
				break;
			}
		}

		if (closest != this)
		{
			Vector3 toClosest = (closest.transform.position - transform.position).Flatten().normalized;

			if (inCombat)
			{
				InCombat = true;
				newHeading = toClosest;

				if (Health < LowHealthThreshold)
					newHeading *= -1f;

				speedMultiplier = 1f;
			}
			else if (HunterType == HunterType.Group)
			{
				speedMultiplier = 0.5f;
				//newHeading = toClosest;
				newHeading = closest.CurrentHeading.normalized;
				InCombat = false;
			}
			else
			{
				speedMultiplier = 0.5f;
				newHeading = CurrentHeading.AddNoise(25f);
				InCombat = false;
			}
		}
		else
		{
			speedMultiplier = 0.5f;
			newHeading = CurrentHeading.AddNoise(25f);
		}

		CurrentHeading = Vector3.Lerp(CurrentHeading, newHeading, Time.deltaTime * 5f);
	}

	public void Move()
	{
		RaycastHit hitInfo;
		if (Physics.Raycast(transform.position, CurrentHeading, out hitInfo, SightDistance, terrainLayerMask))
		{
			CurrentHeading = Vector3.Lerp(CurrentHeading, (transform.position - hitInfo.point).normalized, Time.deltaTime * 5f);
		}

		rb.AddForce(CurrentHeading.Flatten().normalized * Acceleration * speedMultiplier, ForceMode.Force);

		if (rb.velocity.magnitude > MaxSpeed)
		{
			rb.velocity *= (MaxSpeed / rb.velocity.magnitude);
		}

		areaOfEffectRing.position = transform.position;
		areaOfEffectRing.forward = CurrentHeading.normalized;
	}

	public void ReSize()
	{
		//if (!iframe)
		//	mr.material.SetColor("_Color", ringRenderer.color);
	}

	public void InitialSize()
	{
		rb.mass = Health * sizeMultiplier;

		transform.localScale = Vector3.one * rb.mass;

		areaOfEffectRing.localScale = areaOfEffectBaseScale * SightDistance;
		ReSize();
	}

	void OnCollisionEnter(Collision other)
	{
		Hunter otherHunter = other.gameObject.GetComponent<Hunter>();

		if (otherHunter != null)
		{
			if (otherHunter.HunterType != HunterType)
			{
				Vector3 dir = -(other.contacts[0].point - transform.position).normalized;
				rb.AddForce(dir * 500f);

				otherHunter.TakeDamage(Attack);
			}
			else
			{
				if (canProcreate)
				{
					if (procreationCoroutine != null)
						StopCoroutine(procreationCoroutine);

					procreationCoroutine = StartCoroutine(Procreate(otherHunter));

					//if (procreationCoroutine == null)
					//{
					//	procreationCoroutine = StartCoroutine(Procreate(otherHunter));
					//}
				}
			}
		}
	}

	public void TakeDamage(float damage)
	{
		if (iframe)
			return;

		DoHitHighlight();

		Health -= damage;

		if (Health < 1f)
			Died();

		else
		{
			HunterEvent(this, HunterEventType.Damaged);

			if (regenCoroutine != null)
				StopCoroutine(regenCoroutine);

			regenCoroutine = StartCoroutine(Regen());
		}
	}

	void DoHitHighlight()
	{
		iframe = true;

		LeanTween.color(gameObject, Color.white, 0.1f).setOnUpdate((Color val) =>
		{
			mr.material.SetColor("_Color", val);
		}).setEase(LeanTweenType.easeInOutSine).setOnComplete(() =>
		{
			LeanTween.color(gameObject, GetCurrentColor(), 0.2f).setOnUpdate((Color val) =>
			{
				mr.material.SetColor("_Color", val);
			}).setEase(LeanTweenType.easeInOutSine).setOnComplete(() => { iframe = false; });
		});
	}

	Color GetCurrentColor()
	{
		Gradient gradient = (HunterType == HunterType.Solo) ? SimulationManager.Instance.SoloDamageGradient : SimulationManager.Instance.GroupDamageGradient;
		return gradient.Evaluate(Health / maxHealth);
	}

	bool iframe = false;

	IEnumerator Regen()
	{
		yield return new WaitForSeconds(Environment.Instance.InitialRegenDelay);

		while (Health < maxHealth)
		{
			Health += RegenAmount;

			if (Health > maxHealth)
				Health = maxHealth;

			ReSize();

			yield return new WaitForSeconds(Environment.Instance.RegenInterval);
		}
	}

	float cooldowntime = 2f;

	bool canProcreate = true;

	Coroutine procreationCoroutine;

	IEnumerator Procreate(Hunter other)
	{
		//if (Parents != null)
		//	if (Parents.Contains(other))
		//		yield break;

		canProcreate = false;

		float to = Time.time + ProcreationTime;

		while (other != null && (other.transform.position - transform.position).magnitude < SightDistance)
		{
			if (Time.time > to)
			{
				if (UnityEngine.Random.Range(0f, 1f) < ProcreationProbability)
				{
					if (transform.GetSiblingIndex() < other.transform.GetSiblingIndex())
					{
						SimulationManager.Instance.MakeABaby(this, other);
					}

					canProcreate = true;
				}
				else
				{
					yield return new WaitForSeconds(cooldowntime);
					canProcreate = true;
				}

				break;
			}

			yield return null;
		}

		canProcreate = true;
	}

	void Died()
	{
		HunterEvent(this, HunterEventType.Died);
		Destroy(gameObject);
	}
}

[Serializable]
public class HunterStats
{
	[Tooltip("The type of the hunter (Group or Solo).")]
	public HunterType HunterType;

	#region Combat

	[Range(1f, 10f)]
	[Tooltip("How much damage the hunter does when attacking (taken off opponents HP)")]
	public float Attack;

	[Range(10f, 100f)]
	[Tooltip("The hunters HP. Health reaching 0 means dead.")]
	public float Health;

	[Range(1f, 20f)]
	[Tooltip("If HP falls below this, the hunter will run away from combat.")]
	public float LowHealthThreshold;

	[Range(0f, 10f)]
	[Tooltip("How much HP is regenerated each tick (governed by the environment).")]
	public float RegenAmount;

	#endregion

	#region Movement/ Sight

	[Range(1f, 100f)]
	[Tooltip("How fast the hunter accelerates.")]
	public float Acceleration;

	[Range(1f, 100f)]
	[Tooltip("The maximum speed the hunter can go.")]
	public float MaxSpeed;

	[Range(1f, 20f)]
	[Tooltip("How far the hunter can see around it (in a circle)")]
	public float SightDistance;

	#endregion

	#region Procreation

	[Range(0f, 1f)]
	[Tooltip("The chance of procreating with another hunter (of the same type).")]
	public float ProcreationProbability;

	[Range(1f, 25f)]
	[Tooltip("The time (in seconds) needed to be around another hunter (of the same type) in order to procreate.")]
	public float ProcreationTime;

	#endregion

	public HunterStats(HunterStats stats)
	{
		HunterType = stats.HunterType;
		Attack = stats.Attack;
		Health = stats.Health;
		LowHealthThreshold = stats.LowHealthThreshold;
		RegenAmount = stats.RegenAmount;
		Acceleration = stats.Acceleration;
		MaxSpeed = stats.MaxSpeed;
		SightDistance = stats.SightDistance;
		ProcreationProbability = stats.ProcreationProbability;
		ProcreationTime = stats.ProcreationTime;
	}
}

public enum HunterEventType
{
	Damaged,
	Regenerated,
	Died,
	Other
}

public enum HunterType
{
	Solo,
	Group
}
