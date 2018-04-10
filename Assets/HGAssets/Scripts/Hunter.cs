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

	Rigidbody rb;
	SphereCollider sp;

	public HunterType HunterType;

	[Range(10f, 100f)]
	public float Health;

	[Range(1f, 20f)]
	public float LowHealthThreshold;

	[Range(1f, 10f)]
	public float RegenAmount;

	[Range(1f, 10f)]
	public float Acceleration;

	[Range(1f, 20f)]
	public float MaxSpeed;

	[Range(1f, 10f)]
	public float Attack;

	[Range(1f, 10f)]
	public float Defense;

	[Range(1f, 20f)]
	public float SightDistance;

	[Range(0.01f, 1f)]
	public float SizeMultiplier;

	public bool InCombat { get; private set; }

	public Vector3 CurrentHeading = Vector3.forward;
	int terrainLayerMask;
	int hunterLayerMask;

	float maxHealth;
	Coroutine regenCoroutine;

	Transform areaOfEffectRing;
	Vector3 areaOfEffectBaseScale = Vector3.one * 0.4f;

	void Awake()
	{
		if (Hunters == null)
			Hunters = new List<Hunter>();

		Hunters.Add(this);

		rb = GetComponent<Rigidbody>();
		sp = GetComponent<SphereCollider>();

		areaOfEffectRing = transform.Find("Area of Effect");

		terrainLayerMask = 1 << LayerMask.NameToLayer("Terrain");
		hunterLayerMask = 1 << LayerMask.NameToLayer("Hunter");

		maxHealth = Health;
		areaOfEffectRing.SetParent(null, true);

		ReSize();
	}

	void OnDestroy()
	{
		if (areaOfEffectRing != null)
			Destroy(areaOfEffectRing.gameObject);

		Hunters.Remove(this);
	}

	void Update()
	{
		Look();
		Move();
	}

	public void Look()
	{
		List<Collider> seen = Physics.OverlapSphere(transform.position, SightDistance, hunterLayerMask).ToList();

		bool inCombat = false;

		foreach (Collider other in seen)
		{
			Hunter otherHunter = other.GetComponent<Hunter>();

			if (otherHunter.HunterType != HunterType)
			{
				InCombat = true;

				if (regenCoroutine != null)
				{
					StopCoroutine(regenCoroutine);
					regenCoroutine = null;
				}

				Vector3 toOther = (otherHunter.transform.position - transform.position).Flatten();
				CurrentHeading = toOther;

				if (Health < LowHealthThreshold)
					CurrentHeading = toOther * -1f;

				return;
			}
			else
			{
				//Vector3 newHeading = Vector3.Lerp(CurrentHeading.normalized, (otherHunter.transform.position - transform.position).normalized, Time.deltaTime * 15f);
				Vector3 newHeading = otherHunter.CurrentHeading.AddNoise(5f);
				CurrentHeading = newHeading;
			}
		}

		InCombat = inCombat;

		if (!InCombat && regenCoroutine == null && Health < maxHealth)
			regenCoroutine = StartCoroutine(Regen());
	}

	public void Move()
	{
		CurrentHeading = CurrentHeading.AddNoise(25f);

		RaycastHit hitInfo;
		if (Physics.Raycast(transform.position, CurrentHeading, out hitInfo, SightDistance, terrainLayerMask))
		{
			CurrentHeading = transform.position - hitInfo.point;

			if (rb.velocity.magnitude > 2.5f)
				rb.velocity *= 0.5f;
		}

		rb.AddForce(CurrentHeading.Flatten().normalized * Acceleration, ForceMode.Force);

		if (rb.velocity.magnitude > MaxSpeed)
		{
			rb.velocity *= (MaxSpeed / rb.velocity.magnitude);
		}

		areaOfEffectRing.position = transform.position.Flatten() + Vector3.up * 0.5f;
		areaOfEffectRing.forward = Vector3.Lerp(areaOfEffectRing.forward, CurrentHeading.normalized, Time.deltaTime * 5f);
	}

	//void OnDrawGizmos()
	//{
	//	Gizmos.color = Color.cyan.WithAlpha(0.1f);
	//	Gizmos.DrawSphere(transform.position, SightDistance);
	//}

	public void ReSize()
	{
		if (Health > maxHealth)
			Health = maxHealth;

		rb.mass = Health * SizeMultiplier;
		transform.localScale = Vector3.one * rb.mass;

		areaOfEffectRing.localScale = areaOfEffectBaseScale * SightDistance;
	}

	void OnCollisionEnter(Collision other)
	{
		Hunter otherHunter = other.gameObject.GetComponent<Hunter>();

		if (otherHunter != null)
		{
			if (otherHunter.HunterType != HunterType)
				otherHunter.TakeDamage(Attack);
		}
	}

	public void TakeDamage(float damage)
	{
		Health -= damage;

		if (Health < 1f)
			Died();

		else
		{
			ReSize();
			HunterEvent(this, HunterEventType.Damaged);
		}
	}

	IEnumerator Regen()
	{
		yield return new WaitForSeconds(Environment.Instance.RegenInterval);

		while (!InCombat && Health < maxHealth)
		{
			Health += RegenAmount;
			transform.position += Vector3.up * 0.1f;
			ReSize();
			yield return new WaitForSeconds(Environment.Instance.RegenInterval);
		}
	}

	void Died()
	{
		HunterEvent(this, HunterEventType.Died);
		Destroy(gameObject);
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
