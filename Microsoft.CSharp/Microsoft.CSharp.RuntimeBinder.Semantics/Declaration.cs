namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class Declaration : ParentSymbol
{
	public NamespaceOrAggregateSymbol bag;

	public Declaration declNext;
}
