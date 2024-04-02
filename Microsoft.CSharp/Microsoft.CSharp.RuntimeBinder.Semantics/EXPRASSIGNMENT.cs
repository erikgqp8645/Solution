namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class EXPRASSIGNMENT : EXPR
{
	private EXPR LHS;

	private EXPR RHS;

	public EXPR GetLHS()
	{
		return LHS;
	}

	public void SetLHS(EXPR value)
	{
		LHS = value;
	}

	public EXPR GetRHS()
	{
		return RHS;
	}

	public void SetRHS(EXPR value)
	{
		RHS = value;
	}
}
