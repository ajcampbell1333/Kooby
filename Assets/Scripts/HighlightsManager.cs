using System.Collections.Generic;
using UnityEngine;

public class HighlightsManager : MonoBehaviour
{
	[SerializeField] private GameObject koobHighlightPrefab;
	private List<GameObject> highlightPool = new List<GameObject>();
	private int nextAvailableIndex = 0;
	
	private void Start()
	{
		InitializeHighlightPool();
	}
	
	private void InitializeHighlightPool()
	{
		if (koobHighlightPrefab == null)
		{
			KoobyLogManager.LogError(LogCategory.Manager, "koobHighlightPrefab not assigned.");
			return;
		}
		
		// Create 9 highlight instances (6+3 max needed)
		for (int i = 0; i < 9; i++)
		{
			GameObject highlight = Instantiate(koobHighlightPrefab, transform);
			highlight.name = $"Highlight_{i}";
			
			// Disable the MeshRenderer initially
			MeshRenderer renderer = highlight.GetComponent<MeshRenderer>();
			if (renderer != null)
				renderer.enabled = false;
			else
				KoobyLogManager.LogWarning(LogCategory.Manager, $"Highlight_{i} has no MeshRenderer component.");
			
			highlightPool.Add(highlight);
		}
		
		KoobyLogManager.Log(LogCategory.Manager, $"Initialized pool with {highlightPool.Count} highlights.");
	}
	
	public void PlaceHighlight(Vector3 worldPosition)
	{
		if (nextAvailableIndex >= highlightPool.Count)
		{
			KoobyLogManager.LogWarning(LogCategory.Manager, "No more highlights available in pool.");
			return;
		}
		
		GameObject highlight = highlightPool[nextAvailableIndex];
		highlight.transform.position = worldPosition;
		
		MeshRenderer renderer = highlight.GetComponent<MeshRenderer>();
		if (renderer != null)
			renderer.enabled = true;
		
		nextAvailableIndex++;
	}
	
	public void ResetHighlights()
	{
		foreach (GameObject highlight in highlightPool)
		{
			MeshRenderer renderer = highlight.GetComponent<MeshRenderer>();
			if (renderer != null)
				renderer.enabled = false;
		}
		
		nextAvailableIndex = 0;
	}
}
