using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class ExpressionTreeRewriter : ExprVisitorBase
{
	protected ExprFactory expressionFactory;

	protected SymbolLoader symbolLoader;

	protected EXPRBOUNDLAMBDA currentAnonMeth;

	protected bool alwaysRewrite;

	public static EXPR Rewrite(EXPR expr, ExprFactory expressionFactory, SymbolLoader symbolLoader)
	{
		ExpressionTreeRewriter expressionTreeRewriter = new ExpressionTreeRewriter(expressionFactory, symbolLoader);
		expressionTreeRewriter.alwaysRewrite = true;
		return expressionTreeRewriter.Visit(expr);
	}

	protected ExprFactory GetExprFactory()
	{
		return expressionFactory;
	}

	protected SymbolLoader GetSymbolLoader()
	{
		return symbolLoader;
	}

	protected ExpressionTreeRewriter(ExprFactory expressionFactory, SymbolLoader symbolLoader)
	{
		this.expressionFactory = expressionFactory;
		this.symbolLoader = symbolLoader;
		alwaysRewrite = false;
	}

	protected override EXPR Dispatch(EXPR expr)
	{
		EXPR eXPR = base.Dispatch(expr);
		if (eXPR == expr)
		{
			throw Error.InternalCompilerError();
		}
		return eXPR;
	}

	protected override EXPR VisitASSIGNMENT(EXPRASSIGNMENT assignment)
	{
		EXPR arg;
		if (assignment.GetLHS().isPROP())
		{
			EXPRPROP eXPRPROP = assignment.GetLHS().asPROP();
			if (eXPRPROP.GetOptionalArguments() == null)
			{
				arg = Visit(eXPRPROP);
			}
			else
			{
				EXPR arg2 = Visit(eXPRPROP.GetMemberGroup().GetOptionalObject());
				EXPR arg3 = GetExprFactory().CreatePropertyInfo(eXPRPROP.pwtSlot.Prop(), eXPRPROP.pwtSlot.Ats);
				EXPR arg4 = GenerateParamsArray(GenerateArgsList(eXPRPROP.GetOptionalArguments()), PredefinedType.PT_EXPRESSION);
				arg = GenerateCall(PREDEFMETH.PM_EXPRESSION_PROPERTY, arg2, arg3, arg4);
			}
		}
		else
		{
			arg = Visit(assignment.GetLHS());
		}
		EXPR arg5 = Visit(assignment.GetRHS());
		return GenerateCall(PREDEFMETH.PM_EXPRESSION_ASSIGN, arg, arg5);
	}

	protected override EXPR VisitMULTIGET(EXPRMULTIGET pExpr)
	{
		return Visit(pExpr.GetOptionalMulti().Left);
	}

	protected override EXPR VisitMULTI(EXPRMULTI pExpr)
	{
		EXPR arg = Visit(pExpr.Operator);
		EXPR arg2 = Visit(pExpr.Left);
		return GenerateCall(PREDEFMETH.PM_EXPRESSION_ASSIGN, arg2, arg);
	}

	protected override EXPR VisitBOUNDLAMBDA(EXPRBOUNDLAMBDA anonmeth)
	{
		EXPRBOUNDLAMBDA eXPRBOUNDLAMBDA = currentAnonMeth;
		currentAnonMeth = anonmeth;
		MethodSymbol preDefMethod = GetPreDefMethod(PREDEFMETH.PM_EXPRESSION_LAMBDA);
		CType cType = anonmeth.DelegateType();
		TypeArray typeArgs = GetSymbolLoader().getBSymmgr().AllocParams(1, new CType[1] { cType });
		AggregateType optPredefTypeErr = GetSymbolLoader().GetOptPredefTypeErr(PredefinedType.PT_EXPRESSION, fEnsureState: true);
		MethWithInst methWithInst = new MethWithInst(preDefMethod, optPredefTypeErr, typeArgs);
		EXPR eXPR = CreateWraps(anonmeth);
		EXPR op = RewriteLambdaBody(anonmeth);
		EXPR op2 = RewriteLambdaParameters(anonmeth);
		EXPR pOptionalArguments = GetExprFactory().CreateList(op, op2);
		CType pType = GetSymbolLoader().GetTypeManager().SubstType(methWithInst.Meth().RetType, methWithInst.GetType(), methWithInst.TypeArgs);
		EXPRMEMGRP pMemberGroup = GetExprFactory().CreateMemGroup(null, methWithInst);
		EXPR eXPR2 = GetExprFactory().CreateCall((EXPRFLAG)0, pType, pOptionalArguments, pMemberGroup, methWithInst);
		eXPR2.asCALL().PredefinedMethod = PREDEFMETH.PM_EXPRESSION_LAMBDA;
		currentAnonMeth = eXPRBOUNDLAMBDA;
		if (eXPR != null)
		{
			eXPR2 = GetExprFactory().CreateSequence(eXPR, eXPR2);
		}
		EXPR eXPR3 = DestroyWraps(anonmeth, eXPR2);
		if (currentAnonMeth != null)
		{
			eXPR3 = GenerateCall(PREDEFMETH.PM_EXPRESSION_QUOTE, eXPR3);
		}
		return eXPR3;
	}

	protected override EXPR VisitCONSTANT(EXPRCONSTANT expr)
	{
		return GenerateConstant(expr);
	}

	protected override EXPR VisitLOCAL(EXPRLOCAL local)
	{
		if (local.local.wrap != null)
		{
			return local.local.wrap;
		}
		return GetExprFactory().CreateHoistedLocalInExpression(local);
	}

	protected override EXPR VisitTHISPOINTER(EXPRTHISPOINTER expr)
	{
		return GenerateConstant(expr);
	}

	protected override EXPR VisitFIELD(EXPRFIELD expr)
	{
		EXPR arg = ((expr.GetOptionalObject() != null) ? Visit(expr.GetOptionalObject()) : GetExprFactory().CreateNull());
		EXPRFIELDINFO arg2 = GetExprFactory().CreateFieldInfo(expr.fwt.Field(), expr.fwt.GetType());
		return GenerateCall(PREDEFMETH.PM_EXPRESSION_FIELD, arg, arg2);
	}

	protected override EXPR VisitUSERDEFINEDCONVERSION(EXPRUSERDEFINEDCONVERSION expr)
	{
		return GenerateUserDefinedConversion(expr, expr.Argument);
	}

	protected override EXPR VisitCAST(EXPRCAST pExpr)
	{
		EXPR argument = pExpr.GetArgument();
		if (argument.type == pExpr.type || GetSymbolLoader().IsBaseClassOfClass(argument.type, pExpr.type) || CConversions.FImpRefConv(GetSymbolLoader(), argument.type, pExpr.type))
		{
			return Visit(argument);
		}
		if (pExpr.type != null && pExpr.type.isPredefType(PredefinedType.PT_G_EXPRESSION) && argument.isBOUNDLAMBDA())
		{
			return Visit(argument);
		}
		EXPR eXPR = GenerateConversion(argument, pExpr.type, pExpr.isChecked());
		if ((pExpr.flags & EXPRFLAG.EXF_USERCALLABLE) != 0)
		{
			eXPR.flags |= EXPRFLAG.EXF_USERCALLABLE;
		}
		return eXPR;
	}

	protected override EXPR VisitCONCAT(EXPRCONCAT expr)
	{
		PREDEFMETH pdm = ((!expr.GetFirstArgument().type.isPredefType(PredefinedType.PT_STRING) || !expr.GetSecondArgument().type.isPredefType(PredefinedType.PT_STRING)) ? PREDEFMETH.PM_STRING_CONCAT_OBJECT_2 : PREDEFMETH.PM_STRING_CONCAT_STRING_2);
		EXPR arg = Visit(expr.GetFirstArgument());
		EXPR arg2 = Visit(expr.GetSecondArgument());
		MethodSymbol preDefMethod = GetPreDefMethod(pdm);
		EXPR arg3 = GetExprFactory().CreateMethodInfo(preDefMethod, GetSymbolLoader().GetReqPredefType(PredefinedType.PT_STRING), null);
		return GenerateCall(PREDEFMETH.PM_EXPRESSION_ADD_USER_DEFINED, arg, arg2, arg3);
	}

	protected override EXPR VisitBINOP(EXPRBINOP expr)
	{
		if (expr.GetUserDefinedCallMethod() != null)
		{
			return GenerateUserDefinedBinaryOperator(expr);
		}
		return GenerateBuiltInBinaryOperator(expr);
	}

	protected override EXPR VisitUNARYOP(EXPRUNARYOP pExpr)
	{
		if (pExpr.UserDefinedCallMethod != null)
		{
			return GenerateUserDefinedUnaryOperator(pExpr);
		}
		return GenerateBuiltInUnaryOperator(pExpr);
	}

	protected override EXPR VisitARRAYINDEX(EXPRARRAYINDEX pExpr)
	{
		EXPR arg = Visit(pExpr.GetArray());
		EXPR eXPR = GenerateIndexList(pExpr.GetIndex());
		if (eXPR.isLIST())
		{
			EXPR arg2 = GenerateParamsArray(eXPR, PredefinedType.PT_EXPRESSION);
			return GenerateCall(PREDEFMETH.PM_EXPRESSION_ARRAYINDEX2, arg, arg2);
		}
		return GenerateCall(PREDEFMETH.PM_EXPRESSION_ARRAYINDEX, arg, eXPR);
	}

	protected override EXPR VisitARRAYLENGTH(EXPRARRAYLENGTH pExpr)
	{
		return GenerateBuiltInUnaryOperator(PREDEFMETH.PM_EXPRESSION_ARRAYLENGTH, pExpr.GetArray(), pExpr);
	}

	protected override EXPR VisitQUESTIONMARK(EXPRQUESTIONMARK pExpr)
	{
		EXPR arg = Visit(pExpr.GetTestExpression());
		EXPR arg2 = GenerateQuestionMarkOperand(pExpr.GetConsequence().asBINOP().GetOptionalLeftChild());
		EXPR arg3 = GenerateQuestionMarkOperand(pExpr.GetConsequence().asBINOP().GetOptionalRightChild());
		return GenerateCall(PREDEFMETH.PM_EXPRESSION_CONDITION, arg, arg2, arg3);
	}

	protected override EXPR VisitCALL(EXPRCALL expr)
	{
		switch (expr.nubLiftKind)
		{
		case NullableCallLiftKind.NullableConversion:
		case NullableCallLiftKind.NullableConversionConstructor:
		case NullableCallLiftKind.NullableIntermediateConversion:
			return GenerateConversion(expr.GetOptionalArguments(), expr.type, expr.isChecked());
		case NullableCallLiftKind.UserDefinedConversion:
		case NullableCallLiftKind.NotLiftedIntermediateConversion:
			return GenerateUserDefinedConversion(expr.GetOptionalArguments(), expr.type, expr.mwi);
		default:
		{
			if (expr.mwi.Meth().IsConstructor())
			{
				return GenerateConstructor(expr);
			}
			EXPRMEMGRP memberGroup = expr.GetMemberGroup();
			if (memberGroup.isDelegate())
			{
				return GenerateDelegateInvoke(expr);
			}
			EXPR arg;
			if (expr.mwi.Meth().isStatic || expr.GetMemberGroup().GetOptionalObject() == null)
			{
				arg = GetExprFactory().CreateNull();
			}
			else
			{
				arg = expr.GetMemberGroup().GetOptionalObject();
				if (arg != null && arg.isCAST() && arg.asCAST().IsBoxingCast())
				{
					arg = arg.asCAST().GetArgument();
				}
				arg = Visit(arg);
			}
			EXPR arg2 = GetExprFactory().CreateMethodInfo(expr.mwi);
			EXPR args = GenerateArgsList(expr.GetOptionalArguments());
			EXPR arg3 = GenerateParamsArray(args, PredefinedType.PT_EXPRESSION);
			PREDEFMETH pdm = PREDEFMETH.PM_EXPRESSION_CALL;
			return GenerateCall(pdm, arg, arg2, arg3);
		}
		}
	}

	protected override EXPR VisitPROP(EXPRPROP expr)
	{
		EXPR arg = ((!expr.pwtSlot.Prop().isStatic && expr.GetMemberGroup().GetOptionalObject() != null) ? Visit(expr.GetMemberGroup().GetOptionalObject()) : GetExprFactory().CreateNull());
		EXPR arg2 = GetExprFactory().CreatePropertyInfo(expr.pwtSlot.Prop(), expr.pwtSlot.GetType());
		if (expr.GetOptionalArguments() != null)
		{
			EXPR args = GenerateArgsList(expr.GetOptionalArguments());
			EXPR arg3 = GenerateParamsArray(args, PredefinedType.PT_EXPRESSION);
			return GenerateCall(PREDEFMETH.PM_EXPRESSION_PROPERTY, arg, arg2, arg3);
		}
		return GenerateCall(PREDEFMETH.PM_EXPRESSION_PROPERTY, arg, arg2);
	}

	protected override EXPR VisitARRINIT(EXPRARRINIT expr)
	{
		EXPR arg = CreateTypeOf(expr.type.AsArrayType().GetElementType());
		EXPR args = GenerateArgsList(expr.GetOptionalArguments());
		EXPR arg2 = GenerateParamsArray(args, PredefinedType.PT_EXPRESSION);
		return GenerateCall(PREDEFMETH.PM_EXPRESSION_NEWARRAYINIT, arg, arg2);
	}

	protected override EXPR VisitZEROINIT(EXPRZEROINIT expr)
	{
		if (expr.IsConstructor)
		{
			EXPRTYPEOF arg = CreateTypeOf(expr.type);
			return GenerateCall(PREDEFMETH.PM_EXPRESSION_NEW_TYPE, arg);
		}
		return GenerateConstant(expr);
	}

	protected override EXPR VisitTYPEOF(EXPRTYPEOF expr)
	{
		return GenerateConstant(expr);
	}

	protected virtual EXPR GenerateQuestionMarkOperand(EXPR pExpr)
	{
		if (pExpr.isCAST())
		{
			return GenerateConversion(pExpr.asCAST().GetArgument(), pExpr.type, pExpr.isChecked());
		}
		return Visit(pExpr);
	}

	protected virtual EXPR GenerateDelegateInvoke(EXPRCALL expr)
	{
		EXPRMEMGRP memberGroup = expr.GetMemberGroup();
		EXPR optionalObject = memberGroup.GetOptionalObject();
		EXPR arg = Visit(optionalObject);
		EXPR args = GenerateArgsList(expr.GetOptionalArguments());
		EXPR arg2 = GenerateParamsArray(args, PredefinedType.PT_EXPRESSION);
		return GenerateCall(PREDEFMETH.PM_EXPRESSION_INVOKE, arg, arg2);
	}

	protected virtual EXPR GenerateBuiltInBinaryOperator(EXPRBINOP expr)
	{
		PREDEFMETH pdm = expr.kind switch
		{
			ExpressionKind.EK_LSHIFT => PREDEFMETH.PM_EXPRESSION_LEFTSHIFT, 
			ExpressionKind.EK_RSHIFT => PREDEFMETH.PM_EXPRESSION_RIGHTSHIFT, 
			ExpressionKind.EK_BITXOR => PREDEFMETH.PM_EXPRESSION_EXCLUSIVEOR, 
			ExpressionKind.EK_BITOR => PREDEFMETH.PM_EXPRESSION_OR, 
			ExpressionKind.EK_BITAND => PREDEFMETH.PM_EXPRESSION_AND, 
			ExpressionKind.EK_LOGAND => PREDEFMETH.PM_EXPRESSION_ANDALSO, 
			ExpressionKind.EK_LOGOR => PREDEFMETH.PM_EXPRESSION_ORELSE, 
			ExpressionKind.EK_STRINGEQ => PREDEFMETH.PM_EXPRESSION_EQUAL, 
			ExpressionKind.EK_EQ => PREDEFMETH.PM_EXPRESSION_EQUAL, 
			ExpressionKind.EK_STRINGNE => PREDEFMETH.PM_EXPRESSION_NOTEQUAL, 
			ExpressionKind.EK_NE => PREDEFMETH.PM_EXPRESSION_NOTEQUAL, 
			ExpressionKind.EK_GE => PREDEFMETH.PM_EXPRESSION_GREATERTHANOREQUAL, 
			ExpressionKind.EK_LE => PREDEFMETH.PM_EXPRESSION_LESSTHANOREQUAL, 
			ExpressionKind.EK_LT => PREDEFMETH.PM_EXPRESSION_LESSTHAN, 
			ExpressionKind.EK_GT => PREDEFMETH.PM_EXPRESSION_GREATERTHAN, 
			ExpressionKind.EK_MOD => PREDEFMETH.PM_EXPRESSION_MODULO, 
			ExpressionKind.EK_DIV => PREDEFMETH.PM_EXPRESSION_DIVIDE, 
			ExpressionKind.EK_MUL => expr.isChecked() ? PREDEFMETH.PM_EXPRESSION_MULTIPLYCHECKED : PREDEFMETH.PM_EXPRESSION_MULTIPLY, 
			ExpressionKind.EK_SUB => expr.isChecked() ? PREDEFMETH.PM_EXPRESSION_SUBTRACTCHECKED : PREDEFMETH.PM_EXPRESSION_SUBTRACT, 
			ExpressionKind.EK_ADD => expr.isChecked() ? PREDEFMETH.PM_EXPRESSION_ADDCHECKED : PREDEFMETH.PM_EXPRESSION_ADD, 
			_ => throw Error.InternalCompilerError(), 
		};
		EXPR optionalLeftChild = expr.GetOptionalLeftChild();
		EXPR optionalRightChild = expr.GetOptionalRightChild();
		CType cType = optionalLeftChild.type;
		CType cType2 = optionalRightChild.type;
		EXPR arg = Visit(optionalLeftChild);
		EXPR eXPR = Visit(optionalRightChild);
		bool flag = false;
		CType cType3 = null;
		CType cType4 = null;
		if (cType.isEnumType())
		{
			cType3 = GetSymbolLoader().GetTypeManager().GetNullable(cType.underlyingEnumType());
			cType = cType3;
			flag = true;
		}
		else if (cType.IsNullableType() && cType.StripNubs().isEnumType())
		{
			cType3 = GetSymbolLoader().GetTypeManager().GetNullable(cType.StripNubs().underlyingEnumType());
			cType = cType3;
			flag = true;
		}
		if (cType2.isEnumType())
		{
			cType4 = GetSymbolLoader().GetTypeManager().GetNullable(cType2.underlyingEnumType());
			cType2 = cType4;
			flag = true;
		}
		else if (cType2.IsNullableType() && cType2.StripNubs().isEnumType())
		{
			cType4 = GetSymbolLoader().GetTypeManager().GetNullable(cType2.StripNubs().underlyingEnumType());
			cType2 = cType4;
			flag = true;
		}
		if (cType.IsNullableType() && cType.StripNubs() == cType2)
		{
			cType4 = cType;
		}
		if (cType2.IsNullableType() && cType2.StripNubs() == cType)
		{
			cType3 = cType2;
		}
		if (cType3 != null)
		{
			arg = GenerateCall(PREDEFMETH.PM_EXPRESSION_CONVERT, arg, CreateTypeOf(cType3));
		}
		if (cType4 != null)
		{
			eXPR = GenerateCall(PREDEFMETH.PM_EXPRESSION_CONVERT, eXPR, CreateTypeOf(cType4));
		}
		EXPR eXPR2 = GenerateCall(pdm, arg, eXPR);
		if (flag && expr.type.StripNubs().isEnumType())
		{
			eXPR2 = GenerateCall(PREDEFMETH.PM_EXPRESSION_CONVERT, eXPR2, CreateTypeOf(expr.type));
		}
		return eXPR2;
	}

	protected virtual EXPR GenerateBuiltInUnaryOperator(EXPRUNARYOP expr)
	{
		PREDEFMETH pdm;
		switch (expr.kind)
		{
		case ExpressionKind.EK_UPLUS:
			return Visit(expr.Child);
		case ExpressionKind.EK_BITNOT:
			pdm = PREDEFMETH.PM_EXPRESSION_NOT;
			break;
		case ExpressionKind.EK_LOGNOT:
			pdm = PREDEFMETH.PM_EXPRESSION_NOT;
			break;
		case ExpressionKind.EK_NEG:
			pdm = (expr.isChecked() ? PREDEFMETH.PM_EXPRESSION_NEGATECHECKED : PREDEFMETH.PM_EXPRESSION_NEGATE);
			break;
		default:
			throw Error.InternalCompilerError();
		}
		EXPR child = expr.Child;
		return GenerateBuiltInUnaryOperator(pdm, child, expr);
	}

	protected virtual EXPR GenerateBuiltInUnaryOperator(PREDEFMETH pdm, EXPR pOriginalOperator, EXPR pOperator)
	{
		EXPR arg = Visit(pOriginalOperator);
		if (pOriginalOperator.type.IsNullableType() && pOriginalOperator.type.StripNubs().isEnumType())
		{
			CType pUnderlyingType = pOriginalOperator.type.StripNubs().underlyingEnumType();
			CType nullable = GetSymbolLoader().GetTypeManager().GetNullable(pUnderlyingType);
			arg = GenerateCall(PREDEFMETH.PM_EXPRESSION_CONVERT, arg, CreateTypeOf(nullable));
		}
		EXPR eXPR = GenerateCall(pdm, arg);
		if (pOriginalOperator.type.IsNullableType() && pOriginalOperator.type.StripNubs().isEnumType())
		{
			eXPR = GenerateCall(PREDEFMETH.PM_EXPRESSION_CONVERT, eXPR, CreateTypeOf(pOperator.type));
		}
		return eXPR;
	}

	protected virtual EXPR GenerateUserDefinedBinaryOperator(EXPRBINOP expr)
	{
		PREDEFMETH pdm;
		switch (expr.kind)
		{
		case ExpressionKind.EK_LOGOR:
			pdm = PREDEFMETH.PM_EXPRESSION_ORELSE_USER_DEFINED;
			break;
		case ExpressionKind.EK_LOGAND:
			pdm = PREDEFMETH.PM_EXPRESSION_ANDALSO_USER_DEFINED;
			break;
		case ExpressionKind.EK_LSHIFT:
			pdm = PREDEFMETH.PM_EXPRESSION_LEFTSHIFT_USER_DEFINED;
			break;
		case ExpressionKind.EK_RSHIFT:
			pdm = PREDEFMETH.PM_EXPRESSION_RIGHTSHIFT_USER_DEFINED;
			break;
		case ExpressionKind.EK_BITXOR:
			pdm = PREDEFMETH.PM_EXPRESSION_EXCLUSIVEOR_USER_DEFINED;
			break;
		case ExpressionKind.EK_BITOR:
			pdm = PREDEFMETH.PM_EXPRESSION_OR_USER_DEFINED;
			break;
		case ExpressionKind.EK_BITAND:
			pdm = PREDEFMETH.PM_EXPRESSION_AND_USER_DEFINED;
			break;
		case ExpressionKind.EK_MOD:
			pdm = PREDEFMETH.PM_EXPRESSION_MODULO_USER_DEFINED;
			break;
		case ExpressionKind.EK_DIV:
			pdm = PREDEFMETH.PM_EXPRESSION_DIVIDE_USER_DEFINED;
			break;
		case ExpressionKind.EK_EQ:
		case ExpressionKind.EK_NE:
		case ExpressionKind.EK_LT:
		case ExpressionKind.EK_LE:
		case ExpressionKind.EK_GT:
		case ExpressionKind.EK_GE:
		case ExpressionKind.EK_STRINGEQ:
		case ExpressionKind.EK_STRINGNE:
		case ExpressionKind.EK_DELEGATEEQ:
		case ExpressionKind.EK_DELEGATENE:
			return GenerateUserDefinedComparisonOperator(expr);
		case ExpressionKind.EK_SUB:
		case ExpressionKind.EK_DELEGATESUB:
			pdm = (expr.isChecked() ? PREDEFMETH.PM_EXPRESSION_SUBTRACTCHECKED_USER_DEFINED : PREDEFMETH.PM_EXPRESSION_SUBTRACT_USER_DEFINED);
			break;
		case ExpressionKind.EK_ADD:
		case ExpressionKind.EK_DELEGATEADD:
			pdm = (expr.isChecked() ? PREDEFMETH.PM_EXPRESSION_ADDCHECKED_USER_DEFINED : PREDEFMETH.PM_EXPRESSION_ADD_USER_DEFINED);
			break;
		case ExpressionKind.EK_MUL:
			pdm = (expr.isChecked() ? PREDEFMETH.PM_EXPRESSION_MULTIPLYCHECKED_USER_DEFINED : PREDEFMETH.PM_EXPRESSION_MULTIPLY_USER_DEFINED);
			break;
		default:
			throw Error.InternalCompilerError();
		}
		EXPR pExpr = expr.GetOptionalLeftChild();
		EXPR pExpr2 = expr.GetOptionalRightChild();
		EXPR optionalUserDefinedCall = expr.GetOptionalUserDefinedCall();
		if (optionalUserDefinedCall != null)
		{
			if (optionalUserDefinedCall.kind == ExpressionKind.EK_CALL)
			{
				EXPRLIST eXPRLIST = optionalUserDefinedCall.asCALL().GetOptionalArguments().asLIST();
				pExpr = eXPRLIST.GetOptionalElement();
				pExpr2 = eXPRLIST.GetOptionalNextListNode();
			}
			else
			{
				EXPRLIST eXPRLIST2 = optionalUserDefinedCall.asUSERLOGOP().OperatorCall.GetOptionalArguments().asLIST();
				pExpr = eXPRLIST2.GetOptionalElement().asWRAP().GetOptionalExpression();
				pExpr2 = eXPRLIST2.GetOptionalNextListNode();
			}
		}
		pExpr = Visit(pExpr);
		pExpr2 = Visit(pExpr2);
		FixLiftedUserDefinedBinaryOperators(expr, ref pExpr, ref pExpr2);
		EXPR arg = GetExprFactory().CreateMethodInfo(expr.GetUserDefinedCallMethod());
		EXPR eXPR = GenerateCall(pdm, pExpr, pExpr2, arg);
		if (expr.kind == ExpressionKind.EK_DELEGATESUB || expr.kind == ExpressionKind.EK_DELEGATEADD)
		{
			EXPR arg2 = CreateTypeOf(expr.type);
			return GenerateCall(PREDEFMETH.PM_EXPRESSION_CONVERT, eXPR, arg2);
		}
		return eXPR;
	}

	protected virtual EXPR GenerateUserDefinedUnaryOperator(EXPRUNARYOP expr)
	{
		EXPR pExpr = expr.Child;
		EXPRCALL eXPRCALL = expr.OptionalUserDefinedCall.asCALL();
		if (eXPRCALL != null)
		{
			pExpr = eXPRCALL.GetOptionalArguments();
		}
		PREDEFMETH pdm;
		switch (expr.kind)
		{
		case ExpressionKind.EK_TRUE:
		case ExpressionKind.EK_FALSE:
			return Visit(eXPRCALL);
		case ExpressionKind.EK_UPLUS:
			pdm = PREDEFMETH.PM_EXPRESSION_UNARYPLUS_USER_DEFINED;
			break;
		case ExpressionKind.EK_BITNOT:
			pdm = PREDEFMETH.PM_EXPRESSION_NOT_USER_DEFINED;
			break;
		case ExpressionKind.EK_LOGNOT:
			pdm = PREDEFMETH.PM_EXPRESSION_NOT_USER_DEFINED;
			break;
		case ExpressionKind.EK_NEG:
		case ExpressionKind.EK_DECIMALNEG:
			pdm = (expr.isChecked() ? PREDEFMETH.PM_EXPRESSION_NEGATECHECKED_USER_DEFINED : PREDEFMETH.PM_EXPRESSION_NEGATE_USER_DEFINED);
			break;
		case ExpressionKind.EK_INC:
		case ExpressionKind.EK_DEC:
		case ExpressionKind.EK_DECIMALINC:
		case ExpressionKind.EK_DECIMALDEC:
			pdm = PREDEFMETH.PM_EXPRESSION_CALL;
			break;
		default:
			throw Error.InternalCompilerError();
		}
		EXPR eXPR = Visit(pExpr);
		EXPR arg = GetExprFactory().CreateMethodInfo(expr.UserDefinedCallMethod);
		if (expr.kind == ExpressionKind.EK_INC || expr.kind == ExpressionKind.EK_DEC || expr.kind == ExpressionKind.EK_DECIMALINC || expr.kind == ExpressionKind.EK_DECIMALDEC)
		{
			return GenerateCall(pdm, null, arg, GenerateParamsArray(eXPR, PredefinedType.PT_EXPRESSION));
		}
		return GenerateCall(pdm, eXPR, arg);
	}

	protected virtual EXPR GenerateUserDefinedComparisonOperator(EXPRBINOP expr)
	{
		PREDEFMETH pdm = expr.kind switch
		{
			ExpressionKind.EK_STRINGEQ => PREDEFMETH.PM_EXPRESSION_EQUAL_USER_DEFINED, 
			ExpressionKind.EK_STRINGNE => PREDEFMETH.PM_EXPRESSION_NOTEQUAL_USER_DEFINED, 
			ExpressionKind.EK_DELEGATEEQ => PREDEFMETH.PM_EXPRESSION_EQUAL_USER_DEFINED, 
			ExpressionKind.EK_DELEGATENE => PREDEFMETH.PM_EXPRESSION_NOTEQUAL_USER_DEFINED, 
			ExpressionKind.EK_EQ => PREDEFMETH.PM_EXPRESSION_EQUAL_USER_DEFINED, 
			ExpressionKind.EK_NE => PREDEFMETH.PM_EXPRESSION_NOTEQUAL_USER_DEFINED, 
			ExpressionKind.EK_LE => PREDEFMETH.PM_EXPRESSION_LESSTHANOREQUAL_USER_DEFINED, 
			ExpressionKind.EK_LT => PREDEFMETH.PM_EXPRESSION_LESSTHAN_USER_DEFINED, 
			ExpressionKind.EK_GE => PREDEFMETH.PM_EXPRESSION_GREATERTHANOREQUAL_USER_DEFINED, 
			ExpressionKind.EK_GT => PREDEFMETH.PM_EXPRESSION_GREATERTHAN_USER_DEFINED, 
			_ => throw Error.InternalCompilerError(), 
		};
		EXPR pExpr = expr.GetOptionalLeftChild();
		EXPR pExpr2 = expr.GetOptionalRightChild();
		if (expr.GetOptionalUserDefinedCall() != null)
		{
			EXPRCALL eXPRCALL = expr.GetOptionalUserDefinedCall().asCALL();
			EXPRLIST eXPRLIST = eXPRCALL.GetOptionalArguments().asLIST();
			pExpr = eXPRLIST.GetOptionalElement();
			pExpr2 = eXPRLIST.GetOptionalNextListNode();
		}
		pExpr = Visit(pExpr);
		pExpr2 = Visit(pExpr2);
		FixLiftedUserDefinedBinaryOperators(expr, ref pExpr, ref pExpr2);
		EXPR arg = GetExprFactory().CreateBoolConstant(b: false);
		EXPR arg2 = GetExprFactory().CreateMethodInfo(expr.GetUserDefinedCallMethod());
		return GenerateCall(pdm, pExpr, pExpr2, arg, arg2);
	}

	protected EXPR RewriteLambdaBody(EXPRBOUNDLAMBDA anonmeth)
	{
		EXPRBLOCK optionalBody = anonmeth.OptionalBody;
		if (optionalBody.GetOptionalStatements().isRETURN())
		{
			return Visit(optionalBody.GetOptionalStatements().asRETURN().GetOptionalObject());
		}
		throw Error.InternalCompilerError();
	}

	protected EXPR RewriteLambdaParameters(EXPRBOUNDLAMBDA anonmeth)
	{
		EXPR first = null;
		EXPR last = first;
		for (Symbol symbol = anonmeth.ArgumentScope(); symbol != null; symbol = symbol.nextChild)
		{
			if (symbol.IsLocalVariableSymbol())
			{
				LocalVariableSymbol localVariableSymbol = symbol.AsLocalVariableSymbol();
				if (!localVariableSymbol.isThis)
				{
					GetExprFactory().AppendItemToList(localVariableSymbol.wrap, ref first, ref last);
				}
			}
		}
		return GenerateParamsArray(first, PredefinedType.PT_PARAMETEREXPRESSION);
	}

	protected virtual EXPR GenerateConversion(EXPR arg, CType CType, bool bChecked)
	{
		return GenerateConversionWithSource(Visit(arg), CType, bChecked || arg.isChecked());
	}

	protected virtual EXPR GenerateConversionWithSource(EXPR pTarget, CType pType, bool bChecked)
	{
		PREDEFMETH pdm = (bChecked ? PREDEFMETH.PM_EXPRESSION_CONVERTCHECKED : PREDEFMETH.PM_EXPRESSION_CONVERT);
		EXPR arg = CreateTypeOf(pType);
		return GenerateCall(pdm, pTarget, arg);
	}

	protected virtual EXPR GenerateValueAccessConversion(EXPR pArgument)
	{
		CType cType = pArgument.type.StripNubs();
		EXPR arg = CreateTypeOf(cType);
		return GenerateCall(PREDEFMETH.PM_EXPRESSION_CONVERT, Visit(pArgument), arg);
	}

	protected virtual EXPR GenerateUserDefinedConversion(EXPR arg, CType type, MethWithInst method)
	{
		EXPR target = Visit(arg);
		return GenerateUserDefinedConversion(arg, type, target, method);
	}

	protected virtual EXPR GenerateUserDefinedConversion(EXPR arg, CType CType, EXPR target, MethWithInst method)
	{
		if (isEnumToDecimalConversion(arg.type, CType))
		{
			CType pUnderlyingType = arg.type.StripNubs().underlyingEnumType();
			CType nullable = GetSymbolLoader().GetTypeManager().GetNullable(pUnderlyingType);
			EXPR arg2 = CreateTypeOf(nullable);
			target = GenerateCall(PREDEFMETH.PM_EXPRESSION_CONVERT, target, arg2);
		}
		CType cType = GetSymbolLoader().GetTypeManager().SubstType(method.Meth().RetType, method.GetType(), method.TypeArgs);
		bool flag = cType == CType || (IsNullableValueType(arg.type) && IsNullableValueType(CType));
		EXPR arg3 = CreateTypeOf(flag ? CType : cType);
		EXPR arg4 = GetExprFactory().CreateMethodInfo(method);
		PREDEFMETH pdm = (arg.isChecked() ? PREDEFMETH.PM_EXPRESSION_CONVERTCHECKED_USER_DEFINED : PREDEFMETH.PM_EXPRESSION_CONVERT_USER_DEFINED);
		EXPR eXPR = GenerateCall(pdm, target, arg3, arg4);
		if (flag)
		{
			return eXPR;
		}
		PREDEFMETH pdm2 = (arg.isChecked() ? PREDEFMETH.PM_EXPRESSION_CONVERTCHECKED : PREDEFMETH.PM_EXPRESSION_CONVERT);
		EXPR arg5 = CreateTypeOf(CType);
		return GenerateCall(pdm2, eXPR, arg5);
	}

	protected virtual EXPR GenerateUserDefinedConversion(EXPRUSERDEFINEDCONVERSION pExpr, EXPR pArgument)
	{
		EXPR userDefinedCall = pExpr.UserDefinedCall;
		EXPR argument = pExpr.Argument;
		EXPR eXPR = null;
		if (!isEnumToDecimalConversion(pArgument.type, pExpr.type) && IsNullableValueAccess(argument, pArgument))
		{
			eXPR = GenerateValueAccessConversion(pArgument);
		}
		else
		{
			if (userDefinedCall.isCALL() && userDefinedCall.asCALL().pConversions != null)
			{
				EXPR pConversions = userDefinedCall.asCALL().pConversions;
				if (pConversions.isCALL())
				{
					EXPR optionalArguments = pConversions.asCALL().GetOptionalArguments();
					eXPR = ((!IsNullableValueAccess(optionalArguments, pArgument)) ? Visit(optionalArguments) : GenerateValueAccessConversion(pArgument));
					return GenerateConversionWithSource(eXPR, userDefinedCall.type, userDefinedCall.asCALL().isChecked());
				}
				return GenerateUserDefinedConversion(pConversions.asUSERDEFINEDCONVERSION(), pArgument);
			}
			eXPR = Visit(argument);
		}
		return GenerateUserDefinedConversion(argument, pExpr.type, eXPR, pExpr.UserDefinedCallMethod);
	}

	protected virtual EXPR GenerateParameter(string name, CType CType)
	{
		GetSymbolLoader().GetReqPredefType(PredefinedType.PT_STRING);
		EXPRCONSTANT arg = GetExprFactory().CreateStringConstant(name);
		EXPRTYPEOF arg2 = CreateTypeOf(CType);
		return GenerateCall(PREDEFMETH.PM_EXPRESSION_PARAMETER, arg2, arg);
	}

	protected MethodSymbol GetPreDefMethod(PREDEFMETH pdm)
	{
		return GetSymbolLoader().getPredefinedMembers().GetMethod(pdm);
	}

	protected EXPRTYPEOF CreateTypeOf(CType CType)
	{
		return GetExprFactory().CreateTypeOf(CType);
	}

	protected EXPR CreateWraps(EXPRBOUNDLAMBDA anonmeth)
	{
		EXPR eXPR = null;
		for (Symbol symbol = anonmeth.ArgumentScope().firstChild; symbol != null; symbol = symbol.nextChild)
		{
			if (symbol.IsLocalVariableSymbol())
			{
				LocalVariableSymbol localVariableSymbol = symbol.AsLocalVariableSymbol();
				if (!localVariableSymbol.isThis)
				{
					EXPR pOptionalWrap = GenerateParameter(localVariableSymbol.name.Text, localVariableSymbol.GetType());
					localVariableSymbol.wrap = GetExprFactory().CreateWrapNoAutoFree(anonmeth.OptionalBody.OptionalScopeSymbol, pOptionalWrap);
					EXPR eXPR2 = GetExprFactory().CreateSave(localVariableSymbol.wrap);
					eXPR = ((eXPR != null) ? GetExprFactory().CreateSequence(eXPR, eXPR2) : eXPR2);
				}
			}
		}
		return eXPR;
	}

	protected EXPR DestroyWraps(EXPRBOUNDLAMBDA anonmeth, EXPR sequence)
	{
		for (Symbol symbol = anonmeth.ArgumentScope(); symbol != null; symbol = symbol.nextChild)
		{
			if (symbol.IsLocalVariableSymbol())
			{
				LocalVariableSymbol localVariableSymbol = symbol.AsLocalVariableSymbol();
				if (!localVariableSymbol.isThis)
				{
					EXPR p = GetExprFactory().CreateWrap(anonmeth.OptionalBody.OptionalScopeSymbol, localVariableSymbol.wrap);
					sequence = GetExprFactory().CreateReverseSequence(sequence, p);
				}
			}
		}
		return sequence;
	}

	protected virtual EXPR GenerateConstructor(EXPRCALL expr)
	{
		if (IsDelegateConstructorCall(expr))
		{
			return GenerateDelegateConstructor(expr);
		}
		EXPR arg = GetExprFactory().CreateMethodInfo(expr.mwi);
		EXPR args = GenerateArgsList(expr.GetOptionalArguments());
		EXPR arg2 = GenerateParamsArray(args, PredefinedType.PT_EXPRESSION);
		if (expr.type.IsAggregateType() && expr.type.AsAggregateType().getAggregate().IsAnonymousType())
		{
			EXPR arg3 = GenerateMembersArray(expr.type.AsAggregateType(), PredefinedType.PT_METHODINFO);
			return GenerateCall(PREDEFMETH.PM_EXPRESSION_NEW_MEMBERS, arg, arg2, arg3);
		}
		return GenerateCall(PREDEFMETH.PM_EXPRESSION_NEW, arg, arg2);
	}

	protected virtual EXPR GenerateDelegateConstructor(EXPRCALL expr)
	{
		EXPRLIST eXPRLIST = expr.GetOptionalArguments().asLIST();
		EXPR optionalElement = eXPRLIST.GetOptionalElement();
		EXPRFUNCPTR eXPRFUNCPTR = eXPRLIST.GetOptionalNextListNode().asFUNCPTR();
		MethodSymbol preDefMethod = GetPreDefMethod(PREDEFMETH.PM_DELEGATE_CREATEDELEGATE_TYPE_OBJ_METHINFO);
		AggregateType optPredefTypeErr = GetSymbolLoader().GetOptPredefTypeErr(PredefinedType.PT_DELEGATE, fEnsureState: true);
		MethWithInst mwi = new MethWithInst(preDefMethod, optPredefTypeErr);
		EXPR arg = GetExprFactory().CreateNull();
		EXPR arg2 = GetExprFactory().CreateMethodInfo(mwi);
		EXPR op = GenerateConstant(CreateTypeOf(expr.type));
		EXPR op2 = Visit(optionalElement);
		EXPR op3 = GenerateConstant(GetExprFactory().CreateMethodInfo(eXPRFUNCPTR.mwi));
		EXPR args = GetExprFactory().CreateList(op, op2, op3);
		EXPR arg3 = GenerateParamsArray(args, PredefinedType.PT_EXPRESSION);
		EXPR arg4 = GenerateCall(PREDEFMETH.PM_EXPRESSION_CALL, arg, arg2, arg3);
		EXPR arg5 = CreateTypeOf(expr.type);
		return GenerateCall(PREDEFMETH.PM_EXPRESSION_CONVERT, arg4, arg5);
	}

	protected virtual EXPR GenerateArgsList(EXPR oldArgs)
	{
		EXPR first = null;
		EXPR last = first;
		ExpressionIterator expressionIterator = new ExpressionIterator(oldArgs);
		while (!expressionIterator.AtEnd())
		{
			EXPR pExpr = expressionIterator.Current();
			GetExprFactory().AppendItemToList(Visit(pExpr), ref first, ref last);
			expressionIterator.MoveNext();
		}
		return first;
	}

	protected virtual EXPR GenerateIndexList(EXPR oldIndices)
	{
		CType reqPredefType = symbolLoader.GetReqPredefType(PredefinedType.PT_INT, fEnsureState: true);
		EXPR first = null;
		EXPR last = first;
		ExpressionIterator expressionIterator = new ExpressionIterator(oldIndices);
		while (!expressionIterator.AtEnd())
		{
			EXPR eXPR = expressionIterator.Current();
			if (eXPR.type != reqPredefType)
			{
				EXPRCLASS pType = expressionFactory.CreateClass(reqPredefType, null, null);
				eXPR = expressionFactory.CreateCast(EXPRFLAG.EXF_LITERALCONST, pType, eXPR);
				eXPR.flags |= EXPRFLAG.EXF_CHECKOVERFLOW;
			}
			EXPR newItem = Visit(eXPR);
			expressionFactory.AppendItemToList(newItem, ref first, ref last);
			expressionIterator.MoveNext();
		}
		return first;
	}

	protected virtual EXPR GenerateConstant(EXPR expr)
	{
		EXPRFLAG nFlags = (EXPRFLAG)0;
		AggregateType reqPredefType = GetSymbolLoader().GetReqPredefType(PredefinedType.PT_OBJECT, fEnsureState: true);
		if (expr.type.IsNullType())
		{
			EXPRTYPEOF arg = CreateTypeOf(reqPredefType);
			return GenerateCall(PREDEFMETH.PM_EXPRESSION_CONSTANT_OBJECT_TYPE, expr, arg);
		}
		AggregateType reqPredefType2 = GetSymbolLoader().GetReqPredefType(PredefinedType.PT_STRING, fEnsureState: true);
		if (expr.type != reqPredefType2)
		{
			nFlags = EXPRFLAG.EXF_CTOR;
		}
		EXPRCLASS pType = GetExprFactory().MakeClass(reqPredefType);
		EXPRCAST arg2 = GetExprFactory().CreateCast(nFlags, pType, expr);
		EXPRTYPEOF arg3 = CreateTypeOf(expr.type);
		return GenerateCall(PREDEFMETH.PM_EXPRESSION_CONSTANT_OBJECT_TYPE, arg2, arg3);
	}

	protected EXPRCALL GenerateCall(PREDEFMETH pdm, EXPR arg1)
	{
		MethodSymbol preDefMethod = GetPreDefMethod(pdm);
		if (preDefMethod == null)
		{
			return null;
		}
		AggregateType optPredefTypeErr = GetSymbolLoader().GetOptPredefTypeErr(PredefinedType.PT_EXPRESSION, fEnsureState: true);
		MethWithInst methWithInst = new MethWithInst(preDefMethod, optPredefTypeErr);
		EXPRMEMGRP pMemberGroup = GetExprFactory().CreateMemGroup(null, methWithInst);
		EXPRCALL eXPRCALL = GetExprFactory().CreateCall((EXPRFLAG)0, methWithInst.Meth().RetType, arg1, pMemberGroup, methWithInst);
		eXPRCALL.PredefinedMethod = pdm;
		return eXPRCALL;
	}

	protected EXPRCALL GenerateCall(PREDEFMETH pdm, EXPR arg1, EXPR arg2)
	{
		MethodSymbol preDefMethod = GetPreDefMethod(pdm);
		if (preDefMethod == null)
		{
			return null;
		}
		AggregateType optPredefTypeErr = GetSymbolLoader().GetOptPredefTypeErr(PredefinedType.PT_EXPRESSION, fEnsureState: true);
		EXPR pOptionalArguments = GetExprFactory().CreateList(arg1, arg2);
		MethWithInst methWithInst = new MethWithInst(preDefMethod, optPredefTypeErr);
		EXPRMEMGRP pMemberGroup = GetExprFactory().CreateMemGroup(null, methWithInst);
		EXPRCALL eXPRCALL = GetExprFactory().CreateCall((EXPRFLAG)0, methWithInst.Meth().RetType, pOptionalArguments, pMemberGroup, methWithInst);
		eXPRCALL.PredefinedMethod = pdm;
		return eXPRCALL;
	}

	protected EXPRCALL GenerateCall(PREDEFMETH pdm, EXPR arg1, EXPR arg2, EXPR arg3)
	{
		MethodSymbol preDefMethod = GetPreDefMethod(pdm);
		if (preDefMethod == null)
		{
			return null;
		}
		AggregateType optPredefTypeErr = GetSymbolLoader().GetOptPredefTypeErr(PredefinedType.PT_EXPRESSION, fEnsureState: true);
		EXPR pOptionalArguments = GetExprFactory().CreateList(arg1, arg2, arg3);
		MethWithInst methWithInst = new MethWithInst(preDefMethod, optPredefTypeErr);
		EXPRMEMGRP pMemberGroup = GetExprFactory().CreateMemGroup(null, methWithInst);
		EXPRCALL eXPRCALL = GetExprFactory().CreateCall((EXPRFLAG)0, methWithInst.Meth().RetType, pOptionalArguments, pMemberGroup, methWithInst);
		eXPRCALL.PredefinedMethod = pdm;
		return eXPRCALL;
	}

	protected EXPRCALL GenerateCall(PREDEFMETH pdm, EXPR arg1, EXPR arg2, EXPR arg3, EXPR arg4)
	{
		MethodSymbol preDefMethod = GetPreDefMethod(pdm);
		if (preDefMethod == null)
		{
			return null;
		}
		AggregateType optPredefTypeErr = GetSymbolLoader().GetOptPredefTypeErr(PredefinedType.PT_EXPRESSION, fEnsureState: true);
		EXPR pOptionalArguments = GetExprFactory().CreateList(arg1, arg2, arg3, arg4);
		MethWithInst methWithInst = new MethWithInst(preDefMethod, optPredefTypeErr);
		EXPRMEMGRP pMemberGroup = GetExprFactory().CreateMemGroup(null, methWithInst);
		EXPRCALL eXPRCALL = GetExprFactory().CreateCall((EXPRFLAG)0, methWithInst.Meth().RetType, pOptionalArguments, pMemberGroup, methWithInst);
		eXPRCALL.PredefinedMethod = pdm;
		return eXPRCALL;
	}

	protected virtual EXPRARRINIT GenerateParamsArray(EXPR args, PredefinedType pt)
	{
		int num = ExpressionIterator.Count(args);
		AggregateType optPredefTypeErr = GetSymbolLoader().GetOptPredefTypeErr(pt, fEnsureState: true);
		ArrayType array = GetSymbolLoader().GetTypeManager().GetArray(optPredefTypeErr, 1);
		EXPRCONSTANT pOptionalArgumentDimensions = GetExprFactory().CreateIntegerConstant(num);
		EXPRARRINIT eXPRARRINIT = GetExprFactory().CreateArrayInit(EXPRFLAG.EXF_CANTBENULL, array, args, pOptionalArgumentDimensions, null);
		eXPRARRINIT.dimSize = num;
		eXPRARRINIT.dimSizes = new int[1] { eXPRARRINIT.dimSize };
		return eXPRARRINIT;
	}

	protected virtual EXPRARRINIT GenerateMembersArray(AggregateType anonymousType, PredefinedType pt)
	{
		EXPR first = null;
		EXPR last = first;
		int num = 0;
		AggregateSymbol aggregate = anonymousType.getAggregate();
		for (Symbol symbol = aggregate.firstChild; symbol != null; symbol = symbol.nextChild)
		{
			if (symbol.IsMethodSymbol())
			{
				MethodSymbol methodSymbol = symbol.AsMethodSymbol();
				if (methodSymbol.MethKind() == MethodKindEnum.PropAccessor)
				{
					EXPRMETHODINFO newItem = GetExprFactory().CreateMethodInfo(methodSymbol, anonymousType, methodSymbol.Params);
					GetExprFactory().AppendItemToList(newItem, ref first, ref last);
					num++;
				}
			}
		}
		AggregateType optPredefTypeErr = GetSymbolLoader().GetOptPredefTypeErr(pt, fEnsureState: true);
		ArrayType array = GetSymbolLoader().GetTypeManager().GetArray(optPredefTypeErr, 1);
		EXPRCONSTANT pOptionalArgumentDimensions = GetExprFactory().CreateIntegerConstant(num);
		EXPRARRINIT eXPRARRINIT = GetExprFactory().CreateArrayInit(EXPRFLAG.EXF_CANTBENULL, array, first, pOptionalArgumentDimensions, null);
		eXPRARRINIT.dimSize = num;
		eXPRARRINIT.dimSizes = new int[1] { eXPRARRINIT.dimSize };
		return eXPRARRINIT;
	}

	protected void FixLiftedUserDefinedBinaryOperators(EXPRBINOP expr, ref EXPR pp1, ref EXPR pp2)
	{
		MethodSymbol methodSymbol = expr.GetUserDefinedCallMethod().Meth();
		EXPR optionalLeftChild = expr.GetOptionalLeftChild();
		EXPR optionalRightChild = expr.GetOptionalRightChild();
		EXPR eXPR = pp1;
		EXPR eXPR2 = pp2;
		CType cType = methodSymbol.Params.Item(0);
		CType cType2 = methodSymbol.Params.Item(1);
		CType type = optionalLeftChild.type;
		CType type2 = optionalRightChild.type;
		if (!cType.IsNullableType() && !cType2.IsNullableType() && cType.IsAggregateType() && cType2.IsAggregateType() && cType.AsAggregateType().getAggregate().IsValueType() && cType2.AsAggregateType().getAggregate().IsValueType())
		{
			CType nullable = GetSymbolLoader().GetTypeManager().GetNullable(cType);
			CType nullable2 = GetSymbolLoader().GetTypeManager().GetNullable(cType2);
			if (type.IsNullType() || (type == cType && (type2 == nullable2 || type2.IsNullType())))
			{
				eXPR = GenerateCall(PREDEFMETH.PM_EXPRESSION_CONVERT, eXPR, CreateTypeOf(nullable));
			}
			if (type2.IsNullType() || (type2 == cType2 && (type == nullable || type.IsNullType())))
			{
				eXPR2 = GenerateCall(PREDEFMETH.PM_EXPRESSION_CONVERT, eXPR2, CreateTypeOf(nullable2));
			}
			pp1 = eXPR;
			pp2 = eXPR2;
		}
	}

	protected bool IsNullableValueType(CType pType)
	{
		if (pType.IsNullableType())
		{
			CType cType = pType.StripNubs();
			if (cType.IsAggregateType())
			{
				return cType.AsAggregateType().getAggregate().IsValueType();
			}
			return false;
		}
		return false;
	}

	protected bool IsNullableValueAccess(EXPR pExpr, EXPR pObject)
	{
		if (pExpr.isPROP() && pExpr.asPROP().GetMemberGroup().GetOptionalObject() == pObject)
		{
			return pObject.type.IsNullableType();
		}
		return false;
	}

	protected bool IsDelegateConstructorCall(EXPR pExpr)
	{
		if (!pExpr.isCALL())
		{
			return false;
		}
		EXPRCALL eXPRCALL = pExpr.asCALL();
		if (eXPRCALL.mwi.Meth() != null && eXPRCALL.mwi.Meth().IsConstructor() && eXPRCALL.type.isDelegateType() && eXPRCALL.GetOptionalArguments() != null && eXPRCALL.GetOptionalArguments().isLIST())
		{
			return eXPRCALL.GetOptionalArguments().asLIST().GetOptionalNextListNode()
				.kind == ExpressionKind.EK_FUNCPTR;
		}
		return false;
	}

	private static bool isEnumToDecimalConversion(CType argtype, CType desttype)
	{
		CType cType = (argtype.IsNullableType() ? argtype.StripNubs() : argtype);
		CType cType2 = (desttype.IsNullableType() ? desttype.StripNubs() : desttype);
		if (cType.isEnumType())
		{
			return cType2.isPredefType(PredefinedType.PT_DECIMAL);
		}
		return false;
	}
}
