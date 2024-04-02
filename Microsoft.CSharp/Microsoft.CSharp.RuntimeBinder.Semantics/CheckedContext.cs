namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class CheckedContext : BindingContext
{
	public static CheckedContext CreateInstance(BindingContext parentCtx, bool checkedNormal, bool checkedConstant)
	{
		return new CheckedContext(parentCtx, checkedNormal, checkedConstant);
	}

	protected CheckedContext(BindingContext parentCtx, bool checkedNormal, bool checkedConstant)
		: base(parentCtx)
	{
		base.CheckedConstant = checkedConstant;
		base.CheckedNormal = checkedNormal;
	}
}
