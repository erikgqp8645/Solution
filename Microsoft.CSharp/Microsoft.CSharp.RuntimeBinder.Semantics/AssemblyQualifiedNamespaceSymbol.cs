namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class AssemblyQualifiedNamespaceSymbol : ParentSymbol, ITypeOrNamespace
{
	public bool IsType()
	{
		return false;
	}

	public bool IsNamespace()
	{
		return true;
	}

	public AssemblyQualifiedNamespaceSymbol AsNamespace()
	{
		return this;
	}

	public CType AsType()
	{
		return null;
	}

	public NamespaceSymbol GetNS()
	{
		return parent.AsNamespaceSymbol();
	}
}
