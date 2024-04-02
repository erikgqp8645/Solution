using System.Collections.Generic;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal static class ListExtensions
{
	public static bool IsEmpty<T>(this List<T> list)
	{
		if (list != null)
		{
			return list.Count == 0;
		}
		return true;
	}

	public static T Head<T>(this List<T> list)
	{
		return list[0];
	}

	public static List<T> Tail<T>(this List<T> list)
	{
		T[] array = new T[list.Count];
		list.CopyTo(array, 0);
		List<T> list2 = new List<T>(array);
		list2.RemoveAt(0);
		return list2;
	}
}
