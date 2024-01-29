using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace KludgeBox;

public static class ReflectionExtensions
{
	/// <summary>
	/// Get the main executing assembly.
	/// </summary>
	/// <returns>The current executing assembly.</returns>
	public static Assembly GetCurrentAssembly()
	{
		return Assembly.GetEntryAssembly();
	}

	/// <summary>
	/// Get all the loaded assemblies.
	/// </summary>
	/// <returns>An array of all loaded assemblies.</returns>
	public static Assembly[] GetAllAssemblies()
	{
		return AppDomain.CurrentDomain.GetAssemblies();
	}

	/// <summary>
	/// Try to find a type by its full or short name.
	/// </summary>
	/// <param name="typeName">The short or full name of the type.</param>
	/// <returns>The found type or null if not found.</returns>
	public static Type FindTypeByName(string typeName)
	{
		return AppDomain.CurrentDomain.GetAssemblies()
			.SelectMany(a => a.GetTypes())
			.FirstOrDefault(t => t.FullName == typeName || t.Name == typeName);
	}

	/// <summary>
	/// Try to find a type implementing IExposable by its name and instantiate it.
	/// </summary>
	/// <param name="typeName">The name of the type implementing IExposable.</param>
	/// <returns>An instance of the type implementing IExposable or null if not found.</returns>
	public static TType GetInstanceOfType<TType>(string typeName) where TType : class
	{
		Type type = FindTypeByName(typeName);
		if (type != null && typeof(TType).IsAssignableFrom((Type)type))
		{
			return (TType)Activator.CreateInstance(type);
		}

		return null;
	}
	
	/// <summary>
	/// Try to find a type implementing IExposable by its name and instantiate it.
	/// </summary>
	/// <param name="typeName">The name of the type implementing IExposable.</param>
	/// <returns>An instance of the type implementing IExposable or null if not found.</returns>
	public static object GetInstanceOfType(Type type)
	{
		return Activator.CreateInstance(type);
	}

	/// <summary>
	/// Returns the full name of the object's type.
	/// </summary>
	/// <param name="obj">The object whose type name to retrieve.</param>
	/// <returns>The full name of the object's type.</returns>
	public static string GetTypeName(this object obj)
	{
		return obj?.GetType()?.FullName;
	}

	/// <summary>
	/// Try to find types with all of the attributes provided in the attributes param.
	/// </summary>
	/// <param name="attributes">One or more attribute types to search for.</param>
	/// <returns>An array of types that have all of the specified attributes.</returns>
	public static Type[] FindTypesWithAttributes(params Type[] attributes)
	{
		return AppDomain.CurrentDomain.GetAssemblies()
			.SelectMany(a => a.GetTypes())
			.Where(t => attributes.All(attr => t.GetCustomAttribute(attr) != null))
			.ToArray();
	}

	/// <summary>
	/// Try to find types with all of the attributes provided in the attributes param within a specific assembly.
	/// </summary>
	/// <param name="assembly">The assembly to search in.</param>
	/// <param name="attributes">One or more attribute types to search for.</param>
	/// <returns>An array of types that have all of the specified attributes.</returns>
	public static Type[] FindTypesWithAttributes(this Assembly assembly, params Type[] attributes)
	{
		return assembly.GetTypes()
			.Where(t => attributes.All(attr => t.GetCustomAttribute(attr) != null))
			.ToArray();
	}

	/// <summary>
	/// Try to find types with all of the attributes provided in the attributes param within multiple assemblies.
	/// </summary>
	/// <param name="assemblies">The assemblies to search in.</param>
	/// <param name="attributes">One or more attribute types to search for.</param>
	/// <returns>An array of types that have all of the specified attributes.</returns>
	public static Type[] FindTypesWithAttributes(this IEnumerable<Assembly> assemblies, params Type[] attributes)
	{
		return assemblies
			.SelectMany(a => a.GetTypes())
			.Where(t => attributes.All(attr => t.GetCustomAttribute(attr) != null))
			.ToArray();
	}

	/// <summary>
	/// Try to find methods with all of the attributes provided in the attributes param.
	/// </summary>
	/// <param name="type">The type to search in.</param>
	/// <param name="attributes">The attributes that the methods must have.</param>
	/// <returns>The MethodInfo array containing methods that have all of the specified attributes.</returns>
	public static MethodInfo[] FindMethodsWithAttributes(this Type type, params Type[] attributes)
	{
		return type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
		                       BindingFlags.NonPublic)
			.Where(m => attributes.All(attr => m.GetCustomAttributes(attr, true).Length > 0))
			.ToArray();
	}


	/// <summary>
	/// Finds all types that derive from the specified base type.
	/// </summary>
	/// <typeparam name="TBase">The base type.</typeparam>
	/// <returns>An enumerable collection of types that derive from the specified base type.</returns>
	public static IEnumerable<Type> FindAllTypesThatDeriveFrom<TBase>()
	{
		return GetAllAssemblies().SelectMany(a => a.GetTypes()).Where(type => type.IsSubclassOf(typeof(TBase)));
	}


	/// <summary>
	/// Finds all types that derive from the specified base type.
	/// </summary>
	/// <typeparam name="TBase">The base type.</typeparam>
	/// <returns>An enumerable collection of types that derive from the specified base type.</returns>
	public static IEnumerable<Type> FindAllTypesThatDeriveFrom<TBase>(this Assembly assembly)
	{
		return assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(TBase)));
	}
}