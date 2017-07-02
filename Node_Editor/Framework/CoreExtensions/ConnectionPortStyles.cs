using UnityEngine;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using NodeEditorFramework.Utilities;

namespace NodeEditorFramework
{
	/// <summary>
	/// Handles fetching and storing of all ConnectionPortStyle declarations
	/// </summary>
	public static class ConnectionPortStyles
	{
		private static Dictionary<string, ConnectionPortStyle> connectionPortStyles;
		private static Dictionary<string, ValueConnectionType> connectionValueTypes;

		/// <summary>
		/// Fetches every ConnectionPortStyle, ConnectionKnobStyle or ValueConnectionType declaration in the script assemblies to provide the framework with custom connection port styles
		/// </summary>
		public static void FetchConnectionPortStyles () 
		{
			connectionPortStyles = new Dictionary<string, ConnectionPortStyle> ();
			connectionValueTypes = new Dictionary<string, ValueConnectionType> ();
			foreach (Type type in ReflectionUtility.getSubTypes (typeof(ConnectionPortStyle)))
			{
				ConnectionPortStyle portStyle = (ConnectionPortStyle)Activator.CreateInstance (type);
				if (portStyle == null)
					throw new UnityException ("Error with Connection Port Style Declaration " + type.FullName);
				if (!portStyle.isValid ())
					throw new Exception (type.BaseType.Name + " declaration " + portStyle.Identifier + " is invalid!");
				if (connectionPortStyles.ContainsKey (portStyle.Identifier))
					throw new Exception ("Duplicate ConnectionPortStyle declaration " + portStyle.Identifier + "!");

				connectionPortStyles.Add (portStyle.Identifier, portStyle);
				if (type.IsSubclassOf (typeof(ValueConnectionType)))
					connectionValueTypes.Add (portStyle.Identifier, (ValueConnectionType)portStyle);
				if (!portStyle.isValid())
					Debug.LogError("Style " + portStyle.Identifier + " is invalid!");
			}
		}

		/// <summary>
		/// Gets the ValueConnectionType type the specified type name representates or creates it if not defined
		/// </summary>
		public static Type GetValueType (string typeName)
		{
			return ((ValueConnectionType)GetPortStyle (typeName, typeof(ValueConnectionType))).Type ?? typeof(void);
		}

		/// <summary>
		/// Gets the ConnectionPortStyle for the specified style name or creates it if not defined
		/// </summary>
		public static ConnectionPortStyle GetPortStyle (string styleName, Type baseStyleClass = null)
		{
			if (connectionPortStyles == null || connectionPortStyles.Count == 0)
				FetchConnectionPortStyles ();
			if (baseStyleClass == null || !typeof(ConnectionPortStyle).IsAssignableFrom (typeof(ConnectionPortStyle)))
				baseStyleClass = typeof(ConnectionPortStyle);
			ConnectionPortStyle portStyle;
			if (!connectionPortStyles.TryGetValue (styleName, out portStyle))
			{ // No port style with the exact name exists
				if (typeof(ValueConnectionType).IsAssignableFrom (baseStyleClass))
				{ // A ValueConnectionType is searched, try by type name
					Type type = Type.GetType (styleName);
					if (type == null) // No type matching the name found either
						throw new ArgumentException ("No ValueConnectionType could be found or created with name '" + styleName + "'!");
					else // Matching type found, search or create type data based on type
						portStyle = GetValueConnectionType (type);
				}
				else
				{
					portStyle = (ConnectionPortStyle)Activator.CreateInstance (baseStyleClass, styleName);
					connectionPortStyles.Add (styleName, portStyle);
					Debug.LogWarning("Created style from name " + styleName + "!");
				}
			}
			if (!baseStyleClass.IsAssignableFrom (portStyle.GetType ()))
				throw new Exception ("Cannot use Connection Style: '" + styleName + "' is not of type " + baseStyleClass.Name + "!");
			if (!portStyle.isValid())
				Debug.LogError("Fetched style " + portStyle.Identifier + " is invalid!");
			return portStyle;
		}

		/// <summary>
		/// Gets the ValueConnectionType for the specified type or creates it if not defined
		/// </summary>
		public static ValueConnectionType GetValueConnectionType (Type type)
		{
			if (connectionPortStyles == null || connectionPortStyles.Count == 0)
				FetchConnectionPortStyles ();
			ValueConnectionType valueType = connectionValueTypes.Values.FirstOrDefault ((ValueConnectionType data) => data.isValid () && data.Type == type);
			if (valueType == null) // ValueConnectionType with type does not exist, create it
			{
				valueType = new ValueConnectionType (type);
				connectionPortStyles.Add (type.Name, valueType);
				connectionValueTypes.Add (type.Name, valueType);
			}
			return valueType;
		}
	}
}