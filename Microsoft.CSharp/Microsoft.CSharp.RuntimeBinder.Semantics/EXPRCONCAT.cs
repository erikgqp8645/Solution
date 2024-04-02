namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class EXPRCONCAT : EXPR
{
	public EXPR FirstArgument;

	public EXPR SecondArgument;

	public EXPR GetFirstArgument()
	{
		return FirstArgument;
	}

	public void SetFirstArgument(EXPR value)
	{
		FirstArgument = value;
	}

	public EXPR GetSecondArgument()
	{
		return SecondArgument;
	}

	public void SetSecondArgument(EXPR value)
	{
		SecondArgument = value;
	}
}
