using System.Collections.Generic;
using UnityEngine;

public static class Extensions
{
	public static Vector3 AddNoise(this Vector3 v, float noise)
	{
		return new Vector3(v.x + Random.Range(-noise, noise), 0f, v.z + Random.Range(-noise, noise));
	}

	public static Vector3 Flatten(this Vector3 v)
	{
		return new Vector3(v.x, 0f, v.z);
	}

	public static Color WithAlpha(this Color color, float alpha)
	{
		return new Color(color.r, color.g, color.b, alpha);
	}

	public static void DestroyChildren(this GameObject parent)
	{
		for (int i = parent.transform.childCount - 1; i >= 0; i--)
			Object.Destroy(parent.transform.GetChild(i).gameObject);
	}

	public static T SelectRandom<T>(this List<T> list)
	{
		return list[Random.Range(0, list.Count)];
	}

	public static Vector3 WithinRandomRadius(this Vector3 v, float radius)
	{
		Vector2 rand = Random.insideUnitCircle;
		return new Vector3(v.x + rand.x * radius, v.y, v.z + rand.y * radius);
	}

	public static bool LessThan(this Vector3 a, Vector3 b)
	{
		return ((a.x + a.y + a.z) < (b.x + b.y + b.z));
	}

	public static Rect ToScreenSpace(this RectTransform rectTransform)
	{
		Vector2 size = Vector2.Scale(rectTransform.rect.size, rectTransform.lossyScale);
		Rect rect = new Rect(rectTransform.position.x, Screen.height - rectTransform.position.y, size.x, size.y);
		rect.x -= (rectTransform.pivot.x * size.x);
		rect.y -= ((1.0f - rectTransform.pivot.y) * size.y);
		return rect;
	}
}