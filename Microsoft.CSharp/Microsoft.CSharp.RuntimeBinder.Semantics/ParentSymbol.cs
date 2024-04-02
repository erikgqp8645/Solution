namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class ParentSymbol : Symbol
{
	public Symbol firstChild;

	private Symbol lastChild;

	public void AddToChildList(Symbol sym)
	{
		if (lastChild == null)
		{
			firstChild = (lastChild = sym);
		}
		else
		{
			lastChild.nextChild = sym;
			lastChild = sym;
			sym.nextChild = null;
		}
		sym.parent = this;
	}
}
