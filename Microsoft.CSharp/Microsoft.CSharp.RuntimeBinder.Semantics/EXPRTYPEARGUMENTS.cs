namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class EXPRTYPEARGUMENTS : EXPR
{
	public EXPR OptionalElements;

	public EXPR GetOptionalElements()
	{
		return OptionalElements;
	}

	public void SetOptionalElements(EXPR value)
	{
		OptionalElements = value;
	}
}
