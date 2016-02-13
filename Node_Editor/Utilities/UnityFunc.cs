using UnityEngine;
using UnityEngine.Events;
using System;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Collections.Generic;

using Object = UnityEngine.Object;

/// <summary>
/// Base for all UnityFuncs. Includes all serialization stuff of the delegates.
/// </summary>
[Serializable]
public abstract class UnityFuncBase
{
	// Following are the serialized method details
	[SerializeField]
	private Object _targetObject;
	[SerializeField]
	private string _methodName;
	[SerializeField]
	private FuncSerializedType _returnType;
	[SerializeField]
	private FuncSerializedTypeValue[] _arguments;

	public UnityEventCallState callState;

	#region Base Code

	/// <summary>
	/// Creates a serializeable UnityFunc from the passed delegate
	/// </summary>
	public UnityFuncBase (Delegate func) 
	{
		if (func.Target == null || func.Method == null)
			throw new ArgumentException ("Func " + func + " is anonymous!");
		
		_targetObject = (Object)func.Target;
		_methodName = func.Method.Name;
		_returnType = new FuncSerializedType (func.Method.ReturnType);

		ParameterInfo[] parameters = func.Method.GetParameters ();
		_arguments = new FuncSerializedTypeValue[parameters.Length];
		for (int paramCnt = 0; paramCnt < parameters.Length; paramCnt++)
			_arguments[paramCnt] = new FuncSerializedType (parameters[paramCnt].ParameterType) as FuncSerializedTypeValue;
	}

	/// <summary>
	/// Creates a serializeable UnityFunc from the passed method on the targetObject
	/// </summary>
	public UnityFuncBase (Object targetObject, MethodInfo methodInfo) 
	{
		_targetObject = targetObject;
		_methodName = methodInfo.Name;
		_returnType = new FuncSerializedType (methodInfo.ReturnType);

		ParameterInfo[] parameters = methodInfo.GetParameters ();
		_arguments = new FuncSerializedTypeValue[parameters.Length];
		for (int paramCnt = 0; paramCnt < parameters.Length; paramCnt++)
			_arguments[paramCnt] = new FuncSerializedType (parameters[paramCnt].ParameterType) as FuncSerializedTypeValue;
	}

	/// <summary>
	/// Returns the runtime delegate this UnityFunc representates. Create only once on initialisation!
	/// Returns a System.Func with of apropriate type depending on return and argument types boxed as a delegate.
	/// Handled and stored on initialisation by children func variations.
	/// </summary>
	protected Delegate DeserializeToDelegate ()
	{
		if (callState == UnityEventCallState.Off || (callState == UnityEventCallState.RuntimeOnly && !Application.isPlaying))
			return null;

		Type[] runtimeArgumentTypes = new Type[_arguments.Length];
		for (int argCnt = 0; argCnt < _arguments.Length; argCnt++) 
			runtimeArgumentTypes[argCnt] = _arguments[argCnt].GetRuntimeType ();

		MethodInfo methodInfo = GetValidMethodInfo (_targetObject, _methodName, _returnType.GetRuntimeType (), runtimeArgumentTypes);
		if (methodInfo == null)
			return null;
		return Delegate.CreateDelegate (typeof (Func<>), methodInfo);
	}

	#endregion

	#region Static Helpers

	/// <summary>
	/// Gets the valid MethodInfo of the method on targetObj called functionName with the specified returnType (may be null incase of void) and argumentTypes
	/// </summary>
	protected static MethodInfo GetValidMethodInfo (object targetObj, string methodName, Type returnType, Type[] argumentTypes)
	{
		Type targetType = targetObj.GetType ();
		if (returnType == null) // Account for void return type, too
			returnType = typeof(void);
		while (targetType != typeof(object) && targetType != null) 
		{ // Search targetObj's type hierarchy for the functionName until we hit the object base type or found it (incase the function is inherited)
			MethodInfo method = targetType.GetMethod (methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, argumentTypes, null);
			if (method != null && method.ReturnType == returnType) 
			{ // This type contains a method with the specified name, arguments and return type
				ParameterInfo[] parameters = method.GetParameters ();
				bool flag = true;
				for (int paramCnt = 0; paramCnt < parameters.Length; paramCnt++) 
				{ // Check whether the arguments match in that they are primitives (isn't this already sorted out in getMethod?)
					if (!(flag = (argumentTypes [paramCnt].IsPrimitive == parameters [paramCnt].ParameterType.IsPrimitive)))
						break; // Else, this is not the right method
				}
				if (flag) // We found the method!
					return method;
			}
			// Move up in the type hierarchy (function was inherited)
			returnType = returnType.BaseType;
		}
		return null; // No valid method found on targetObj that has this functionName
	}

	/// <summary>
	/// Creates a new UnityFunc from the specified parameters, useful for when arguments are not known compile time.
	/// </summary>
	private static UnityFuncBase GetObjectCall (Object targetObj, MethodInfo method, Type returnType, FuncSerializedTypeValue[] arguments)
	{
		// Built Argument and GenericArgument arrays
		Type[] contrustorArgTypes = new Type[arguments.Length+3];
		contrustorArgTypes[0] = typeof(Object);
		contrustorArgTypes[1] = typeof(MethodInfo);
		contrustorArgTypes[2] = typeof(Type);
		Type[] genericTypeArgs = new Type[arguments.Length+1];
		genericTypeArgs[0] = returnType;
		for (int argCnt = 0; argCnt < arguments.Length; argCnt++) 
			contrustorArgTypes[argCnt+3] = genericTypeArgs[argCnt+1] = arguments[argCnt].GetRuntimeType ();
		// Create Contructor for the func
		Type baseGenericType = typeof(UnityFunc<>);
		Type funcType = baseGenericType.MakeGenericType (genericTypeArgs);
		ConstructorInfo funcContructor = funcType.GetConstructor (contrustorArgTypes);
		// Built Argument array
		object[] contructorArgObjects = new object[arguments.Length+3];
		contructorArgObjects[0] = targetObj;
		contructorArgObjects[1] = method;
		contructorArgObjects[2] = returnType;
		for (int argCnt = 0; argCnt < arguments.Length; argCnt++) 
			contructorArgObjects[argCnt+3] = arguments[argCnt].objectValue;
		// Create func using the generated contructor
		return funcContructor.Invoke (contructorArgObjects) as UnityFuncBase;
	}

	#endregion

	#region Nested Types

	[Serializable]
	private class FuncSerializedType : ISerializationCallbackReceiver
	{
		[SerializeField]
		private string argAssemblyTypeName;

		[NonSerialized]
		private Type runtimeType;

		public FuncSerializedType (Type type)
		{
			SetType (type);
		}

		protected void SetType (Type type)
		{
			runtimeType = type;
			argAssemblyTypeName = type.AssemblyQualifiedName;
		}

		public void OnAfterDeserialize ()
		{
			TidyAssemblyTypeName ();
		}

		public void OnBeforeSerialize ()
		{
			TidyAssemblyTypeName ();
		}

		private void TidyAssemblyTypeName ()
		{
			if (String.IsNullOrEmpty (argAssemblyTypeName))
				return;
			argAssemblyTypeName = Regex.Replace (argAssemblyTypeName, @", Version=\d+.\d+.\d+.\d+", String.Empty);
			argAssemblyTypeName = Regex.Replace (argAssemblyTypeName, @", Culture=\w+", String.Empty);
			argAssemblyTypeName = Regex.Replace (argAssemblyTypeName, @", PublicKeyToken=\w+", String.Empty);
		}

		public Type GetRuntimeType () 
		{
			return runtimeType ?? (runtimeType = Type.GetType (argAssemblyTypeName, false) ?? typeof(Object));
		}
	}

	[Serializable]
	private class FuncSerializedTypeValue : FuncSerializedType
	{
		[SerializeField]
		private Object objVal;

		public Object objectValue
		{
			get { return objVal; }
			set 
			{
				if (GetRuntimeType () != objVal.GetType ())
					throw new ArgumentException ("Object " + objVal + " not of required type " + GetRuntimeType ().Name + " but of " + objVal.GetType ().Name);
				objVal = value;
				SetType (objVal.GetType ());
			}
		}

		public FuncSerializedTypeValue (Object obj) : base (obj.GetType ())
		{
			objVal = obj;
		}
	}


	#endregion
}

#region Parameter Variation

[Serializable]
public class UnityFunc<T1, T2, T3, T4, TR> : UnityFuncBase
{
	[NonSerialized]
	private Func<T1, T2, T3, T4, TR> runtimeDelegate;

	// Retarget constructors to base
	public UnityFunc (Object targetObject, MethodInfo methodInfo) : base (targetObject, methodInfo) {}
	public UnityFunc (Delegate func) : base (func) {}

	public TR Invoke (T1 arg1, T2 arg2, T3 arg3, T4 arg4) 
	{
		if (runtimeDelegate == null)
			runtimeDelegate = DeserializeToDelegate () as Func<T1, T2, T3, T4, TR>;
		if (runtimeDelegate != null)
			return runtimeDelegate.Invoke (arg1, arg2, arg3, arg4);
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
	private Func<T1, T2, T3, TR> runtimeDelegate;

	// Retarget constructors to base
	public UnityFunc (Object targetObject, MethodInfo methodInfo) : base (targetObject, methodInfo) {}
	public UnityFunc (Delegate func) : base (func) {}

	public TR Invoke (T1 arg1, T2 arg2, T3 arg3) 
	{
		if (runtimeDelegate == null)
			runtimeDelegate = DeserializeToDelegate () as Func<T1, T2, T3, TR>;
		if (runtimeDelegate != null)
			return runtimeDelegate.Invoke (arg1, arg2, arg3);
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
	private Func<T1, T2, TR> runtimeDelegate;

	// Retarget constructors to base
	public UnityFunc (Object targetObject, MethodInfo methodInfo) : base (targetObject, methodInfo) {}
	public UnityFunc (Delegate func) : base (func) {}

	public TR Invoke (T1 arg1, T2 arg2) 
	{
		if (runtimeDelegate == null)
			runtimeDelegate = DeserializeToDelegate () as Func<T1, T2, TR>;
		if (runtimeDelegate != null)
			return runtimeDelegate.Invoke (arg1, arg2);
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
	private Func<T1, TR> runtimeDelegate;

	// Retarget constructors to base
	public UnityFunc (Object targetObject, MethodInfo methodInfo) : base (targetObject, methodInfo) {}
	public UnityFunc (Delegate func) : base (func) {}

	public TR Invoke (T1 arg) 
	{
		if (runtimeDelegate == null)
			runtimeDelegate = DeserializeToDelegate () as Func<T1, TR>;
		if (runtimeDelegate != null)
			return runtimeDelegate.Invoke (arg);
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
	private Func<TR> runtimeDelegate;

	// Retarget constructors to base
	public UnityFunc (Object targetObject, MethodInfo methodInfo) : base (targetObject, methodInfo) {}
	public UnityFunc (Delegate func) : base (func) {}

	public TR Invoke () 
	{
		if (runtimeDelegate == null)
			runtimeDelegate = DeserializeToDelegate () as Func<TR>;
		if (runtimeDelegate != null)
			return runtimeDelegate.Invoke ();
		return default(TR);
	}
}

#endregion