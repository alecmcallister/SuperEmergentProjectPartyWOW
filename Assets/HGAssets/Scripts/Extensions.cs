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

}