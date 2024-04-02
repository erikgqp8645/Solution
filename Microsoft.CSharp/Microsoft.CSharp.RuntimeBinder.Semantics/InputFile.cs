using System.Collections.Generic;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class InputFile : FileRecord
{
	private HashSet<KAID> bsetFilter;

	private KAID aid;

	public bool isSource;

	public InputFile()
	{
		bsetFilter = new HashSet<KAID>();
	}

	public void SetAssemblyID(KAID aid)
	{
		this.aid = aid;
		bsetFilter.Add(aid);
		if (aid == KAID.kaidThisAssembly)
		{
			bsetFilter.Add(KAID.kaidGlobal);
		}
	}

	public void AddToAlias(KAID aid)
	{
		bsetFilter.Add(aid);
	}

	public void UnionAliasFilter(ref HashSet<KAID> bsetDst)
	{
		bsetDst.UnionWith(bsetFilter);
	}

	public KAID GetAssemblyID()
	{
		return aid;
	}

	public bool InAlias(KAID aid)
	{
		return bsetFilter.Contains(aid);
	}
}
