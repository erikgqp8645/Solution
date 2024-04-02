using System.Reflection;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class PropertySymbol : MethodOrPropertySymbol
{
	public MethodSymbol methGet;

	public MethodSymbol methSet;

	public PropertyInfo AssociatedPropertyInfo;

	public bool isIndexer()
	{
		return isOperator;
	}

	public IndexerSymbol AsIndexerSymbol()
	{
		return (IndexerSymbol)this;
	}
}
