using System.Collections.Generic;
using Microsoft.CSharp.RuntimeBinder.Errors;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class BindingContext
{
	public Declaration m_pParentDecl;

	protected ExprFactory m_ExprFactory;

	protected OutputContext m_outputContext;

	protected NameGenerator m_pNameGenerator;

	protected InputFile m_pInputFile;

	protected AggregateSymbol m_pContainingAgg;

	protected CType m_pCurrentSwitchType;

	protected FieldSymbol m_pOriginalConstantField;

	protected FieldSymbol m_pCurrentFieldSymbol;

	protected LocalVariableSymbol m_pImplicitlyTypedLocal;

	protected Scope m_pOuterScope;

	protected Scope m_pFinallyScope;

	protected Scope m_pTryScope;

	protected Scope m_pCatchScope;

	protected Scope m_pCurrentScope;

	protected Scope m_pSwitchScope;

	protected EXPRBLOCK m_pCurrentBlock;

	protected List<EXPRBOUNDLAMBDA> m_ppamis;

	protected EXPRBOUNDLAMBDA m_pamiCurrent;

	protected UNSAFESTATES m_UnsafeState;

	protected int m_FinallyNestingCount;

	protected bool m_bInsideTryOfCatch;

	protected bool m_bInFieldInitializer;

	protected bool m_bInBaseConstructorCall;

	protected bool m_bAllowUnsafeBlocks;

	protected bool m_bIsOptimizingSwitchAndArrayInit;

	protected bool m_bShowReachability;

	protected bool m_bWrapNonExceptionThrows;

	protected bool m_bInRefactoring;

	protected bool m_bInAttribute;

	protected bool m_bflushLocalVariableTypesForEachStatement;

	protected bool m_bRespectSemanticsAndReportErrors;

	protected CType m_pInitType;

	protected IErrorSink m_returnErrorSink;

	public SymbolLoader SymbolLoader { get; private set; }

	public KAID m_aidExternAliasLookupContext { get; private set; }

	public CSemanticChecker SemanticChecker { get; private set; }

	public bool CheckedNormal { get; set; }

	public bool CheckedConstant { get; set; }

	public static BindingContext CreateInstance(CSemanticChecker pSemanticChecker, ExprFactory exprFactory, OutputContext outputContext, NameGenerator nameGenerator, bool bflushLocalVariableTypesForEachStatement, bool bAllowUnsafeBlocks, bool bIsOptimizingSwitchAndArrayInit, bool bShowReachability, bool bWrapNonExceptionThrows, bool bInRefactoring, KAID aidLookupContext)
	{
		return new BindingContext(pSemanticChecker, exprFactory, outputContext, nameGenerator, bflushLocalVariableTypesForEachStatement, bAllowUnsafeBlocks, bIsOptimizingSwitchAndArrayInit, bShowReachability, bWrapNonExceptionThrows, bInRefactoring, aidLookupContext);
	}

	protected BindingContext(CSemanticChecker pSemanticChecker, ExprFactory exprFactory, OutputContext outputContext, NameGenerator nameGenerator, bool bflushLocalVariableTypesForEachStatement, bool bAllowUnsafeBlocks, bool bIsOptimizingSwitchAndArrayInit, bool bShowReachability, bool bWrapNonExceptionThrows, bool bInRefactoring, KAID aidLookupContext)
	{
		m_ExprFactory = exprFactory;
		m_outputContext = outputContext;
		m_pNameGenerator = nameGenerator;
		m_pInputFile = null;
		m_pParentDecl = null;
		m_pContainingAgg = null;
		m_pCurrentSwitchType = null;
		m_pOriginalConstantField = null;
		m_pCurrentFieldSymbol = null;
		m_pImplicitlyTypedLocal = null;
		m_pOuterScope = null;
		m_pFinallyScope = null;
		m_pTryScope = null;
		m_pCatchScope = null;
		m_pCurrentScope = null;
		m_pSwitchScope = null;
		m_pCurrentBlock = null;
		m_UnsafeState = UNSAFESTATES.UNSAFESTATES_Unknown;
		m_FinallyNestingCount = 0;
		m_bInsideTryOfCatch = false;
		m_bInFieldInitializer = false;
		m_bInBaseConstructorCall = false;
		m_bAllowUnsafeBlocks = bAllowUnsafeBlocks;
		m_bIsOptimizingSwitchAndArrayInit = bIsOptimizingSwitchAndArrayInit;
		m_bShowReachability = bShowReachability;
		m_bWrapNonExceptionThrows = bWrapNonExceptionThrows;
		m_bInRefactoring = bInRefactoring;
		m_bInAttribute = false;
		m_bRespectSemanticsAndReportErrors = true;
		m_bflushLocalVariableTypesForEachStatement = bflushLocalVariableTypesForEachStatement;
		m_ppamis = null;
		m_pamiCurrent = null;
		m_pInitType = null;
		m_returnErrorSink = null;
		SemanticChecker = pSemanticChecker;
		SymbolLoader = SemanticChecker.GetSymbolLoader();
		m_outputContext.m_pThisPointer = null;
		m_outputContext.m_pCurrentMethodSymbol = null;
		m_aidExternAliasLookupContext = aidLookupContext;
		CheckedNormal = false;
		CheckedConstant = false;
	}

	protected BindingContext(BindingContext parent)
	{
		m_ExprFactory = parent.m_ExprFactory;
		m_outputContext = parent.m_outputContext;
		m_pNameGenerator = parent.m_pNameGenerator;
		m_pInputFile = parent.m_pInputFile;
		m_pParentDecl = parent.m_pParentDecl;
		m_pContainingAgg = parent.m_pContainingAgg;
		m_pCurrentSwitchType = parent.m_pCurrentSwitchType;
		m_pOriginalConstantField = parent.m_pOriginalConstantField;
		m_pCurrentFieldSymbol = parent.m_pCurrentFieldSymbol;
		m_pImplicitlyTypedLocal = parent.m_pImplicitlyTypedLocal;
		m_pOuterScope = parent.m_pOuterScope;
		m_pFinallyScope = parent.m_pFinallyScope;
		m_pTryScope = parent.m_pTryScope;
		m_pCatchScope = parent.m_pCatchScope;
		m_pCurrentScope = parent.m_pCurrentScope;
		m_pSwitchScope = parent.m_pSwitchScope;
		m_pCurrentBlock = parent.m_pCurrentBlock;
		m_ppamis = parent.m_ppamis;
		m_pamiCurrent = parent.m_pamiCurrent;
		m_UnsafeState = parent.m_UnsafeState;
		m_FinallyNestingCount = parent.m_FinallyNestingCount;
		m_bInsideTryOfCatch = parent.m_bInsideTryOfCatch;
		m_bInFieldInitializer = parent.m_bInFieldInitializer;
		m_bInBaseConstructorCall = parent.m_bInBaseConstructorCall;
		CheckedNormal = parent.CheckedNormal;
		CheckedConstant = parent.CheckedConstant;
		m_aidExternAliasLookupContext = parent.m_aidExternAliasLookupContext;
		m_bAllowUnsafeBlocks = parent.m_bAllowUnsafeBlocks;
		m_bIsOptimizingSwitchAndArrayInit = parent.m_bIsOptimizingSwitchAndArrayInit;
		m_bShowReachability = parent.m_bShowReachability;
		m_bWrapNonExceptionThrows = parent.m_bWrapNonExceptionThrows;
		m_bflushLocalVariableTypesForEachStatement = parent.m_bflushLocalVariableTypesForEachStatement;
		m_bInRefactoring = parent.m_bInRefactoring;
		m_bInAttribute = parent.m_bInAttribute;
		m_bRespectSemanticsAndReportErrors = parent.m_bRespectSemanticsAndReportErrors;
		m_pInitType = parent.m_pInitType;
		m_returnErrorSink = parent.m_returnErrorSink;
		SemanticChecker = parent.SemanticChecker;
		SymbolLoader = SemanticChecker.GetSymbolLoader();
	}

	public Declaration ContextForMemberLookup()
	{
		return m_pParentDecl;
	}

	public OutputContext GetOutputContext()
	{
		return m_outputContext;
	}

	public virtual void Dispose()
	{
	}

	public bool InMethod()
	{
		return m_outputContext.m_pCurrentMethodSymbol != null;
	}

	public bool InStaticMethod()
	{
		if (m_outputContext.m_pCurrentMethodSymbol != null)
		{
			return m_outputContext.m_pCurrentMethodSymbol.isStatic;
		}
		return false;
	}

	public bool InConstructor()
	{
		if (m_outputContext.m_pCurrentMethodSymbol != null)
		{
			return m_outputContext.m_pCurrentMethodSymbol.IsConstructor();
		}
		return false;
	}

	public bool InAnonymousMethod()
	{
		return m_pamiCurrent != null;
	}

	public bool InFieldInitializer()
	{
		return m_bInFieldInitializer;
	}

	public bool IsThisPointer(EXPR expr)
	{
		bool flag = expr.isANYLOCAL() && expr.asANYLOCAL().local == m_outputContext.m_pThisPointer;
		bool flag2 = false;
		return flag || flag2;
	}

	public bool RespectReadonly()
	{
		return m_bRespectSemanticsAndReportErrors;
	}

	public bool IsUnsafeContext()
	{
		return m_UnsafeState == UNSAFESTATES.UNSAFESTATES_Unsafe;
	}

	public bool ReportUnsafeErrors()
	{
		if (!m_outputContext.m_bUnsafeErrorGiven)
		{
			return m_bRespectSemanticsAndReportErrors;
		}
		return false;
	}

	public AggregateSymbol ContainingAgg()
	{
		return m_pContainingAgg;
	}

	public LocalVariableSymbol GetThisPointer()
	{
		return m_outputContext.m_pThisPointer;
	}

	public UNSAFESTATES GetUnsafeState()
	{
		return m_UnsafeState;
	}

	public ExprFactory GetExprFactory()
	{
		return m_ExprFactory;
	}
}
