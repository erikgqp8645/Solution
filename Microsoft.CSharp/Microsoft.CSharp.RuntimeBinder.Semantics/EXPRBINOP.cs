namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class EXPRBINOP : EXPR
{
	private EXPR OptionalLeftChild;

	private EXPR OptionalRightChild;

	private EXPR OptionalUserDefinedCall;

	public MethWithInst predefinedMethodToCall;

	public bool isLifted;

	private MethPropWithInst UserDefinedCallMethod;

	public EXPR GetOptionalLeftChild()
	{
		return OptionalLeftChild;
	}

	public void SetOptionalLeftChild(EXPR value)
	{
		OptionalLeftChild = value;
	}

	public EXPR GetOptionalRightChild()
	{
		return OptionalRightChild;
	}

	public void SetOptionalRightChild(EXPR value)
	{
		OptionalRightChild = value;
	}

	public EXPR GetOptionalUserDefinedCall()
	{
		return OptionalUserDefinedCall;
	}

	public void SetOptionalUserDefinedCall(EXPR value)
	{
		OptionalUserDefinedCall = value;
	}

	public MethPropWithInst GetUserDefinedCallMethod()
	{
		return UserDefinedCallMethod;
	}

	public void SetUserDefinedCallMethod(MethPropWithInst value)
	{
		UserDefinedCallMethod = value;
	}
}
