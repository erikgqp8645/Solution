namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class EXPRQUESTIONMARK : EXPR
{
	public EXPR TestExpression;

	public EXPRBINOP Consequence;

	public EXPR GetTestExpression()
	{
		return TestExpression;
	}

	public void SetTestExpression(EXPR value)
	{
		TestExpression = value;
	}

	public EXPRBINOP GetConsequence()
	{
		return Consequence;
	}

	public void SetConsequence(EXPRBINOP value)
	{
		Consequence = value;
	}
}
