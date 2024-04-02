namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class EXPRARRINIT : EXPR
{
	private EXPR OptionalArguments;

	private EXPR OptionalArgumentDimensions;

	public int[] dimSizes;

	public int dimSize;

	public bool GeneratedForParamArray;

	public EXPR GetOptionalArguments()
	{
		return OptionalArguments;
	}

	public void SetOptionalArguments(EXPR value)
	{
		OptionalArguments = value;
	}

	public EXPR GetOptionalArgumentDimensions()
	{
		return OptionalArgumentDimensions;
	}

	public void SetOptionalArgumentDimensions(EXPR value)
	{
		OptionalArgumentDimensions = value;
	}
}
