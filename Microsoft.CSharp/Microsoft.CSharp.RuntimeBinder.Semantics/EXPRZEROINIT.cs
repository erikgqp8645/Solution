namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class EXPRZEROINIT : EXPR
{
	public EXPR OptionalArgument;

	public EXPR OptionalConstructorCall;

	public bool IsConstructor;
}
