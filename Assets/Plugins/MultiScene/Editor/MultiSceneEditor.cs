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

[CustomEditor( typeof( MultiScene ) )]
public class MultiSceneEditor : Editor {

	static Type proxyType;

	[InitializeOnLoadMethod]
	static void InitializeCustomDrag () {
		proxyType = TreeViewDraggingProxyBuilder.BuildType();

		if( proxyType == null ) {
			// we're pre-2017, don't inject anything
			return;
		}

		var originalField = TypeCache._TreeViewController.GetProperty( "dragging", BindingFlags.Instance | BindingFlags.Public );
		var treeViewProp = TypeCache._SceneHierarchyWindow.GetProperty( "treeView", BindingFlags.Instance | BindingFlags.NonPublic );
		var SetAsLastInteractedHierarchy = TypeCache._SceneHierarchyWindow.GetMethod( "SetAsLastInteractedHierarchy", BindingFlags.Instance | BindingFlags.NonPublic );
		var hierarchyWindows = Resources.FindObjectsOfTypeAll( TypeCache._SceneHierarchyWindow );

		foreach( var window in hierarchyWindows ) {
			// avoid nullreference by making sure last interacted hierarchy is set
			SetAsLastInteractedHierarchy.Invoke( window, null );
			var treeView = treeViewProp.GetValue( window, null );
			Debug.Log( "in value " + originalField.GetValue( treeView, null ) );
			var original = originalField.GetValue( treeView, null );
			var proxy = Activator.CreateInstance( proxyType, treeView );

			// Debug.Log( proxy.GetType() );
			// Debug.Log( treeView );
			// Debug.Log( originalField.PropertyType );

			originalField.SetValue( treeView, proxy, null );
			Debug.Log( "out value " + originalField.GetValue( treeView, null ) );
		}
	}

	// static double countdown;
	// static double lastTime;
	// static void WaitForHierarchyInit () {
	// 	var time = EditorApplication.timeSinceStartup;
	// 	var deltaTime = time - lastTime;

	// 	if( countdown > 0 ) {
	// 		lastTime = time;
	// 		countdown -= deltaTime;
	// 		return;
	// 	}

	// 	countdown = 2;
	// }

	// static void Init () {
	// 	EditorApplication.update -= Init;

	// 	var dragging = TreeViewController.GetProperty( "dragging", BindingFlags.Instance | BindingFlags.Public );
	// 	var windows = Resources.FindObjectsOfTypeAll( SceneHierarchyWindow );
	// 	var shw_m_TreeView = SceneHierarchyWindow.GetField( "m_TreeView", BindingFlags.Instance | BindingFlags.NonPublic );
	// 	var wrapperType = TreeViewDraggingProxy = builder.CreateType();

	// 	foreach( var win in windows ) {
	// 		var treeView = shw_m_TreeView.GetValue( win );
	// 		// Debug.Log( shw_m_TreeView );
	// 		// Debug.Log( win );
	// 		Debug.Log( treeView );
	// 		// var existing = dragging.GetValue( treeView, null );
	// 		// Debug.Log( existing );
	// 		// var instance = Activator.CreateInstance( wrapperType, existing, treeView );
	// 		// Debug.Log( instance );
	// 		// Debug.Log( dragging.PropertyType );
	// 		// dragging.SetValue( treeView, instance, null );
	// 	}
	// }

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


/*
#if UNITY_2018_3_OR_NEWER
	static Type GameObjectTreeViewItem;
	static Type TreeViewDragging_DropPosition;
	static Type GameObjectsTreeViewDragging;
	static Type GameObjectsTreeViewDragging_CustomDraggingDelegate;

	public static void InitTypes () {
		var editorAssembly = Assembly.GetAssembly( typeof( SceneView ) );
		GameObjectTreeViewItem = editorAssembly.GetType( "UnityEditor.GameObjectTreeViewItem" );
		TreeViewDragging_DropPosition = editorAssembly.GetType( "UnityEditor.IMGUI.Controls.TreeViewDragging" ).GetNestedType( "DropPosition", BindingFlags.Public );
		GameObjectsTreeViewDragging = editorAssembly.GetType( "UnityEditor.GameObjectsTreeViewDragging" );
		var membs = GameObjectsTreeViewDragging.GetMembers();

		foreach( var m in membs ) {
			Debug.Log( m );
		}

		Debug.Log( GameObjectTreeViewItem );
		Debug.Log( TreeViewDragging_DropPosition );
		Debug.Log( GameObjectsTreeViewDragging );
		Debug.Log( GameObjectsTreeViewDragging_CustomDraggingDelegate );

		return;

		var argTypes = new [] {
			GameObjectTreeViewItem,
			GameObjectTreeViewItem,
			TreeViewDragging_DropPosition,
			typeof( bool )
		};

		var draggingMethod = new DynamicMethod( "MultiSceneDraggingDelegate", typeof( DragAndDropVisualMode ), argTypes );
		var g = draggingMethod.GetILGenerator();

		var methodType = typeof( MultiSceneDragHandler ).GetMethod( "InternalMethod", BindingFlags.Static );

		g.Emit( OpCodes.Ldarg_0 );
		g.Emit( OpCodes.Ldarg_1 );
		g.Emit( OpCodes.Ldarg_2 );
		g.Emit( OpCodes.Ldarg_3 );
		g.Emit( OpCodes.Call, methodType );
		g.Emit( OpCodes.Ret );

		// Delegate draggingDelegate = draggingMethod.CreateDelegate( GameObjectsTreeViewDragging_CustomDraggingDelegate );
	}
#else 
	// internal interface ITreeViewDragging {
    //     void OnInitialize();
    //     bool CanStartDrag(TreeViewItem targetItem, List<int> draggedItemIDs, Vector2 mouseDownPosition);
    //     void StartDrag(TreeViewItem draggedItem, List<int> draggedItemIDs);
    //     bool DragElement(TreeViewItem targetItem, Rect targetItemRect, int row);             // 'targetItem' is null when not hovering over any target Item.  Returns true if drag was handled.
    //     void DragCleanup(bool revertExpanded);
    //     int GetDropTargetControlID();
    //     int GetRowMarkerControlID();
    //     bool drawRowMarkerAbove { get; set; }
    // }


//  internal abstract class TreeViewDragging : ITreeViewDragging
//     {
//         public TreeViewDragging(TreeViewController treeView);
//         virtual public void OnInitialize();
//         public virtual bool CanStartDrag(TreeViewItem targetItem, List<int> draggedItemIDs, Vector2 mouseDownPosition);
//         public abstract void StartDrag(TreeViewItem draggedItem, List<int> draggedItemIDs);
//         public abstract DragAndDropVisualMode DoDrag(TreeViewItem parentItem, TreeViewItem targetItem, bool perform, DropPosition dropPosition);
//         public virtual bool DragElement(TreeViewItem targetItem, Rect targetItemRect, bool firstItem);
//         protected virtual void HandleAutoExpansion(int itemControlID, TreeViewItem targetItem, Rect targetItemRect, float betweenHalfHeight, Vector2 currentMousePos);
//         public virtual void DragCleanup(bool revertExpanded);
//     }

	public static Type TreeViewDraggingProxy;

	public static Type TreeViewDragging;
	public static Type ITreeViewDragging;
	public static FieldInfo m_TreeView;
	public static Type TreeViewController;
	public static Type SceneHierarchyWindow;

	static TypeBuilder builder;

	private static TypeBuilder GetTypeBuilder () {
		var asmName = new AssemblyName( string.Format( "{0}_{1}", "MultiSceneRuntime", Guid.NewGuid().ToString( "N" ) ) );
		AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly( asmName, AssemblyBuilderAccess.Run );

		ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule( "core" );

		var typeName = "TreeViewDraggingProxy";
		TypeBuilder tb = moduleBuilder.DefineType( typeName,
				TypeAttributes.Public |
				TypeAttributes.Class |
				TypeAttributes.AutoClass |
				TypeAttributes.AnsiClass |
				TypeAttributes.AutoLayout );

		return tb;
	}


	public static void InitTypes () {
		if( TreeViewDraggingProxy != null ) return;

		var editorAssembly = Assembly.GetAssembly( typeof( SceneView ) );
		ITreeViewDragging = editorAssembly.GetType( "UnityEditor.IMGUI.Controls.ITreeViewDragging" );
		TreeViewDragging = editorAssembly.GetType( "UnityEditor.IMGUI.Controls.TreeViewDragging" );
		m_TreeView = TreeViewDragging.GetField( "m_TreeView", BindingFlags.NonPublic | BindingFlags.Instance );

		TreeViewController = editorAssembly.GetType( "UnityEditor.IMGUI.Controls.TreeViewController" );
		SceneHierarchyWindow = editorAssembly.GetType( "UnityEditor.SceneHierarchyWindow" );

		var m_TreeViewField = typeof( MultiSceneDragHandler ).GetField( "m_TreeView", BindingFlags.Static | BindingFlags.Public );

		builder = GetTypeBuilder();
		builder.SetParent( TreeViewDragging );

		var target = builder.DefineField( "target", TreeViewDragging, FieldAttributes.Private );

		var baseConstructor = TreeViewDragging.GetConstructor( BindingFlags.Public | BindingFlags.Instance, null, new Type[] { TreeViewController }, null );
		var cb = builder.DefineConstructor( MethodAttributes.Public, CallingConventions.Standard, new Type[] { TreeViewDragging, TreeViewController } );
		var g = cb.GetILGenerator();
		
		g.Emit( OpCodes.Ldarg_0 ); // push `this`
		g.Emit( OpCodes.Ldarg_2 ); // push `controller`
		g.Emit( OpCodes.Call, baseConstructor ); // this.base( controller );

		g.Emit( OpCodes.Ldarg_0 ); // push `this`
		g.Emit( OpCodes.Ldarg_1 ); // push arg_1
		g.Emit( OpCodes.Stfld, target ); // this.target = arg_1;

		g.Emit( OpCodes.Nop );
		g.Emit( OpCodes.Nop );

		g.Emit( OpCodes.Ret );

		CreateWrapperMethod( "OnInitialize", builder, target );
		CreateWrapperMethod( "CanStartDrag", builder, target );
		CreateWrapperMethod( "StartDrag", builder, target );
		CreateWrapperMethod( "DragElement", builder, target );
		CreateWrapperMethod( "HandleAutoExpansion", builder, target );
		CreateWrapperMethod( "DragCleanup", builder, target );

		CreateDoDrag( builder, target );

		Init();
		// EditorApplication.update += Init;
	}

	static void Init () {
		EditorApplication.update -= Init;

		var dragging = TreeViewController.GetProperty( "dragging", BindingFlags.Instance | BindingFlags.Public );
		var windows = Resources.FindObjectsOfTypeAll( SceneHierarchyWindow );
		var shw_m_TreeView = SceneHierarchyWindow.GetField( "m_TreeView", BindingFlags.Instance | BindingFlags.NonPublic );
		var wrapperType = TreeViewDraggingProxy = builder.CreateType();

		foreach( var win in windows ) {
			var treeView = shw_m_TreeView.GetValue( win );
			// Debug.Log( shw_m_TreeView );
			// Debug.Log( win );
			Debug.Log( treeView );
			// var existing = dragging.GetValue( treeView, null );
			// Debug.Log( existing );
			// var instance = Activator.CreateInstance( wrapperType, existing, treeView );
			// Debug.Log( instance );
			// Debug.Log( dragging.PropertyType );
			// dragging.SetValue( treeView, instance, null );
		}
	}

	static void CreateDoDrag ( TypeBuilder builder, FieldInfo wrapperField ) {
		var methodInfo = TreeViewDragging.GetMethod( "DoDrag", BindingFlags.Instance | BindingFlags.Public );
		var parameters = methodInfo.GetParameters();

		var parameterTypes = new Type[ parameters.Length ];

		for( int i = 0; i < parameters.Length; i++ ) {
			parameterTypes[ i ] = parameters[ i ].ParameterType;
		}

		var mb = builder.DefineMethod( "DoDrag", 
			MethodAttributes.Public | 
			MethodAttributes.ReuseSlot | 
			MethodAttributes.Virtual |
			MethodAttributes.HideBySig, 
			CallingConventions.HasThis, methodInfo.ReturnType, parameterTypes );

		var g = mb.GetILGenerator();

		var lb = g.DeclareLocal( typeof( DragAndDropVisualMode ) ); // DragAndDropVisualMode dragResult;

		var noDragResult = g.DefineLabel();

		var doDrag = typeof( MultiSceneDragHandler ).GetMethod( "DoDrag", BindingFlags.Public | BindingFlags.Static );
		g.Emit( OpCodes.Ldarg_2 ); // push targetItem
		g.Emit( OpCodes.Ldarg_3 ); // push perform
		g.Emit( OpCodes.Call, doDrag ); // call MultiSceneDragHandler.DoDrag( targetItem, perform );, push return value onto stack
		g.Emit( OpCodes.Stloc_0 ); // push `dragResult` onto stack, pop the return value off the stack

		g.Emit( OpCodes.Ldloc_0 ); // push `dragResult`
		g.Emit( OpCodes.Ldc_I4, (int)DragAndDropVisualMode.None ); // push DragAndDropVisualMode.None
		g.Emit( OpCodes.Beq, noDragResult ); // jump to noDragResult if `dragResult` == DragAndDropVisualMode.None

		g.Emit( OpCodes.Ldloc_0 );  // push `dragResult`
		g.Emit( OpCodes.Ret );      // return `dragResult`
		
		g.MarkLabel( noDragResult );

		g.Emit( OpCodes.Ldarg_0 ); // push `this`
		g.Emit( OpCodes.Ldfld, wrapperField ); // pop `this`, push `this.target`

		g.Emit( OpCodes.Ldarg_1 ); // push `arg_1`
		g.Emit( OpCodes.Ldarg_2 ); // push `arg_2`
		g.Emit( OpCodes.Ldarg_3 ); // push `arg_3`
		g.Emit( OpCodes.Ldarg_S, 4 ); // push `arg_4`

		g.Emit( OpCodes.Callvirt, methodInfo ); // call DoDrag( arg_1, arg_2, arg_3, arg_4 );
		g.Emit( OpCodes.Ret ); // return DoDrag result
	}

	static void CreateWrapperMethod ( string methodName, TypeBuilder builder, FieldInfo wrapperField ) {
		var methodInfo = TreeViewDragging.GetMethod( methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance );

		var parameters = methodInfo.GetParameters();

		var parameterTypes = new Type[ parameters.Length ];

		for( int i = 0; i < parameters.Length; i++ ) {
			parameterTypes[ i ] = parameters[ i ].ParameterType;
		}

		var mb = builder.DefineMethod( methodName, 
			MethodAttributes.Public | 
			MethodAttributes.ReuseSlot | 
			MethodAttributes.Virtual |
			MethodAttributes.HideBySig, 
			CallingConventions.HasThis, methodInfo.ReturnType, parameterTypes );

		var g = mb.GetILGenerator();

		g.Emit( OpCodes.Ldarg_0 );
		g.Emit( OpCodes.Ldfld, wrapperField );

		switch( parameters.Length ) {
			case 0:
				break;
			case 1:
				g.Emit( OpCodes.Ldarg_1 );
				break;
			case 2:
				g.Emit( OpCodes.Ldarg_1 );
				g.Emit( OpCodes.Ldarg_2 );
				break;
			case 3:
				g.Emit( OpCodes.Ldarg_1 );
				g.Emit( OpCodes.Ldarg_2 );
				g.Emit( OpCodes.Ldarg_3 );
				break;
			default:
				g.Emit( OpCodes.Ldarg_1 );
				g.Emit( OpCodes.Ldarg_2 );
				g.Emit( OpCodes.Ldarg_3 );
				for( byte i = 3; i < parameters.Length; i++ ) {
					g.Emit( OpCodes.Ldarg_S, i + 1 );
				}
				break;
		}

		g.Emit( OpCodes.Callvirt, methodInfo ); // target.[methodName]( [args] );
		g.Emit( OpCodes.Ret );
	}

#endif

*/
	