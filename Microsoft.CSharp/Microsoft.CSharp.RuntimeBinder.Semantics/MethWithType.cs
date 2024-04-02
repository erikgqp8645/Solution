namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class MethWithType : MethPropWithType
{
	public MethWithType()
	{
	}

	public MethWithType(MethodSymbol meth, AggregateType ats)
	{
		Set(meth, ats);
	}
}
