namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class NamespaceDeclaration : Declaration
{
	public NamespaceSymbol Bag()
	{
		return bag.AsNamespaceSymbol();
	}

	public NamespaceSymbol NameSpace()
	{
		return bag.AsNamespaceSymbol();
	}
}
