namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class ExprVisitorBase
{
	public EXPR Visit(EXPR pExpr)
	{
		if (pExpr == null)
		{
			return null;
		}
		if (IsCachedExpr(pExpr, out var pTransformedExpr))
		{
			return pTransformedExpr;
		}
		if (pExpr.isSTMT())
		{
			return CacheExprMapping(pExpr, DispatchStatementList(pExpr.asSTMT()));
		}
		return CacheExprMapping(pExpr, Dispatch(pExpr));
	}

	protected EXPRSTMT DispatchStatementList(EXPRSTMT expr)
	{
		EXPRSTMT eXPRSTMT = expr;
		EXPRSTMT eXPRSTMT2 = eXPRSTMT;
		while (eXPRSTMT2 != null)
		{
			EXPRSTMT optionalNextStatement = eXPRSTMT2.GetOptionalNextStatement();
			EXPRSTMT eXPRSTMT3 = eXPRSTMT2;
			eXPRSTMT2.SetOptionalNextStatement(null);
			EXPR eXPR = Dispatch(eXPRSTMT2);
			if (eXPRSTMT2 == eXPRSTMT)
			{
				eXPRSTMT = eXPR?.asSTMT();
			}
			else
			{
				eXPRSTMT2.SetOptionalNextStatement(eXPR?.asSTMT());
			}
			while (eXPRSTMT2.GetOptionalNextStatement() != null)
			{
				eXPRSTMT2 = eXPRSTMT2.GetOptionalNextStatement();
			}
			eXPRSTMT2.SetOptionalNextStatement(optionalNextStatement);
		}
		return eXPRSTMT;
	}

	protected bool IsCachedExpr(EXPR pExpr, out EXPR pTransformedExpr)
	{
		pTransformedExpr = null;
		return false;
	}

	protected EXPR CacheExprMapping(EXPR pExpr, EXPR pTransformedExpr)
	{
		return pTransformedExpr;
	}

	protected virtual EXPR Dispatch(EXPR pExpr)
	{
		return pExpr.kind switch
		{
			ExpressionKind.EK_BLOCK => VisitBLOCK(pExpr as EXPRBLOCK), 
			ExpressionKind.EK_RETURN => VisitRETURN(pExpr as EXPRRETURN), 
			ExpressionKind.EK_BINOP => VisitBINOP(pExpr as EXPRBINOP), 
			ExpressionKind.EK_UNARYOP => VisitUNARYOP(pExpr as EXPRUNARYOP), 
			ExpressionKind.EK_ASSIGNMENT => VisitASSIGNMENT(pExpr as EXPRASSIGNMENT), 
			ExpressionKind.EK_LIST => VisitLIST(pExpr as EXPRLIST), 
			ExpressionKind.EK_QUESTIONMARK => VisitQUESTIONMARK(pExpr as EXPRQUESTIONMARK), 
			ExpressionKind.EK_ARRAYINDEX => VisitARRAYINDEX(pExpr as EXPRARRAYINDEX), 
			ExpressionKind.EK_ARRAYLENGTH => VisitARRAYLENGTH(pExpr as EXPRARRAYLENGTH), 
			ExpressionKind.EK_CALL => VisitCALL(pExpr as EXPRCALL), 
			ExpressionKind.EK_EVENT => VisitEVENT(pExpr as EXPREVENT), 
			ExpressionKind.EK_FIELD => VisitFIELD(pExpr as EXPRFIELD), 
			ExpressionKind.EK_LOCAL => VisitLOCAL(pExpr as EXPRLOCAL), 
			ExpressionKind.EK_THISPOINTER => VisitTHISPOINTER(pExpr as EXPRTHISPOINTER), 
			ExpressionKind.EK_CONSTANT => VisitCONSTANT(pExpr as EXPRCONSTANT), 
			ExpressionKind.EK_TYPEARGUMENTS => VisitTYPEARGUMENTS(pExpr as EXPRTYPEARGUMENTS), 
			ExpressionKind.EK_TYPEORNAMESPACE => VisitTYPEORNAMESPACE(pExpr as EXPRTYPEORNAMESPACE), 
			ExpressionKind.EK_CLASS => VisitCLASS(pExpr as EXPRCLASS), 
			ExpressionKind.EK_FUNCPTR => VisitFUNCPTR(pExpr as EXPRFUNCPTR), 
			ExpressionKind.EK_PROP => VisitPROP(pExpr as EXPRPROP), 
			ExpressionKind.EK_MULTI => VisitMULTI(pExpr as EXPRMULTI), 
			ExpressionKind.EK_MULTIGET => VisitMULTIGET(pExpr as EXPRMULTIGET), 
			ExpressionKind.EK_WRAP => VisitWRAP(pExpr as EXPRWRAP), 
			ExpressionKind.EK_CONCAT => VisitCONCAT(pExpr as EXPRCONCAT), 
			ExpressionKind.EK_ARRINIT => VisitARRINIT(pExpr as EXPRARRINIT), 
			ExpressionKind.EK_CAST => VisitCAST(pExpr as EXPRCAST), 
			ExpressionKind.EK_USERDEFINEDCONVERSION => VisitUSERDEFINEDCONVERSION(pExpr as EXPRUSERDEFINEDCONVERSION), 
			ExpressionKind.EK_TYPEOF => VisitTYPEOF(pExpr as EXPRTYPEOF), 
			ExpressionKind.EK_ZEROINIT => VisitZEROINIT(pExpr as EXPRZEROINIT), 
			ExpressionKind.EK_USERLOGOP => VisitUSERLOGOP(pExpr as EXPRUSERLOGOP), 
			ExpressionKind.EK_MEMGRP => VisitMEMGRP(pExpr as EXPRMEMGRP), 
			ExpressionKind.EK_BOUNDLAMBDA => VisitBOUNDLAMBDA(pExpr as EXPRBOUNDLAMBDA), 
			ExpressionKind.EK_UNBOUNDLAMBDA => VisitUNBOUNDLAMBDA(pExpr as EXPRUNBOUNDLAMBDA), 
			ExpressionKind.EK_HOISTEDLOCALEXPR => VisitHOISTEDLOCALEXPR(pExpr as EXPRHOISTEDLOCALEXPR), 
			ExpressionKind.EK_FIELDINFO => VisitFIELDINFO(pExpr as EXPRFIELDINFO), 
			ExpressionKind.EK_METHODINFO => VisitMETHODINFO(pExpr as EXPRMETHODINFO), 
			ExpressionKind.EK_EQUALS => VisitEQUALS(pExpr.asBIN()), 
			ExpressionKind.EK_COMPARE => VisitCOMPARE(pExpr.asBIN()), 
			ExpressionKind.EK_NE => VisitNE(pExpr.asBIN()), 
			ExpressionKind.EK_LT => VisitLT(pExpr.asBIN()), 
			ExpressionKind.EK_LE => VisitLE(pExpr.asBIN()), 
			ExpressionKind.EK_GT => VisitGT(pExpr.asBIN()), 
			ExpressionKind.EK_GE => VisitGE(pExpr.asBIN()), 
			ExpressionKind.EK_ADD => VisitADD(pExpr.asBIN()), 
			ExpressionKind.EK_SUB => VisitSUB(pExpr.asBIN()), 
			ExpressionKind.EK_MUL => VisitMUL(pExpr.asBIN()), 
			ExpressionKind.EK_DIV => VisitDIV(pExpr.asBIN()), 
			ExpressionKind.EK_MOD => VisitMOD(pExpr.asBIN()), 
			ExpressionKind.EK_BITAND => VisitBITAND(pExpr.asBIN()), 
			ExpressionKind.EK_BITOR => VisitBITOR(pExpr.asBIN()), 
			ExpressionKind.EK_BITXOR => VisitBITXOR(pExpr.asBIN()), 
			ExpressionKind.EK_LSHIFT => VisitLSHIFT(pExpr.asBIN()), 
			ExpressionKind.EK_RSHIFT => VisitRSHIFT(pExpr.asBIN()), 
			ExpressionKind.EK_LOGAND => VisitLOGAND(pExpr.asBIN()), 
			ExpressionKind.EK_LOGOR => VisitLOGOR(pExpr.asBIN()), 
			ExpressionKind.EK_SEQUENCE => VisitSEQUENCE(pExpr.asBIN()), 
			ExpressionKind.EK_SEQREV => VisitSEQREV(pExpr.asBIN()), 
			ExpressionKind.EK_SAVE => VisitSAVE(pExpr.asBIN()), 
			ExpressionKind.EK_SWAP => VisitSWAP(pExpr.asBIN()), 
			ExpressionKind.EK_INDIR => VisitINDIR(pExpr.asBIN()), 
			ExpressionKind.EK_STRINGEQ => VisitSTRINGEQ(pExpr.asBIN()), 
			ExpressionKind.EK_STRINGNE => VisitSTRINGNE(pExpr.asBIN()), 
			ExpressionKind.EK_DELEGATEEQ => VisitDELEGATEEQ(pExpr.asBIN()), 
			ExpressionKind.EK_DELEGATENE => VisitDELEGATENE(pExpr.asBIN()), 
			ExpressionKind.EK_DELEGATEADD => VisitDELEGATEADD(pExpr.asBIN()), 
			ExpressionKind.EK_DELEGATESUB => VisitDELEGATESUB(pExpr.asBIN()), 
			ExpressionKind.EK_EQ => VisitEQ(pExpr.asBIN()), 
			ExpressionKind.EK_TRUE => VisitTRUE(pExpr.asUnaryOperator()), 
			ExpressionKind.EK_FALSE => VisitFALSE(pExpr.asUnaryOperator()), 
			ExpressionKind.EK_INC => VisitINC(pExpr.asUnaryOperator()), 
			ExpressionKind.EK_DEC => VisitDEC(pExpr.asUnaryOperator()), 
			ExpressionKind.EK_LOGNOT => VisitLOGNOT(pExpr.asUnaryOperator()), 
			ExpressionKind.EK_NEG => VisitNEG(pExpr.asUnaryOperator()), 
			ExpressionKind.EK_UPLUS => VisitUPLUS(pExpr.asUnaryOperator()), 
			ExpressionKind.EK_BITNOT => VisitBITNOT(pExpr.asUnaryOperator()), 
			ExpressionKind.EK_ADDR => VisitADDR(pExpr.asUnaryOperator()), 
			ExpressionKind.EK_DECIMALNEG => VisitDECIMALNEG(pExpr.asUnaryOperator()), 
			ExpressionKind.EK_DECIMALINC => VisitDECIMALINC(pExpr.asUnaryOperator()), 
			ExpressionKind.EK_DECIMALDEC => VisitDECIMALDEC(pExpr.asUnaryOperator()), 
			_ => throw Error.InternalCompilerError(), 
		};
	}

	protected void VisitChildren(EXPR pExpr)
	{
		EXPR eXPR = null;
		if (pExpr.isLIST())
		{
			EXPRLIST eXPRLIST = pExpr.asLIST();
			while (true)
			{
				eXPRLIST.SetOptionalElement(Visit(eXPRLIST.GetOptionalElement()));
				if (eXPRLIST.GetOptionalNextListNode() == null)
				{
					return;
				}
				if (!eXPRLIST.GetOptionalNextListNode().isLIST())
				{
					break;
				}
				eXPRLIST = eXPRLIST.GetOptionalNextListNode().asLIST();
			}
			eXPRLIST.SetOptionalNextListNode(Visit(eXPRLIST.GetOptionalNextListNode()));
			return;
		}
		switch (pExpr.kind)
		{
		default:
			if (!pExpr.isUnaryOperator())
			{
				goto case ExpressionKind.EK_BINOP;
			}
			goto case ExpressionKind.EK_UNARYOP;
		case ExpressionKind.EK_BINOP:
			eXPR = Visit((pExpr as EXPRBINOP).GetOptionalLeftChild());
			(pExpr as EXPRBINOP).SetOptionalLeftChild(eXPR);
			eXPR = Visit((pExpr as EXPRBINOP).GetOptionalRightChild());
			(pExpr as EXPRBINOP).SetOptionalRightChild(eXPR);
			break;
		case ExpressionKind.EK_LIST:
			eXPR = Visit((pExpr as EXPRLIST).GetOptionalElement());
			(pExpr as EXPRLIST).SetOptionalElement(eXPR);
			eXPR = Visit((pExpr as EXPRLIST).GetOptionalNextListNode());
			(pExpr as EXPRLIST).SetOptionalNextListNode(eXPR);
			break;
		case ExpressionKind.EK_ASSIGNMENT:
			eXPR = Visit((pExpr as EXPRASSIGNMENT).GetLHS());
			(pExpr as EXPRASSIGNMENT).SetLHS(eXPR);
			eXPR = Visit((pExpr as EXPRASSIGNMENT).GetRHS());
			(pExpr as EXPRASSIGNMENT).SetRHS(eXPR);
			break;
		case ExpressionKind.EK_QUESTIONMARK:
			eXPR = Visit((pExpr as EXPRQUESTIONMARK).GetTestExpression());
			(pExpr as EXPRQUESTIONMARK).SetTestExpression(eXPR);
			eXPR = Visit((pExpr as EXPRQUESTIONMARK).GetConsequence());
			(pExpr as EXPRQUESTIONMARK).SetConsequence(eXPR as EXPRBINOP);
			break;
		case ExpressionKind.EK_ARRAYINDEX:
			eXPR = Visit((pExpr as EXPRARRAYINDEX).GetArray());
			(pExpr as EXPRARRAYINDEX).SetArray(eXPR);
			eXPR = Visit((pExpr as EXPRARRAYINDEX).GetIndex());
			(pExpr as EXPRARRAYINDEX).SetIndex(eXPR);
			break;
		case ExpressionKind.EK_ARRAYLENGTH:
			eXPR = Visit((pExpr as EXPRARRAYLENGTH).GetArray());
			(pExpr as EXPRARRAYLENGTH).SetArray(eXPR);
			break;
		case ExpressionKind.EK_UNARYOP:
			eXPR = Visit((pExpr as EXPRUNARYOP).Child);
			(pExpr as EXPRUNARYOP).Child = eXPR;
			break;
		case ExpressionKind.EK_USERLOGOP:
			eXPR = Visit((pExpr as EXPRUSERLOGOP).TrueFalseCall);
			(pExpr as EXPRUSERLOGOP).TrueFalseCall = eXPR;
			eXPR = Visit((pExpr as EXPRUSERLOGOP).OperatorCall);
			(pExpr as EXPRUSERLOGOP).OperatorCall = eXPR as EXPRCALL;
			eXPR = Visit((pExpr as EXPRUSERLOGOP).FirstOperandToExamine);
			(pExpr as EXPRUSERLOGOP).FirstOperandToExamine = eXPR;
			break;
		case ExpressionKind.EK_TYPEOF:
			eXPR = Visit((pExpr as EXPRTYPEOF).GetSourceType());
			(pExpr as EXPRTYPEOF).SetSourceType(eXPR as EXPRTYPEORNAMESPACE);
			break;
		case ExpressionKind.EK_CAST:
			eXPR = Visit((pExpr as EXPRCAST).GetArgument());
			(pExpr as EXPRCAST).SetArgument(eXPR);
			eXPR = Visit((pExpr as EXPRCAST).GetDestinationType());
			(pExpr as EXPRCAST).SetDestinationType(eXPR as EXPRTYPEORNAMESPACE);
			break;
		case ExpressionKind.EK_USERDEFINEDCONVERSION:
			eXPR = Visit((pExpr as EXPRUSERDEFINEDCONVERSION).UserDefinedCall);
			(pExpr as EXPRUSERDEFINEDCONVERSION).UserDefinedCall = eXPR;
			break;
		case ExpressionKind.EK_ZEROINIT:
			eXPR = Visit((pExpr as EXPRZEROINIT).OptionalArgument);
			(pExpr as EXPRZEROINIT).OptionalArgument = eXPR;
			eXPR = Visit((pExpr as EXPRZEROINIT).OptionalConstructorCall);
			(pExpr as EXPRZEROINIT).OptionalConstructorCall = eXPR;
			break;
		case ExpressionKind.EK_BLOCK:
			eXPR = Visit((pExpr as EXPRBLOCK).GetOptionalStatements());
			(pExpr as EXPRBLOCK).SetOptionalStatements(eXPR as EXPRSTMT);
			break;
		case ExpressionKind.EK_MEMGRP:
			eXPR = Visit((pExpr as EXPRMEMGRP).GetOptionalObject());
			(pExpr as EXPRMEMGRP).SetOptionalObject(eXPR);
			break;
		case ExpressionKind.EK_CALL:
			eXPR = Visit((pExpr as EXPRCALL).GetOptionalArguments());
			(pExpr as EXPRCALL).SetOptionalArguments(eXPR);
			eXPR = Visit((pExpr as EXPRCALL).GetMemberGroup());
			(pExpr as EXPRCALL).SetMemberGroup(eXPR as EXPRMEMGRP);
			break;
		case ExpressionKind.EK_PROP:
			eXPR = Visit((pExpr as EXPRPROP).GetOptionalArguments());
			(pExpr as EXPRPROP).SetOptionalArguments(eXPR);
			eXPR = Visit((pExpr as EXPRPROP).GetMemberGroup());
			(pExpr as EXPRPROP).SetMemberGroup(eXPR as EXPRMEMGRP);
			break;
		case ExpressionKind.EK_FIELD:
			eXPR = Visit((pExpr as EXPRFIELD).GetOptionalObject());
			(pExpr as EXPRFIELD).SetOptionalObject(eXPR);
			break;
		case ExpressionKind.EK_EVENT:
			eXPR = Visit((pExpr as EXPREVENT).OptionalObject);
			(pExpr as EXPREVENT).OptionalObject = eXPR;
			break;
		case ExpressionKind.EK_RETURN:
			eXPR = Visit((pExpr as EXPRRETURN).GetOptionalObject());
			(pExpr as EXPRRETURN).SetOptionalObject(eXPR);
			break;
		case ExpressionKind.EK_CONSTANT:
			eXPR = Visit((pExpr as EXPRCONSTANT).GetOptionalConstructorCall());
			(pExpr as EXPRCONSTANT).SetOptionalConstructorCall(eXPR);
			break;
		case ExpressionKind.EK_TYPEARGUMENTS:
			eXPR = Visit((pExpr as EXPRTYPEARGUMENTS).GetOptionalElements());
			(pExpr as EXPRTYPEARGUMENTS).SetOptionalElements(eXPR);
			break;
		case ExpressionKind.EK_MULTI:
			eXPR = Visit((pExpr as EXPRMULTI).GetLeft());
			(pExpr as EXPRMULTI).SetLeft(eXPR);
			eXPR = Visit((pExpr as EXPRMULTI).GetOperator());
			(pExpr as EXPRMULTI).SetOperator(eXPR);
			break;
		case ExpressionKind.EK_CONCAT:
			eXPR = Visit((pExpr as EXPRCONCAT).GetFirstArgument());
			(pExpr as EXPRCONCAT).SetFirstArgument(eXPR);
			eXPR = Visit((pExpr as EXPRCONCAT).GetSecondArgument());
			(pExpr as EXPRCONCAT).SetSecondArgument(eXPR);
			break;
		case ExpressionKind.EK_ARRINIT:
			eXPR = Visit((pExpr as EXPRARRINIT).GetOptionalArguments());
			(pExpr as EXPRARRINIT).SetOptionalArguments(eXPR);
			eXPR = Visit((pExpr as EXPRARRINIT).GetOptionalArgumentDimensions());
			(pExpr as EXPRARRINIT).SetOptionalArgumentDimensions(eXPR);
			break;
		case ExpressionKind.EK_BOUNDLAMBDA:
			eXPR = Visit((pExpr as EXPRBOUNDLAMBDA).OptionalBody);
			(pExpr as EXPRBOUNDLAMBDA).OptionalBody = eXPR as EXPRBLOCK;
			break;
		case ExpressionKind.EK_NOOP:
		case ExpressionKind.EK_LOCAL:
		case ExpressionKind.EK_THISPOINTER:
		case ExpressionKind.EK_TYPEORNAMESPACE:
		case ExpressionKind.EK_CLASS:
		case ExpressionKind.EK_FUNCPTR:
		case ExpressionKind.EK_MULTIGET:
		case ExpressionKind.EK_WRAP:
		case ExpressionKind.EK_UNBOUNDLAMBDA:
		case ExpressionKind.EK_HOISTEDLOCALEXPR:
		case ExpressionKind.EK_FIELDINFO:
		case ExpressionKind.EK_METHODINFO:
			break;
		}
	}

	protected virtual EXPR VisitEXPR(EXPR pExpr)
	{
		VisitChildren(pExpr);
		return pExpr;
	}

	protected virtual EXPR VisitBLOCK(EXPRBLOCK pExpr)
	{
		return VisitSTMT(pExpr);
	}

	protected virtual EXPR VisitTHISPOINTER(EXPRTHISPOINTER pExpr)
	{
		return VisitLOCAL(pExpr);
	}

	protected virtual EXPR VisitRETURN(EXPRRETURN pExpr)
	{
		return VisitSTMT(pExpr);
	}

	protected virtual EXPR VisitCLASS(EXPRCLASS pExpr)
	{
		return VisitTYPEORNAMESPACE(pExpr);
	}

	protected virtual EXPR VisitSTMT(EXPRSTMT pExpr)
	{
		return VisitEXPR(pExpr);
	}

	protected virtual EXPR VisitBINOP(EXPRBINOP pExpr)
	{
		return VisitEXPR(pExpr);
	}

	protected virtual EXPR VisitLIST(EXPRLIST pExpr)
	{
		return VisitEXPR(pExpr);
	}

	protected virtual EXPR VisitASSIGNMENT(EXPRASSIGNMENT pExpr)
	{
		return VisitEXPR(pExpr);
	}

	protected virtual EXPR VisitQUESTIONMARK(EXPRQUESTIONMARK pExpr)
	{
		return VisitEXPR(pExpr);
	}

	protected virtual EXPR VisitARRAYINDEX(EXPRARRAYINDEX pExpr)
	{
		return VisitEXPR(pExpr);
	}

	protected virtual EXPR VisitARRAYLENGTH(EXPRARRAYLENGTH pExpr)
	{
		return VisitEXPR(pExpr);
	}

	protected virtual EXPR VisitUNARYOP(EXPRUNARYOP pExpr)
	{
		return VisitEXPR(pExpr);
	}

	protected virtual EXPR VisitUSERLOGOP(EXPRUSERLOGOP pExpr)
	{
		return VisitEXPR(pExpr);
	}

	protected virtual EXPR VisitTYPEOF(EXPRTYPEOF pExpr)
	{
		return VisitEXPR(pExpr);
	}

	protected virtual EXPR VisitCAST(EXPRCAST pExpr)
	{
		return VisitEXPR(pExpr);
	}

	protected virtual EXPR VisitUSERDEFINEDCONVERSION(EXPRUSERDEFINEDCONVERSION pExpr)
	{
		return VisitEXPR(pExpr);
	}

	protected virtual EXPR VisitZEROINIT(EXPRZEROINIT pExpr)
	{
		return VisitEXPR(pExpr);
	}

	protected virtual EXPR VisitMEMGRP(EXPRMEMGRP pExpr)
	{
		return VisitEXPR(pExpr);
	}

	protected virtual EXPR VisitCALL(EXPRCALL pExpr)
	{
		return VisitEXPR(pExpr);
	}

	protected virtual EXPR VisitPROP(EXPRPROP pExpr)
	{
		return VisitEXPR(pExpr);
	}

	protected virtual EXPR VisitFIELD(EXPRFIELD pExpr)
	{
		return VisitEXPR(pExpr);
	}

	protected virtual EXPR VisitEVENT(EXPREVENT pExpr)
	{
		return VisitEXPR(pExpr);
	}

	protected virtual EXPR VisitLOCAL(EXPRLOCAL pExpr)
	{
		return VisitEXPR(pExpr);
	}

	protected virtual EXPR VisitCONSTANT(EXPRCONSTANT pExpr)
	{
		return VisitEXPR(pExpr);
	}

	protected virtual EXPR VisitTYPEARGUMENTS(EXPRTYPEARGUMENTS pExpr)
	{
		return VisitEXPR(pExpr);
	}

	protected virtual EXPR VisitTYPEORNAMESPACE(EXPRTYPEORNAMESPACE pExpr)
	{
		return VisitEXPR(pExpr);
	}

	protected virtual EXPR VisitFUNCPTR(EXPRFUNCPTR pExpr)
	{
		return VisitEXPR(pExpr);
	}

	protected virtual EXPR VisitMULTIGET(EXPRMULTIGET pExpr)
	{
		return VisitEXPR(pExpr);
	}

	protected virtual EXPR VisitMULTI(EXPRMULTI pExpr)
	{
		return VisitEXPR(pExpr);
	}

	protected virtual EXPR VisitWRAP(EXPRWRAP pExpr)
	{
		return VisitEXPR(pExpr);
	}

	protected virtual EXPR VisitCONCAT(EXPRCONCAT pExpr)
	{
		return VisitEXPR(pExpr);
	}

	protected virtual EXPR VisitARRINIT(EXPRARRINIT pExpr)
	{
		return VisitEXPR(pExpr);
	}

	protected virtual EXPR VisitBOUNDLAMBDA(EXPRBOUNDLAMBDA pExpr)
	{
		return VisitEXPR(pExpr);
	}

	protected virtual EXPR VisitUNBOUNDLAMBDA(EXPRUNBOUNDLAMBDA pExpr)
	{
		return VisitEXPR(pExpr);
	}

	protected virtual EXPR VisitHOISTEDLOCALEXPR(EXPRHOISTEDLOCALEXPR pExpr)
	{
		return VisitEXPR(pExpr);
	}

	protected virtual EXPR VisitFIELDINFO(EXPRFIELDINFO pExpr)
	{
		return VisitEXPR(pExpr);
	}

	protected virtual EXPR VisitMETHODINFO(EXPRMETHODINFO pExpr)
	{
		return VisitEXPR(pExpr);
	}

	protected virtual EXPR VisitEQUALS(EXPRBINOP pExpr)
	{
		return VisitBINOP(pExpr);
	}

	protected virtual EXPR VisitCOMPARE(EXPRBINOP pExpr)
	{
		return VisitBINOP(pExpr);
	}

	protected virtual EXPR VisitEQ(EXPRBINOP pExpr)
	{
		return VisitBINOP(pExpr);
	}

	protected virtual EXPR VisitNE(EXPRBINOP pExpr)
	{
		return VisitBINOP(pExpr);
	}

	protected virtual EXPR VisitLE(EXPRBINOP pExpr)
	{
		return VisitBINOP(pExpr);
	}

	protected virtual EXPR VisitGE(EXPRBINOP pExpr)
	{
		return VisitBINOP(pExpr);
	}

	protected virtual EXPR VisitADD(EXPRBINOP pExpr)
	{
		return VisitBINOP(pExpr);
	}

	protected virtual EXPR VisitSUB(EXPRBINOP pExpr)
	{
		return VisitBINOP(pExpr);
	}

	protected virtual EXPR VisitDIV(EXPRBINOP pExpr)
	{
		return VisitBINOP(pExpr);
	}

	protected virtual EXPR VisitBITAND(EXPRBINOP pExpr)
	{
		return VisitBINOP(pExpr);
	}

	protected virtual EXPR VisitBITOR(EXPRBINOP pExpr)
	{
		return VisitBINOP(pExpr);
	}

	protected virtual EXPR VisitLSHIFT(EXPRBINOP pExpr)
	{
		return VisitBINOP(pExpr);
	}

	protected virtual EXPR VisitLOGAND(EXPRBINOP pExpr)
	{
		return VisitBINOP(pExpr);
	}

	protected virtual EXPR VisitSEQUENCE(EXPRBINOP pExpr)
	{
		return VisitBINOP(pExpr);
	}

	protected virtual EXPR VisitSAVE(EXPRBINOP pExpr)
	{
		return VisitBINOP(pExpr);
	}

	protected virtual EXPR VisitINDIR(EXPRBINOP pExpr)
	{
		return VisitBINOP(pExpr);
	}

	protected virtual EXPR VisitSTRINGEQ(EXPRBINOP pExpr)
	{
		return VisitBINOP(pExpr);
	}

	protected virtual EXPR VisitDELEGATEEQ(EXPRBINOP pExpr)
	{
		return VisitBINOP(pExpr);
	}

	protected virtual EXPR VisitDELEGATEADD(EXPRBINOP pExpr)
	{
		return VisitBINOP(pExpr);
	}

	protected virtual EXPR VisitRANGE(EXPRBINOP pExpr)
	{
		return VisitBINOP(pExpr);
	}

	protected virtual EXPR VisitLT(EXPRBINOP pExpr)
	{
		return VisitBINOP(pExpr);
	}

	protected virtual EXPR VisitMUL(EXPRBINOP pExpr)
	{
		return VisitBINOP(pExpr);
	}

	protected virtual EXPR VisitBITXOR(EXPRBINOP pExpr)
	{
		return VisitBINOP(pExpr);
	}

	protected virtual EXPR VisitRSHIFT(EXPRBINOP pExpr)
	{
		return VisitBINOP(pExpr);
	}

	protected virtual EXPR VisitLOGOR(EXPRBINOP pExpr)
	{
		return VisitBINOP(pExpr);
	}

	protected virtual EXPR VisitSEQREV(EXPRBINOP pExpr)
	{
		return VisitBINOP(pExpr);
	}

	protected virtual EXPR VisitSTRINGNE(EXPRBINOP pExpr)
	{
		return VisitBINOP(pExpr);
	}

	protected virtual EXPR VisitDELEGATENE(EXPRBINOP pExpr)
	{
		return VisitBINOP(pExpr);
	}

	protected virtual EXPR VisitGT(EXPRBINOP pExpr)
	{
		return VisitBINOP(pExpr);
	}

	protected virtual EXPR VisitMOD(EXPRBINOP pExpr)
	{
		return VisitBINOP(pExpr);
	}

	protected virtual EXPR VisitSWAP(EXPRBINOP pExpr)
	{
		return VisitBINOP(pExpr);
	}

	protected virtual EXPR VisitDELEGATESUB(EXPRBINOP pExpr)
	{
		return VisitBINOP(pExpr);
	}

	protected virtual EXPR VisitTRUE(EXPRUNARYOP pExpr)
	{
		return VisitUNARYOP(pExpr);
	}

	protected virtual EXPR VisitINC(EXPRUNARYOP pExpr)
	{
		return VisitUNARYOP(pExpr);
	}

	protected virtual EXPR VisitLOGNOT(EXPRUNARYOP pExpr)
	{
		return VisitUNARYOP(pExpr);
	}

	protected virtual EXPR VisitNEG(EXPRUNARYOP pExpr)
	{
		return VisitUNARYOP(pExpr);
	}

	protected virtual EXPR VisitBITNOT(EXPRUNARYOP pExpr)
	{
		return VisitUNARYOP(pExpr);
	}

	protected virtual EXPR VisitADDR(EXPRUNARYOP pExpr)
	{
		return VisitUNARYOP(pExpr);
	}

	protected virtual EXPR VisitDECIMALNEG(EXPRUNARYOP pExpr)
	{
		return VisitUNARYOP(pExpr);
	}

	protected virtual EXPR VisitDECIMALDEC(EXPRUNARYOP pExpr)
	{
		return VisitUNARYOP(pExpr);
	}

	protected virtual EXPR VisitFALSE(EXPRUNARYOP pExpr)
	{
		return VisitUNARYOP(pExpr);
	}

	protected virtual EXPR VisitDEC(EXPRUNARYOP pExpr)
	{
		return VisitUNARYOP(pExpr);
	}

	protected virtual EXPR VisitUPLUS(EXPRUNARYOP pExpr)
	{
		return VisitUNARYOP(pExpr);
	}

	protected virtual EXPR VisitDECIMALINC(EXPRUNARYOP pExpr)
	{
		return VisitUNARYOP(pExpr);
	}
}
