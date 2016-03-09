using UnityEngine;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using NodeEditorFramework.Utilities;

namespace NodeEditorFramework
{
	public enum ConnectionDrawMethod { Bezier, StraightLine }

	/// <summary>
	/// Handles fetching and storing of all ConnecionTypes
	/// </summary>
	public static class ConnectionTypes
	{
		private static Type NullType { get { return typeof(ConnectionTypes); } }
		
		// Static consistent information about types
		private static Dictionary<string, TypeData> types;

		/// <summary>
		/// Gets the Type the specified type name representates, if declared
		/// </summary>
		public static Type GetType (string typeName)
		{
			return GetTypeData (typeName).Type ?? NullType;
		}

		/// <summary>
		/// Gets the type data for the specified type name, if declared
		/// </summary>
		public static TypeData GetTypeData (string typeName)
		{
			if (types == null || types.Count == 0)
				FetchTypes ();
			TypeData typeData;
			if (!types.TryGetValue (typeName, out typeData))
			{
				Type type = Type.GetType (typeName);
				if (type == null)
				{
					typeData = types.First ().Value;
					Debug.LogError ("No TypeData defined for: " + typeName + " and type could not be found either");
				}
				else 
				{
					List<TypeData> typeDatas = types.Values.ToList ();
					//typeData = typeDatas.First ((TypeData data) => data.isValid () && data.Type == type);
					typeData = typeDatas.Find ((TypeData data) => data.isValid () && data.Type == type);
					if (typeData == null)
						types.Add (typeName, typeData = new TypeData (type));
				}
			}
			return typeData;
		}
		
		/// <summary>
		/// Fetches every Type Declaration in the script assemblies and the executing one, if the NodeEditor is packed into a .dll
		/// </summary>
		internal static void FetchTypes () 
		{
			types = new Dictionary<string, TypeData> ();

			IEnumerable<Assembly> scriptAssemblies = AppDomain.CurrentDomain.GetAssemblies ().Where ((Assembly assembly) => assembly.FullName.Contains ("Assembly"));
			foreach (Assembly assembly in scriptAssemblies) 
			{
				foreach (Type type in assembly.GetTypes ().Where (T => T.IsClass && !T.IsAbstract && T.GetInterfaces ().Contains (typeof (ITypeDeclaration)))) 
				{
					ITypeDeclaration typeDecl = assembly.CreateInstance (type.FullName) as ITypeDeclaration;
					if (typeDecl == null)
						throw new UnityException ("Error with Type Declaration " + type.FullName);
					types.Add (typeDecl.name, new TypeData (typeDecl));
				}
			}
		}
	}

	public class TypeData 
	{
		public ITypeDeclaration declaration;
		public Type Type;
		public Color col;
		public Texture2D InputKnob;
		public Texture2D OutputKnob;
		
		public TypeData (ITypeDeclaration typeDecl) 
		{
			declaration = typeDecl;
			Type = declaration.Type;
			col = declaration.col;

			InputKnob = ResourceManager.GetTintedTexture (declaration.InputKnob_TexPath, col);
			OutputKnob = ResourceManager.GetTintedTexture (declaration.OutputKnob_TexPath, col);
		}

		public TypeData (Type type) 
		{
			declaration = null;
			Type = type;
			col = Color.white;//(float)type.GetHashCode() / (int.MaxValue/3);

			// int - 3x float
			int srcInt = type.GetHashCode ();
			byte[] bytes = BitConverter.GetBytes (srcInt);
			//Debug.Log ("hash " + srcInt + " from type " + type.FullName + " has byte count of " + bytes.Length);
			col = new Color (Mathf.Pow (((float)bytes[0])/255, 0.5f), Mathf.Pow (((float)bytes[1])/255, 0.5f), Mathf.Pow (((float)bytes[2])/255, 0.5f));
			//Debug.Log ("Color " + col.ToString ());

			InputKnob = ResourceManager.GetTintedTexture ("Textures/In_Knob.png", col);
			OutputKnob = ResourceManager.GetTintedTexture ("Textures/Out_Knob.png", col);
		}

		public bool isValid () 
		{
			return Type != null && InputKnob != null && OutputKnob != null;
		}
	}

	public interface ITypeDeclaration
	{
		string name { get; }
		Color col { get; }
		string InputKnob_TexPath { get; }
		string OutputKnob_TexPath { get; }
		Type Type { get; }
	}

	// TODO: Node Editor: Built-In Connection Types
	public class FloatType : ITypeDeclaration 
	{
		public string name { get { return "Float"; } }
		public Color col { get { return Color.cyan; } }
		public string InputKnob_TexPath { get { return "Textures/In_Knob.png"; } }
		public string OutputKnob_TexPath { get { return "Textures/Out_Knob.png"; } }
		public Type Type { get { return typeof(float); } }
	}
}