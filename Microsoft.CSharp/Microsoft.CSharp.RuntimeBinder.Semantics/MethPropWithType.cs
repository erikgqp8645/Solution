namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class MethPropWithType : SymWithType
{
	public MethPropWithType()
	{
	}

	public MethPropWithType(MethodOrPropertySymbol mps, AggregateType ats)
	{
		Set(mps, ats);
	}
}
