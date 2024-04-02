namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class LocalVariableSymbol : VariableSymbol
{
	public EXPRWRAP wrap;

	public bool isThis;

	public bool fUsedInAnonMeth;

	public void SetType(CType pType)
	{
		type = pType;
	}

	public new CType GetType()
	{
		return type;
	}
}
