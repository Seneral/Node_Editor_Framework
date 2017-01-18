using UnityEngine;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NodeEditorFramework.Utilities
{
	// Using MenuForm to be able to switch between GenericMenu implementations quickly
	#if UNITY_EDITOR
	using MenuForm = UnityEditor.GenericMenu;
	using MenuCallbackData = UnityEditor.GenericMenu.MenuFunction2;
	#else
	using MenuForm = NodeEditorFramework.Utilities.GenericMenu;
	using MenuCallbackData = NodeEditorFramework.Utilities.PopupMenu.MenuFunctionData;
	#endif

	/// <summary>
	/// Class that provides GenericMenu type selector building functionality
	/// </summary>
	public static class TypeSelector
	{
		// Reflection cache
		private static Dictionary<Assembly, List<TypeMenuEntry>> cachedTypeEntries = new Dictionary<Assembly, List<TypeMenuEntry>> ();

		/// <summary>
		/// Creates a menu to select types from the UnityEditor/UnityEngine and all script assemblies
		/// Caches assembly type search results. Structure in the type menu is defined by the namespaces though, not by assembly
		/// </summary>
		public static MenuForm BuildTypeSelection (MenuCallbackData typeSelectionCallback) 
		{
			return BuildTypeSelection (typeSelectionCallback, null, null); // (Assembly assembly) => assembly.FullName.Contains ("Assembly") || assembly.FullName.Contains ("Unity")
		}

		/// <summary>
		/// Creates a menu to select types from all assemblies which pass the given assembly filter. 
		/// Caches assembly type search results. Structure in the type menu is defined by the namespaces though, not by assembly
		/// </summary>
		public static MenuForm BuildTypeSelection (MenuCallbackData typeSelectionCallback, Func<Assembly, bool> assemblyFilter, Func<Type, bool> typeFilter) 
		{
			MenuForm typeMenu = new MenuForm();

			// Enumerate over the selected assemblies
			IEnumerable<Assembly> assemblies = AppDomain.CurrentDomain.GetAssemblies();
			if (assemblyFilter != null)
				assemblies = assemblies.Where ((Assembly assembly) => assemblyFilter.Invoke (assembly));
			foreach (Assembly assembly in assemblies) 
			{
				List<TypeMenuEntry> assemblyTypeEntries;
				if (cachedTypeEntries.ContainsKey (assembly))
				{ // Retrieve cached type entries if existant
					assemblyTypeEntries = cachedTypeEntries [assembly];
				}
				else
				{ // else, built a new one for this assembly
					assemblyTypeEntries = new List<TypeMenuEntry> ();
					foreach (Type type in assembly.GetTypes())
					{ // Iterate through each type and add it to the cached type entries
						string typePath = GetNamespaceHierarchy (type);
						assemblyTypeEntries.Add (new TypeMenuEntry (typePath, type));
						foreach (Type nestedType in type.GetNestedTypes()) 
						{ // also account for the nested types
							assemblyTypeEntries.Add (new TypeMenuEntry (typePath + " Subtypes/" + nestedType.Name, nestedType));
						}
					}
					// cache this assembly result
					assemblyTypeEntries.OrderBy ((TypeMenuEntry entry) => entry.menuPath);
					//assemblyTypeEntries.Sort ((TypeMenuEntry entry1, TypeMenuEntry entry2) => Regex.Match (entry2.menuPath, "namespace").Length.CompareTo (Regex.Match (entry1.menuPath, "namespace").Length));
					cachedTypeEntries.Add (assembly, assemblyTypeEntries);
				}
				// Fill the actual menu with the type entries for this assembly
				foreach (TypeMenuEntry type in assemblyTypeEntries) 
				{
					if (typeFilter != null && !typeFilter.Invoke (type.type))
						continue;
					typeMenu.AddItem (new GUIContent (type.menuPath), false, typeSelectionCallback, type.type);
				}
			}
			return typeMenu;
		}

		/// <summary>
		/// Gets the path to the specified type in a menu based on namespaces
		/// </summary>
		private static string GetNamespaceHierarchy (Type type) 
		{
			string namespacePath = "";
			if (!string.IsNullOrEmpty (type.Namespace))
			{
				string[] namespaces = type.Namespace.Split ('.');
				foreach (string nameLayer in namespaces)
					namespacePath += "namespace " + nameLayer + "/";
			}
			namespacePath += "free/";
			return namespacePath + (type.DeclaringType != null? type.DeclaringType.Name + "/" : "") + type.Name;
		}

		/// <summary>
		/// Returns the default value of type when a default constructor is existant or type is a value type, else null
		/// </summary>
		public static T GetDefault<T> ()
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
		public static object GetDefault (Type type)
		{
			// Try to create using an empty constructor if existant
			if (type.GetConstructor (System.Type.EmptyTypes) != null)
				return System.Activator.CreateInstance (type);
			return null;
		}

		/// <summary>
		/// Type with associated menu item path
		/// </summary>
		private class TypeMenuEntry 
		{
			public string menuPath;
			public Type type;
			public TypeMenuEntry (string MenuPath, Type Type) 
			{
				menuPath = MenuPath;
				type = Type;
			}
		}
	}

	/// <summary>
	/// Class that provides GenericMenu command selector building functionality
	/// </summary>
	public static class CommandSelector
	{
		// Reflection cache
		private static List<ObjectCommands> cachedCommands = new List<ObjectCommands>();

		/// <summary>
		/// Builds a GenericMenu with a hierarchial command selection of the specified type and bindingFlags.
		/// executionSelectionCallback is the contextMenu callback that will receive a list of all commands that should be executed one after another
		/// </summary>
		/// <param name="executionSelectionCallback">The menu item callback receiving the selected command list (List<Command>)</param>
		public static MenuForm BuildCommandExecutionSelection (Type targetType, BindingFlags bindingFlags, MenuCallbackData commandSelectCallback,	// Selection data
																int maxDepth, bool expandPrimitives, bool collapseInheritedCommands) 				// Additional selection properties
		{
			MenuForm menu = new MenuForm();
			if (collapseInheritedCommands)
				bindingFlags |= BindingFlags.DeclaredOnly;
			else
				bindingFlags &= ~BindingFlags.DeclaredOnly;
			FillCommandExecutionSelectionMenu (menu, "", new List<Command> (), targetType, bindingFlags, commandSelectCallback, maxDepth, expandPrimitives, collapseInheritedCommands);
			return menu;
		}

		/// <summary>
		/// Fills a layer in the GenericMenu at the passedPath with all commands on the passed objectType and creates sub paths
		/// </summary>
		/// <param name="parentCommands">A list of commands that will be executed before this. Should be parallel to the path.</param>
		/// <param name="bindingFlags">The BindingFlag criteria for the commands of the objectType to account for.</param>
		/// <param name="executionSelectionCallback">The menu item callback receiving the selected command list to execute.</param>
		private static void FillCommandExecutionSelectionMenu (MenuForm menu, string sourceMenuPath, List<Command> prevCommands, 					// Additional Fill data
			Type targetType, BindingFlags bindingFlags, MenuCallbackData commandSelectCallback,	// Selection data
			int maxDepth, bool expandPrimitives, bool collapseInheritedCommands)				// Additional selection properties
		{
			if (maxDepth < 1)
				return;
			// Fill base
			if (collapseInheritedCommands && targetType.BaseType != null && targetType != typeof(object))
				FillCommandExecutionSelectionMenu (menu, sourceMenuPath + "base " + targetType.BaseType.Name + "/", prevCommands, targetType.BaseType, bindingFlags, commandSelectCallback, maxDepth-1, expandPrimitives, collapseInheritedCommands);
			// Fetch all commands on this type
			ObjectCommands objCommands = cachedCommands.Find ((ObjectCommands objCmds) => objCmds.type == targetType && objCmds.flags == bindingFlags);
			if (objCommands == null)
			{
				objCommands = new ObjectCommands (targetType, bindingFlags);
				cachedCommands.Add (objCommands);
			}
			// Static only makes sense on the first function in the command execution list
			if ((bindingFlags & BindingFlags.Static) == BindingFlags.Static) 
			{
				bindingFlags &= ~BindingFlags.Static;
				bindingFlags |= BindingFlags.Instance;
			}

			// Iterate through the commands and add them to the menuItem
			foreach (Command command in objCommands.commands) 
			{
				// Create new command list
				List<Command> combinedCommands = new List<Command> (prevCommands);
				combinedCommands.Add (command);
				// Add the entry to the genericMenu and pass the existing command list
				string funcItemPath = sourceMenuPath + command.GetRepresentationName ();
				menu.AddItem (new GUIContent (funcItemPath), true, commandSelectCallback, combinedCommands);
				// If this command is implicit (without parameters) and we should go further down, add an extra slection layer
				if (command.isImplicitCall && maxDepth > 1 && (expandPrimitives || !command.returnType.IsPrimitive))
					FillCommandExecutionSelectionMenu (menu, funcItemPath + "/", combinedCommands, command.returnType, bindingFlags, commandSelectCallback, maxDepth-1, expandPrimitives, collapseInheritedCommands);
			}
		}
	}

	/// <summary>
	/// Holds commands (Fields and methods) of a type
	/// </summary>
	public class ObjectCommands
	{
		public Type type { get; private set; }
		public BindingFlags flags { get; private set; }
		public List<Command> methodCommands { get; private set; }
		public List<Command> fieldCommands { get; private set; }

		public List<Command> commands { get; private set; }

		public ObjectCommands (Type targetType, BindingFlags bindingFlags) 
		{
			type = targetType;
			flags = bindingFlags;

			commands = new List<Command> ();
			// Fill methods and properties
			MethodInfo[] objectMethods = targetType.GetMethods (flags);
			foreach (MethodInfo method in objectMethods) 
				commands.Add (new Command (targetType, method, flags));
			// Fill fields
			FieldInfo[] objectFields = targetType.GetFields (flags);
			foreach (FieldInfo field in objectFields) 
			{
				commands.Add (new Command (targetType, field, flags, true));
				commands.Add (new Command (targetType, field, flags, false));
			}
		}
	}

	/// <summary>
	/// Represents any command (field or method) on baseType matching with flags. 
	/// Implicit means it needs no parameters or is a field. This helps for the creation of a command selection hierarchy.
	/// Provides functions to create a hierarchy of implicitly callable commands.
	/// </summary>
	public class Command
	{
		private MethodInfo _method;
		public MethodInfo method { get { 
				if (_field != null) throw new UnityException ("Field command doesn't have a method!"); 
				return _method; } }

		private FieldInfo _field;
		public FieldInfo field { get { 
				if (_method != null) throw new UnityException ("Method command doesn't have a field!"); 
				return _field; } }

		private Type _baseType;
		public Type baseType { get { return _baseType; } }

		private BindingFlags _flags;
		public BindingFlags flags { get { return _flags; } }

		private bool _isFieldGet;
		public bool isFieldGet { get { return _isFieldGet; } }

		public bool isMethod { get { return _method != null; } }
		public bool isImplicitCall { get { return (_method == null || _method.GetParameters ().Length == 0) && returnType != typeof(void); } }
		public Type returnType { get { return _method != null? _method.ReturnType : _field.FieldType; } }

		public string representationName;

		public Command (Type objectType, MethodInfo commandMethod, BindingFlags bindingFlags) 
		{
			_method = commandMethod;
			_field = null;
			_baseType = objectType;
			_flags = bindingFlags;
			_isFieldGet = false;
			representationName = "";
		}

		public Command (Type objectType, FieldInfo commandField, BindingFlags bindingFlags, bool isGetAccessor) 
		{
			_method = null;
			_field = commandField;
			_baseType = objectType;
			_flags = bindingFlags;
			_isFieldGet = isGetAccessor;
			representationName = "";
		}

		/// <summary>
		/// Returns the child commands of the return type of this command
		/// </summary>
		public ObjectCommands GetChildCommands () 
		{
			if (!isImplicitCall)
				throw new UnityException ("Cannot implicitly call methods with parameters!");
			return new ObjectCommands (returnType, flags);
		}

		/// <summary>
		/// Invokes this implicit command and returns the result. If it is not implicit, it'll throw an error.
		/// </summary>
		public object InvokeImplicit (object instanceObject) 
		{
			if (!isImplicitCall) 
				throw new UnityException ("Cannot implicitly call methods with parameters!");
			return isMethod? _method.Invoke (instanceObject, new object[0]) : _field.GetValue (instanceObject);
		}

		/// <summary>
		/// Invokes this command with the given parameters and returns the result. If it is implicit, it'll throw an error.
		/// </summary>
		public object Invoke (object targetObject, object[] parameter) 
		{
			if (isImplicitCall) 
				throw new UnityException ("Cannot call implicit commands with parameters!");
			return _method.Invoke (targetObject, parameter);
		}

		/// <summary>
		/// Gets the name of the Command with the type prefixed
		/// </summary>
		public string GetRepresentationName () 
		{
			if (String.IsNullOrEmpty (representationName))
			{
				// Return type
				representationName = returnType.Name + " ";
				// Name
				string name = isMethod? _method.Name : _field.Name;
				if (name.StartsWith ("get_") || name.StartsWith ("set_"))
					name = name.Substring (4);
				representationName += name;

				if (isMethod) 
				{
					// Generic Type Parameters
					if (_method.IsGenericMethod)
					{
						representationName += "<";
						Type[] genericParams = _method.GetGenericArguments ();
						for (int genericCnt = 0; genericCnt < genericParams.Length; genericCnt++)
							representationName += genericParams[genericCnt].Name + (genericCnt < genericParams.Length-1? ", " : "");
						// TODO: Generic Parameters
						representationName += ">";
					}
					// Parameters
					representationName += " (";
					ParameterInfo[] parameters = _method.GetParameters ();
					for (int paramCnt = 0; paramCnt < parameters.Length; paramCnt++)
						representationName += parameters[paramCnt].ParameterType.Name + (paramCnt < parameters.Length-1? ", " : "");
					representationName += ")";
				}
			}

			return representationName;
		}
	}
}
