using System.Linq;
using System.Runtime.CompilerServices;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class TypeArray
{
	private CType[] items;

	public int Size => items.Length;

	public int size => Size;

	[IndexerName("EyeTim")]
	public CType this[int i] => items[i];

	public int Count => items.Length;

	public TypeArray(CType[] types)
	{
		items = types;
		if (items == null)
		{
			items = new CType[0];
		}
	}

	public bool HasErrors()
	{
		return false;
	}

	public CType Item(int i)
	{
		return items[i];
	}

	public TypeParameterType ItemAsTypeParameterType(int i)
	{
		return items[i].AsTypeParameterType();
	}

	public CType[] ToArray()
	{
		return items.ToArray();
	}

	public void CopyItems(int i, int c, CType[] dest)
	{
		for (int j = 0; j < c; j++)
		{
			dest[j] = items[i + j];
		}
	}
}
