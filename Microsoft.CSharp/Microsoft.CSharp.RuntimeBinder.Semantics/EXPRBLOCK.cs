namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class EXPRBLOCK : EXPRSTMT
{
	private EXPRSTMT OptionalStatements;

	public Scope OptionalScopeSymbol;

	public EXPRSTMT GetOptionalStatements()
	{
		return OptionalStatements;
	}

	public void SetOptionalStatements(EXPRSTMT value)
	{
		OptionalStatements = value;
	}
}
