using System;
using System.Linq;
using System.Reflection;

namespace Microsoft.CSharp.RuntimeBinder;

internal static class RuntimeBinderExtensions
{
	internal static bool IsNullableType(this Type t)
	{
		if (t.IsGenericType)
		{
			return t.GetGenericTypeDefinition() == typeof(Nullable<>);
		}
		return false;
	}

	internal static bool IsEquivalentTo(this MemberInfo mi1, MemberInfo mi2)
	{
		if (mi1 == null || mi2 == null)
		{
			if (mi1 == null)
			{
				return mi2 == null;
			}
			return false;
		}
		if (mi1 == mi2 || (mi1.DeclaringType.IsGenericallyEqual(mi2.DeclaringType) && mi1.MetadataToken == mi2.MetadataToken))
		{
			return true;
		}
		if (mi1 is MethodInfo && mi2 is MethodInfo)
		{
			MethodInfo methodInfo = mi1 as MethodInfo;
			MethodInfo methodInfo2 = mi2 as MethodInfo;
			ParameterInfo[] parameters;
			ParameterInfo[] parameters2;
			if (methodInfo != methodInfo2 && !methodInfo.IsGenericMethod && !methodInfo2.IsGenericMethod && methodInfo.Name == methodInfo2.Name && methodInfo.DeclaringType.IsEquivalentTo(methodInfo2.DeclaringType) && methodInfo.ReturnType.IsEquivalentTo(methodInfo2.ReturnType) && (parameters = methodInfo.GetParameters()).Length == (parameters2 = methodInfo2.GetParameters()).Length)
			{
				return parameters.Zip(parameters2, (ParameterInfo pi1, ParameterInfo pi2) => pi1.IsEquivalentTo(pi2)).All((bool x) => x);
			}
			return false;
		}
		if (mi1 is ConstructorInfo && mi2 is ConstructorInfo)
		{
			ConstructorInfo constructorInfo = mi1 as ConstructorInfo;
			ConstructorInfo constructorInfo2 = mi2 as ConstructorInfo;
			ParameterInfo[] parameters3;
			ParameterInfo[] parameters4;
			if (constructorInfo != constructorInfo2 && constructorInfo.DeclaringType.IsEquivalentTo(constructorInfo2.DeclaringType) && (parameters3 = constructorInfo.GetParameters()).Length == (parameters4 = constructorInfo2.GetParameters()).Length)
			{
				return parameters3.Zip(parameters4, (ParameterInfo pi1, ParameterInfo pi2) => pi1.IsEquivalentTo(pi2)).All((bool x) => x);
			}
			return false;
		}
		if (mi1 is PropertyInfo && mi2 is PropertyInfo)
		{
			PropertyInfo propertyInfo = mi1 as PropertyInfo;
			PropertyInfo propertyInfo2 = mi2 as PropertyInfo;
			if (propertyInfo != propertyInfo2 && propertyInfo.Name == propertyInfo2.Name && propertyInfo.DeclaringType.IsEquivalentTo(propertyInfo2.DeclaringType) && propertyInfo.PropertyType.IsEquivalentTo(propertyInfo2.PropertyType) && propertyInfo.GetGetMethod(nonPublic: true).IsEquivalentTo(propertyInfo2.GetGetMethod(nonPublic: true)))
			{
				return propertyInfo.GetSetMethod(nonPublic: true).IsEquivalentTo(propertyInfo2.GetSetMethod(nonPublic: true));
			}
			return false;
		}
		return false;
	}

	internal static bool IsEquivalentTo(this ParameterInfo pi1, ParameterInfo pi2)
	{
		if (pi1 == null || pi2 == null)
		{
			if (pi1 == null)
			{
				return pi2 == null;
			}
			return false;
		}
		if (pi1 == pi2)
		{
			return true;
		}
		return pi1.ParameterType.IsEquivalentTo(pi2.ParameterType);
	}

	internal static bool IsGenericallyEqual(this Type t1, Type t2)
	{
		if (t1 == null || t2 == null)
		{
			if (t1 == null)
			{
				return t2 == null;
			}
			return false;
		}
		if (t1 == t2)
		{
			return true;
		}
		if (t1.IsGenericType && t2.IsGenericType)
		{
			Type genericTypeDefinition = t1.GetGenericTypeDefinition();
			Type genericTypeDefinition2 = t2.GetGenericTypeDefinition();
			return genericTypeDefinition == genericTypeDefinition2;
		}
		return false;
	}
}
