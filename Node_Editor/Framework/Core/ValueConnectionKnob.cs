using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

using NodeEditorFramework.Utilities;

namespace NodeEditorFramework 
{
	[System.Serializable]
	public class ValueConnectionKnob : ConnectionKnob
	{
		// Connections
		new public List<ValueConnectionKnob> connections { get { return _connections.OfType<ValueConnectionKnob> ().ToList (); } }

		// Knob Style
		protected override Type styleBaseClass { get { return typeof(ValueConnectionType); } }
		new protected ValueConnectionType ConnectionStyle { get { CheckConnectionStyle (); return (ValueConnectionType)_connectionStyle; } }

		// Knob Value
		public Type valueType { get { return ConnectionStyle.Type; } }
		public bool IsValueNull { get { return value == null; } }
		[System.NonSerialized]
		private object _value = null;
		private object value {
			get { return _value; }
			set {
				_value = value;
				if (direction == Direction.Out)
				{
					foreach (ValueConnectionKnob connectionKnob in connections)
						connectionKnob.SetValue(value);
				}
			}
		}

		public void Init (Node body, string name, Direction dir, string type)
		{
			base.Init (body, name, dir);
			styleID = type;
		}

		public void Init (Node body, string name, Direction dir, string type, NodeSide nodeSide, float nodeSidePosition = 0)
		{
			base.Init (body, name, dir, nodeSide, nodeSidePosition);
			styleID = type;
		}

		new public ValueConnectionKnob connection (int index) 
		{
			if (connections.Count <= index)
				throw new IndexOutOfRangeException ("connections[" + index + "] of '" + name + "'");
			return connections[index];
		}

		public override bool CanApplyConnection (ConnectionPort port)
		{
			ValueConnectionKnob valueKnob = port as ValueConnectionKnob;
			if (valueKnob == null || !valueType.IsAssignableFrom (valueKnob.valueType))
				return false;
			return base.CanApplyConnection (port);
		}

		#region Knob Value

		/// <summary>
		/// Gets the knob value anonymously. Not advised as it may lead to unwanted behaviour!
		/// </summary>
		public object GetValue ()
		{
			return value;
		}

		/// <summary>
		/// Gets the output value if the type matches or null. If possible, use strongly typed version instead.
		/// </summary>
		public object GetValue (Type type)
		{
			if (type == null)
				throw new ArgumentException ("Trying to GetValue of knob " + name + " with null type!");
			if (type.IsAssignableFrom (valueType))
				return value?? (value = GetDefault (type));
			throw new ArgumentException ("Trying to GetValue of type " + type.FullName + " for Output Type: " + valueType.FullName);
		}

		/// <summary>
		/// Sets the output value if the type matches. If possible, use strongly typed version instead.
		/// </summary>
		public void SetValue (object Value)
		{
			if (Value != null && !valueType.IsAssignableFrom (Value.GetType ()))
				throw new ArgumentException("Trying to SetValue of type " + Value.GetType().FullName + " for Output Type: " + valueType.FullName);
			value = Value;
		}

		/// <summary>
		/// Gets the output value if the type matches
		/// </summary>
		/// <returns>Value, if null default(T) (-> For reference types, null. For value types, default value)</returns>
		public T GetValue<T> ()
		{
			if (typeof(T).IsAssignableFrom (valueType))
				return (T)(value?? (value = GetDefault<T> ()));
			Debug.LogError ("Trying to GetValue<" + typeof(T).FullName + "> for Output Type: " + valueType.FullName);
			return GetDefault<T> ();
		}

		/// <summary>
		/// Sets the output value if the type matches
		/// </summary>
		public void SetValue<T> (T Value)
		{
			if (valueType.IsAssignableFrom (typeof(T)))
				value = Value;
			else
				Debug.LogError ("Trying to SetValue<" + typeof(T).FullName + "> for Output Type: " + valueType.FullName);
		}

		/// <summary>
		/// Resets the output value to null.
		/// </summary>
		public void ResetValue () 
		{
			value = null;
		}

		/// <summary>
		/// Returns the default value of type when a default constructor is existant or type is a value type, else null
		/// </summary>
		private static T GetDefault<T> ()
		{
			// Try to create using an empty constructor if existant
			if (typeof(T).GetConstructor (System.Type.EmptyTypes) != null)
				return System.Activator.CreateInstance<T> ();
			// Else try to get default. Returns null only on reference types
			return default(T);
		}

		/// <summary>
		/// Returns the default value of type when a default constructor is existant, else null
		/// </summary>
		private static object GetDefault (Type type)
		{
			// Try to create using an empty constructor if existant
			if (type.GetConstructor (System.Type.EmptyTypes) != null)
				return System.Activator.CreateInstance (type);
			return null;
		}

		#endregion
	}

	[AttributeUsage(AttributeTargets.Field)]
	public class ValueConnectionKnobAttribute : ConnectionKnobAttribute
	{
		public override Type ConnectionType { get { return typeof(ValueConnectionKnob); } }

		public ValueConnectionKnobAttribute(string name, Direction direction, string type) : base (name, direction, type) { }
		public ValueConnectionKnobAttribute(string name, Direction direction, string type, ConnectionCount maxCount) : base (name, direction, type, maxCount) { }
		public ValueConnectionKnobAttribute(string name, Direction direction, string type, NodeSide nodeSide, float nodeSidePos = 0) : base (name, direction, type, nodeSide, nodeSidePos) { }
		public ValueConnectionKnobAttribute(string name, Direction direction, string type, ConnectionCount maxCount, NodeSide nodeSide, float nodeSidePos = 0) : base (name, direction, type, maxCount, nodeSide, nodeSidePos) { }

		public override bool IsCompatibleWith (ConnectionPort port) 
		{
			if (!(Direction == Direction.None && port.direction == Direction.None)
				&& !(Direction == Direction.In && port.direction == Direction.Out)
				&& !(Direction == Direction.Out && port.direction == Direction.In))
				return false;
			ValueConnectionKnob valueKnob = port as ValueConnectionKnob;
			if (valueKnob == null)
				return false;
			Type knobType = ConnectionPortStyles.GetValueType (StyleID);
			return knobType.IsAssignableFrom (valueKnob.valueType);
		}

		public override ConnectionPort CreateNew (Node body) 
		{
			ValueConnectionKnob knob = ScriptableObject.CreateInstance<ValueConnectionKnob> ();
			knob.Init (body, Name, Direction, StyleID, NodeSide, NodeSidePos);
			knob.maxConnectionCount = MaxConnectionCount;
			return knob;
		}

		public override void UpdateProperties (ConnectionPort port) 
		{
			ValueConnectionKnob knob = (ValueConnectionKnob)port;
			knob.name = Name;
			knob.direction = Direction;
			knob.styleID = StyleID;
			knob.maxConnectionCount = MaxConnectionCount;
			knob.side = NodeSide;
			if (NodeSidePos != 0)
				knob.sidePosition = NodeSidePos;
			knob.sideOffset = 0;
		}
	}
	
	[ReflectionUtility.ReflectionSearchIgnoreAttribute ()]
	public class ValueConnectionType : ConnectionKnobStyle
	{
		protected Type type;
		public virtual Type Type { get { return type; } }

		public ValueConnectionType () : base () { }

		public ValueConnectionType (Type valueType) : base (valueType.AssemblyQualifiedName)
		{
			identifier = valueType.Name;
			type = valueType;
		}

		public override bool isValid ()
		{
			bool valid = Type != null && InKnobTex != null && OutKnobTex != null;
			if (!valid)
				Debug.LogError("Type " + Identifier + " is invalid! Type-Null?" + (type == null) + ", InTex-Null?" + (InKnobTex == null) + ", OutTex-Null?" + (OutKnobTex == null));
			return valid;
		}
	}
}
