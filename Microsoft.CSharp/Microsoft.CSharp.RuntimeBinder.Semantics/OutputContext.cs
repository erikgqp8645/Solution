namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class OutputContext
{
	public LocalVariableSymbol m_pThisPointer;

	public MethodSymbol m_pCurrentMethodSymbol;

	public bool m_bUnsafeErrorGiven;
}
