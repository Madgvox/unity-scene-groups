//      
//   ^\.-
//  c====ɔ   Crafted with <3 by Nate Tessman
//   L__J    nate@madgvox.com
// 

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MultiScene : ScriptableObject {
	public SceneAsset activeScene;
	public List<SceneAsset> sceneAssets = new List<SceneAsset>();
}
