//      
//   ^\.-
//  c====ɔ   Crafted with <3 by Nate Tessman
//   L__J    nate@madgvox.com
// 

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MultiScene : ScriptableObject {
	[Serializable]
	public struct SceneInfo {
		public SceneAsset asset;
		public bool loadScene;

		public SceneInfo ( SceneAsset asset, bool loadScene = true ) {
			this.asset = asset;
			this.loadScene = loadScene;
		}
	}

	public SceneAsset activeScene;
	public List<SceneInfo> sceneAssets = new List<SceneInfo>();
}
