//      
//   ^\.-
//  c====ɔ   Crafted with <3 by Nate Tessman
//   L__J    nate@madgvox.com
// 

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class MultiSceneManager : SceneManager {
	public static void LoadMultiScene ( string multiSceneName, bool includeUnloaded ) {

	}

	public static void LoadMultiScene ( string multiSceneName, bool includeUnloaded, LoadSceneMode mode ) {
		
	}
}

[CustomEditor( typeof( SceneAsset ) )]
public class SceneAssetInspector : Editor {
	public override void OnInspectorGUI () {
		GUILayout.Label( "Hello world" );
	}
}
