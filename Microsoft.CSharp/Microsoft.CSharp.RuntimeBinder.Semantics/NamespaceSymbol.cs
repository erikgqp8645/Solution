using System.Collections.Generic;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class NamespaceSymbol : NamespaceOrAggregateSymbol
{
	private HashSet<KAID> bsetFilter;

	public NamespaceSymbol()
	{
		bsetFilter = new HashSet<KAID>();
	}

	public bool InAlias(KAID aid)
	{
		return bsetFilter.Contains(aid);
	}

	public void DeclAdded(NamespaceDeclaration decl)
	{
		InputFile inputFile = decl.getInputFile();
		if (inputFile.isSource)
		{
			bsetFilter.Add(KAID.kaidGlobal);
			bsetFilter.Add(KAID.kaidThisAssembly);
		}
		else
		{
			inputFile.UnionAliasFilter(ref bsetFilter);
		}
	}

	public void AddAid(KAID aid)
	{
		if (aid == KAID.kaidThisAssembly)
		{
			bsetFilter.Add(KAID.kaidGlobal);
		}
		bsetFilter.Add(aid);
	}
}
