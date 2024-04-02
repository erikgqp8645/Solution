namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class EXPRRETURN : EXPRSTMT
{
	public EXPR OptionalObject;

	public EXPR GetOptionalObject()
	{
		return OptionalObject;
	}

	public void SetOptionalObject(EXPR value)
	{
		OptionalObject = value;
	}
}
