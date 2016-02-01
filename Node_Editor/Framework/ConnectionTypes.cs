using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using NodeEditorFramework.Utilities;

namespace NodeEditorFramework
{
	public enum ConnectionDrawMethod { Bezier, StraightLine }

	public static class ConnectionTypes
	{
		private static Type NullType { get { return typeof(ConnectionTypes); } }
		
		// Static consistent information about types
		internal static Dictionary<string, TypeData> Types = new Dictionary<string, TypeData> ();

		/// <summary>
		/// Gets the type data for the specified type name, if declared
		/// </summary>
		public static TypeData GetTypeData (string typeName)
		{
			if (Types == null || Types.Count == 0)
				NodeEditor.ReInit (false);
			TypeData typeData;
			if (!Types.TryGetValue (typeName, out typeData))
			{
				Debug.LogError ("No TypeData defined for: " + typeName);
				typeData = Types.First ().Value;
			}
			if (typeData.Declaration == null || typeData.InputKnob == null || typeData.OutputKnob == null)
			{
				NodeEditor.ReInit (false);
				typeData = GetTypeData (typeName);
			}
			return typeData;
		}

		/// <summary>
		/// Gets the Type the specified type name representates, if declared
		/// </summary>
		public static Type GetType (string typeName)
		{
			return GetTypeData (typeName).Type ?? NullType;
		}
		
		/// <summary>
		/// Fetches every Type Declaration in the script assembly and the executing one, if the NodeEditor is packed into a .dll
		/// </summary>
		internal static void FetchTypes () 
		{
			Types = new Dictionary<string, TypeData> ();

			List<Assembly> scriptAssemblies = AppDomain.CurrentDomain.GetAssemblies ().Where (assembly => assembly.FullName.Contains ("Assembly")).ToList ();
			if (!scriptAssemblies.Contains (Assembly.GetExecutingAssembly ()))
				scriptAssemblies.Add (Assembly.GetExecutingAssembly ());
			foreach (Assembly assembly in scriptAssemblies) 
			{
				foreach (Type type in assembly.GetTypes ().Where (T => T.IsClass && !T.IsAbstract && T.GetInterfaces ().Contains (typeof (ITypeDeclaration)))) 
				{
					ITypeDeclaration typeDecl = assembly.CreateInstance (type.FullName) as ITypeDeclaration;
					if (typeDecl == null)
						throw new UnityException ("Error with Type Declaration " + type.FullName);
					Types.Add (typeDecl.Name, new TypeData (typeDecl));
				}
			}
		}
	}

	public struct TypeData 
	{
		public ITypeDeclaration Declaration;
		public Type Type;
		public Color Col;
		public Texture2D InputKnob;
		public Texture2D OutputKnob;
		
		public TypeData (ITypeDeclaration typeDecl) 
		{
			Declaration = typeDecl;
			Type = Declaration.Type;
			Col = Declaration.Col;

			InputKnob = ResourceManager.GetTintedTexture (Declaration.InputKnobTexPath, Col);
			OutputKnob = ResourceManager.GetTintedTexture (Declaration.OutputKnobTexPath, Col);
		}
	}

	public interface ITypeDeclaration
	{
		string Name { get; }
		Color Col { get; }
		string InputKnobTexPath { get; }
		string OutputKnobTexPath { get; }
		Type Type { get; }
	}

	// TODO: Node Editor: Built-In Connection Types
	public class FloatType : ITypeDeclaration 
	{
		public string Name { get { return "Float"; } }
		public Color Col { get { return Color.cyan; } }
		public string InputKnobTexPath { get { return "Textures/In_Knob.png"; } }
		public string OutputKnobTexPath { get { return "Textures/Out_Knob.png"; } }
		public Type Type { get { return typeof(float); } }
	}


}