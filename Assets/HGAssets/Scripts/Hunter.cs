using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public class Hunter : MonoBehaviour
{
	public static List<Hunter> Hunters;
	public static int SoloHunters = 0;
	public static int GroupHunters = 0;

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
	[Tooltip("The maximum speed the hunter can go.")]
	public float Speed;

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

	float sizeMultiplier = 0.3f;
	float maxHealth;

	public Vector3 CurrentHeading = Vector3.forward;
	int terrainLayerMask;
	int hunterLayerMask;
	int foodLayerMask;
	float speedMultiplier = 0.5f;

	Coroutine regenCoroutine;

	Transform areaOfEffectRing;
	SpriteRenderer ringRenderer;

	Vector3 areaOfEffectBaseScale = Vector3.one * 0.4f;

	Vector3 minSize = Vector3.one * 0.2f;

	MeshRenderer mr;
	Color originalColor;


	RectTransform hunterUI;
	Image hunterUIBar;

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
		foodLayerMask = 1 << LayerMask.NameToLayer("Food");

		ringRenderer.color = ringRenderer.color.WithAlpha(0f);

		hunterUI = Instantiate(Resources.Load<GameObject>("Prefabs/HunterUI"), UIManager.Instance.transform).GetComponent<RectTransform>();
		hunterUIBar = hunterUI.GetChild(0).GetComponent<Image>();
	}

	public void Init(HunterStats stats, Vector3 position, List<Hunter> parents = null)
	{
		HunterType = stats.HunterType;
		Attack = stats.Attack;
		Health = stats.Health;
		LowHealthThreshold = stats.LowHealthThreshold;
		RegenAmount = stats.RegenAmount;
		Speed = stats.Speed;
		SightDistance = stats.SightDistance;
		ProcreationProbability = stats.ProcreationProbability;
		ProcreationTime = stats.ProcreationTime;

		transform.position = position;

		if (parents != null)
			Parents = parents;

		maxHealth = stats.Health;

		InitialSize();

		switch (HunterType)
		{
			case HunterType.Solo:
				SoloHunters += 1;
				break;
			case HunterType.Group:
				GroupHunters += 1;
				break;
		}
	}

	void OnDestroy()
	{
		StopAllCoroutines();

		if (areaOfEffectRing != null)
			Destroy(areaOfEffectRing.gameObject);

		if (hunterUI != null)
			Destroy(hunterUI.gameObject);

		Hunters.Remove(this);
	}

	void Update()
	{
		Look();
		Move();

		Color current = GetCurrentColor();
		ringRenderer.color = SimulationManager.Instance.ViewAOERings ? current : current.WithAlpha(0f);

		if (!iframe)
			mr.material.color = current;

		UpdateUI();
	}

	void UpdateUI()
	{
		Vector2 pos;
		RectTransform root = UIManager.Instance.GetComponent<RectTransform>();
		RectTransformUtility.ScreenPointToLocalPointInRectangle(root, RectTransformUtility.WorldToScreenPoint(Camera.main, transform.position), Camera.main, out pos);

		hunterUI.anchoredPosition = pos + (Vector2.right * maxHealth);

		float height = (Health / maxHealth) * 40f;
		hunterUIBar.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

		if (Health < LowHealthThreshold)
			hunterUIBar.color = Color.red.WithAlpha(200f);

		else if (Health < maxHealth * 0.6f)
			hunterUIBar.color = Color.yellow.WithAlpha(200f);

		else
			hunterUIBar.color = Color.green.WithAlpha(200f);
	}


	public void Look()
	{
		List<Collider> seen = Physics.OverlapSphere(transform.position, SightDistance, hunterLayerMask).ToList();
		List<Collider> foodSeen = Physics.OverlapSphere(transform.position, SightDistance, foodLayerMask).ToList();

		bool inCombat = false;
		Hunter closest = this;

		Vector3 newHeading = CurrentHeading;

		int allies = 0;
		int enemies = 0;

		foreach (Collider other in seen)
		{
			Hunter otherHunter = other.GetComponent<Hunter>();

			if (closest == this)
				closest = otherHunter;

			else if (!inCombat)
				if ((otherHunter.transform.position - transform.position).magnitude < (closest.transform.position - transform.position).magnitude)
					closest = otherHunter;

			if (otherHunter.HunterType != HunterType)
			{
				closest = otherHunter;
				inCombat = true;
				enemies += 1;
			}
			else
			{
				allies += 1;
			}
		}

		if (closest != this)
		{
			Vector3 toClosest = (closest.transform.position - transform.position).Flatten().normalized;

			if (inCombat)
			{
				InCombat = true;
				newHeading = toClosest;

				if (Health < LowHealthThreshold || (allies < 4 && HunterType == HunterType.Group))
					newHeading *= -1f;

				else
				{
					if (isGrounded() && HunterType == HunterType.Group)
					{
						rb.AddForce((closest.transform.position - transform.position).normalized * 1000f, ForceMode.Impulse);
					}
				}

				speedMultiplier = 1f;
			}
			else if (HunterType == HunterType.Group && Health >= LowHealthThreshold)
			{
				speedMultiplier = 0.5f;
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

		if (foodSeen.Count > 0 && Health < maxHealth * 0.6f)
		{
			speedMultiplier = 0.3f;
			timeMultiplier = 5f;
			newHeading = (foodSeen[0].transform.position - transform.position).Flatten().normalized;
		}
		else
		{
			timeMultiplier = 1f;
		}

		CurrentHeading = Vector3.Lerp(CurrentHeading, newHeading, Time.deltaTime * UnityEngine.Random.Range(3f, 10f));
	}

	float timeMultiplier = 1f;

	public void Move()
	{
		RaycastHit hitInfo;
		if (Physics.Raycast(transform.position, CurrentHeading.Flatten().normalized, out hitInfo, SightDistance, terrainLayerMask))
		{
			CurrentHeading = Vector3.Lerp(CurrentHeading, (transform.position - hitInfo.point).normalized, Time.deltaTime * 5f * UnityEngine.Random.Range(0.5f, 1.5f));
		}

		rb.velocity = Vector3.Lerp(rb.velocity + Vector3.down * 0.25f, CurrentHeading.Flatten().normalized * Speed * speedMultiplier, Time.deltaTime * timeMultiplier);
		//rb.velocity = Vector3.Lerp(rb.velocity, (CurrentHeading.Flatten() + Vector3.down * 0.5f).normalized * Speed * speedMultiplier, Time.deltaTime * timeMultiplier);

		if (rb.velocity.magnitude > Speed)
		{
			rb.velocity *= (Speed / rb.velocity.magnitude);
		}

		areaOfEffectRing.position = transform.position;
		areaOfEffectRing.forward = CurrentHeading.normalized;
	}

	bool isGrounded()
	{
		return Physics.Raycast(transform.position, -Vector3.up, sp.bounds.extents.y + 0.1f);
	}

	public void InitialSize()
	{
		rb.mass = Health * sizeMultiplier;

		transform.localScale = Vector3.one * rb.mass;

		areaOfEffectRing.localScale = areaOfEffectBaseScale * SightDistance;
	}

	void OnCollisionEnter(Collision other)
	{
		Hunter otherHunter = other.gameObject.GetComponent<Hunter>();

		if (otherHunter != null)
		{
			if (otherHunter.HunterType != HunterType)
			{
				Vector3 dir = -(other.contacts[0].point - transform.position).normalized;
				rb.AddForce(dir * 1000f);

				otherHunter.TakeDamage(Attack, false, GainStats);
			}
			else
			{
				//Health = (otherHunter.Health + Health) / 2f;
				if (canProcreate)
				{
					if (procreationCoroutine != null)
						StopCoroutine(procreationCoroutine);

					procreationCoroutine = StartCoroutine(Procreate(otherHunter));
				}
			}
		}
	}

	void GainStats(Hunter other)
	{
		Health += 5f;

		if (Health > maxHealth)
			Health = maxHealth;
	}

	void OnTriggerEnter(Collider other)
	{
		Food food = other.gameObject.GetComponent<Food>();

		if (food)
		{
			if (Health < maxHealth * 0.6f)
				Eat(food.EatFood());
		}
	}

	public void Eat(float hp)
	{
		Health += hp;

		if (Health > maxHealth)
			Health = maxHealth;
	}

	public void TakeDamage(float damage, bool ignoreiframe = false, Action<Hunter> callback = null)
	{
		if (iframe && !ignoreiframe)
			return;

		if (!ignoreiframe)
			DoHitHighlight();

		Health -= damage;

		if (Health < 1f)
		{
			if (callback != null)
				callback(this);

			Died();
		}

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
		switch (HunterType)
		{
			case HunterType.Solo:
				SoloHunters -= 1;
				break;
			case HunterType.Group:
				GroupHunters -= 1;
				break;
		}

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
	[Tooltip("The maximum speed the hunter can go.")]
	public float Speed;

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
		Speed = stats.Speed;
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
