namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class EXPRCAST : EXPR
{
	public EXPR Argument;

	public EXPRTYPEORNAMESPACE DestinationType;

	public EXPR GetArgument()
	{
		return Argument;
	}

	public void SetArgument(EXPR expr)
	{
		Argument = expr;
	}

	public EXPRTYPEORNAMESPACE GetDestinationType()
	{
		return DestinationType;
	}

	public void SetDestinationType(EXPRTYPEORNAMESPACE expr)
	{
		DestinationType = expr;
	}

	public bool IsBoxingCast()
	{
		return (flags & (EXPRFLAG)34) != 0;
	}
}
