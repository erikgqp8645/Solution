using Microsoft.CSharp.RuntimeBinder.Semantics;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Errors;

internal class ErrArgRef : ErrArg
{
	public ErrArgRef()
	{
	}

	public ErrArgRef(int n)
		: base(n)
	{
	}

	public ErrArgRef(Name name)
		: base(name)
	{
		eaf = ErrArgFlags.Ref;
	}

	public ErrArgRef(string psz)
		: base(psz)
	{
		eaf = ErrArgFlags.Ref;
	}

	public ErrArgRef(Symbol sym)
		: base(sym)
	{
		eaf = ErrArgFlags.Ref;
	}

	public ErrArgRef(CType pType)
		: base(pType)
	{
		eaf = ErrArgFlags.Ref;
	}

	public ErrArgRef(SymWithType swt)
		: base(swt)
	{
		eaf = ErrArgFlags.Ref;
	}

	public ErrArgRef(MethPropWithInst mpwi)
		: base(mpwi)
	{
		eaf = ErrArgFlags.Ref;
	}

	public ErrArgRef(CType pType, ErrArgFlags eaf)
		: base(pType)
	{
		base.eaf = eaf | ErrArgFlags.Ref;
	}

	public static implicit operator ErrArgRef(string s)
	{
		return new ErrArgRef(s);
	}

	public static implicit operator ErrArgRef(Name name)
	{
		return new ErrArgRef(name);
	}

	public static implicit operator ErrArgRef(int n)
	{
		return new ErrArgRef(n);
	}

	public static implicit operator ErrArgRef(Symbol sym)
	{
		return new ErrArgRef(sym);
	}

	public static implicit operator ErrArgRef(CType type)
	{
		return new ErrArgRef(type);
	}

	public static implicit operator ErrArgRef(SymWithType swt)
	{
		return new ErrArgRef(swt);
	}

	public static implicit operator ErrArgRef(MethPropWithInst mpwi)
	{
		return new ErrArgRef(mpwi);
	}
}
