using Microsoft.CSharp.RuntimeBinder.Errors;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal abstract class CSemanticChecker
{
	public abstract SymbolLoader SymbolLoader { get; }

	private ErrorHandling ErrorContext => SymbolLoader.ErrorContext;

	public bool CheckForStaticClass(Symbol symCtx, CType CType, ErrorCode err)
	{
		if (!CType.isStaticClass())
		{
			return false;
		}
		ReportStaticClassError(symCtx, CType, err);
		return true;
	}

	public virtual ACCESSERROR CheckAccess2(Symbol symCheck, AggregateType atsCheck, Symbol symWhere, CType typeThru)
	{
		ACCESSERROR aCCESSERROR = CheckAccessCore(symCheck, atsCheck, symWhere, typeThru);
		if (ACCESSERROR.ACCESSERROR_NOERROR != aCCESSERROR)
		{
			return aCCESSERROR;
		}
		CType cType = symCheck.getType();
		if (cType == null)
		{
			return ACCESSERROR.ACCESSERROR_NOERROR;
		}
		if (atsCheck.getAggregate().IsSource())
		{
			return ACCESSERROR.ACCESSERROR_NOERROR;
		}
		if (atsCheck.GetTypeArgsAll().size > 0)
		{
			cType = SymbolLoader.GetTypeManager().SubstType(cType, atsCheck);
		}
		if (!CheckTypeAccess(cType, symWhere))
		{
			return ACCESSERROR.ACCESSERROR_NOACCESS;
		}
		return ACCESSERROR.ACCESSERROR_NOERROR;
	}

	public virtual bool CheckTypeAccess(CType type, Symbol symWhere)
	{
		type = type.GetNakedType(fStripNub: true);
		if (!type.IsAggregateType())
		{
			return true;
		}
		for (AggregateType aggregateType = type.AsAggregateType(); aggregateType != null; aggregateType = aggregateType.outerType)
		{
			if (ACCESSERROR.ACCESSERROR_NOERROR != CheckAccessCore(aggregateType.GetOwningAggregate(), aggregateType.outerType, symWhere, null))
			{
				return false;
			}
		}
		TypeArray typeArgsAll = type.AsAggregateType().GetTypeArgsAll();
		for (int i = 0; i < typeArgsAll.size; i++)
		{
			if (!CheckTypeAccess(typeArgsAll.Item(i), symWhere))
			{
				return false;
			}
		}
		return true;
	}

	public void ReportStaticClassError(Symbol symCtx, CType CType, ErrorCode err)
	{
		if (symCtx != null)
		{
			ErrorContext.Error(err, CType, new ErrArgRef(symCtx));
		}
		else
		{
			ErrorContext.Error(err, CType);
		}
	}

	public abstract SymbolLoader GetSymbolLoader();

	public ErrorHandling GetErrorContext()
	{
		return ErrorContext;
	}

	public NameManager GetNameManager()
	{
		return SymbolLoader.GetNameManager();
	}

	public TypeManager GetTypeManager()
	{
		return SymbolLoader.GetTypeManager();
	}

	public BSYMMGR getBSymmgr()
	{
		return SymbolLoader.getBSymmgr();
	}

	public SymFactory GetGlobalSymbolFactory()
	{
		return SymbolLoader.GetGlobalSymbolFactory();
	}

	public MiscSymFactory GetGlobalMiscSymFactory()
	{
		return SymbolLoader.GetGlobalMiscSymFactory();
	}

	public PredefinedTypes getPredefTypes()
	{
		return SymbolLoader.getPredefTypes();
	}

	protected ACCESSERROR CheckAccessCore(Symbol symCheck, AggregateType atsCheck, Symbol symWhere, CType typeThru)
	{
		switch (symCheck.GetAccess())
		{
		default:
			throw Error.InternalCompilerError();
		case ACCESS.ACC_UNKNOWN:
			return ACCESSERROR.ACCESSERROR_NOACCESS;
		case ACCESS.ACC_PUBLIC:
			return ACCESSERROR.ACCESSERROR_NOERROR;
		case ACCESS.ACC_PRIVATE:
		case ACCESS.ACC_PROTECTED:
			if (symWhere == null)
			{
				return ACCESSERROR.ACCESSERROR_NOACCESS;
			}
			break;
		case ACCESS.ACC_INTERNAL:
		case ACCESS.ACC_INTERNALPROTECTED:
			if (symWhere == null)
			{
				return ACCESSERROR.ACCESSERROR_NOACCESS;
			}
			if (symWhere.SameAssemOrFriend(symCheck))
			{
				return ACCESSERROR.ACCESSERROR_NOERROR;
			}
			if (symCheck.GetAccess() == ACCESS.ACC_INTERNAL)
			{
				return ACCESSERROR.ACCESSERROR_NOACCESS;
			}
			break;
		}
		AggregateSymbol aggregateSymbol = symCheck.parent.AsAggregateSymbol();
		AggregateSymbol aggregateSymbol2 = null;
		for (Symbol symbol = symWhere; symbol != null; symbol = symbol.parent)
		{
			if (symbol.IsAggregateSymbol())
			{
				aggregateSymbol2 = symbol.AsAggregateSymbol();
				break;
			}
			if (symbol.IsAggregateDeclaration())
			{
				aggregateSymbol2 = symbol.AsAggregateDeclaration().Agg();
				break;
			}
		}
		if (aggregateSymbol2 == null)
		{
			return ACCESSERROR.ACCESSERROR_NOACCESS;
		}
		for (AggregateSymbol aggregateSymbol3 = aggregateSymbol2; aggregateSymbol3 != null; aggregateSymbol3 = aggregateSymbol3.GetOuterAgg())
		{
			if (aggregateSymbol3 == aggregateSymbol)
			{
				return ACCESSERROR.ACCESSERROR_NOERROR;
			}
		}
		if (symCheck.GetAccess() == ACCESS.ACC_PRIVATE)
		{
			return ACCESSERROR.ACCESSERROR_NOACCESS;
		}
		AggregateType aggregateType = null;
		if (typeThru != null && !symCheck.isStatic)
		{
			aggregateType = SymbolLoader.GetAggTypeSym(typeThru);
		}
		bool flag = false;
		for (AggregateSymbol aggregateSymbol4 = aggregateSymbol2; aggregateSymbol4 != null; aggregateSymbol4 = aggregateSymbol4.GetOuterAgg())
		{
			if (aggregateSymbol4.FindBaseAgg(aggregateSymbol))
			{
				flag = true;
				if (aggregateType == null || aggregateType.getAggregate().FindBaseAgg(aggregateSymbol4))
				{
					return ACCESSERROR.ACCESSERROR_NOERROR;
				}
			}
		}
		if (!flag)
		{
			return ACCESSERROR.ACCESSERROR_NOACCESS;
		}
		if (aggregateType != null)
		{
			return ACCESSERROR.ACCESSERROR_NOACCESSTHRU;
		}
		return ACCESSERROR.ACCESSERROR_NOACCESS;
	}

	public bool CheckBogus(Symbol sym)
	{
		if (sym == null)
		{
			return false;
		}
		if (!sym.hasBogus())
		{
			bool flag = sym.computeCurrentBogusState();
			if (flag)
			{
				sym.setBogus(flag);
			}
		}
		if (sym.hasBogus())
		{
			return sym.checkBogus();
		}
		return false;
	}

	public bool CheckBogus(CType pType)
	{
		if (pType == null)
		{
			return false;
		}
		if (!pType.hasBogus())
		{
			bool flag = pType.computeCurrentBogusState();
			if (flag)
			{
				pType.setBogus(flag);
			}
		}
		if (pType.hasBogus())
		{
			return pType.checkBogus();
		}
		return false;
	}

	public void ReportAccessError(SymWithType swtBad, Symbol symWhere, CType typeQual)
	{
		if (CheckAccess2(swtBad.Sym, swtBad.GetType(), symWhere, typeQual) == ACCESSERROR.ACCESSERROR_NOACCESSTHRU)
		{
			ErrorContext.Error(ErrorCode.ERR_BadProtectedAccess, swtBad, typeQual, symWhere);
		}
		else
		{
			ErrorContext.ErrorRef(ErrorCode.ERR_BadAccess, swtBad);
		}
	}

	public bool CheckAccess(Symbol symCheck, AggregateType atsCheck, Symbol symWhere, CType typeThru)
	{
		return CheckAccess2(symCheck, atsCheck, symWhere, typeThru) == ACCESSERROR.ACCESSERROR_NOERROR;
	}
}
