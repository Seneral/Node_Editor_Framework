using UnityEngine;
using UnityEngine.Events;
using System;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using Object = UnityEngine.Object;

namespace NodeEditorFramework.Utilities
{
	/// <summary>
	/// Base for all UnityFuncs. Includes all serialization stuff of the delegates. Can also be used as an anonymous serialized delegate
	/// </summary>
	[Serializable]
	public class UnityFuncBase
	{
		#region Serialized Data

		// Command definition data get seperate accessors so they can't be changed from outside and auto-properties cannot be serialized

		/// <summary>
		/// Gets the deserialized targetType of the method or field
		/// </summary>
		public Type TargetType { get { return _targetType != null && _targetType.Validate ()? _targetType.GetRuntimeType () : null; } }
		[SerializeField]
		private SerializedType _targetType = null;

		/// <summary>
		/// Gets the targetObject of the method or field. If null the function cannot be invoked.
		/// </summary>
		public Object TargetObject { get { return _targetObject; } }
		[SerializeField]
		private Object _targetObject = null;

		/// <summary>
		/// Gets the name of the method or field
		/// </summary>
		public string CommandName { get { return _commandName; } }
		[SerializeField]
		private string _commandName;

		/// <summary>
		/// When this UnityFunc representates is a field, this defines whether it gets or sets the value of it, else it throws
		/// </summary>
		public bool FieldGetAccessor { get { 
				if (_isMethod) throw new UnityException ("Cannot request field accessor type of a method!"); 
				return _fieldGetAccessor; } }
		[SerializeField]
		private bool _fieldGetAccessor;

		/// <summary>
		/// Gets whether this UnityFunc representates a method or a field
		/// </summary>
		public bool isMethod { get { return _isMethod; } }
		[SerializeField]
		private bool _isMethod;

		/// <summary>
		/// Gets whether this method or field is static
		/// </summary>
		public bool isStatic { get { return _isStatic; } }
		[SerializeField]
		private bool _isStatic;

		/// <summary>
		/// Gets the deserialized returnType of the method or field(get)
		/// </summary>
		public Type ReturnType { get { return _returnType != null && _returnType.Validate ()? _returnType.GetRuntimeType () : null; } }
		[SerializeField]
		private SerializedType _returnType;

		/// <summary>
		/// Gets the deserialized argumentTypes of the method or field(set)
		/// </summary>
		public Type[] ArgumentTypes { get { return _argumentTypes.Select ((SerializedType argType) => argType.GetRuntimeType ()).ToArray (); } }
		[SerializeField]
		private SerializedType[] _argumentTypes;

		#endregion

		#region Deserialized Data

		/// <summary>
		/// Gets the deserialized delegate represented by this UnityFunc, be it method or field
		/// </summary>
		[NonSerialized]
		protected Delegate runtimeDelegate;

		/// <summary>
		/// Gets the deserialized method represented by this UnityFunc. 
		/// Throws when this UnityFunc representates a field
		/// </summary>
		public MethodInfo RuntimeMethod { get { 
				if (!_isMethod) throw new UnityException ("This UnityFunc is defined as a field!");
				if (_method == null) DeserializeCommand (true); 
				return _method; } }
		[NonSerialized]
		private MethodInfo _method;

		/// <summary>
		/// Gets the deserialized field represented by this UnityFunc. 
		/// Throws when this UnityFunc representates a method
		/// </summary>
		public FieldInfo RuntimeField { get { 
				if (_isMethod) throw new UnityException ("This UnityFunc is defined as a method!");
				if (_field == null) DeserializeCommand (true); 
				return _field; } }
		[NonSerialized]
		private FieldInfo _field;

		#endregion

		/// <summary>
		/// Gets whether the method or field represented by this UnityFunc has been deserialized
		/// </summary>
		public bool isDeserialized { get { return _isMethod? _method != null : _field != null; } }

		/// <summary>
		/// Gets whether the definition of this method or field is correct
		/// </summary>
		private bool isCommandDefinitionValid { get { return !String.IsNullOrEmpty (_commandName) && _returnType != null && _returnType.Validate () && _argumentTypes != null; } }

		/// <summary>
		/// Dynamically invokes the method or field represented by this UnityFunc
		/// </summary>
		public object DynamicInvoke (params object[] parameter) 
		{
			if (runtimeDelegate == null)
				runtimeDelegate = CreateDelegate ();
			if (runtimeDelegate != null)
				return runtimeDelegate.DynamicInvoke (parameter);
			return null;
		}

		#region Creation and retargeting

		/// <summary>
		/// Creates a serializeable UnityFunc from the passed delegate
		/// </summary>
		public UnityFuncBase (Delegate func) 
		{
			if (func.Method == null)
				throw new ArgumentException ("Func " + func + " is anonymous!");
			if (func.Target != null && !(func.Target is Object))
				throw new ArgumentException ("Target of func " + func + " is not serializeable, it has to inherit from UnityEngine.Object!");
			
			Type[] argumentTypes = func.Method.GetParameters ().Select ((ParameterInfo param) => param.ParameterType).ToArray ();
			InternalSetup (func.Method.DeclaringType, (Object)func.Target, func.Method.Name, func.Method.ReturnType, argumentTypes, true, func.Method.IsStatic);
		}

		/// <summary>
		/// Creates a serializeable UnityFunc from the passed method on the targetObject
		/// </summary>
		public UnityFuncBase (Object targetObject, MethodInfo method) 
		{
			Type[] argumentTypes = method.GetParameters ().Select ((ParameterInfo param) => param.ParameterType).ToArray ();
			InternalSetup (method.DeclaringType, targetObject, method.Name, method.ReturnType, argumentTypes, true, method.IsStatic);
		}

		/// <summary>
		/// Creates a serializeable UnityFunc from the passed method. If method is not static, the targetObject has to be set before invoking
		/// </summary>
		public UnityFuncBase (MethodInfo method) : this (null, method) {}

		/// <summary>
		/// Creates a serializeable UnityFunc from the passed field on the targetObject
		/// </summary>
		public UnityFuncBase (Object targetObject, FieldInfo field, bool getAccessor) 
		{
			_fieldGetAccessor = getAccessor;
			if (getAccessor) // Get
				InternalSetup (field.DeclaringType, targetObject, field.Name, field.FieldType, new Type[0], false, field.IsStatic);
			else // Set
				InternalSetup (field.DeclaringType, targetObject, field.Name, null, new Type[] { field.FieldType }, false, field.IsStatic);
		}

		/// <summary>
		/// Creates a serializeable UnityFunc from the passed field. If field is not static, the targetObject has to be set before invoking
		/// </summary>
		public UnityFuncBase (FieldInfo field, bool getAccessor) : this (null, field, getAccessor) {}

		/// <summary>
		/// Internal setup of the UnityFunc
		/// </summary>
		private void InternalSetup (Type targetType, Object targetObject, string commandName, Type returnType, Type[] argumentTypes, bool isMethod, bool isStatic) 
		{
			// Command definition
			_targetType = new SerializedType (targetType);
			if (targetObject != null && targetType.IsAssignableFrom (targetObject.GetType ()))
				_targetObject = targetObject;
			_commandName = commandName;
			_isMethod = isMethod;
			_isStatic = isStatic;

			// Types
			if (returnType == null)
				returnType = typeof(void);
			_returnType = new SerializedType (returnType);
			_argumentTypes = new SerializedType[argumentTypes.Length];
			for (int argCnt = 0; argCnt < argumentTypes.Length; argCnt++)
				_argumentTypes[argCnt] = new SerializedType (argumentTypes[argCnt]);
		}

		/// <summary>
		/// Reassigns the targetObject of this UnityFunc. Has to be assignable from the preselected targetType and has to be null when the command is static
		/// </summary>
		public void ReassignTargetObject (Object newTargetObject) 
		{
			if (newTargetObject != null) 
			{
				if (isStatic)
					throw new UnityException ("Cannot assign targetObject to a static command!");
				else if (!TargetType.IsAssignableFrom (newTargetObject.GetType ()))
					throw new UnityException ("Cannot assign targetObject of type " + newTargetObject.GetType ().FullName + " to UnityFunc of type " + TargetType.FullName + "!");
			}
			if (newTargetObject == null && !isStatic)
				Debug.LogWarning ("Assigning null targetObject to an instance command! UnityFunc won't be invokeable!");

			_targetObject = newTargetObject;
			// Reset deserialized delegate so it gets created new with the new targetObject when needed
			runtimeDelegate = null;
		}

		#endregion

		#region Deserialization

		/// <summary>
		/// Returns the runtime delegate this UnityFunc representates, a System.Func with of apropriate type depending on return and argument types boxed as a delegate.
		/// Handled and stored on initialisation by children func variations.
		/// </summary>
		protected Delegate CreateDelegate ()
		{
			// Make sure method is deserialized
			if (!isDeserialized)
				DeserializeCommand (true);
			
			if (!isStatic && _targetObject == null)
				throw new UnityException ("Cannot create delegate without a targetObject! Make sure to reassign a targetObject to UnityFunc " + _commandName + "!");
			if (isMethod) 
			{ // Create a delegate from the method
				if (isStatic)
				{
					Debug.Log (_method.Name + " method is fetched for the action node! ReturnType is " + _method.ReturnType.FullName + " (should be " + ReturnType.FullName + ")!");
					runtimeDelegate = Delegate.CreateDelegate (typeof (Func<>), _method, true);
				}
				else if (_targetObject != null)
					runtimeDelegate = Delegate.CreateDelegate (typeof (Func<>), _targetObject, _method, true);
			}
			else
			{ // compile a delegate from the field
				ParameterExpression paramExp = Expression.Parameter (TargetType, "");
				MemberExpression fieldExp = Expression.Field (paramExp, _field);
				runtimeDelegate = Expression.Lambda (typeof(Func<>), fieldExp, paramExp).Compile ();
			}
			return runtimeDelegate;
		}

		/// <summary>
		/// Fetches the methodInfo this UnityFunc representates. Call only once on initialisation!
		/// </summary>
		private void DeserializeCommand (bool throwOnBindFailure)
		{
			if (!isCommandDefinitionValid)
			{
				if (throwOnBindFailure)
					throw new UnityException ("Invalid " + (_isMethod? "method" : "field") + " definition in UnityFunc " + _commandName  + "!");
				return;
			}
			// Get the method Info that this UnityFunc representates
			if (_isMethod)
			{
				_method = GetValidMethodInfo (TargetType, _commandName, ReturnType, ArgumentTypes, isStatic);
				if (_method == null && throwOnBindFailure)
					throw new UnityException ("Invalid method definition in UnityFunc " + _commandName  + "!");
			}
			else
			{
				_field = GetValidFieldInfo (TargetType, _commandName, isStatic);
				if (_field == null && throwOnBindFailure)
					throw new UnityException ("Invalid field definition in UnityFunc " + _commandName  + "!");
			}
		}

		/// <summary>
		/// Gets the valid MethodInfo of the method on targetObj called functionName with the specified returnType (may be null in case of void) and argumentTypes
		/// </summary>
		public static FieldInfo GetValidFieldInfo (Type targetType, string fieldName, bool isStatic)
		{
			return targetType.GetField (fieldName, 
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | (isStatic? BindingFlags.Static : BindingFlags.Instance)); // No valid method found on targetObj that has this functionName
		}

		/// <summary>
		/// Gets the valid MethodInfo of the method on targetObj called functionName with the specified returnType (may be null in case of void) and argumentTypes
		/// </summary>
		public static MethodInfo GetValidMethodInfo (Type targetType, string methodName, Type returnType, Type[] argumentTypes, bool isStatic)
		{
			BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | (isStatic? BindingFlags.Static : BindingFlags.Instance);
			return targetType.GetMethod (methodName, flags, null, argumentTypes, null);

			// Unity default implementation
//			if (returnType == null) // Account for void return type, too
//				returnType = typeof(void);
//			BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | (isStatic? BindingFlags.Static : BindingFlags.Instance);
//			while (targetType != null && targetType != typeof(object) && targetType != typeof(void)) 
//			{ // Search targetObj's type hierarchy for the functionName until we hit the object base type or found it (incase the function is inherited)
//				MethodInfo method = targetType.GetMethod (methodName, flags, null, argumentTypes, null);
//				if (method != null && method.ReturnType == returnType) 
//				{ // This type contains a method with the specified name, arguments and return type
//					ParameterInfo[] parameters = method.GetParameters ();
//					bool flag = true;
//					for (int paramCnt = 0; paramCnt < parameters.Length; paramCnt++) 
//					{ // Check whether the arguments match in that they are primitives (isn't this already sorted out in getMethod?)
//						if (!(flag = (argumentTypes [paramCnt].IsPrimitive == parameters [paramCnt].ParameterType.IsPrimitive)))
//							break; // Else, this is not the right method
//					}
//					if (flag) // We found the method!
//						return method;
//				}
//				// Move up in the type hierarchy (function was inherited)
//				targetType = targetType.BaseType;
//			}
//			return null; // No valid method found on targetObj that has this functionName
		}

		#endregion
	}

	#region Serialized Type

	[Serializable]
	public class SerializedType // : ISerializationCallbackReceiver
	{
		[SerializeField]
		private string argAssemblyTypeName;

		[NonSerialized]
		private Type runtimeType;

		public SerializedType (Type type)
		{
			SetType (type);
		}

		public void SetType (Type type)
		{
			runtimeType = type;
			argAssemblyTypeName = type.FullName;
			if (String.IsNullOrEmpty (argAssemblyTypeName))
				throw new UnityException ("Could not setup type as it does not contain serializeable data!");
//			argAssemblyTypeName = TidyAssemblyTypeName (argAssemblyTypeName);
		}

//		public void OnAfterDeserialize ()
//		{
//			argAssemblyTypeName = TidyAssemblyTypeName (argAssemblyTypeName);
//		}
//
//		public void OnBeforeSerialize ()
//		{
//			argAssemblyTypeName = TidyAssemblyTypeName (argAssemblyTypeName);
//		}

//		internal static string TidyAssemblyTypeName (string typeName)
//		{
//			if (String.IsNullOrEmpty (typeName))
//				return null;
//			return typeName.Split (',')[0];
//		}

		public bool Validate () 
		{
			return !String.IsNullOrEmpty (argAssemblyTypeName);
		}

		public Type GetRuntimeType () 
		{
			if (string.IsNullOrEmpty (argAssemblyTypeName))
				throw new UnityException ("Could not deserialize type as it does not contain serialized data!");
			return runtimeType ?? (runtimeType = Type.GetType (argAssemblyTypeName, false) ?? typeof(Object));
		}
	}

	#endregion

	#region Parameter Variations

	[Serializable]
	public class UnityFunc<T1, T2, T3, T4, TR> : UnityFuncBase
	{
		[NonSerialized]
		private Func<T1, T2, T3, T4, TR> runtimeFunc;

		// Retarget constructors to base
		public UnityFunc (Delegate func) : base (func) {}
		public UnityFunc (Object targetObject, MethodInfo method) : base (targetObject, method) {}
		public UnityFunc (MethodInfo method) : base (method) {}
		public UnityFunc (Object targetObject, FieldInfo field, bool isGetAccessor) : base (targetObject, field, isGetAccessor) {}
		public UnityFunc (FieldInfo field, bool isGetAccessor) : base (field, isGetAccessor) {}

		public TR Invoke (T1 arg1, T2 arg2, T3 arg3, T4 arg4) 
		{
			if (runtimeFunc == null)
			{
				if (runtimeDelegate == null)
					runtimeDelegate = CreateDelegate ();
				runtimeFunc = runtimeDelegate as Func<T1, T2, T3, T4, TR>;
			}
			if (runtimeFunc != null)
				return runtimeFunc.Invoke (arg1, arg2, arg3, arg4);
			return default(TR);
		}
	}

	/// <summary>
	/// UnityFunc with three paramteters. Extend this and use that class to make it serializeable
	/// </summary>
	[Serializable]
	public class UnityFunc<T1, T2, T3, TR> : UnityFuncBase
	{
		[NonSerialized]
		private Func<T1, T2, T3, TR> runtimeFunc;

		// Retarget constructors to base
		public UnityFunc (Delegate func) : base (func) {}
		public UnityFunc (Object targetObject, MethodInfo method) : base (targetObject, method) {}
		public UnityFunc (MethodInfo method) : base (method) {}
		public UnityFunc (Object targetObject, FieldInfo field, bool isGetAccessor) : base (targetObject, field, isGetAccessor) {}
		public UnityFunc (FieldInfo field, bool isGetAccessor) : base (field, isGetAccessor) {}

		public TR Invoke (T1 arg1, T2 arg2, T3 arg3) 
		{
			if (runtimeFunc == null)
			{
				if (runtimeDelegate == null)
					runtimeDelegate = CreateDelegate ();
				runtimeFunc = runtimeDelegate as Func<T1, T2, T3, TR>;
			}
			if (runtimeFunc != null)
				return runtimeFunc.Invoke (arg1, arg2, arg3);
			return default(TR);
		}
	}

	/// <summary>
	/// UnityFunc with two paramteters. Extend this and use that class to make it serializeable
	/// </summary>
	[Serializable]
	public class UnityFunc<T1, T2, TR> : UnityFuncBase
	{
		[NonSerialized]
		private Func<T1, T2, TR> runtimeFunc;

		// Retarget constructors to base
		public UnityFunc (Delegate func) : base (func) {}
		public UnityFunc (Object targetObject, MethodInfo method) : base (targetObject, method) {}
		public UnityFunc (MethodInfo method) : base (method) {}
		public UnityFunc (Object targetObject, FieldInfo field, bool isGetAccessor) : base (targetObject, field, isGetAccessor) {}
		public UnityFunc (FieldInfo field, bool isGetAccessor) : base (field, isGetAccessor) {}

		public TR Invoke (T1 arg1, T2 arg2) 
		{
			if (runtimeFunc == null)
			{
				if (runtimeDelegate == null)
					runtimeDelegate = CreateDelegate ();
				runtimeFunc = runtimeDelegate as Func<T1, T2, TR>;
			}
			if (runtimeFunc != null)
				return runtimeFunc.Invoke (arg1, arg2);
			return default(TR);
		}
	}

	/// <summary>
	/// UnityFunc with one paramteter. Extend this and use that class to make it serializeable
	/// </summary>
	[Serializable]
	public class UnityFunc<T1, TR> : UnityFuncBase
	{
		[NonSerialized]
		private Func<T1, TR> runtimeFunc;

		// Retarget constructors to base
		public UnityFunc (Delegate func) : base (func) {}
		public UnityFunc (Object targetObject, MethodInfo method) : base (targetObject, method) {}
		public UnityFunc (MethodInfo method) : base (method) {}
		public UnityFunc (Object targetObject, FieldInfo field, bool isGetAccessor) : base (targetObject, field, isGetAccessor) {}
		public UnityFunc (FieldInfo field, bool isGetAccessor) : base (field, isGetAccessor) {}

		public TR Invoke (T1 arg) 
		{
			if (runtimeFunc == null)
			{
				if (runtimeDelegate == null)
					runtimeDelegate = CreateDelegate ();
				runtimeFunc = runtimeDelegate as Func<T1, TR>;
			}
			if (runtimeFunc != null)
				return runtimeFunc.Invoke (arg);
			return default(TR);
		}
	}

	/// <summary>
	/// UnityFunc with no paramteters. Extend this and use that class to make it serializeable
	/// </summary>
	[Serializable]
	public class UnityFunc<TR> : UnityFuncBase
	{
		[NonSerialized]
		private Func<TR> runtimeFunc;

		// Retarget constructors to base
		public UnityFunc (Delegate func) : base (func) {}
		public UnityFunc (Object targetObject, MethodInfo method) : base (targetObject, method) {}
		public UnityFunc (MethodInfo method) : base (method) {}
		public UnityFunc (Object targetObject, FieldInfo field, bool isGetAccessor) : base (targetObject, field, isGetAccessor) {}
		public UnityFunc (FieldInfo field, bool isGetAccessor) : base (field, isGetAccessor) {}

		public TR Invoke () 
		{
			if (runtimeFunc == null)
			{
				if (runtimeDelegate == null)
					runtimeDelegate = CreateDelegate ();
				runtimeFunc = runtimeDelegate as Func<TR>;
			}
			if (runtimeFunc != null)
				return runtimeFunc.Invoke ();
			return default(TR);
		}
	}

	#endregion
}