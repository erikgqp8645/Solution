namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class EXPRFIELDINFO : EXPR
{
	private FieldSymbol field;

	private AggregateType fieldType;

	public FieldSymbol Field()
	{
		return field;
	}

	public AggregateType FieldType()
	{
		return fieldType;
	}

	public void Init(FieldSymbol f, AggregateType ft)
	{
		field = f;
		fieldType = ft;
	}
}
