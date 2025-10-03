using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class KoobySceneBootstrap
{
	[InitializeOnLoadMethod]
	private static void CreateMainSceneIfMissing()
	{
		const string scenePath = "Assets/Scenes/Main.unity";
		if (File.Exists(scenePath))
		{
			return;
		}

		var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
		bool saved = EditorSceneManager.SaveScene(scene, scenePath);
		if (saved)
		{
			AssetDatabase.Refresh();

			// Ensure the scene is in Build Settings
			var existingScenes = EditorBuildSettings.scenes;
			bool alreadyListed = false;
			for (int i = 0; i < existingScenes.Length; i++)
			{
				if (existingScenes[i].path == scenePath)
				{
					alreadyListed = true;
					break;
				}
			}
			if (!alreadyListed)
			{
				var newList = new EditorBuildSettingsScene[existingScenes.Length + 1];
				existingScenes.CopyTo(newList, 0);
				newList[newList.Length - 1] = new EditorBuildSettingsScene(scenePath, true);
				EditorBuildSettings.scenes = newList;
			}

			Debug.Log($"Created scene: {scenePath} and ensured it's in Build Settings.");
		}
		else
		{
			Debug.LogError($"Failed to create scene at: {scenePath}");
		}
	}
} 