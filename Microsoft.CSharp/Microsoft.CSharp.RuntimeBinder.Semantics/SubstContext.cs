namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class SubstContext
{
	public CType[] prgtypeCls;

	public int ctypeCls;

	public CType[] prgtypeMeth;

	public int ctypeMeth;

	public SubstTypeFlags grfst;

	public SubstContext(TypeArray typeArgsCls, TypeArray typeArgsMeth, SubstTypeFlags grfst)
	{
		Init(typeArgsCls, typeArgsMeth, grfst);
	}

	public SubstContext(AggregateType type)
		: this(type, null, SubstTypeFlags.NormNone)
	{
	}

	public SubstContext(AggregateType type, TypeArray typeArgsMeth)
		: this(type, typeArgsMeth, SubstTypeFlags.NormNone)
	{
	}

	public SubstContext(AggregateType type, TypeArray typeArgsMeth, SubstTypeFlags grfst)
	{
		Init(type?.GetTypeArgsAll(), typeArgsMeth, grfst);
	}

	public SubstContext(CType[] prgtypeCls, int ctypeCls, CType[] prgtypeMeth, int ctypeMeth)
		: this(prgtypeCls, ctypeCls, prgtypeMeth, ctypeMeth, SubstTypeFlags.NormNone)
	{
	}

	public SubstContext(CType[] prgtypeCls, int ctypeCls, CType[] prgtypeMeth, int ctypeMeth, SubstTypeFlags grfst)
	{
		this.prgtypeCls = prgtypeCls;
		this.ctypeCls = ctypeCls;
		this.prgtypeMeth = prgtypeMeth;
		this.ctypeMeth = ctypeMeth;
		this.grfst = grfst;
	}

	public bool FNop()
	{
		if (ctypeCls == 0 && ctypeMeth == 0)
		{
			return (grfst & SubstTypeFlags.NormAll) == 0;
		}
		return false;
	}

	public void Init(TypeArray typeArgsCls, TypeArray typeArgsMeth, SubstTypeFlags grfst)
	{
		if (typeArgsCls != null)
		{
			ctypeCls = typeArgsCls.size;
			prgtypeCls = typeArgsCls.ToArray();
		}
		else
		{
			ctypeCls = 0;
			prgtypeCls = null;
		}
		if (typeArgsMeth != null)
		{
			ctypeMeth = typeArgsMeth.size;
			prgtypeMeth = typeArgsMeth.ToArray();
		}
		else
		{
			ctypeMeth = 0;
			prgtypeMeth = null;
		}
		this.grfst = grfst;
	}
}
