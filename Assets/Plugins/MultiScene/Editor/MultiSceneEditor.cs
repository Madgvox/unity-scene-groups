//      
//   ^\.-
//  c====ɔ   Crafted with <3 by Nate Tessman
//   L__J    nate@madgvox.com
// 

using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System.IO;
using UnityEditor.Callbacks;
using UnityEditorInternal;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

[CustomEditor( typeof( MultiScene ) )]
public class MultiSceneEditor : Editor {

	[MenuItem( "Assets/Create/Multi-Scene", false, 201 )]
	static void CreateMultiScene () {
		var multi = CreateInstance<MultiScene>();
		multi.name = "New Multi-Scene";

		var parent = Selection.activeObject;

		string directory = "Assets";
		if( parent != null ) {
			directory = AssetDatabase.GetAssetPath( parent.GetInstanceID() );
			if( !Directory.Exists( directory ) ) {
				directory = Path.GetDirectoryName( directory );
			}
		}

		ProjectWindowUtil.CreateAsset( multi, string.Format( "{0}/{1}.asset", directory, multi.name ) );
	}

	[MenuItem( "Edit/Multi-Scene From Open Scenes %#&s", false, 0 )]
	static void CreatePresetFromOpenScenes () {
		var multi = CreateInstance<MultiScene>();
		multi.name = "New Multi-Scene";

		var activeScene = EditorSceneManager.GetActiveScene();
		var sceneCount = EditorSceneManager.sceneCount;

		for( int i = 0; i < sceneCount; i++ ) {
			var scene = EditorSceneManager.GetSceneAt( i );

			var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>( scene.path );
			if( activeScene == scene ) {
				multi.activeScene = sceneAsset;
			}
			multi.sceneAssets.Add( sceneAsset );
		}

		var directory = AssetDatabase.GetAssetPath( Selection.activeObject.GetInstanceID() );
		var isDirectory = Directory.Exists( directory );
		if( !isDirectory ) {
			directory = Path.GetDirectoryName( directory );
		}

		ProjectWindowUtil.CreateAsset( multi, string.Format( "{0}/{1}.asset", directory, multi.name ) );
	}

	[OnOpenAsset( 1 )]
	static bool OnOpenAsset ( int id, int line ) {
		var obj = EditorUtility.InstanceIDToObject( id );
		if( obj is MultiScene ) {	
			OpenMultiscene( (MultiScene)obj, Event.current.alt );
			return true;
		} else if( obj is SceneAsset ) {
			if( Event.current.alt ) {
				EditorSceneManager.OpenScene( AssetDatabase.GetAssetPath( obj.GetInstanceID() ), OpenSceneMode.Additive );
				return true;
			} else {
				return false;
			}
		} else {
			return false;
		}
	}

	new MultiScene target;

	ScenePresetList list;

	void OnEnable () {
		target = (MultiScene)base.target;
		list = new ScenePresetList( target, target.sceneAssets, typeof( SceneAsset ) );
	}

	private static void OpenMultiscene ( MultiScene obj, bool additive ) {
		if( EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() ) {
			for( int i = 0; i < obj.sceneAssets.Count; i++ ) {
				var scene = obj.sceneAssets[ i ];
				if( scene == null ) continue;
				var path = AssetDatabase.GetAssetPath( scene.GetInstanceID() );
				var mode = ( additive || i > 0 ) ? OpenSceneMode.Additive : OpenSceneMode.Single;
				EditorSceneManager.OpenScene( path, mode );
			}
		}
		if( !additive && obj.activeScene != null ) {
			EditorSceneManager.SetActiveScene( SceneAssetToScene( obj.activeScene ) );
		}
	}

	static Scene SceneAssetToScene ( SceneAsset asset ) {
		return EditorSceneManager.GetSceneByName( asset.name );
	}

	protected override void OnHeaderGUI () {
		if( target.sceneAssets == null ) return;
		base.OnHeaderGUI();	
	}

	public override void OnInspectorGUI () {
		if( target.sceneAssets == null ) return;
		list.DoLayoutList();

		var evt = Event.current;

		if( evt.type == EventType.DragPerform || evt.type == EventType.DragUpdated ) {
			var objects = DragAndDrop.objectReferences;
			var canDrag = false;
			foreach( var obj in objects ) {
				if( !( obj is SceneAsset ) ) {
					canDrag = false;
					break;
				} else {
					canDrag = true;
				}
			}

			if( canDrag ) {
				DragAndDrop.visualMode = DragAndDropVisualMode.Move;

				if( evt.type == EventType.DragPerform ) {
					Undo.RecordObject( target, "Add Scenes" );
					foreach( var obj in objects ) {
						var scene = (SceneAsset)obj;
						if( !target.sceneAssets.Contains( scene ) ) {
							target.sceneAssets.Add( scene );
							GUI.changed = true;
						}
					}
				}
			}

			if( canDrag ) {
				DragAndDrop.AcceptDrag();
				evt.Use();
			}
		} else if( evt.type == EventType.DragExited ) {
			Repaint();
		}
	}

	class ScenePresetList : ReorderableList {
		MultiScene target;
		new List<SceneAsset> list;

		public ScenePresetList ( MultiScene target, List<SceneAsset> elements, Type elementType ) : base( elements, elementType, true, false, true, true ) {
			this.target = target;
			this.list = elements;

			drawElementCallback = DrawElement;
			drawHeaderCallback = DrawHeader;
			onRemoveCallback = OnRemove;
			onAddCallback = OnAdd;
		}

		void DrawHeader ( Rect rect ) {
			GUI.Label( rect, "Scenes" );
		}

		void DrawElement ( Rect rect, int index, bool isActive, bool isFocused ) {
			rect.y += 2;

			const int toggleWidth = 17;

			var toggleRect = new Rect( rect.x + rect.width - toggleWidth, rect.y, toggleWidth, rect.height );
			var labelRect = new Rect( rect.x, rect.y, rect.width - toggleRect.width - 5, EditorGUIUtility.singleLineHeight );

			SceneAsset scene = list[ index ];
			EditorGUI.BeginChangeCheck();
			scene = (SceneAsset)EditorGUI.ObjectField( labelRect, scene, typeof( SceneAsset ), false );
			if( EditorGUI.EndChangeCheck() ) {
				Undo.RecordObject( target, "Change Scene" );
				target.sceneAssets[ index ] = scene;
			}

			EditorGUI.BeginChangeCheck();
			var check = GUI.Toggle( toggleRect, scene == target.activeScene, GUIContent.none );
			if( EditorGUI.EndChangeCheck() ) {
				if( check ) {
					Undo.RecordObject( target, "Change Active Scene" );
					target.activeScene = scene;
				}
			}
		}

		void OnRemove ( ReorderableList l ) {
			Undo.RecordObject( target, "Remove Scene" );
			target.sceneAssets.RemoveAt( index );
		}

		void OnAdd ( ReorderableList l ) {
			index = list.Count;
			Undo.RecordObject( target, "Add Scene" );
			list.Add( default( SceneAsset ) );
		}
	}
}
