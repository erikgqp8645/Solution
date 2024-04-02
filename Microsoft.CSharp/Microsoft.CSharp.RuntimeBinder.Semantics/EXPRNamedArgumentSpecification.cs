using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class EXPRNamedArgumentSpecification : EXPR
{
	public Name Name;

	public EXPR Value;
}
