using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq.Expressions;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.CSharp.RuntimeBinder.Semantics;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder;

internal class RuntimeBinder
{
	private class ArgumentObject
	{
		internal Type Type;

		internal object Value;

		internal CSharpArgumentInfo Info;
	}

	private static readonly object s_singletonLock = new object();

	private static volatile RuntimeBinder s_instance;

	private SymbolTable m_symbolTable;

	private CSemanticChecker m_semanticChecker;

	private ExprFactory m_exprFactory;

	private OutputContext m_outputContext;

	private NameGenerator m_nameGenerator;

	private BindingContext m_bindingContext;

	private ExpressionBinder m_binder;

	private RuntimeBinderController m_controller;

	private readonly object m_bindLock = new object();

	private SymbolLoader SymbolLoader => m_semanticChecker.GetSymbolLoader();

	public static RuntimeBinder GetInstance()
	{
		if (s_instance == null)
		{
			lock (s_singletonLock)
			{
				if (s_instance == null)
				{
					s_instance = new RuntimeBinder();
				}
			}
		}
		return s_instance;
	}

	public RuntimeBinder()
	{
		Reset();
	}

	private void Reset()
	{
		m_controller = new RuntimeBinderController();
		m_semanticChecker = new LangCompiler(m_controller, new NameManager());
		BSYMMGR bSymmgr = m_semanticChecker.getBSymmgr();
		NameManager nameManager = m_semanticChecker.GetNameManager();
		InputFile inputFile = bSymmgr.GetMiscSymFactory().CreateMDInfile(nameManager.Lookup(""), mdToken.mdtModule);
		inputFile.SetAssemblyID(bSymmgr.AidAlloc(inputFile));
		inputFile.AddToAlias(KAID.kaidThisAssembly);
		inputFile.AddToAlias(KAID.kaidGlobal);
		m_symbolTable = new SymbolTable(bSymmgr.GetSymbolTable(), bSymmgr.GetSymFactory(), nameManager, m_semanticChecker.GetTypeManager(), bSymmgr, m_semanticChecker, inputFile);
		m_semanticChecker.getPredefTypes().Init(m_semanticChecker.GetErrorContext(), m_symbolTable);
		m_semanticChecker.GetTypeManager().InitTypeFactory(m_symbolTable);
		SymbolLoader.getPredefinedMembers().RuntimeBinderSymbolTable = m_symbolTable;
		SymbolLoader.SetSymbolTable(m_symbolTable);
		m_exprFactory = new ExprFactory(m_semanticChecker.GetSymbolLoader().GetGlobalSymbolContext());
		m_outputContext = new OutputContext();
		m_nameGenerator = new NameGenerator();
		m_bindingContext = BindingContext.CreateInstance(m_semanticChecker, m_exprFactory, m_outputContext, m_nameGenerator, bflushLocalVariableTypesForEachStatement: false, bAllowUnsafeBlocks: true, bIsOptimizingSwitchAndArrayInit: false, bShowReachability: false, bWrapNonExceptionThrows: false, bInRefactoring: false, KAID.kaidGlobal);
		m_binder = new ExpressionBinder(m_bindingContext);
	}

	public Expression Bind(DynamicMetaObjectBinder payload, IEnumerable<Expression> parameters, DynamicMetaObject[] args, out DynamicMetaObject deferredBinding)
	{
		lock (m_bindLock)
		{
			try
			{
				return BindCore(payload, parameters, args, out deferredBinding);
			}
			catch (ResetBindException)
			{
				Reset();
				try
				{
					return BindCore(payload, parameters, args, out deferredBinding);
				}
				catch (ResetBindException)
				{
					Reset();
					throw Error.InternalCompilerError();
				}
			}
		}
	}

	private Expression BindCore(DynamicMetaObjectBinder payload, IEnumerable<Expression> parameters, DynamicMetaObject[] args, out DynamicMetaObject deferredBinding)
	{
		if (args.Length < 1)
		{
			throw Error.BindRequireArguments();
		}
		InitializeCallingContext(payload);
		ArgumentObject[] array = CreateArgumentArray(payload, parameters, args);
		ICSharpInvokeOrInvokeMemberBinder iCSharpInvokeOrInvokeMemberBinder = payload as ICSharpInvokeOrInvokeMemberBinder;
		PopulateSymbolTableWithPayloadInformation(payload, array[0].Type, array);
		AddConversionsForArguments(array);
		Dictionary<int, LocalVariableSymbol> dictionary = new Dictionary<int, LocalVariableSymbol>();
		Scope pScope = m_semanticChecker.GetGlobalMiscSymFactory().CreateScope(null);
		PopulateLocalScope(payload, pScope, array, parameters, dictionary);
		DynamicMetaObject deferredBinding2 = null;
		if (DeferBinding(payload, array, args, dictionary, out deferredBinding2))
		{
			deferredBinding = deferredBinding2;
			return null;
		}
		EXPR pResult = DispatchPayload(payload, array, dictionary);
		deferredBinding = null;
		return CreateExpressionTreeFromResult(parameters, array, pScope, pResult);
	}

	private bool DeferBinding(DynamicMetaObjectBinder payload, ArgumentObject[] arguments, DynamicMetaObject[] args, Dictionary<int, LocalVariableSymbol> dictionary, out DynamicMetaObject deferredBinding)
	{
		if (payload is CSharpInvokeMemberBinder)
		{
			ICSharpInvokeOrInvokeMemberBinder iCSharpInvokeOrInvokeMemberBinder = payload as ICSharpInvokeOrInvokeMemberBinder;
			int arity = ((iCSharpInvokeOrInvokeMemberBinder.TypeArguments != null) ? iCSharpInvokeOrInvokeMemberBinder.TypeArguments.Count : 0);
			MemberLookup mem = new MemberLookup();
			EXPR callingObject = CreateCallingObjectForCall(iCSharpInvokeOrInvokeMemberBinder, arguments, dictionary);
			SymWithType symWithType = m_symbolTable.LookupMember(iCSharpInvokeOrInvokeMemberBinder.Name, callingObject, m_bindingContext.ContextForMemberLookup(), arity, mem, (iCSharpInvokeOrInvokeMemberBinder.Flags & CSharpCallFlags.EventHookup) != 0, requireInvocable: true);
			if (symWithType != null && symWithType.Sym.getKind() != SYMKIND.SK_MethodSymbol)
			{
				CSharpGetMemberBinder cSharpGetMemberBinder = new CSharpGetMemberBinder(iCSharpInvokeOrInvokeMemberBinder.Name, resultIndexed: false, iCSharpInvokeOrInvokeMemberBinder.CallingContext, new CSharpArgumentInfo[1] { iCSharpInvokeOrInvokeMemberBinder.ArgumentInfo[0] });
				CSharpArgumentInfo[] array = new CSharpArgumentInfo[iCSharpInvokeOrInvokeMemberBinder.ArgumentInfo.Count];
				iCSharpInvokeOrInvokeMemberBinder.ArgumentInfo.CopyTo(array, 0);
				array[0] = CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null);
				CSharpInvokeBinder cSharpInvokeBinder = new CSharpInvokeBinder(iCSharpInvokeOrInvokeMemberBinder.Flags, iCSharpInvokeOrInvokeMemberBinder.CallingContext, array);
				DynamicMetaObject[] array2 = new DynamicMetaObject[args.Length - 1];
				Array.Copy(args, 1, array2, 0, args.Length - 1);
				deferredBinding = cSharpInvokeBinder.Defer(cSharpGetMemberBinder.Defer(args[0]), array2);
				return true;
			}
		}
		deferredBinding = null;
		return false;
	}

	private void InitializeCallingContext(DynamicMetaObjectBinder payload)
	{
		Type type = null;
		bool flag = false;
		if (payload is ICSharpInvokeOrInvokeMemberBinder)
		{
			type = (payload as ICSharpInvokeOrInvokeMemberBinder).CallingContext;
		}
		else if (payload is CSharpGetMemberBinder)
		{
			type = (payload as CSharpGetMemberBinder).CallingContext;
		}
		else if (payload is CSharpSetMemberBinder)
		{
			CSharpSetMemberBinder cSharpSetMemberBinder = (CSharpSetMemberBinder)payload;
			type = cSharpSetMemberBinder.CallingContext;
			flag = cSharpSetMemberBinder.IsChecked;
		}
		else if (payload is CSharpGetIndexBinder)
		{
			type = (payload as CSharpGetIndexBinder).CallingContext;
		}
		else if (payload is CSharpSetIndexBinder)
		{
			CSharpSetIndexBinder cSharpSetIndexBinder = (CSharpSetIndexBinder)payload;
			type = cSharpSetIndexBinder.CallingContext;
			flag = cSharpSetIndexBinder.IsChecked;
		}
		else if (payload is CSharpUnaryOperationBinder)
		{
			CSharpUnaryOperationBinder cSharpUnaryOperationBinder = (CSharpUnaryOperationBinder)payload;
			type = cSharpUnaryOperationBinder.CallingContext;
			flag = cSharpUnaryOperationBinder.IsChecked;
		}
		else if (payload is CSharpBinaryOperationBinder)
		{
			CSharpBinaryOperationBinder cSharpBinaryOperationBinder = (CSharpBinaryOperationBinder)payload;
			type = cSharpBinaryOperationBinder.CallingContext;
			flag = cSharpBinaryOperationBinder.IsChecked;
		}
		else if (payload is CSharpConvertBinder)
		{
			CSharpConvertBinder cSharpConvertBinder = (CSharpConvertBinder)payload;
			type = cSharpConvertBinder.CallingContext;
			flag = cSharpConvertBinder.IsChecked;
		}
		else if (payload is CSharpIsEventBinder)
		{
			type = (payload as CSharpIsEventBinder).CallingContext;
		}
		if (type != null)
		{
			AggregateSymbol owningAggregate = m_symbolTable.GetCTypeFromType(type).AsAggregateType().GetOwningAggregate();
			m_bindingContext.m_pParentDecl = m_semanticChecker.GetGlobalSymbolFactory().CreateAggregateDecl(owningAggregate, null);
		}
		else
		{
			m_bindingContext.m_pParentDecl = null;
		}
		m_bindingContext.CheckedConstant = flag;
		m_bindingContext.CheckedNormal = flag;
	}

	private Expression CreateExpressionTreeFromResult(IEnumerable<Expression> parameters, ArgumentObject[] arguments, Scope pScope, EXPR pResult)
	{
		EXPRBOUNDLAMBDA expr = GenerateBoundLambda(arguments, pScope, pResult);
		EXPR pExpr = ExpressionTreeRewriter.Rewrite(expr, m_exprFactory, SymbolLoader);
		return ExpressionTreeCallRewriter.Rewrite(SymbolLoader.GetTypeManager(), pExpr, parameters);
	}

	private ArgumentObject[] CreateArgumentArray(DynamicMetaObjectBinder payload, IEnumerable<Expression> parameters, DynamicMetaObject[] args)
	{
		List<ArgumentObject> list = new List<ArgumentObject>();
		Func<DynamicMetaObjectBinder, CSharpArgumentInfo, Expression, DynamicMetaObject, int, Type> func = null;
		Func<DynamicMetaObjectBinder, int, CSharpArgumentInfo> func2 = null;
		if (payload is ICSharpInvokeOrInvokeMemberBinder)
		{
			func2 = (DynamicMetaObjectBinder p, int index) => (p as ICSharpInvokeOrInvokeMemberBinder).ArgumentInfo[index];
		}
		else if (payload is CSharpBinaryOperationBinder)
		{
			func2 = (DynamicMetaObjectBinder p, int index) => (p as CSharpBinaryOperationBinder).ArgumentInfo[index];
		}
		else if (payload is CSharpUnaryOperationBinder)
		{
			func2 = (DynamicMetaObjectBinder p, int index) => (p as CSharpUnaryOperationBinder).ArgumentInfo[index];
		}
		else if (payload is CSharpGetMemberBinder)
		{
			func2 = (DynamicMetaObjectBinder p, int index) => (p as CSharpGetMemberBinder).ArgumentInfo[index];
		}
		else if (payload is CSharpSetMemberBinder)
		{
			func2 = (DynamicMetaObjectBinder p, int index) => (p as CSharpSetMemberBinder).ArgumentInfo[index];
		}
		else if (payload is CSharpGetIndexBinder)
		{
			func2 = (DynamicMetaObjectBinder p, int index) => (p as CSharpGetIndexBinder).ArgumentInfo[index];
		}
		else if (payload is CSharpSetIndexBinder)
		{
			func2 = (DynamicMetaObjectBinder p, int index) => (p as CSharpSetIndexBinder).ArgumentInfo[index];
		}
		else
		{
			if (!(payload is CSharpConvertBinder) && !(payload is CSharpIsEventBinder))
			{
				throw Error.InternalCompilerError();
			}
			func2 = (DynamicMetaObjectBinder p, int index) => CSharpArgumentInfo.None;
		}
		func = delegate(DynamicMetaObjectBinder p, CSharpArgumentInfo argInfo, Expression param, DynamicMetaObject arg, int index)
		{
			Type type = (argInfo.UseCompileTimeType ? param.Type : arg.LimitType);
			if ((argInfo.Flags & (CSharpArgumentInfoFlags.IsRef | CSharpArgumentInfoFlags.IsOut)) != 0)
			{
				if (index != 0 || !IsBinderThatCanHaveRefReceiver(p))
				{
					type = type.MakeByRefType();
				}
			}
			else if (!argInfo.UseCompileTimeType)
			{
				CType cTypeFromType = m_symbolTable.GetCTypeFromType(type);
				if (!m_semanticChecker.GetTypeManager().GetBestAccessibleType(m_semanticChecker, m_bindingContext, cTypeFromType, out var typeDst))
				{
					type = typeof(object);
				}
				type = typeDst.AssociatedSystemType;
			}
			return type;
		};
		int num = 0;
		foreach (Expression parameter in parameters)
		{
			ArgumentObject argumentObject = new ArgumentObject();
			argumentObject.Value = args[num].Value;
			argumentObject.Info = func2(payload, num);
			argumentObject.Type = func(payload, argumentObject.Info, parameter, args[num], num);
			list.Add(argumentObject);
			num++;
		}
		return list.ToArray();
	}

	private bool IsBinderThatCanHaveRefReceiver(DynamicMetaObjectBinder binder)
	{
		if (!(binder is ICSharpInvokeOrInvokeMemberBinder) && !(binder is CSharpSetIndexBinder))
		{
			return binder is CSharpGetIndexBinder;
		}
		return true;
	}

	private void PopulateSymbolTableWithPayloadInformation(DynamicMetaObjectBinder payload, Type callingType, ArgumentObject[] arguments)
	{
		if (payload is ICSharpInvokeOrInvokeMemberBinder iCSharpInvokeOrInvokeMemberBinder)
		{
			Type callingType2;
			if (iCSharpInvokeOrInvokeMemberBinder.StaticCall)
			{
				if (arguments[0].Value == null || !(arguments[0].Value is Type))
				{
					throw Error.InternalCompilerError();
				}
				callingType2 = arguments[0].Value as Type;
			}
			else
			{
				callingType2 = callingType;
			}
			m_symbolTable.PopulateSymbolTableWithName(iCSharpInvokeOrInvokeMemberBinder.Name, iCSharpInvokeOrInvokeMemberBinder.TypeArguments, callingType2);
			if (iCSharpInvokeOrInvokeMemberBinder.Name.StartsWith("set_", StringComparison.Ordinal) || iCSharpInvokeOrInvokeMemberBinder.Name.StartsWith("get_", StringComparison.Ordinal))
			{
				m_symbolTable.PopulateSymbolTableWithName(iCSharpInvokeOrInvokeMemberBinder.Name.Substring(4), iCSharpInvokeOrInvokeMemberBinder.TypeArguments, callingType2);
			}
		}
		else if (payload is CSharpGetMemberBinder cSharpGetMemberBinder)
		{
			m_symbolTable.PopulateSymbolTableWithName(cSharpGetMemberBinder.Name, null, arguments[0].Type);
		}
		else if (payload is CSharpSetMemberBinder cSharpSetMemberBinder)
		{
			m_symbolTable.PopulateSymbolTableWithName(cSharpSetMemberBinder.Name, null, arguments[0].Type);
		}
		else if (payload is CSharpGetIndexBinder || payload is CSharpSetIndexBinder)
		{
			m_symbolTable.PopulateSymbolTableWithName("$Item$", null, arguments[0].Type);
		}
		else if (payload is CSharpBinaryOperationBinder)
		{
			CSharpBinaryOperationBinder cSharpBinaryOperationBinder = payload as CSharpBinaryOperationBinder;
			if (GetCLROperatorName(cSharpBinaryOperationBinder.Operation) == null)
			{
				throw Error.InternalCompilerError();
			}
			m_symbolTable.PopulateSymbolTableWithName(GetCLROperatorName(cSharpBinaryOperationBinder.Operation), null, arguments[0].Type);
			m_symbolTable.PopulateSymbolTableWithName(GetCLROperatorName(cSharpBinaryOperationBinder.Operation), null, arguments[1].Type);
		}
		else if (payload is CSharpUnaryOperationBinder)
		{
			CSharpUnaryOperationBinder cSharpUnaryOperationBinder = payload as CSharpUnaryOperationBinder;
			m_symbolTable.PopulateSymbolTableWithName(GetCLROperatorName(cSharpUnaryOperationBinder.Operation), null, arguments[0].Type);
		}
		else if (payload is CSharpIsEventBinder)
		{
			CSharpIsEventBinder cSharpIsEventBinder = payload as CSharpIsEventBinder;
			m_symbolTable.PopulateSymbolTableWithName(cSharpIsEventBinder.Name, null, arguments[0].Info.IsStaticType ? (arguments[0].Value as Type) : arguments[0].Type);
		}
		else if (!(payload is CSharpConvertBinder))
		{
			throw Error.InternalCompilerError();
		}
	}

	private void AddConversionsForArguments(ArgumentObject[] arguments)
	{
		foreach (ArgumentObject argumentObject in arguments)
		{
			m_symbolTable.AddConversionsForType(argumentObject.Type);
		}
	}

	private EXPR DispatchPayload(DynamicMetaObjectBinder payload, ArgumentObject[] arguments, Dictionary<int, LocalVariableSymbol> dictionary)
	{
		EXPR eXPR = null;
		if (payload is CSharpBinaryOperationBinder)
		{
			return BindBinaryOperation(payload as CSharpBinaryOperationBinder, arguments, dictionary);
		}
		if (payload is CSharpUnaryOperationBinder)
		{
			return BindUnaryOperation(payload as CSharpUnaryOperationBinder, arguments, dictionary);
		}
		if (payload is CSharpSetMemberBinder)
		{
			return BindAssignment(payload as CSharpSetMemberBinder, arguments, dictionary);
		}
		if (payload is CSharpConvertBinder)
		{
			CSharpConvertBinder cSharpConvertBinder = payload as CSharpConvertBinder;
			return cSharpConvertBinder.ConversionKind switch
			{
				CSharpConversionKind.ImplicitConversion => BindImplicitConversion(arguments, cSharpConvertBinder.Type, dictionary, bIsArrayCreationConversion: false), 
				CSharpConversionKind.ExplicitConversion => BindExplicitConversion(arguments, cSharpConvertBinder.Type, dictionary), 
				CSharpConversionKind.ArrayCreationConversion => BindImplicitConversion(arguments, cSharpConvertBinder.Type, dictionary, bIsArrayCreationConversion: true), 
				_ => throw Error.InternalCompilerError(), 
			};
		}
		if (payload is ICSharpInvokeOrInvokeMemberBinder)
		{
			EXPR callingObject = CreateCallingObjectForCall(payload as ICSharpInvokeOrInvokeMemberBinder, arguments, dictionary);
			return BindCall(payload as ICSharpInvokeOrInvokeMemberBinder, callingObject, arguments, dictionary);
		}
		if (payload is CSharpGetMemberBinder)
		{
			return BindProperty(payload, arguments[0], dictionary[0], null, fEventsPermitted: false);
		}
		if (payload is CSharpGetIndexBinder)
		{
			EXPR optionalIndexerArguments = CreateArgumentListEXPR(arguments, dictionary, 1, arguments.Length);
			return BindProperty(payload, arguments[0], dictionary[0], optionalIndexerArguments, fEventsPermitted: false);
		}
		if (payload is CSharpSetIndexBinder)
		{
			return BindAssignment(payload as CSharpSetIndexBinder, arguments, dictionary);
		}
		if (payload is CSharpIsEventBinder)
		{
			return BindIsEvent(payload as CSharpIsEventBinder, arguments, dictionary);
		}
		throw Error.InternalCompilerError();
	}

	private void PopulateLocalScope(DynamicMetaObjectBinder payload, Scope pScope, ArgumentObject[] arguments, IEnumerable<Expression> parameterExpressions, Dictionary<int, LocalVariableSymbol> dictionary)
	{
		int num = 0;
		foreach (Expression parameterExpression in parameterExpressions)
		{
			CType cType = m_symbolTable.GetCTypeFromType(parameterExpression.Type);
			bool flag = false;
			if (num == 0 && IsBinderThatCanHaveRefReceiver(payload))
			{
				flag = true;
			}
			if (parameterExpression is ParameterExpression && (parameterExpression as ParameterExpression).IsByRef && (arguments[num].Info.IsByRef || arguments[num].Info.IsOut) && !flag)
			{
				cType = m_semanticChecker.GetTypeManager().GetParameterModifier(cType, arguments[num].Info.IsOut);
			}
			LocalVariableSymbol localVariableSymbol = m_semanticChecker.GetGlobalSymbolFactory().CreateLocalVar(m_semanticChecker.GetNameManager().Add("p" + num), pScope, cType);
			localVariableSymbol.fUsedInAnonMeth = true;
			dictionary.Add(num++, localVariableSymbol);
			flag = false;
		}
	}

	private EXPRBOUNDLAMBDA GenerateBoundLambda(ArgumentObject[] arguments, Scope pScope, EXPR call)
	{
		AggregateType delegateType = m_symbolTable.GetCTypeFromType(typeof(Func<int>)).AsAggregateType();
		LocalVariableSymbol localVariableSymbol = m_semanticChecker.GetGlobalSymbolFactory().CreateLocalVar(m_semanticChecker.GetNameManager().Add("this"), pScope, m_symbolTable.GetCTypeFromType(typeof(object)));
		localVariableSymbol.isThis = true;
		EXPRBOUNDLAMBDA eXPRBOUNDLAMBDA = m_exprFactory.CreateAnonymousMethod(delegateType);
		EXPRUNBOUNDLAMBDA eXPRUNBOUNDLAMBDA = m_exprFactory.CreateLambda();
		List<Type> list = new List<Type>();
		foreach (ArgumentObject argumentObject in arguments)
		{
			list.Add(argumentObject.Type);
		}
		eXPRBOUNDLAMBDA.Initialize(pScope);
		EXPRRETURN pOptionalStatements = m_exprFactory.CreateReturn((EXPRFLAG)0, pScope, call);
		EXPRBLOCK optionalBody = m_exprFactory.CreateBlock(null, pOptionalStatements, pScope);
		eXPRBOUNDLAMBDA.OptionalBody = optionalBody;
		return eXPRBOUNDLAMBDA;
	}

	private EXPR CreateLocal(Type type, bool bIsOut, LocalVariableSymbol local)
	{
		CType cType = m_symbolTable.GetCTypeFromType(type);
		if (bIsOut)
		{
			cType = m_semanticChecker.GetTypeManager().GetParameterModifier(cType.AsParameterModifierType().GetParameterType(), isOut: true);
		}
		EXPRLOCAL expr = m_exprFactory.CreateLocal(EXPRFLAG.EXF_LVALUE, local);
		EXPR eXPR = m_binder.tryConvert(expr, cType);
		if (eXPR == null)
		{
			eXPR = m_binder.mustCast(expr, cType);
		}
		eXPR.flags |= EXPRFLAG.EXF_LVALUE;
		return eXPR;
	}

	private EXPR CreateArgumentListEXPR(ArgumentObject[] arguments, Dictionary<int, LocalVariableSymbol> dictionary, int startIndex, int endIndex)
	{
		EXPR first = null;
		EXPR last = null;
		if (arguments != null)
		{
			for (int i = startIndex; i < endIndex; i++)
			{
				ArgumentObject argument = arguments[i];
				EXPR eXPR = CreateArgumentEXPR(argument, dictionary[i]);
				if (first == null)
				{
					first = eXPR;
					last = first;
				}
				else
				{
					m_exprFactory.AppendItemToList(eXPR, ref first, ref last);
				}
			}
		}
		return first;
	}

	private EXPR CreateArgumentEXPR(ArgumentObject argument, LocalVariableSymbol local)
	{
		EXPR eXPR = (argument.Info.LiteralConstant ? ((argument.Value != null) ? m_exprFactory.CreateConstant(m_symbolTable.GetCTypeFromType(argument.Type), new CONSTVAL(argument.Value)) : ((!argument.Info.UseCompileTimeType) ? m_exprFactory.CreateNull() : m_exprFactory.CreateConstant(m_symbolTable.GetCTypeFromType(argument.Type), new CONSTVAL()))) : ((argument.Info.UseCompileTimeType || argument.Value != null) ? CreateLocal(argument.Type, argument.Info.IsOut, local) : m_exprFactory.CreateNull()));
		if (argument.Info.NamedArgument)
		{
			eXPR = m_exprFactory.CreateNamedArgumentSpecification(SymbolTable.GetName(argument.Info.Name, m_semanticChecker.GetNameManager()), eXPR);
		}
		if (!argument.Info.UseCompileTimeType && argument.Value != null)
		{
			eXPR.RuntimeObject = argument.Value;
			eXPR.RuntimeObjectActualType = m_symbolTable.GetCTypeFromType(argument.Value.GetType());
		}
		return eXPR;
	}

	private EXPRMEMGRP CreateMemberGroupEXPR(string Name, IList<Type> typeArguments, EXPR callingObject, SYMKIND kind)
	{
		Name name = SymbolTable.GetName(Name, m_semanticChecker.GetNameManager());
		AggregateType aggregateType = (callingObject.type.IsArrayType() ? m_semanticChecker.GetSymbolLoader().GetReqPredefType(PredefinedType.PT_ARRAY) : (callingObject.type.IsNullableType() ? callingObject.type.AsNullableType().GetAts(m_semanticChecker.GetSymbolLoader().GetErrorContext()) : ((!callingObject.type.IsAggregateType()) ? null : callingObject.type.AsAggregateType())));
		List<CType> list = new List<CType>();
		symbmask_t mask = symbmask_t.MASK_MethodSymbol;
		switch (kind)
		{
		case SYMKIND.SK_PropertySymbol:
		case SYMKIND.SK_IndexerSymbol:
			mask = symbmask_t.MASK_PropertySymbol;
			break;
		case SYMKIND.SK_MethodSymbol:
			mask = symbmask_t.MASK_MethodSymbol;
			break;
		}
		bool flag = name == SymbolLoader.GetNameManager().GetPredefinedName(PredefinedName.PN_CTOR);
		for (AggregateType aggregateType2 = aggregateType; aggregateType2 != null; aggregateType2 = aggregateType2.GetBaseClass())
		{
			if (m_symbolTable.AggregateContainsMethod(aggregateType2.GetOwningAggregate(), Name, mask))
			{
				list.Add(aggregateType2);
			}
			if (flag)
			{
				break;
			}
		}
		if (aggregateType.IsWindowsRuntimeType())
		{
			TypeArray winRTCollectionIfacesAll = aggregateType.GetWinRTCollectionIfacesAll(SymbolLoader);
			for (int i = 0; i < winRTCollectionIfacesAll.size; i++)
			{
				CType cType = winRTCollectionIfacesAll.Item(i);
				if (m_symbolTable.AggregateContainsMethod(cType.AsAggregateType().GetOwningAggregate(), Name, mask))
				{
					list.Add(cType);
				}
			}
		}
		EXPRFLAG eXPRFLAG = EXPRFLAG.EXF_USERCALLABLE;
		if (Name == "Invoke" && callingObject.type.isDelegateType())
		{
			eXPRFLAG |= EXPRFLAG.EXF_GOTONOTBLOCKED;
		}
		if (Name == ".ctor")
		{
			eXPRFLAG |= EXPRFLAG.EXF_CTOR;
		}
		if (Name == "$Item$")
		{
			eXPRFLAG |= EXPRFLAG.EXF_INDEXER;
		}
		TypeArray pTypeArgs = BSYMMGR.EmptyTypeArray();
		if (typeArguments != null && typeArguments.Count > 0)
		{
			pTypeArgs = m_semanticChecker.getBSymmgr().AllocParams(m_symbolTable.GetCTypeArrayFromTypes(typeArguments));
		}
		EXPRMEMGRP eXPRMEMGRP = m_exprFactory.CreateMemGroup(eXPRFLAG, name, pTypeArgs, kind, aggregateType, null, null, new CMemberLookupResults(m_semanticChecker.getBSymmgr().AllocParams(list.Count, list.ToArray()), name));
		if (callingObject.isCLASS())
		{
			eXPRMEMGRP.SetOptionalLHS(callingObject);
		}
		else
		{
			eXPRMEMGRP.SetOptionalObject(callingObject);
		}
		return eXPRMEMGRP;
	}

	private EXPR CreateProperty(SymWithType swt, EXPR callingObject, BindingFlag flags)
	{
		PropertySymbol propertySymbol = swt.Prop();
		AggregateType type = swt.GetType();
		PropWithType pwt = new PropWithType(propertySymbol, type);
		EXPRMEMGRP pMemGroup = CreateMemberGroupEXPR(propertySymbol.name.Text, null, callingObject, SYMKIND.SK_PropertySymbol);
		return m_binder.BindToProperty(callingObject.isCLASS() ? null : callingObject, pwt, flags, null, null, pMemGroup);
	}

	private EXPR CreateIndexer(SymWithType swt, EXPR callingObject, EXPR arguments, BindingFlag bindFlags)
	{
		IndexerSymbol indexerSymbol = swt.Sym as IndexerSymbol;
		AggregateType type = swt.GetType();
		EXPRMEMGRP grp = CreateMemberGroupEXPR(indexerSymbol.name.Text, null, callingObject, SYMKIND.SK_PropertySymbol);
		EXPR pResult = m_binder.BindMethodGroupToArguments(bindFlags, grp, arguments);
		return ReorderArgumentsForNamedAndOptional(callingObject, pResult);
	}

	private EXPR CreateArray(EXPR callingObject, EXPR optionalIndexerArguments)
	{
		return m_binder.BindArrayIndexCore((BindingFlag)0, callingObject, optionalIndexerArguments);
	}

	private EXPR CreateField(SymWithType swt, EXPR callingObject)
	{
		FieldSymbol fieldSymbol = swt.Field();
		CType type = fieldSymbol.GetType();
		AggregateType type2 = swt.GetType();
		FieldWithType fwt = new FieldWithType(fieldSymbol, type2);
		return m_binder.BindToField(callingObject.isCLASS() ? null : callingObject, fwt, (BindingFlag)0);
	}

	private EXPREVENT CreateEvent(SymWithType swt, EXPR callingObject)
	{
		EventSymbol eventSymbol = swt.Event();
		return m_exprFactory.CreateEvent(eventSymbol.type, callingObject, new EventWithType(eventSymbol, swt.GetType()));
	}

	private EXPR CreateCallingObjectForCall(ICSharpInvokeOrInvokeMemberBinder payload, ArgumentObject[] arguments, Dictionary<int, LocalVariableSymbol> dictionary)
	{
		EXPR eXPR;
		if (payload.StaticCall)
		{
			if (arguments[0].Value == null || !(arguments[0].Value is Type))
			{
				throw Error.InternalCompilerError();
			}
			Type type = arguments[0].Value as Type;
			eXPR = m_exprFactory.CreateClass(m_symbolTable.GetCTypeFromType(type), null, type.ContainsGenericParameters ? m_exprFactory.CreateTypeArguments(SymbolLoader.getBSymmgr().AllocParams(m_symbolTable.GetCTypeArrayFromTypes(type.GetGenericArguments())), null) : null);
		}
		else
		{
			if (!arguments[0].Info.UseCompileTimeType && arguments[0].Value == null)
			{
				throw Error.NullReferenceOnMemberException();
			}
			eXPR = m_binder.mustConvert(CreateArgumentEXPR(arguments[0], dictionary[0]), m_symbolTable.GetCTypeFromType(arguments[0].Type));
			if (arguments[0].Type.IsValueType && eXPR.isCAST())
			{
				eXPR.flags |= EXPRFLAG.EXF_USERCALLABLE;
			}
		}
		return eXPR;
	}

	private EXPR BindCall(ICSharpInvokeOrInvokeMemberBinder payload, EXPR callingObject, ArgumentObject[] arguments, Dictionary<int, LocalVariableSymbol> dictionary)
	{
		if (payload is InvokeBinder && !callingObject.type.isDelegateType())
		{
			throw Error.BindInvokeFailedNonDelegate();
		}
		EXPR eXPR = null;
		int arity = ((payload.TypeArguments != null) ? payload.TypeArguments.Count : 0);
		MemberLookup memberLookup = new MemberLookup();
		SymWithType symWithType = m_symbolTable.LookupMember(payload.Name, callingObject, m_bindingContext.ContextForMemberLookup(), arity, memberLookup, (payload.Flags & CSharpCallFlags.EventHookup) != 0, requireInvocable: true);
		if (symWithType == null)
		{
			memberLookup.ReportErrors();
		}
		if (symWithType.Sym.getKind() != SYMKIND.SK_MethodSymbol)
		{
			throw Error.InternalCompilerError();
		}
		EXPRMEMGRP eXPRMEMGRP = CreateMemberGroupEXPR(payload.Name, payload.TypeArguments, callingObject, symWithType.Sym.getKind());
		if ((payload.Flags & CSharpCallFlags.SimpleNameCall) != 0)
		{
			callingObject.flags |= EXPRFLAG.EXF_UNREALIZEDGOTO;
		}
		if ((payload.Flags & CSharpCallFlags.EventHookup) != 0)
		{
			memberLookup = new MemberLookup();
			SymWithType symWithType2 = m_symbolTable.LookupMember(payload.Name.Split('_')[1], callingObject, m_bindingContext.ContextForMemberLookup(), arity, memberLookup, (payload.Flags & CSharpCallFlags.EventHookup) != 0, requireInvocable: true);
			if (symWithType2 == null)
			{
				memberLookup.ReportErrors();
			}
			CType typeSrc = null;
			if (symWithType2.Sym.getKind() == SYMKIND.SK_FieldSymbol)
			{
				typeSrc = symWithType2.Field().GetType();
			}
			else if (symWithType2.Sym.getKind() == SYMKIND.SK_EventSymbol)
			{
				typeSrc = symWithType2.Event().type;
			}
			Type associatedSystemType = SymbolLoader.GetTypeManager().SubstType(typeSrc, symWithType2.Ats).AssociatedSystemType;
			if (associatedSystemType != null)
			{
				BindImplicitConversion(new ArgumentObject[1] { arguments[1] }, associatedSystemType, dictionary, bIsArrayCreationConversion: false);
			}
			eXPRMEMGRP.flags &= (EXPRFLAG)(-257);
			if (symWithType2.Sym.getKind() == SYMKIND.SK_EventSymbol && symWithType2.Event().IsWindowsRuntimeEvent)
			{
				return BindWinRTEventAccessor(new EventWithType(symWithType2.Event(), symWithType2.Ats), callingObject, arguments, dictionary, payload.Name.StartsWith("add_", StringComparison.Ordinal));
			}
		}
		if ((payload.Name.StartsWith("set_", StringComparison.Ordinal) && symWithType.Sym.AsMethodSymbol().Params.Size > 1) || (payload.Name.StartsWith("get_", StringComparison.Ordinal) && symWithType.Sym.AsMethodSymbol().Params.Size > 0))
		{
			eXPRMEMGRP.flags &= (EXPRFLAG)(-257);
		}
		eXPR = m_binder.BindMethodGroupToArguments((BindingFlag)257, eXPRMEMGRP, CreateArgumentListEXPR(arguments, dictionary, 1, arguments.Length));
		if (eXPR == null || !eXPR.isOK())
		{
			throw Error.BindCallFailedOverloadResolution();
		}
		CheckForConditionalMethodError(eXPR);
		return ReorderArgumentsForNamedAndOptional(callingObject, eXPR);
	}

	private EXPR BindWinRTEventAccessor(EventWithType ewt, EXPR callingObject, ArgumentObject[] arguments, Dictionary<int, LocalVariableSymbol> dictionary, bool isAddAccessor)
	{
		Type associatedSystemType = ewt.Event().type.AssociatedSystemType;
		MethPropWithInst mwi = new MethPropWithInst(ewt.Event().methRemove, ewt.Ats);
		EXPRMEMGRP eXPRMEMGRP = m_exprFactory.CreateMemGroup(callingObject, mwi);
		eXPRMEMGRP.flags &= (EXPRFLAG)(-257);
		Type actionType = Expression.GetActionType(typeof(EventRegistrationToken));
		EXPR eXPR = m_binder.mustConvert(eXPRMEMGRP, m_symbolTable.GetCTypeFromType(actionType));
		EXPR eXPR2 = CreateArgumentEXPR(arguments[1], dictionary[1]);
		EXPRLIST args;
		string text;
		if (isAddAccessor)
		{
			MethPropWithInst mwi2 = new MethPropWithInst(ewt.Event().methAdd, ewt.Ats);
			EXPRMEMGRP eXPRMEMGRP2 = m_exprFactory.CreateMemGroup(callingObject, mwi2);
			eXPRMEMGRP2.flags &= (EXPRFLAG)(-257);
			Type funcType = Expression.GetFuncType(associatedSystemType, typeof(EventRegistrationToken));
			EXPR op = m_binder.mustConvert(eXPRMEMGRP2, m_symbolTable.GetCTypeFromType(funcType));
			args = m_exprFactory.CreateList(op, eXPR, eXPR2);
			text = SymbolLoader.GetNameManager().GetPredefName(PredefinedName.PN_ADDEVENTHANDLER).Text;
		}
		else
		{
			args = m_exprFactory.CreateList(eXPR, eXPR2);
			text = SymbolLoader.GetNameManager().GetPredefName(PredefinedName.PN_REMOVEEVENTHANDLER).Text;
		}
		m_symbolTable.PopulateSymbolTableWithName(text, new List<Type> { associatedSystemType }, typeof(WindowsRuntimeMarshal));
		EXPRCLASS callingObject2 = m_exprFactory.CreateClass(m_symbolTable.GetCTypeFromType(typeof(WindowsRuntimeMarshal)), null, null);
		EXPRMEMGRP grp = CreateMemberGroupEXPR(text, new List<Type> { associatedSystemType }, callingObject2, SYMKIND.SK_MethodSymbol);
		return m_binder.BindMethodGroupToArguments((BindingFlag)257, grp, args);
	}

	private void CheckForConditionalMethodError(EXPR pExpr)
	{
		if (pExpr.isCALL())
		{
			EXPRCALL eXPRCALL = pExpr.asCALL();
			MethodSymbol methodSymbol = eXPRCALL.mwi.Meth();
			if (methodSymbol.isOverride)
			{
				methodSymbol = methodSymbol.swtSlot.Meth();
			}
			object[] customAttributes = methodSymbol.AssociatedMemberInfo.GetCustomAttributes(typeof(ConditionalAttribute), inherit: false);
			if (customAttributes.Length != 0)
			{
				throw Error.BindCallToConditionalMethod(methodSymbol.name);
			}
		}
	}

	private EXPR ReorderArgumentsForNamedAndOptional(EXPR callingObject, EXPR pResult)
	{
		EXPR optionalArguments;
		AggregateType ats;
		EXPRMEMGRP memberGroup;
		TypeArray typeArgsMeth;
		MethodOrPropertySymbol methodOrPropertySymbol;
		if (pResult.isCALL())
		{
			EXPRCALL eXPRCALL = pResult.asCALL();
			optionalArguments = eXPRCALL.GetOptionalArguments();
			ats = eXPRCALL.mwi.Ats;
			methodOrPropertySymbol = eXPRCALL.mwi.Meth();
			memberGroup = eXPRCALL.GetMemberGroup();
			typeArgsMeth = eXPRCALL.mwi.TypeArgs;
		}
		else
		{
			EXPRPROP eXPRPROP = pResult.asPROP();
			optionalArguments = eXPRPROP.GetOptionalArguments();
			ats = eXPRPROP.pwtSlot.Ats;
			methodOrPropertySymbol = eXPRPROP.pwtSlot.Prop();
			memberGroup = eXPRPROP.GetMemberGroup();
			typeArgsMeth = null;
		}
		ArgInfos argInfos = new ArgInfos();
		argInfos.carg = ExpressionBinder.CountArguments(optionalArguments, out var _);
		m_binder.FillInArgInfoFromArgList(argInfos, optionalArguments);
		TypeArray typeArray = SymbolLoader.GetTypeManager().SubstTypeArray(methodOrPropertySymbol.Params, ats, typeArgsMeth);
		methodOrPropertySymbol = ExpressionBinder.GroupToArgsBinder.FindMostDerivedMethod(SymbolLoader, methodOrPropertySymbol, callingObject.type);
		ExpressionBinder.GroupToArgsBinder.ReOrderArgsForNamedArguments(methodOrPropertySymbol, typeArray, ats, memberGroup, argInfos, m_semanticChecker.GetTypeManager(), m_exprFactory, SymbolLoader);
		EXPR eXPR = null;
		for (int num = argInfos.carg - 1; num >= 0; num--)
		{
			EXPR pArg = argInfos.prgexpr[num];
			pArg = StripNamedArgument(pArg);
			pArg = m_binder.tryConvert(pArg, typeArray[num]);
			eXPR = ((eXPR != null) ? m_exprFactory.CreateList(pArg, eXPR) : pArg);
		}
		if (pResult.isCALL())
		{
			pResult.asCALL().SetOptionalArguments(eXPR);
		}
		else
		{
			pResult.asPROP().SetOptionalArguments(eXPR);
		}
		return pResult;
	}

	private EXPR StripNamedArgument(EXPR pArg)
	{
		if (pArg.isNamedArgumentSpecification())
		{
			pArg = pArg.asNamedArgumentSpecification().Value;
		}
		else if (pArg.isARRINIT())
		{
			pArg.asARRINIT().SetOptionalArguments(StripNamedArguments(pArg.asARRINIT().GetOptionalArguments()));
		}
		return pArg;
	}

	private EXPR StripNamedArguments(EXPR pArg)
	{
		if (pArg.isLIST())
		{
			EXPRLIST eXPRLIST = pArg.asLIST();
			while (eXPRLIST != null)
			{
				eXPRLIST.SetOptionalElement(StripNamedArgument(eXPRLIST.GetOptionalElement()));
				if (eXPRLIST.GetOptionalNextListNode().isLIST())
				{
					eXPRLIST = eXPRLIST.GetOptionalNextListNode().asLIST();
					continue;
				}
				eXPRLIST.SetOptionalNextListNode(StripNamedArgument(eXPRLIST.GetOptionalNextListNode()));
				break;
			}
		}
		return StripNamedArgument(pArg);
	}

	private EXPR BindUnaryOperation(CSharpUnaryOperationBinder payload, ArgumentObject[] arguments, Dictionary<int, LocalVariableSymbol> dictionary)
	{
		if (arguments.Length != 1)
		{
			throw Error.BindUnaryOperatorRequireOneArgument();
		}
		OperatorKind operatorKind = GetOperatorKind(payload.Operation);
		EXPR eXPR = CreateArgumentEXPR(arguments[0], dictionary[0]);
		eXPR.errorString = Operators.GetDisplayName(GetOperatorKind(payload.Operation));
		if (operatorKind == OperatorKind.OP_TRUE || operatorKind == OperatorKind.OP_FALSE)
		{
			EXPR eXPR2 = m_binder.tryConvert(eXPR, SymbolLoader.GetReqPredefType(PredefinedType.PT_BOOL));
			if (eXPR2 != null && operatorKind == OperatorKind.OP_FALSE)
			{
				eXPR2 = m_binder.BindStandardUnaryOperator(OperatorKind.OP_LOGNOT, eXPR2);
			}
			if (eXPR2 == null)
			{
				eXPR2 = m_binder.bindUDUnop((operatorKind == OperatorKind.OP_TRUE) ? ExpressionKind.EK_TRUE : ExpressionKind.EK_FALSE, eXPR);
			}
			if (eXPR2 == null)
			{
				eXPR2 = m_binder.mustConvert(eXPR, SymbolLoader.GetReqPredefType(PredefinedType.PT_BOOL));
			}
			return eXPR2;
		}
		return m_binder.BindStandardUnaryOperator(operatorKind, eXPR);
	}

	private EXPR BindBinaryOperation(CSharpBinaryOperationBinder payload, ArgumentObject[] arguments, Dictionary<int, LocalVariableSymbol> dictionary)
	{
		if (arguments.Length != 2)
		{
			throw Error.BindBinaryOperatorRequireTwoArguments();
		}
		ExpressionKind expressionKind = Operators.GetExpressionKind(GetOperatorKind(payload.Operation, payload.IsLogicalOperation));
		EXPR eXPR = CreateArgumentEXPR(arguments[0], dictionary[0]);
		EXPR eXPR2 = CreateArgumentEXPR(arguments[1], dictionary[1]);
		eXPR.errorString = Operators.GetDisplayName(GetOperatorKind(payload.Operation, payload.IsLogicalOperation));
		eXPR2.errorString = Operators.GetDisplayName(GetOperatorKind(payload.Operation, payload.IsLogicalOperation));
		if (expressionKind > ExpressionKind.EK_MULTIOFFSET)
		{
			expressionKind -= 85;
		}
		return m_binder.BindStandardBinop(expressionKind, eXPR, eXPR2);
	}

	private static OperatorKind GetOperatorKind(ExpressionType p)
	{
		return GetOperatorKind(p, bIsLogical: false);
	}

	private static OperatorKind GetOperatorKind(ExpressionType p, bool bIsLogical)
	{
		switch (p)
		{
		default:
			throw Error.InternalCompilerError();
		case ExpressionType.Add:
			return OperatorKind.OP_ADD;
		case ExpressionType.Subtract:
			return OperatorKind.OP_SUB;
		case ExpressionType.Multiply:
			return OperatorKind.OP_MUL;
		case ExpressionType.Divide:
			return OperatorKind.OP_DIV;
		case ExpressionType.Modulo:
			return OperatorKind.OP_MOD;
		case ExpressionType.LeftShift:
			return OperatorKind.OP_LSHIFT;
		case ExpressionType.RightShift:
			return OperatorKind.OP_RSHIFT;
		case ExpressionType.LessThan:
			return OperatorKind.OP_LT;
		case ExpressionType.GreaterThan:
			return OperatorKind.OP_GT;
		case ExpressionType.LessThanOrEqual:
			return OperatorKind.OP_LE;
		case ExpressionType.GreaterThanOrEqual:
			return OperatorKind.OP_GE;
		case ExpressionType.Equal:
			return OperatorKind.OP_EQ;
		case ExpressionType.NotEqual:
			return OperatorKind.OP_NEQ;
		case ExpressionType.And:
			if (!bIsLogical)
			{
				return OperatorKind.OP_BITAND;
			}
			return OperatorKind.OP_LOGAND;
		case ExpressionType.ExclusiveOr:
			return OperatorKind.OP_BITXOR;
		case ExpressionType.Or:
			if (!bIsLogical)
			{
				return OperatorKind.OP_BITOR;
			}
			return OperatorKind.OP_LOGOR;
		case ExpressionType.AddAssign:
			return OperatorKind.OP_ADDEQ;
		case ExpressionType.SubtractAssign:
			return OperatorKind.OP_SUBEQ;
		case ExpressionType.MultiplyAssign:
			return OperatorKind.OP_MULEQ;
		case ExpressionType.DivideAssign:
			return OperatorKind.OP_DIVEQ;
		case ExpressionType.ModuloAssign:
			return OperatorKind.OP_MODEQ;
		case ExpressionType.AndAssign:
			return OperatorKind.OP_ANDEQ;
		case ExpressionType.ExclusiveOrAssign:
			return OperatorKind.OP_XOREQ;
		case ExpressionType.OrAssign:
			return OperatorKind.OP_OREQ;
		case ExpressionType.LeftShiftAssign:
			return OperatorKind.OP_LSHIFTEQ;
		case ExpressionType.RightShiftAssign:
			return OperatorKind.OP_RSHIFTEQ;
		case ExpressionType.Negate:
			return OperatorKind.OP_NEG;
		case ExpressionType.UnaryPlus:
			return OperatorKind.OP_UPLUS;
		case ExpressionType.Not:
			return OperatorKind.OP_LOGNOT;
		case ExpressionType.OnesComplement:
			return OperatorKind.OP_BITNOT;
		case ExpressionType.IsTrue:
			return OperatorKind.OP_TRUE;
		case ExpressionType.IsFalse:
			return OperatorKind.OP_FALSE;
		case ExpressionType.Increment:
			return OperatorKind.OP_PREINC;
		case ExpressionType.Decrement:
			return OperatorKind.OP_PREDEC;
		}
	}

	private static string GetCLROperatorName(ExpressionType p)
	{
		return p switch
		{
			ExpressionType.Add => "op_Addition", 
			ExpressionType.Subtract => "op_Subtraction", 
			ExpressionType.Multiply => "op_Multiply", 
			ExpressionType.Divide => "op_Division", 
			ExpressionType.Modulo => "op_Modulus", 
			ExpressionType.LeftShift => "op_LeftShift", 
			ExpressionType.RightShift => "op_RightShift", 
			ExpressionType.LessThan => "op_LessThan", 
			ExpressionType.GreaterThan => "op_GreaterThan", 
			ExpressionType.LessThanOrEqual => "op_LessThanOrEqual", 
			ExpressionType.GreaterThanOrEqual => "op_GreaterThanOrEqual", 
			ExpressionType.Equal => "op_Equality", 
			ExpressionType.NotEqual => "op_Inequality", 
			ExpressionType.And => "op_BitwiseAnd", 
			ExpressionType.ExclusiveOr => "op_ExclusiveOr", 
			ExpressionType.Or => "op_BitwiseOr", 
			ExpressionType.AddAssign => "op_Addition", 
			ExpressionType.SubtractAssign => "op_Subtraction", 
			ExpressionType.MultiplyAssign => "op_Multiply", 
			ExpressionType.DivideAssign => "op_Division", 
			ExpressionType.ModuloAssign => "op_Modulus", 
			ExpressionType.AndAssign => "op_BitwiseAnd", 
			ExpressionType.ExclusiveOrAssign => "op_ExclusiveOr", 
			ExpressionType.OrAssign => "op_BitwiseOr", 
			ExpressionType.LeftShiftAssign => "op_LeftShift", 
			ExpressionType.RightShiftAssign => "op_RightShift", 
			ExpressionType.Negate => "op_UnaryNegation", 
			ExpressionType.UnaryPlus => "op_UnaryPlus", 
			ExpressionType.Not => "op_LogicalNot", 
			ExpressionType.OnesComplement => "op_OnesComplement", 
			ExpressionType.IsTrue => "op_True", 
			ExpressionType.IsFalse => "op_False", 
			ExpressionType.Increment => "op_Increment", 
			ExpressionType.Decrement => "op_Decrement", 
			_ => null, 
		};
	}

	private EXPR BindProperty(DynamicMetaObjectBinder payload, ArgumentObject argument, LocalVariableSymbol local, EXPR optionalIndexerArguments, bool fEventsPermitted)
	{
		EXPR eXPR = (argument.Info.IsStaticType ? m_exprFactory.CreateClass(m_symbolTable.GetCTypeFromType(argument.Value as Type), null, null) : CreateLocal(argument.Type, argument.Info.IsOut, local));
		if (!argument.Info.UseCompileTimeType && argument.Value == null)
		{
			throw Error.NullReferenceOnMemberException();
		}
		if (argument.Type.IsValueType && eXPR.isCAST())
		{
			eXPR.flags |= EXPRFLAG.EXF_USERCALLABLE;
		}
		string name = GetName(payload);
		BindingFlag bindingFlags = GetBindingFlags(payload);
		MemberLookup memberLookup = new MemberLookup();
		SymWithType symWithType = m_symbolTable.LookupMember(name, eXPR, m_bindingContext.ContextForMemberLookup(), 0, memberLookup, allowSpecialNames: false, requireInvocable: false);
		if (symWithType == null)
		{
			if (optionalIndexerArguments != null)
			{
				int num = ExpressionIterator.Count(optionalIndexerArguments);
				if ((argument.Type.IsArray && argument.Type.GetArrayRank() == num) || argument.Type == typeof(string))
				{
					return CreateArray(eXPR, optionalIndexerArguments);
				}
			}
			memberLookup.ReportErrors();
		}
		switch (symWithType.Sym.getKind())
		{
		case SYMKIND.SK_MethodSymbol:
			throw Error.BindPropertyFailedMethodGroup(name);
		case SYMKIND.SK_PropertySymbol:
		{
			if (symWithType.Sym is IndexerSymbol)
			{
				return CreateIndexer(symWithType, eXPR, optionalIndexerArguments, bindingFlags);
			}
			BindingFlag flags = (BindingFlag)0;
			if (payload is CSharpGetMemberBinder || payload is CSharpGetIndexBinder)
			{
				flags = BindingFlag.BIND_RVALUEREQUIRED;
			}
			eXPR.flags |= EXPRFLAG.EXF_LVALUE;
			return CreateProperty(symWithType, eXPR, flags);
		}
		case SYMKIND.SK_FieldSymbol:
			return CreateField(symWithType, eXPR);
		case SYMKIND.SK_EventSymbol:
			if (fEventsPermitted)
			{
				return CreateEvent(symWithType, eXPR);
			}
			throw Error.BindPropertyFailedEvent(name);
		default:
			throw Error.InternalCompilerError();
		}
	}

	private EXPR BindImplicitConversion(ArgumentObject[] arguments, Type returnType, Dictionary<int, LocalVariableSymbol> dictionary, bool bIsArrayCreationConversion)
	{
		if (arguments.Length != 1)
		{
			throw Error.BindImplicitConversionRequireOneArgument();
		}
		m_symbolTable.AddConversionsForType(returnType);
		EXPR eXPR = CreateArgumentEXPR(arguments[0], dictionary[0]);
		CType cTypeFromType = m_symbolTable.GetCTypeFromType(returnType);
		if (bIsArrayCreationConversion)
		{
			CType cType = m_binder.chooseArrayIndexType(eXPR);
			if (cType == null)
			{
				cType = SymbolLoader.GetReqPredefType(PredefinedType.PT_INT, fEnsureState: true);
			}
			return m_binder.mustCast(m_binder.mustConvert(eXPR, cType), cTypeFromType, (CONVERTTYPE)9);
		}
		return m_binder.mustConvert(eXPR, cTypeFromType);
	}

	private EXPR BindExplicitConversion(ArgumentObject[] arguments, Type returnType, Dictionary<int, LocalVariableSymbol> dictionary)
	{
		if (arguments.Length != 1)
		{
			throw Error.BindExplicitConversionRequireOneArgument();
		}
		m_symbolTable.AddConversionsForType(returnType);
		EXPR expr = CreateArgumentEXPR(arguments[0], dictionary[0]);
		CType cTypeFromType = m_symbolTable.GetCTypeFromType(returnType);
		return m_binder.mustCast(expr, cTypeFromType);
	}

	private EXPR BindAssignment(DynamicMetaObjectBinder payload, ArgumentObject[] arguments, Dictionary<int, LocalVariableSymbol> dictionary)
	{
		if (arguments.Length < 2)
		{
			throw Error.BindBinaryAssignmentRequireTwoArguments();
		}
		string name = GetName(payload);
		EXPR optionalIndexerArguments = null;
		bool flag = false;
		if (payload is CSharpSetIndexBinder)
		{
			optionalIndexerArguments = CreateArgumentListEXPR(arguments, dictionary, 1, arguments.Length - 1);
			flag = (payload as CSharpSetIndexBinder).IsCompoundAssignment;
		}
		else
		{
			flag = (payload as CSharpSetMemberBinder).IsCompoundAssignment;
		}
		m_symbolTable.PopulateSymbolTableWithName(name, null, arguments[0].Type);
		EXPR op = BindProperty(payload, arguments[0], dictionary[0], optionalIndexerArguments, fEventsPermitted: false);
		int num = arguments.Length - 1;
		EXPR op2 = CreateArgumentEXPR(arguments[num], dictionary[num]);
		if (arguments[0] == null)
		{
			throw Error.BindBinaryAssignmentFailedNullReference();
		}
		return m_binder.bindAssignment(op, op2, flag);
	}

	private EXPR BindIsEvent(CSharpIsEventBinder binder, ArgumentObject[] arguments, Dictionary<int, LocalVariableSymbol> dictionary)
	{
		EXPR callingObject = CreateLocal(arguments[0].Type, bIsOut: false, dictionary[0]);
		MemberLookup mem = new MemberLookup();
		CType reqPredefType = SymbolLoader.GetReqPredefType(PredefinedType.PT_BOOL);
		bool value = false;
		if (arguments[0].Value == null)
		{
			throw Error.NullReferenceOnMemberException();
		}
		SymWithType symWithType = m_symbolTable.LookupMember(binder.Name, callingObject, m_bindingContext.ContextForMemberLookup(), 0, mem, allowSpecialNames: false, requireInvocable: false);
		if (symWithType != null && symWithType.Sym.getKind() == SYMKIND.SK_EventSymbol)
		{
			value = true;
		}
		if (symWithType != null && symWithType.Sym.getKind() == SYMKIND.SK_FieldSymbol && symWithType.Sym.AsFieldSymbol().isEvent)
		{
			value = true;
		}
		return m_exprFactory.CreateConstant(reqPredefType, ConstValFactory.GetBool(value));
	}

	private string GetName(DynamicMetaObjectBinder payload)
	{
		string result = null;
		if (payload is CSharpGetMemberBinder)
		{
			result = ((CSharpGetMemberBinder)payload).Name;
		}
		else if (payload is CSharpSetMemberBinder)
		{
			result = ((CSharpSetMemberBinder)payload).Name;
		}
		else if (payload is CSharpGetIndexBinder || payload is CSharpSetIndexBinder)
		{
			result = "$Item$";
		}
		return result;
	}

	private BindingFlag GetBindingFlags(DynamicMetaObjectBinder payload)
	{
		if (payload is CSharpGetMemberBinder || payload is CSharpGetIndexBinder)
		{
			return BindingFlag.BIND_RVALUEREQUIRED;
		}
		return (BindingFlag)0;
	}
}
