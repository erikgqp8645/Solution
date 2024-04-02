namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class PropWithType : MethPropWithType
{
	public PropWithType()
	{
	}

	public PropWithType(PropertySymbol prop, AggregateType ats)
	{
		Set(prop, ats);
	}

	public PropWithType(SymWithType swt)
	{
		Set(swt.Sym as PropertySymbol, swt.Ats);
	}
}
