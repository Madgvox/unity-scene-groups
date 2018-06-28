using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ScenePreset : ScriptableObject {
	public SceneAsset activeScene;
	public List<SceneAsset> sceneAssets;
}