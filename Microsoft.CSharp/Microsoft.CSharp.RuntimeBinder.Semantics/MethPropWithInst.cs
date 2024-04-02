namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class MethPropWithInst : MethPropWithType
{
	public TypeArray TypeArgs { get; private set; }

	public MethPropWithInst()
	{
		Set(null, null, null);
	}

	public MethPropWithInst(MethodOrPropertySymbol mps, AggregateType ats)
		: this(mps, ats, null)
	{
	}

	public MethPropWithInst(MethodOrPropertySymbol mps, AggregateType ats, TypeArray typeArgs)
	{
		Set(mps, ats, typeArgs);
	}

	public override void Clear()
	{
		base.Clear();
		TypeArgs = null;
	}

	public void Set(MethodOrPropertySymbol mps, AggregateType ats, TypeArray typeArgs)
	{
		if (mps == null)
		{
			ats = null;
			typeArgs = null;
		}
		Set(mps, ats);
		TypeArgs = typeArgs;
	}
}
