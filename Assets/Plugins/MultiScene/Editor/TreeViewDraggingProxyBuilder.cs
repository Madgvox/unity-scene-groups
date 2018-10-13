//      
//   ^\.-
//  c====ɔ   Crafted with <3 by Nate Tessman
//   L__J    nate@madgvox.com
// 

using UnityEditor;
using System;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

public class TypeCache {

	public static readonly Type _TreeViewDragging;
	public static readonly Type _SceneHierarchyWindow;
	public static readonly Type _TreeViewController;
	public static readonly Type _MultiSceneDragHandler;
	public static readonly Type _GameObjectsTreeViewDragging;

	static TypeCache () {
		var editorAssembly = Assembly.GetAssembly( typeof( SceneView ) );
		_TreeViewDragging = editorAssembly.GetType( "UnityEditor.IMGUI.Controls.TreeViewDragging" );
		_TreeViewController = editorAssembly.GetType( "UnityEditor.IMGUI.Controls.TreeViewController" );
		_GameObjectsTreeViewDragging = editorAssembly.GetType( "UnityEditor.GameObjectsTreeViewDragging" );
		_SceneHierarchyWindow = editorAssembly.GetType( "UnityEditor.SceneHierarchyWindow" );
		_MultiSceneDragHandler = typeof( MultiSceneDragHandler );
	}
}

#if UNITY_2017_1_OR_NEWER
public class TreeViewDraggingProxyBuilder {
	const string ASM_NAME        = "MultiSceneRuntime";
	const string PROXY_TYPE_NAME = "TreeViewDraggingProxy";

	public static Type BuildType () {
		var builder = BuildTypeBuilder();

		var original = builder.DefineField( "recursed", typeof( bool ), FieldAttributes.Private );

		BuildConstructor( builder, original );

		BuildDoDragMethod( builder, original );

		return builder.CreateType();
	}

	/* This method generates the equivalent of this code:
	 *
	 *     public class TreeViewDraggingProxy : GameObjectsTreeViewDragging {
	 */
	static TypeBuilder BuildTypeBuilder () {
		var asmName = new AssemblyName( string.Format( "{0}_{1}", ASM_NAME, Guid.NewGuid().ToString( "N" ) ) );
		AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly( asmName, AssemblyBuilderAccess.Run );
		ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule( "core" );

		return moduleBuilder.DefineType( PROXY_TYPE_NAME,
				TypeAttributes.Public |
				TypeAttributes.Class |
				TypeAttributes.AutoClass |
				TypeAttributes.AnsiClass |
				TypeAttributes.AutoLayout,
				TypeCache._GameObjectsTreeViewDragging );
	}


	/* This method generates the equivalent of this code:
	 *
	 *     public TreeViewDraggingProxy ( TreeViewController controller ) : base( controller ) {}
	 */
	static void BuildConstructor ( TypeBuilder builder, FieldInfo original ) {
		var baseConstructor = TypeCache._TreeViewDragging.GetConstructor( 
			BindingFlags.Public | 
			BindingFlags.Instance,
			null, 
			new Type[] { TypeCache._TreeViewController }, 
			null
		);

		var constructorBuilder = builder.DefineConstructor( 
			MethodAttributes.Public, 
			CallingConventions.Standard, 
			new Type[] { TypeCache._TreeViewController }
		);

		var g = constructorBuilder.GetILGenerator();

		g.Emit( OpCodes.Ldarg_0 ); // push `this`
		g.Emit( OpCodes.Ldarg_1 ); // push arg_1
		g.Emit( OpCodes.Call, baseConstructor ); // this.base( arg_1 );

		g.Emit( OpCodes.Ret );
	}

	static void EmitDebugLog ( ILGenerator g, string message ) {
		var Log = typeof( Debug ).GetMethod( "Log", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof( object ) }, null );
		g.Emit( OpCodes.Ldstr, message );
		g.Emit( OpCodes.Call, Log );
	}

	static void EmitDebugLog ( ILGenerator g, OpCode opCode, Type valueType = null ) {
		var Log = typeof( Debug ).GetMethod( "Log", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof( object ) }, null );
		g.Emit( opCode );
		if( valueType != null ) g.Emit( OpCodes.Box, valueType );
		g.Emit( OpCodes.Call, Log );
	}

    /* This method generates the equivalent of this code:
	 *
	 *     public override DragAndDropVisualMode DoDrag( TreeViewItem parentItem, TreeViewItem targetItem, bool perform, DropPosition dropPosition ) {
	 *         var dragResult = MultiSceneDragHandler.DoDrag( parentItem, targetItem, perform, dropPosition );
	 *         if( dragResult != DragAndDropVisualMode.None ) {
	 *             return dragResult;
	 *         }
	 *
	 *         return base.DoDrag( parentItem, targetItem, perform, dropPosition );
	 *     }
	 */
	static void BuildDoDragMethod ( TypeBuilder builder, FieldInfo original ) {
		const string methodName = "DoDrag";

		var method = TypeCache._GameObjectsTreeViewDragging.GetMethod( methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance );
		var parameters = method.GetParameters();
		var parameterTypes = new Type[ parameters.Length ];

		for( int i = 0; i < parameters.Length; i++ ) {
			parameterTypes[ i ] = parameters[ i ].ParameterType;
		}

		var mb = builder.DefineMethod( methodName, 
			method.Attributes, 
			method.ReturnType, parameterTypes );

		var g = mb.GetILGenerator();

		g.DeclareLocal( typeof( DragAndDropVisualMode ) ); // DragAndDropVisualMode dragResult;

		var noDragResult = g.DefineLabel();

		var doDrag = TypeCache._MultiSceneDragHandler.GetMethod( "DoDrag", BindingFlags.Public | BindingFlags.Static );

		EmitDebugLog( g, "HUE" );

		g.Emit( OpCodes.Ldarg_1 ); // push parentItem
		g.Emit( OpCodes.Ldarg_2 ); // push targetItem
		g.Emit( OpCodes.Ldarg_3 ); // push perform
		g.Emit( OpCodes.Ldarg_S, 4 ); // push dropPosition
		g.Emit( OpCodes.Call, doDrag ); // call MultiSceneDragHandler.DoDrag( parentItem, targetItem, perform, dropPosition );, push return value onto stack
		g.Emit( OpCodes.Stloc_0 ); // set `dragResult` to return value, pop the return value off the stack

		g.Emit( OpCodes.Ldfld, original );
		g.Emit( OpCodes.Ldc_I4_1 );
		g.Emit( OpCodes.Bne_Un, noDragResult );

		g.Emit( OpCodes.Ldloc_0 ); // push `dragResult`
		g.Emit( OpCodes.Ldc_I4_0 ); // push DragAndDropVisualMode.None (0)

		g.Emit( OpCodes.Beq, noDragResult ); // jump to noDragResult if `dragResult` == DragAndDropVisualMode.None

		g.Emit( OpCodes.Ldloc_0 );  // push `dragResult`
		g.Emit( OpCodes.Ret );      // return `dragResult`
		
		g.MarkLabel( noDragResult );

		g.Emit( OpCodes.Ldc_I4_1 );
		g.Emit( OpCodes.Stfld, original );
		
		g.Emit( OpCodes.Ldarg_0 );

		g.Emit( OpCodes.Ldarg_1 );
		g.Emit( OpCodes.Ldarg_2 );
		g.Emit( OpCodes.Ldarg_3 );
		g.Emit( OpCodes.Ldarg_S, 4 );

		g.Emit( OpCodes.Callvirt, method ); // base.DoDrag( etc. );
		g.Emit( OpCodes.Ret );
	}
}
#else
public class TreeViewDraggingProxyBuilder {
	public static Type BuildType () {
		return null;
	}
}
#endif

// public class TreeViewDraggingProxyBuilder {
// 	// internal interface ITreeViewDragging {
//     //     void OnInitialize();
//     //     bool CanStartDrag(TreeViewItem targetItem, List<int> draggedItemIDs, Vector2 mouseDownPosition);
//     //     void StartDrag(TreeViewItem draggedItem, List<int> draggedItemIDs);
//     //     bool DragElement(TreeViewItem targetItem, Rect targetItemRect, int row);             // 'targetItem' is null when not hovering over any target Item.  Returns true if drag was handled.
//     //     void DragCleanup(bool revertExpanded);
//     //     int GetDropTargetControlID();
//     //     int GetRowMarkerControlID();
//     //     bool drawRowMarkerAbove { get; set; }
//     // }


// //  internal abstract class TreeViewDragging : ITreeViewDragging
// //     {
// //         public TreeViewDragging(TreeViewController treeView);
// //         virtual public void OnInitialize();
// //         public virtual bool CanStartDrag(TreeViewItem targetItem, List<int> draggedItemIDs, Vector2 mouseDownPosition);
// //         public abstract void StartDrag(TreeViewItem draggedItem, List<int> draggedItemIDs);
// //         public abstract DragAndDropVisualMode DoDrag(TreeViewItem parentItem, TreeViewItem targetItem, bool perform, DropPosition dropPosition);
// //         public virtual bool DragElement(TreeViewItem targetItem, Rect targetItemRect, bool firstItem);
// //         protected virtual void HandleAutoExpansion(int itemControlID, TreeViewItem targetItem, Rect targetItemRect, float betweenHalfHeight, Vector2 currentMousePos);
// //         public virtual void DragCleanup(bool revertExpanded);
// //     }

// 	public static Type TreeViewDraggingProxy;

// 	public static Type TreeViewDragging;
// 	public static Type ITreeViewDragging;
// 	public static FieldInfo m_TreeView;
// 	public static Type TreeViewController;
// 	public static Type SceneHierarchyWindow;

// 	static TypeBuilder builder;

// 	private static TypeBuilder GetTypeBuilder () {
// 		var asmName = new AssemblyName( string.Format( "{0}_{1}", "MultiSceneRuntime", Guid.NewGuid().ToString( "N" ) ) );
// 		AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly( asmName, AssemblyBuilderAccess.Run );

// 		ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule( "core" );

// 		var typeName = "TreeViewDraggingProxy";
// 		TypeBuilder tb = moduleBuilder.DefineType( typeName,
// 				TypeAttributes.Public |
// 				TypeAttributes.Class |
// 				TypeAttributes.AutoClass |
// 				TypeAttributes.AnsiClass |
// 				TypeAttributes.AutoLayout );

// 		return tb;
// 	}


// 	public static Type BuildType () {
// 		if( TreeViewDraggingProxy != null ) return TreeViewDraggingProxy;

// 		var editorAssembly = Assembly.GetAssembly( typeof( SceneView ) );
// 		ITreeViewDragging = editorAssembly.GetType( "UnityEditor.IMGUI.Controls.ITreeViewDragging" );
// 		TreeViewDragging = editorAssembly.GetType( "UnityEditor.IMGUI.Controls.TreeViewDragging" );
// 		m_TreeView = TreeViewDragging.GetField( "m_TreeView", BindingFlags.NonPublic | BindingFlags.Instance );

// 		TreeViewController = editorAssembly.GetType( "UnityEditor.IMGUI.Controls.TreeViewController" );
// 		SceneHierarchyWindow = editorAssembly.GetType( "UnityEditor.SceneHierarchyWindow" );

// 		var m_TreeViewField = typeof( MultiSceneDragHandler ).GetField( "m_TreeView", BindingFlags.Static | BindingFlags.Public );

// 		builder = GetTypeBuilder();
// 		builder.SetParent( TreeViewDragging );

// 		var target = builder.DefineField( "target", TreeViewDragging, FieldAttributes.Private );

// 		var baseConstructor = TreeViewDragging.GetConstructor( BindingFlags.Public | BindingFlags.Instance, null, new Type[] { TreeViewController }, null );
// 		var cb = builder.DefineConstructor( MethodAttributes.Public, CallingConventions.Standard, new Type[] { TreeViewDragging, TreeViewController } );
// 		var g = cb.GetILGenerator();
		
// 		g.Emit( OpCodes.Ldarg_0 ); // push `this`
// 		g.Emit( OpCodes.Ldarg_2 ); // push `controller`
// 		g.Emit( OpCodes.Call, baseConstructor ); // this.base( controller );

// 		g.Emit( OpCodes.Ldarg_0 ); // push `this`
// 		g.Emit( OpCodes.Ldarg_1 ); // push arg_1
// 		g.Emit( OpCodes.Stfld, target ); // this.target = arg_1;

// 		g.Emit( OpCodes.Nop );
// 		g.Emit( OpCodes.Nop );

// 		g.Emit( OpCodes.Ret );

// 		CreateWrapperMethod( "OnInitialize", builder, target );
// 		CreateWrapperMethod( "CanStartDrag", builder, target );
// 		CreateWrapperMethod( "StartDrag", builder, target );
// 		CreateWrapperMethod( "DragElement", builder, target );
// 		CreateWrapperMethod( "HandleAutoExpansion", builder, target );
// 		CreateWrapperMethod( "DragCleanup", builder, target );

// 		CreateDoDrag( builder, target );
		
// 		TreeViewDraggingProxy = builder.CreateType();
// 		return TreeViewDraggingProxy;
// 	}

// 	static void CreateDoDrag ( TypeBuilder builder, FieldInfo wrapperField ) {
// 		var methodInfo = TreeViewDragging.GetMethod( "DoDrag", BindingFlags.Instance | BindingFlags.Public );
// 		var parameters = methodInfo.GetParameters();

// 		var parameterTypes = new Type[ parameters.Length ];

// 		for( int i = 0; i < parameters.Length; i++ ) {
// 			parameterTypes[ i ] = parameters[ i ].ParameterType;
// 		}

// 		var mb = builder.DefineMethod( "DoDrag", 
// 			MethodAttributes.Public | 
// 			MethodAttributes.ReuseSlot | 
// 			MethodAttributes.Virtual |
// 			MethodAttributes.HideBySig, 
// 			CallingConventions.HasThis, methodInfo.ReturnType, parameterTypes );

// 		var g = mb.GetILGenerator();

// 		var lb = g.DeclareLocal( typeof( DragAndDropVisualMode ) ); // DragAndDropVisualMode dragResult;

// 		var noDragResult = g.DefineLabel();

// 		var doDrag = typeof( MultiSceneDragHandler ).GetMethod( "DoDrag", BindingFlags.Public | BindingFlags.Static );
// 		g.Emit( OpCodes.Ldarg_2 ); // push targetItem
// 		g.Emit( OpCodes.Ldarg_3 ); // push perform
// 		g.Emit( OpCodes.Call, doDrag ); // call MultiSceneDragHandler.DoDrag( targetItem, perform );, push return value onto stack
// 		g.Emit( OpCodes.Stloc_0 ); // push `dragResult` onto stack, pop the return value off the stack

// 		g.Emit( OpCodes.Ldloc_0 ); // push `dragResult`
// 		g.Emit( OpCodes.Ldc_I4, (int)DragAndDropVisualMode.None ); // push DragAndDropVisualMode.None
// 		g.Emit( OpCodes.Beq, noDragResult ); // jump to noDragResult if `dragResult` == DragAndDropVisualMode.None

// 		g.Emit( OpCodes.Ldloc_0 );  // push `dragResult`
// 		g.Emit( OpCodes.Ret );      // return `dragResult`
		
// 		g.MarkLabel( noDragResult );

// 		g.Emit( OpCodes.Ldarg_0 ); // push `this`
// 		g.Emit( OpCodes.Ldfld, wrapperField ); // pop `this`, push `this.target`

// 		g.Emit( OpCodes.Ldarg_1 ); // push `arg_1`
// 		g.Emit( OpCodes.Ldarg_2 ); // push `arg_2`
// 		g.Emit( OpCodes.Ldarg_3 ); // push `arg_3`
// 		g.Emit( OpCodes.Ldarg_S, 4 ); // push `arg_4`

// 		g.Emit( OpCodes.Callvirt, methodInfo ); // call DoDrag( arg_1, arg_2, arg_3, arg_4 );
// 		g.Emit( OpCodes.Ret ); // return DoDrag result
// 	}

// 	static void CreateWrapperMethod ( string methodName, TypeBuilder builder, FieldInfo wrapperField ) {
// 		var methodInfo = TreeViewDragging.GetMethod( methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance );

// 		var parameters = methodInfo.GetParameters();

// 		var parameterTypes = new Type[ parameters.Length ];

// 		for( int i = 0; i < parameters.Length; i++ ) {
// 			parameterTypes[ i ] = parameters[ i ].ParameterType;
// 		}

// 		var mb = builder.DefineMethod( methodName, 
// 			MethodAttributes.Public | 
// 			MethodAttributes.ReuseSlot | 
// 			MethodAttributes.Virtual |
// 			MethodAttributes.HideBySig, 
// 			CallingConventions.HasThis, methodInfo.ReturnType, parameterTypes );

// 		var g = mb.GetILGenerator();

// 		g.Emit( OpCodes.Ldarg_0 );
// 		g.Emit( OpCodes.Ldfld, wrapperField );

// 		switch( parameters.Length ) {
// 			case 0:
// 				break;
// 			case 1:
// 				g.Emit( OpCodes.Ldarg_1 );
// 				break;
// 			case 2:
// 				g.Emit( OpCodes.Ldarg_1 );
// 				g.Emit( OpCodes.Ldarg_2 );
// 				break;
// 			case 3:
// 				g.Emit( OpCodes.Ldarg_1 );
// 				g.Emit( OpCodes.Ldarg_2 );
// 				g.Emit( OpCodes.Ldarg_3 );
// 				break;
// 			default:
// 				g.Emit( OpCodes.Ldarg_1 );
// 				g.Emit( OpCodes.Ldarg_2 );
// 				g.Emit( OpCodes.Ldarg_3 );
// 				for( byte i = 3; i < parameters.Length; i++ ) {
// 					g.Emit( OpCodes.Ldarg_S, i + 1 );
// 				}
// 				break;
// 		}

// 		g.Emit( OpCodes.Callvirt, methodInfo ); // target.[methodName]( [args] );
// 		g.Emit( OpCodes.Ret );
// 	}
// }	