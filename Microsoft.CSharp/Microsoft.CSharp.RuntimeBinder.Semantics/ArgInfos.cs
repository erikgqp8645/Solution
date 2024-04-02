using System.Collections.Generic;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class ArgInfos
{
	public int carg;

	public TypeArray types;

	public bool fHasExprs;

	public List<EXPR> prgexpr;
}
