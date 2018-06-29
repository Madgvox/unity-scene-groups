using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MultiScene : ScriptableObject {
	public SceneAsset activeScene;
	public List<SceneAsset> sceneAssets = new List<SceneAsset>();
}
