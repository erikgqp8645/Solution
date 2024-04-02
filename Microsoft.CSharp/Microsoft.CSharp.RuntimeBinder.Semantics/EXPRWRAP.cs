namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class EXPRWRAP : EXPR
{
	public EXPR OptionalExpression;

	public EXPR GetOptionalExpression()
	{
		return OptionalExpression;
	}

	public void SetOptionalExpression(EXPR value)
	{
		OptionalExpression = value;
	}
}
