namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class EXPRUNARYOP : EXPR
{
	public EXPR Child;

	public EXPR OptionalUserDefinedCall;

	public MethWithInst predefinedMethodToCall;

	public MethPropWithInst UserDefinedCallMethod;
}
