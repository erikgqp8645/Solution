using System.Reflection;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class AggregateDeclaration : Declaration
{
	public AggregateSymbol Agg()
	{
		return bag.AsAggregateSymbol();
	}

	public new InputFile getInputFile()
	{
		return null;
	}

	public new Assembly GetAssembly()
	{
		return Agg().AssociatedAssembly;
	}
}
