using Microsoft.CSharp.RuntimeBinder.Errors;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class CNullable
{
	private SymbolLoader m_pSymbolLoader;

	private ExprFactory m_exprFactory;

	private ErrorHandling m_pErrorContext;

	private SymbolLoader GetSymbolLoader()
	{
		return m_pSymbolLoader;
	}

	private ExprFactory GetExprFactory()
	{
		return m_exprFactory;
	}

	private ErrorHandling GetErrorContext()
	{
		return m_pErrorContext;
	}

	public static bool IsNullableConstructor(EXPR expr)
	{
		if (!expr.isCALL())
		{
			return false;
		}
		EXPRCALL eXPRCALL = expr.asCALL();
		if (eXPRCALL.GetMemberGroup().GetOptionalObject() != null)
		{
			return false;
		}
		return eXPRCALL.mwi.Meth()?.IsNullableConstructor() ?? false;
	}

	public static EXPR StripNullableConstructor(EXPR pExpr)
	{
		while (IsNullableConstructor(pExpr))
		{
			pExpr = pExpr.asCALL().GetOptionalArguments();
		}
		return pExpr;
	}

	public EXPR BindValue(EXPR exprSrc)
	{
		if (IsNullableConstructor(exprSrc))
		{
			return exprSrc.asCALL().GetOptionalArguments();
		}
		CType underlyingType = exprSrc.type.AsNullableType().GetUnderlyingType();
		AggregateType ats = exprSrc.type.AsNullableType().GetAts(GetErrorContext());
		if (ats == null)
		{
			EXPRPROP eXPRPROP = GetExprFactory().CreateProperty(underlyingType, exprSrc);
			eXPRPROP.SetError();
			return eXPRPROP;
		}
		PropertySymbol propertySymbol = GetSymbolLoader().getBSymmgr().propNubValue;
		if (propertySymbol == null)
		{
			propertySymbol = GetSymbolLoader().getPredefinedMembers().GetProperty(PREDEFPROP.PP_G_OPTIONAL_VALUE);
			GetSymbolLoader().getBSymmgr().propNubValue = propertySymbol;
		}
		PropWithType pwtSlot = new PropWithType(propertySymbol, ats);
		MethWithType mwtGet = new MethWithType(propertySymbol?.methGet, ats);
		MethPropWithInst mwi = new MethPropWithInst(propertySymbol, ats);
		EXPRMEMGRP pMemberGroup = GetExprFactory().CreateMemGroup(exprSrc, mwi);
		EXPRPROP eXPRPROP2 = GetExprFactory().CreateProperty(underlyingType, null, null, pMemberGroup, pwtSlot, mwtGet, null);
		if (propertySymbol == null)
		{
			eXPRPROP2.SetError();
		}
		return eXPRPROP2;
	}

	public EXPRCALL BindNew(EXPR pExprSrc)
	{
		NullableType nullable = GetSymbolLoader().GetTypeManager().GetNullable(pExprSrc.type);
		AggregateType ats = nullable.GetAts(GetErrorContext());
		if (ats == null)
		{
			MethWithInst mwi = new MethWithInst(null, null);
			EXPRMEMGRP pMemberGroup = GetExprFactory().CreateMemGroup(pExprSrc, mwi);
			EXPRCALL eXPRCALL = GetExprFactory().CreateCall((EXPRFLAG)0, nullable, null, pMemberGroup, null);
			eXPRCALL.SetError();
			return eXPRCALL;
		}
		MethodSymbol methodSymbol = GetSymbolLoader().getBSymmgr().methNubCtor;
		if (methodSymbol == null)
		{
			methodSymbol = GetSymbolLoader().getPredefinedMembers().GetMethod(PREDEFMETH.PM_G_OPTIONAL_CTOR);
			GetSymbolLoader().getBSymmgr().methNubCtor = methodSymbol;
		}
		MethWithInst methWithInst = new MethWithInst(methodSymbol, ats, BSYMMGR.EmptyTypeArray());
		EXPRMEMGRP pMemberGroup2 = GetExprFactory().CreateMemGroup(null, methWithInst);
		EXPRCALL eXPRCALL2 = GetExprFactory().CreateCall((EXPRFLAG)131088, nullable, pExprSrc, pMemberGroup2, methWithInst);
		if (methodSymbol == null)
		{
			eXPRCALL2.SetError();
		}
		return eXPRCALL2;
	}

	public CNullable(SymbolLoader symbolLoader, ErrorHandling errorContext, ExprFactory exprFactory)
	{
		m_pSymbolLoader = symbolLoader;
		m_pErrorContext = errorContext;
		m_exprFactory = exprFactory;
	}
}
