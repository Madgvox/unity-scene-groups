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

		BuildConstructor( builder );

		BuildDoDragMethod( builder );

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

	/*
	 *     public TreeViewDraggingProxy ( TreeViewController controller ) : base( controller ) {}
	 */
	static void BuildConstructor ( TypeBuilder builder ) {
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

	/*     public override DragAndDropVisualMode DoDrag( TreeViewItem parentItem, TreeViewItem targetItem, bool perform, DropPosition dropPosition ) {
	 *         var dragResult = MultiSceneDragHandler.DoDrag( parentItem, targetItem, perform, dropPosition );
	 *         if( dragResult != DragAndDropVisualMode.None ) {
	 *             return dragResult;
	 *         }
	 *
	 *         return base.DoDrag( parentItem, targetItem, perform, dropPosition );
	 *     }
	 */
	static void BuildDoDragMethod ( TypeBuilder builder ) {
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

		g.DeclareLocal( typeof( DragAndDropVisualMode ) );

		var noDragResult = g.DefineLabel();

		var doDrag = TypeCache._MultiSceneDragHandler.GetMethod( "DoDrag", BindingFlags.Public | BindingFlags.Static );

		g.Emit( OpCodes.Ldarg_1 ); 
		g.Emit( OpCodes.Ldarg_2 ); 
		g.Emit( OpCodes.Ldarg_3 ); 
		g.Emit( OpCodes.Ldarg_S, 4 );
		g.Emit( OpCodes.Call, doDrag ); 
		g.Emit( OpCodes.Stloc_0 );

		g.Emit( OpCodes.Ldloc_0 );
		g.Emit( OpCodes.Ldc_I4_0 );

		g.Emit( OpCodes.Beq, noDragResult );

		g.Emit( OpCodes.Ldloc_0 );
		g.Emit( OpCodes.Ret );
		
		g.MarkLabel( noDragResult );

		g.Emit( OpCodes.Ldarg_0 );

		g.Emit( OpCodes.Ldarg_1 );
		g.Emit( OpCodes.Ldarg_2 );
		g.Emit( OpCodes.Ldarg_3 );
		g.Emit( OpCodes.Ldarg_S, 4 );

		g.Emit( OpCodes.Call, method );
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
