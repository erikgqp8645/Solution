namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal abstract class NamespaceOrAggregateSymbol : ParentSymbol
{
	private Declaration declFirst;

	private Declaration declLast;

	public NamespaceOrAggregateSymbol()
	{
	}

	public Declaration DeclFirst()
	{
		return declFirst;
	}

	public void AddDecl(Declaration decl)
	{
		if (declLast == null)
		{
			declFirst = (declLast = decl);
		}
		else
		{
			declLast.declNext = decl;
			declLast = decl;
		}
		decl.declNext = null;
		decl.bag = this;
		if (decl.IsNamespaceDeclaration())
		{
			decl.AsNamespaceDeclaration().Bag().DeclAdded(decl.AsNamespaceDeclaration());
		}
	}
}
