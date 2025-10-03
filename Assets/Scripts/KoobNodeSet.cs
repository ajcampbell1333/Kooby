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
		KoobyLogManager.LogWarning(LogCategory.Matrix, $"GetPos: No KoobNodeMarker found with logical position {position}.");
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
		KoobyLogManager.LogWarning(LogCategory.Matrix, $"GetLogicalPos: No KoobNodeMarker found near world position {worldPosition}.");
		return Vector3.zero;
	}

	public void SetPos(Transform playerNode, Vector3 position)
	{
		KoobyLogManager.Log(LogCategory.Matrix, $"SetPos: Looking for position {position}");
		for (int i = 0; i < koobNodeMarkers.Count; i++)
		{
			var marker = koobNodeMarkers[i];
			if (marker != null)
			{
					KoobyLogManager.Log(LogCategory.Matrix, $"  Checking marker {i}: position = {marker.position}, match = {marker.position == position}");
				if (marker.position == position)
				{
					playerNode.position = marker.transform.position;
						KoobyLogManager.Log(LogCategory.Matrix, $"  Found match! Moving to world position {marker.transform.position}");
					return;
				}
			}
		}
		KoobyLogManager.LogWarning(LogCategory.Matrix, $"SetPos: No KoobNodeMarker found with logical position {position}.");
	}
} 