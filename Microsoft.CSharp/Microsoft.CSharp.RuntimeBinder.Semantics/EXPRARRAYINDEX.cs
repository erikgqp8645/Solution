namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class EXPRARRAYINDEX : EXPR
{
	private EXPR Array;

	private EXPR Index;

	public EXPR GetArray()
	{
		return Array;
	}

	public void SetArray(EXPR value)
	{
		Array = value;
	}

	public EXPR GetIndex()
	{
		return Index;
	}

	public void SetIndex(EXPR value)
	{
		Index = value;
	}
}
