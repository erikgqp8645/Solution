namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class FieldWithType : SymWithType
{
	public FieldWithType()
	{
	}

	public FieldWithType(FieldSymbol field, AggregateType ats)
	{
		Set(field, ats);
	}
}
