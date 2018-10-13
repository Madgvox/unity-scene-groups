//      
//   ^\.-
//  c====ɔ   Crafted with <3 by Nate Tessman
//   L__J    nate@madgvox.com
// 

using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class MultiSceneDragHandler {
	public enum DropPosition {
		Upon = 0,
		Below = 1,
		Above = 2
	}

	public static DragAndDropVisualMode DoDrag ( object parentItem, object targetItem, bool perform, DropPosition dropPosition ) {
		const string kSceneHeaderDragString = "SceneHeaderList";
		
		bool insertNewScenes = true;
		bool hasMultiScene = false;
		if ( DragAndDrop.objectReferences.Length > 0 ) {
			foreach ( var obj in DragAndDrop.objectReferences ) {
				if( obj is MultiScene ) {
					hasMultiScene = true;
				}
				if ( !( obj is MultiScene || obj is SceneAsset ) ) {
					insertNewScenes = false;
					break;
				}
			}
		}

		if( !hasMultiScene || !insertNewScenes ) {
			return DragAndDropVisualMode.None;
		}

		if( perform && insertNewScenes ) {
			var sceneList = DragAndDrop.GetGenericData( kSceneHeaderDragString ) as List<Scene>;
			if( sceneList == null ) {
				sceneList = new List<Scene>();
			}

			foreach( var obj in DragAndDrop.objectReferences ) {
				if( obj is MultiScene ) {
					var ms = (MultiScene)obj;

					foreach( var info in ms.sceneAssets ) {
						sceneList.Add( LoadScene( info.asset, info.loadScene ) );
					}
				} else { // obj is SceneAsset
					sceneList.Add( LoadScene( (SceneAsset)obj, true ) );
				}
			}

			if( targetItem != null ) {
				DragAndDrop.SetGenericData( kSceneHeaderDragString, sceneList );
				return DragAndDropVisualMode.None;  // return None so that we can use Unity's default scene re-ordering behaviour
			}
		}

		return DragAndDropVisualMode.Move;
	} 

	static Scene LoadScene ( SceneAsset sceneAsset, bool loaded ) {
		string scenePath = AssetDatabase.GetAssetPath( sceneAsset );
		Scene scene = SceneManager.GetSceneByPath( scenePath );

		if( scene.IsValid() ) {
		} else {
			bool unloaded = !loaded || Event.current.alt;
			if( unloaded ) {
				scene = EditorSceneManager.OpenScene( scenePath, OpenSceneMode.AdditiveWithoutLoading );
			} else {
				scene = EditorSceneManager.OpenScene( scenePath, OpenSceneMode.Additive );
			}
		}

		return scene;
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
	
