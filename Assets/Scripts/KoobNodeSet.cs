using System.Collections.Generic;
using UnityEngine;

public class KoobNodeSet : MonoBehaviour
{
	private List<KoobNodeMarker> koobNodeMarkers = new List<KoobNodeMarker>();

	private void Awake()
	{
		koobNodeMarkers.Clear();
		GetComponentsInChildren(true, koobNodeMarkers);
	}

	public Vector3 GetPos(Vector3 position)
	{
		for (int i = 0; i < koobNodeMarkers.Count; i++)
		{
			var marker = koobNodeMarkers[i];
			if (marker != null && marker.position == position)
				return marker.transform.position;
		}
		Debug.LogWarning($"GetPos: No KoobNodeMarker found with logical position {position}.");
		return Vector3.zero;
	}
	
	public Vector3 GetLogicalPos(Vector3 worldPosition)
	{
		for (int i = 0; i < koobNodeMarkers.Count; i++)
		{
			var marker = koobNodeMarkers[i];
			if (marker != null && Vector3.Distance(marker.transform.position, worldPosition) < 0.1f)
				return marker.position;
		}
		Debug.LogWarning($"GetLogicalPos: No KoobNodeMarker found near world position {worldPosition}.");
		return Vector3.zero;
	}

	public void SetPos(Transform playerNode, Vector3 position)
	{
		Debug.Log($"SetPos: Looking for position {position}");
		for (int i = 0; i < koobNodeMarkers.Count; i++)
		{
			var marker = koobNodeMarkers[i];
			if (marker != null)
			{
				Debug.Log($"  Checking marker {i}: position = {marker.position}, match = {marker.position == position}");
				if (marker.position == position)
				{
					playerNode.position = marker.transform.position;
					Debug.Log($"  Found match! Moving to world position {marker.transform.position}");
					return;
				}
			}
		}
		Debug.LogWarning($"SetPos: No KoobNodeMarker found with logical position {position}.");
	}
} 