namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class EXPRMULTIGET : EXPR
{
	public EXPRMULTI OptionalMulti;

	public EXPRMULTI GetOptionalMulti()
	{
		return OptionalMulti;
	}

	public void SetOptionalMulti(EXPRMULTI value)
	{
		OptionalMulti = value;
	}
}
