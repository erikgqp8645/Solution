using System.Collections.Generic;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class SYMTBL
{
	private sealed class Key
	{
		private readonly Name name;

		private readonly ParentSymbol parent;

		public Key(Name name, ParentSymbol parent)
		{
			this.name = name;
			this.parent = parent;
		}

		public override bool Equals(object obj)
		{
			if (obj is Key key && name.Equals(key.name))
			{
				return parent.Equals(key.parent);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return name.GetHashCode() ^ parent.GetHashCode();
		}
	}

	private Dictionary<Key, Symbol> dictionary;

	public SYMTBL()
	{
		dictionary = new Dictionary<Key, Symbol>();
	}

	public Symbol LookupSym(Name name, ParentSymbol parent, symbmask_t kindmask)
	{
		Key key = new Key(name, parent);
		if (dictionary.TryGetValue(key, out var value))
		{
			return FindCorrectKind(value, kindmask);
		}
		return null;
	}

	public void InsertChild(ParentSymbol parent, Symbol child)
	{
		child.parent = parent;
		InsertChildNoGrow(child);
	}

	private void InsertChildNoGrow(Symbol child)
	{
		Key key = new Key(child.name, child.parent);
		if (dictionary.TryGetValue(key, out var value))
		{
			while (value != null && value.nextSameName != null)
			{
				value = value.nextSameName;
			}
			value.nextSameName = child;
		}
		else
		{
			dictionary.Add(key, child);
		}
	}

	private static Symbol FindCorrectKind(Symbol sym, symbmask_t kindmask)
	{
		do
		{
			if ((kindmask & sym.mask()) != ~symbmask_t.MASK_ALL)
			{
				return sym;
			}
			sym = sym.nextSameName;
		}
		while (sym != null);
		return null;
	}
}
