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
using System.Reflection;
using System.Reflection.Emit;

public class MultiSceneDragHandler { 
        // public delegate DragAndDropVisualMode CustomDraggingDelegate(GameObjectTreeViewItem parentItem, GameObjectTreeViewItem targetItem, DropPosition dropPos, bool perform);

	static Type GameObjectTreeViewItem;
	static Type TreeViewDragging_DropPosition;

	static void InitTypes () {
		GameObjectTreeViewItem = Type.GetType( "GameObjectTreeViewItem" );
		TreeViewDragging_DropPosition = Type.GetType( "TreeViewDragging.DropPosition" );

		var argTypes = new [] {
			GameObjectTreeViewItem,
			GameObjectTreeViewItem,
			TreeViewDragging_DropPosition,
			typeof( bool )
		};

		var draggingDelegate = new DynamicMethod( "MultiSceneDraggingDelegate", typeof( DragAndDropVisualMode ), argTypes );
	}

	enum SomeEnum {
		Blah
	}

	DragAndDropVisualMode CastEx ( GameObject parentItem, GameObject targetItem, SomeEnum dropPos, bool perform ) {
		return InternalMethod( parentItem, targetItem, dropPos, perform );
	}

	DragAndDropVisualMode InternalMethod ( object parentItem, object targetItem, object dropPos, bool perform ) {
		return DragAndDropVisualMode.None;
	}
}

[CustomEditor( typeof( MultiScene ) )]
public class MultiSceneEditor : Editor {

	static class Content {
		public static readonly GUIContent listTitle              = new GUIContent( "Scenes" );
		public static readonly GUIContent loadScene              = new GUIContent( "L", "Set whether the scene should be loaded or just added." );
		public static readonly GUIContent activeScene            = new GUIContent( "A", "Set the scene that should be active." );
		public static readonly GUIContent toggleLoadScene        = new GUIContent( string.Empty, "Load Scene" );
		public static readonly GUIContent toggleActiveScene      = new GUIContent( string.Empty, "Set Active Scene" );
	}

	static class Styles {
		public static readonly GUIStyle dragInfoStyle;

		static Styles () {
			dragInfoStyle = new GUIStyle( EditorStyles.centeredGreyMiniLabel );
			dragInfoStyle.wordWrap = true;
		}
	}

	static string GetClosestDirectory ( string path ) {
		if( !Directory.Exists( path ) ) {
			return Path.GetDirectoryName( path );
		}
		return path;
	}

	static void SaveAssetToProject ( MultiScene asset ) {
		var path = "Assets";
		if( Selection.activeObject != null ) {
			path = GetClosestDirectory( AssetDatabase.GetAssetPath( asset.GetInstanceID() ) );
		}

		ProjectWindowUtil.CreateAsset( asset, string.Format( "{0}/{1}.asset", path, asset.name ) );
	}

	[MenuItem( "Assets/Create/Multi-Scene", false, 201 )]
	static void CreateMultiScene () {
		var multi = CreateInstance<MultiScene>();
		multi.name = "New Multi-Scene";

		SaveAssetToProject( multi );
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

			multi.sceneAssets.Add( new MultiScene.SceneInfo( sceneAsset, scene.isLoaded ) );
		}

		SaveAssetToProject( multi );
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
		Scene activeScene = default( Scene );
		if( additive || EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() ) {
			var firstUnloadedScenes = new List<string>();
			var inFirstUnloadedScenes = true;
			Scene firstLoadedScene = default( Scene );
			for( int i = 0; i < obj.sceneAssets.Count; i++ ) {
				var info = obj.sceneAssets[ i ];
				if( info.asset == null ) continue;
				var path = AssetDatabase.GetAssetPath( info.asset.GetInstanceID() );
				var mode = OpenSceneMode.Single;
				var isActiveScene = info.asset == obj.activeScene;

				var exitedFirstUnloadedScenes = false;
				if( inFirstUnloadedScenes ) {
					if( !isActiveScene && !info.loadScene ) {
						firstUnloadedScenes.Add( path );
						continue;
					} else {
						inFirstUnloadedScenes = false;
						exitedFirstUnloadedScenes = true;
					}
				}

				if( ( !inFirstUnloadedScenes && !exitedFirstUnloadedScenes ) || ( additive && exitedFirstUnloadedScenes ) ) { 
					if( ( !additive && isActiveScene ) || info.loadScene ) {
						mode = OpenSceneMode.Additive;
					} else {
						mode = OpenSceneMode.AdditiveWithoutLoading;
					}
				}

				var scene = EditorSceneManager.OpenScene( path, mode );

				if( isActiveScene ) activeScene = scene;
				if( exitedFirstUnloadedScenes ) firstLoadedScene = scene;
			}

			for( int i = 0; i < firstUnloadedScenes.Count; i++ ) {
				var path = firstUnloadedScenes[ i ];
				var scene = EditorSceneManager.OpenScene( path, OpenSceneMode.AdditiveWithoutLoading );
				if( firstLoadedScene.IsValid() ) EditorSceneManager.MoveSceneBefore( scene, firstLoadedScene );
			}
		}
		if( !additive && activeScene.IsValid() ) {
			EditorSceneManager.SetActiveScene( activeScene );
		}
	}

	static Scene SceneAssetToScene ( SceneAsset asset ) {
		return EditorSceneManager.GetSceneByPath( AssetDatabase.GetAssetPath( asset ) );
	}

	protected override void OnHeaderGUI () {
		if( target.sceneAssets == null ) return;
		base.OnHeaderGUI();	
	}

	public override void OnInspectorGUI () {
		if( target.sceneAssets == null ) return;
		EditorGUI.BeginChangeCheck();
		list.DoLayoutList();

		var evt = Event.current;

		if( evt.type == EventType.DragPerform || evt.type == EventType.DragUpdated ) {
			var objects = DragAndDrop.objectReferences;
			var canDrag = true;
			foreach( var obj in objects ) {
				if( !( obj is SceneAsset ) ) {
					canDrag = false;
					break;
				}
			}

			if( canDrag ) {
				DragAndDrop.visualMode = DragAndDropVisualMode.Move;

				if( evt.type == EventType.DragPerform ) {
					Undo.RecordObject( target, "Add Scenes" );
					GUI.changed = true;
					foreach( var obj in objects ) {
						var scene = (SceneAsset)obj;
						var index = target.sceneAssets.FindIndex( i => i.asset == scene );
						if( index > -1 ) {
							target.sceneAssets.RemoveAt( index );
						}
						target.sceneAssets.Add( new MultiScene.SceneInfo( scene ) );
					}
				}

				DragAndDrop.AcceptDrag();
				evt.Use();
			}
		} else if( evt.type == EventType.DragExited ) {
			Repaint();
		}

		if( EditorGUI.EndChangeCheck() ) {
			EditorUtility.SetDirty( target );
		}

		EditorGUILayout.Space();

		GUILayout.Label( "Drag and drop scenes into the inspector window to append them to the list.", Styles.dragInfoStyle );
	}

	class ScenePresetList : ReorderableList {

		MultiScene target;
		new List<MultiScene.SceneInfo> list;

		public ScenePresetList ( MultiScene target, List<MultiScene.SceneInfo> elements, Type elementType ) : base( elements, elementType, true, false, true, true ) {
			this.target = target;
			this.list = elements;

			drawElementCallback = DrawElement;
			drawHeaderCallback = DrawHeader;
			onRemoveCallback = OnRemove;
			onAddCallback = OnAdd;
		}

		void DrawHeader ( Rect rect ) {
			const int toggleWidth = 17;

			var loadSceneRect = new Rect( rect.x + rect.width - toggleWidth * 2, rect.y, toggleWidth, rect.height );
			var activeSceneRect = new Rect( rect.x + rect.width - toggleWidth, rect.y, toggleWidth, rect.height );
			var labelRect = new Rect( rect.x, rect.y, rect.width - ( toggleWidth * 2 ) - 5, EditorGUIUtility.singleLineHeight );

			GUI.Label( labelRect, Content.listTitle );
			GUI.Label( loadSceneRect, Content.loadScene );
			GUI.Label( activeSceneRect, Content.activeScene );
		}

		void DrawElement ( Rect rect, int index, bool isActive, bool isFocused ) {
			rect.y += 2;

			const int toggleWidth = 17;

			var loadSceneRect = new Rect( rect.x + rect.width - toggleWidth * 2, rect.y, toggleWidth, rect.height );
			var activeSceneRect = new Rect( rect.x + rect.width - toggleWidth, rect.y, toggleWidth, rect.height );
			var labelRect = new Rect( rect.x, rect.y, rect.width - ( toggleWidth * 2 ) - 5, EditorGUIUtility.singleLineHeight );

			MultiScene.SceneInfo info = list[ index ];
			EditorGUI.BeginChangeCheck();
			var scene = (SceneAsset)EditorGUI.ObjectField( labelRect, info.asset, typeof( SceneAsset ), false );
			if( EditorGUI.EndChangeCheck() ) {
				Undo.RecordObject( target, "Change Scene" );
				info.asset = scene;
				target.sceneAssets[ index ] = info;
			}

			var active = info.asset == target.activeScene;
			GUI.enabled = !active;
			EditorGUI.BeginChangeCheck();
			var loadScene = GUI.Toggle( loadSceneRect, info.loadScene, Content.toggleLoadScene );
			if( EditorGUI.EndChangeCheck() ) {
				Undo.RecordObject( target, "Change Load Scene" );
				info.loadScene = loadScene;
				target.sceneAssets[ index ] = info;
			}
			GUI.enabled = true;

			EditorGUI.BeginChangeCheck();
			var setActive = GUI.Toggle( activeSceneRect, active, Content.toggleActiveScene );
			if( EditorGUI.EndChangeCheck() ) {
				if( setActive ) {
					Undo.RecordObject( target, "Change Active Scene" );
					target.activeScene = info.asset;
				}
			}
		}

		void OnRemove ( ReorderableList l ) {
			Undo.RecordObject( target, "Remove Scene" );
			var removed = target.sceneAssets[ index ];
			if( removed.asset == target.activeScene ) {
				target.activeScene = null;
			}
			target.sceneAssets.RemoveAt( index );
		}

		void OnAdd ( ReorderableList l ) {
			index = list.Count;
			Undo.RecordObject( target, "Add Scene" );
			list.Add( default( MultiScene.SceneInfo ) );
		}
	}
}
