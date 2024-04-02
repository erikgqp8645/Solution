using System.Collections.Generic;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal static class UtilityTypeExtensions
{
	public static IEnumerable<AggregateType> InterfaceAndBases(this AggregateType type)
	{
		yield return type;
		CType[] array = type.GetIfacesAll().ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			yield return (AggregateType)array[i];
		}
	}

	public static IEnumerable<AggregateType> AllConstraintInterfaces(this TypeArray constraints)
	{
		CType[] array = constraints.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			AggregateType type = (AggregateType)array[i];
			foreach (AggregateType item in type.InterfaceAndBases())
			{
				yield return item;
			}
		}
	}

	public static IEnumerable<AggregateType> TypeAndBaseClasses(this AggregateType type)
	{
		for (AggregateType t = type; t != null; t = t.GetBaseClass())
		{
			yield return t;
		}
	}

	public static IEnumerable<AggregateType> TypeAndBaseClassInterfaces(this AggregateType type)
	{
		foreach (AggregateType item in type.TypeAndBaseClasses())
		{
			CType[] array = item.GetIfacesAll().ToArray();
			for (int i = 0; i < array.Length; i++)
			{
				yield return (AggregateType)array[i];
			}
		}
	}

	public static IEnumerable<CType> AllPossibleInterfaces(this CType type)
	{
		if (type.IsAggregateType())
		{
			foreach (AggregateType item in type.AsAggregateType().TypeAndBaseClassInterfaces())
			{
				yield return item;
			}
		}
		else
		{
			if (!type.IsTypeParameterType())
			{
				yield break;
			}
			foreach (AggregateType item2 in type.AsTypeParameterType().GetEffectiveBaseClass().TypeAndBaseClassInterfaces())
			{
				yield return item2;
			}
			foreach (AggregateType item3 in type.AsTypeParameterType().GetInterfaceBounds().AllConstraintInterfaces())
			{
				yield return item3;
			}
		}
	}
}
