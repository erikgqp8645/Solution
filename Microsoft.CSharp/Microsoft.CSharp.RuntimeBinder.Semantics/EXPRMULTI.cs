namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class EXPRMULTI : EXPR
{
	public EXPR Left;

	public EXPR Operator;

	public EXPR GetLeft()
	{
		return Left;
	}

	public void SetLeft(EXPR value)
	{
		Left = value;
	}

	public EXPR GetOperator()
	{
		return Operator;
	}

	public void SetOperator(EXPR value)
	{
		Operator = value;
	}
}
