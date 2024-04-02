namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class EXPRFIELD : EXPR
{
	public EXPR OptionalObject;

	public FieldWithType fwt;

	public EXPR GetOptionalObject()
	{
		return OptionalObject;
	}

	public void SetOptionalObject(EXPR value)
	{
		OptionalObject = value;
	}
}
