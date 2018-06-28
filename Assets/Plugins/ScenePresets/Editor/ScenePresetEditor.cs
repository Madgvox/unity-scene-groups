using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System.IO;
using UnityEditor.Callbacks;
using UnityEditorInternal;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

[CustomEditor( typeof( ScenePreset ) )]
public class ScenePresetEditor : Editor {

	[MenuItem( "Assets/Create/Scene Preset", false, 201 )]
	static void CreateMultiScene () {
		var multi = CreateInstance<ScenePreset>();
		multi.name = "New Scene Preset";

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
		if( obj is ScenePreset ) {
			OpenMultiscene( (ScenePreset)obj );
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

	new ScenePreset target;

	ScenePresetList list;

	void OnEnable () {
		target = (ScenePreset)base.target;
		list = new ScenePresetList( target, target.sceneAssets, typeof( SceneAsset ) );
	}

	private static void OpenMultiscene ( ScenePreset obj ) {
		if( EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() ) {
			for( int i = 0; i < obj.sceneAssets.Count; i++ ) {
				var scene = obj.sceneAssets[ i ];
				if( scene == null ) continue;
				var path = AssetDatabase.GetAssetPath( scene.GetInstanceID() );
				var mode = i == 0 ? OpenSceneMode.Single : OpenSceneMode.Additive;
				EditorSceneManager.OpenScene( path, mode );
			}
		}
		if( obj.activeScene != null ) {
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

		var controlId = GUIUtility.GetControlID( FocusType.Passive );

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
		ScenePreset target;
		new List<SceneAsset> list;

		public ScenePresetList ( ScenePreset target, List<SceneAsset> elements, Type elementType ) : base( elements, elementType, true, false, true, true ) {
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
				Undo.RecordObject( target, "Change Active Scene" );
				target.activeScene = scene;
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
