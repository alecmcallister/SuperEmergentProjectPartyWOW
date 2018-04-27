using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Food : MonoBehaviour
{
	float HP = 1f;
	int upDownTween;
	Action EatenCallback;

	void Update()
	{
		transform.Rotate(Vector3.up, 2f);
	}

	public void InitFood(float hp, Action callback)
	{
		HP = hp;
		EatenCallback = callback;
		upDownTween = LeanTween.moveY(gameObject, transform.position.y + 1f, 1.5f).setEase(LeanTweenType.easeInOutSine).setLoopType(LeanTweenType.easeInOutSine).setLoopPingPong().uniqueId;
	}

	public float EatFood()
	{
		LeanTween.cancel(upDownTween);
		EatenCallback();
		Destroy(gameObject, 0.01f);
		return HP;
	}
}
