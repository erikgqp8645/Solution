namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class EXPRLIST : EXPR
{
	public EXPR OptionalElement;

	public EXPR OptionalNextListNode;

	public EXPR GetOptionalElement()
	{
		return OptionalElement;
	}

	public void SetOptionalElement(EXPR value)
	{
		OptionalElement = value;
	}

	public EXPR GetOptionalNextListNode()
	{
		return OptionalNextListNode;
	}

	public void SetOptionalNextListNode(EXPR value)
	{
		OptionalNextListNode = value;
	}
}
