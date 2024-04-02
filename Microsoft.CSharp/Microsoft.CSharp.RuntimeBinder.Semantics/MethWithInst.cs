namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class MethWithInst : MethPropWithInst
{
	public MethWithInst()
	{
	}

	public MethWithInst(MethodSymbol meth, AggregateType ats)
		: this(meth, ats, null)
	{
	}

	public MethWithInst(MethodSymbol meth, AggregateType ats, TypeArray typeArgs)
	{
		Set(meth, ats, typeArgs);
	}

	public MethWithInst(MethPropWithInst mpwi)
	{
		Set(mpwi.Sym.AsMethodSymbol(), mpwi.Ats, mpwi.TypeArgs);
	}
}
