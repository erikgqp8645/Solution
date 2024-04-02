namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class EXPRBOUNDLAMBDA : EXPR
{
	public EXPRBLOCK OptionalBody;

	private Scope argumentScope;

	public void Initialize(Scope argScope)
	{
		argumentScope = argScope;
	}

	public AggregateType DelegateType()
	{
		return type.AsAggregateType();
	}

	public Scope ArgumentScope()
	{
		return argumentScope;
	}
}
