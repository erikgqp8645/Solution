using System;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal sealed class ExprFactory
{
	private GlobalSymbolContext m_globalSymbolContext;

	private ConstValFactory m_constants;

	public ExprFactory(GlobalSymbolContext globalSymbolContext)
	{
		m_globalSymbolContext = globalSymbolContext;
		m_constants = new ConstValFactory();
	}

	public ConstValFactory GetExprConstants()
	{
		return m_constants;
	}

	private TypeManager GetTypes()
	{
		return m_globalSymbolContext.GetTypes();
	}

	private BSYMMGR GetGlobalSymbols()
	{
		return m_globalSymbolContext.GetGlobalSymbols();
	}

	public EXPRCALL CreateCall(EXPRFLAG nFlags, CType pType, EXPR pOptionalArguments, EXPRMEMGRP pMemberGroup, MethWithInst MWI)
	{
		EXPRCALL eXPRCALL = new EXPRCALL();
		eXPRCALL.kind = ExpressionKind.EK_CALL;
		eXPRCALL.type = pType;
		eXPRCALL.flags = nFlags;
		eXPRCALL.SetOptionalArguments(pOptionalArguments);
		eXPRCALL.SetMemberGroup(pMemberGroup);
		eXPRCALL.nubLiftKind = NullableCallLiftKind.NotLifted;
		eXPRCALL.castOfNonLiftedResultToLiftedType = null;
		eXPRCALL.mwi = MWI;
		return eXPRCALL;
	}

	public EXPRFIELD CreateField(EXPRFLAG nFlags, CType pType, EXPR pOptionalObject, uint nOffset, FieldWithType FWT, EXPR pOptionalLHS)
	{
		EXPRFIELD eXPRFIELD = new EXPRFIELD();
		eXPRFIELD.kind = ExpressionKind.EK_FIELD;
		eXPRFIELD.type = pType;
		eXPRFIELD.flags = nFlags;
		eXPRFIELD.SetOptionalObject(pOptionalObject);
		if (FWT != null)
		{
			eXPRFIELD.fwt = FWT;
		}
		return eXPRFIELD;
	}

	public EXPRFUNCPTR CreateFunctionPointer(EXPRFLAG nFlags, CType pType, EXPR pObject, MethWithInst MWI)
	{
		EXPRFUNCPTR eXPRFUNCPTR = new EXPRFUNCPTR();
		eXPRFUNCPTR.kind = ExpressionKind.EK_FUNCPTR;
		eXPRFUNCPTR.type = pType;
		eXPRFUNCPTR.flags = nFlags;
		eXPRFUNCPTR.OptionalObject = pObject;
		eXPRFUNCPTR.mwi = new MethWithInst(MWI);
		return eXPRFUNCPTR;
	}

	public EXPRARRINIT CreateArrayInit(EXPRFLAG nFlags, CType pType, EXPR pOptionalArguments, EXPR pOptionalArgumentDimensions, int[] pDimSizes)
	{
		EXPRARRINIT eXPRARRINIT = new EXPRARRINIT();
		eXPRARRINIT.kind = ExpressionKind.EK_ARRINIT;
		eXPRARRINIT.type = pType;
		eXPRARRINIT.SetOptionalArguments(pOptionalArguments);
		eXPRARRINIT.SetOptionalArgumentDimensions(pOptionalArgumentDimensions);
		eXPRARRINIT.dimSizes = pDimSizes;
		eXPRARRINIT.dimSize = ((pDimSizes != null) ? pDimSizes.Length : 0);
		return eXPRARRINIT;
	}

	public EXPRPROP CreateProperty(CType pType, EXPR pOptionalObject)
	{
		MethPropWithInst mwi = new MethPropWithInst();
		EXPRMEMGRP pMemberGroup = CreateMemGroup(pOptionalObject, mwi);
		return CreateProperty(pType, null, null, pMemberGroup, null, null, null);
	}

	public EXPRPROP CreateProperty(CType pType, EXPR pOptionalObjectThrough, EXPR pOptionalArguments, EXPRMEMGRP pMemberGroup, PropWithType pwtSlot, MethWithType mwtGet, MethWithType mwtSet)
	{
		EXPRPROP eXPRPROP = new EXPRPROP();
		eXPRPROP.kind = ExpressionKind.EK_PROP;
		eXPRPROP.type = pType;
		eXPRPROP.flags = (EXPRFLAG)0;
		eXPRPROP.SetOptionalObjectThrough(pOptionalObjectThrough);
		eXPRPROP.SetOptionalArguments(pOptionalArguments);
		eXPRPROP.SetMemberGroup(pMemberGroup);
		if (pwtSlot != null)
		{
			eXPRPROP.pwtSlot = pwtSlot;
		}
		if (mwtSet != null)
		{
			eXPRPROP.mwtSet = mwtSet;
		}
		return eXPRPROP;
	}

	public EXPREVENT CreateEvent(CType pType, EXPR pOptionalObject, EventWithType EWT)
	{
		EXPREVENT eXPREVENT = new EXPREVENT();
		eXPREVENT.kind = ExpressionKind.EK_EVENT;
		eXPREVENT.type = pType;
		eXPREVENT.flags = (EXPRFLAG)0;
		eXPREVENT.OptionalObject = pOptionalObject;
		if (EWT != null)
		{
			eXPREVENT.ewt = EWT;
		}
		return eXPREVENT;
	}

	public EXPRMEMGRP CreateMemGroup(EXPRFLAG nFlags, Name pName, TypeArray pTypeArgs, SYMKIND symKind, CType pTypePar, MethodOrPropertySymbol pMPS, EXPR pObject, CMemberLookupResults memberLookupResults)
	{
		EXPRMEMGRP eXPRMEMGRP = new EXPRMEMGRP();
		eXPRMEMGRP.kind = ExpressionKind.EK_MEMGRP;
		eXPRMEMGRP.type = GetTypes().GetMethGrpType();
		eXPRMEMGRP.flags = nFlags;
		eXPRMEMGRP.name = pName;
		eXPRMEMGRP.typeArgs = pTypeArgs;
		eXPRMEMGRP.sk = symKind;
		eXPRMEMGRP.SetParentType(pTypePar);
		eXPRMEMGRP.SetOptionalObject(pObject);
		eXPRMEMGRP.SetMemberLookupResults(memberLookupResults);
		eXPRMEMGRP.SetOptionalLHS(null);
		if (eXPRMEMGRP.typeArgs == null)
		{
			eXPRMEMGRP.typeArgs = BSYMMGR.EmptyTypeArray();
		}
		return eXPRMEMGRP;
	}

	public EXPRMEMGRP CreateMemGroup(EXPR pObject, MethPropWithInst mwi)
	{
		Name name = ((mwi.Sym != null) ? mwi.Sym.name : null);
		MethodOrPropertySymbol methodOrPropertySymbol = mwi.MethProp();
		CType cType = mwi.GetType();
		if (cType == null)
		{
			cType = GetTypes().GetErrorSym();
		}
		return CreateMemGroup((EXPRFLAG)0, name, mwi.TypeArgs, methodOrPropertySymbol?.getKind() ?? SYMKIND.SK_MethodSymbol, mwi.GetType(), methodOrPropertySymbol, pObject, new CMemberLookupResults(GetGlobalSymbols().AllocParams(1, new CType[1] { cType }), name));
	}

	public EXPRUSERDEFINEDCONVERSION CreateUserDefinedConversion(EXPR arg, EXPR call, MethWithInst mwi)
	{
		EXPRUSERDEFINEDCONVERSION eXPRUSERDEFINEDCONVERSION = new EXPRUSERDEFINEDCONVERSION();
		eXPRUSERDEFINEDCONVERSION.kind = ExpressionKind.EK_USERDEFINEDCONVERSION;
		eXPRUSERDEFINEDCONVERSION.type = call.type;
		eXPRUSERDEFINEDCONVERSION.flags = (EXPRFLAG)0;
		eXPRUSERDEFINEDCONVERSION.Argument = arg;
		eXPRUSERDEFINEDCONVERSION.UserDefinedCall = call;
		eXPRUSERDEFINEDCONVERSION.UserDefinedCallMethod = mwi;
		if (call.HasError())
		{
			eXPRUSERDEFINEDCONVERSION.SetError();
		}
		return eXPRUSERDEFINEDCONVERSION;
	}

	public EXPRCAST CreateCast(EXPRFLAG nFlags, CType pType, EXPR pArg)
	{
		return CreateCast(nFlags, CreateClass(pType, null, null), pArg);
	}

	public EXPRCAST CreateCast(EXPRFLAG nFlags, EXPRTYPEORNAMESPACE pType, EXPR pArg)
	{
		EXPRCAST eXPRCAST = new EXPRCAST();
		eXPRCAST.type = pType.TypeOrNamespace as CType;
		eXPRCAST.kind = ExpressionKind.EK_CAST;
		eXPRCAST.Argument = pArg;
		eXPRCAST.flags = nFlags;
		eXPRCAST.DestinationType = pType;
		return eXPRCAST;
	}

	public EXPRRETURN CreateReturn(EXPRFLAG nFlags, Scope pCurrentScope, EXPR pOptionalObject)
	{
		return CreateReturn(nFlags, pCurrentScope, pOptionalObject, pOptionalObject);
	}

	public EXPRRETURN CreateReturn(EXPRFLAG nFlags, Scope pCurrentScope, EXPR pOptionalObject, EXPR pOptionalOriginalObject)
	{
		EXPRRETURN eXPRRETURN = new EXPRRETURN();
		eXPRRETURN.kind = ExpressionKind.EK_RETURN;
		eXPRRETURN.type = null;
		eXPRRETURN.flags = nFlags;
		eXPRRETURN.SetOptionalObject(pOptionalObject);
		return eXPRRETURN;
	}

	public EXPRLOCAL CreateLocal(EXPRFLAG nFlags, LocalVariableSymbol pLocal)
	{
		CType type = null;
		if (pLocal != null)
		{
			type = pLocal.GetType();
		}
		EXPRLOCAL eXPRLOCAL = new EXPRLOCAL();
		eXPRLOCAL.kind = ExpressionKind.EK_LOCAL;
		eXPRLOCAL.type = type;
		eXPRLOCAL.flags = nFlags;
		eXPRLOCAL.local = pLocal;
		return eXPRLOCAL;
	}

	public EXPRTHISPOINTER CreateThis(LocalVariableSymbol pLocal, bool fImplicit)
	{
		CType cType = null;
		if (pLocal != null)
		{
			cType = pLocal.GetType();
		}
		EXPRFLAG eXPRFLAG = EXPRFLAG.EXF_CANTBENULL;
		if (fImplicit)
		{
			eXPRFLAG |= EXPRFLAG.EXF_IMPLICITTHIS;
		}
		if (cType != null && cType.isStructType())
		{
			eXPRFLAG |= EXPRFLAG.EXF_LVALUE;
		}
		EXPRTHISPOINTER eXPRTHISPOINTER = new EXPRTHISPOINTER();
		eXPRTHISPOINTER.kind = ExpressionKind.EK_THISPOINTER;
		eXPRTHISPOINTER.type = cType;
		eXPRTHISPOINTER.flags = eXPRFLAG;
		eXPRTHISPOINTER.local = pLocal;
		return eXPRTHISPOINTER;
	}

	public EXPRBOUNDLAMBDA CreateAnonymousMethod(AggregateType delegateType)
	{
		EXPRBOUNDLAMBDA eXPRBOUNDLAMBDA = new EXPRBOUNDLAMBDA();
		eXPRBOUNDLAMBDA.kind = ExpressionKind.EK_BOUNDLAMBDA;
		eXPRBOUNDLAMBDA.type = delegateType;
		eXPRBOUNDLAMBDA.flags = (EXPRFLAG)0;
		return eXPRBOUNDLAMBDA;
	}

	public EXPRUNBOUNDLAMBDA CreateLambda()
	{
		CType anonMethType = GetTypes().GetAnonMethType();
		EXPRUNBOUNDLAMBDA eXPRUNBOUNDLAMBDA = new EXPRUNBOUNDLAMBDA();
		eXPRUNBOUNDLAMBDA.kind = ExpressionKind.EK_UNBOUNDLAMBDA;
		eXPRUNBOUNDLAMBDA.type = anonMethType;
		eXPRUNBOUNDLAMBDA.flags = (EXPRFLAG)0;
		return eXPRUNBOUNDLAMBDA;
	}

	public EXPRHOISTEDLOCALEXPR CreateHoistedLocalInExpression(EXPRLOCAL localToHoist)
	{
		EXPRHOISTEDLOCALEXPR eXPRHOISTEDLOCALEXPR = new EXPRHOISTEDLOCALEXPR();
		eXPRHOISTEDLOCALEXPR.kind = ExpressionKind.EK_HOISTEDLOCALEXPR;
		eXPRHOISTEDLOCALEXPR.type = GetTypes().GetOptPredefAgg(PredefinedType.PT_EXPRESSION).getThisType();
		eXPRHOISTEDLOCALEXPR.flags = (EXPRFLAG)0;
		return eXPRHOISTEDLOCALEXPR;
	}

	public EXPRMETHODINFO CreateMethodInfo(MethPropWithInst mwi)
	{
		return CreateMethodInfo(mwi.Meth(), mwi.GetType(), mwi.TypeArgs);
	}

	public EXPRMETHODINFO CreateMethodInfo(MethodSymbol method, AggregateType methodType, TypeArray methodParameters)
	{
		EXPRMETHODINFO eXPRMETHODINFO = new EXPRMETHODINFO();
		CType type = ((!method.IsConstructor()) ? GetTypes().GetOptPredefAgg(PredefinedType.PT_METHODINFO).getThisType() : GetTypes().GetOptPredefAgg(PredefinedType.PT_CONSTRUCTORINFO).getThisType());
		eXPRMETHODINFO.kind = ExpressionKind.EK_METHODINFO;
		eXPRMETHODINFO.type = type;
		eXPRMETHODINFO.flags = (EXPRFLAG)0;
		eXPRMETHODINFO.Method = new MethWithInst(method, methodType, methodParameters);
		return eXPRMETHODINFO;
	}

	public EXPRPropertyInfo CreatePropertyInfo(PropertySymbol prop, AggregateType propertyType)
	{
		EXPRPropertyInfo eXPRPropertyInfo = new EXPRPropertyInfo();
		eXPRPropertyInfo.kind = ExpressionKind.EK_PROPERTYINFO;
		eXPRPropertyInfo.type = GetTypes().GetOptPredefAgg(PredefinedType.PT_PROPERTYINFO).getThisType();
		eXPRPropertyInfo.flags = (EXPRFLAG)0;
		eXPRPropertyInfo.Property = new PropWithType(prop, propertyType);
		return eXPRPropertyInfo;
	}

	public EXPRFIELDINFO CreateFieldInfo(FieldSymbol field, AggregateType fieldType)
	{
		EXPRFIELDINFO eXPRFIELDINFO = new EXPRFIELDINFO();
		eXPRFIELDINFO.kind = ExpressionKind.EK_FIELDINFO;
		eXPRFIELDINFO.type = GetTypes().GetOptPredefAgg(PredefinedType.PT_FIELDINFO).getThisType();
		eXPRFIELDINFO.flags = (EXPRFLAG)0;
		eXPRFIELDINFO.Init(field, fieldType);
		return eXPRFIELDINFO;
	}

	public EXPRTYPEOF CreateTypeOf(EXPRTYPEORNAMESPACE pSourceType)
	{
		EXPRTYPEOF eXPRTYPEOF = new EXPRTYPEOF();
		eXPRTYPEOF.kind = ExpressionKind.EK_TYPEOF;
		eXPRTYPEOF.type = GetTypes().GetReqPredefAgg(PredefinedType.PT_TYPE).getThisType();
		eXPRTYPEOF.flags = EXPRFLAG.EXF_CANTBENULL;
		eXPRTYPEOF.SetSourceType(pSourceType);
		return eXPRTYPEOF;
	}

	public EXPRTYPEOF CreateTypeOf(CType pSourceType)
	{
		return CreateTypeOf(MakeClass(pSourceType));
	}

	public EXPRUSERLOGOP CreateUserLogOp(CType pType, EXPR pCallTF, EXPRCALL pCallOp)
	{
		EXPRUSERLOGOP eXPRUSERLOGOP = new EXPRUSERLOGOP();
		EXPR eXPR = pCallOp.GetOptionalArguments().asLIST().GetOptionalElement();
		if (eXPR.isWRAP())
		{
			eXPR = eXPR.asWRAP().GetOptionalExpression();
		}
		eXPRUSERLOGOP.kind = ExpressionKind.EK_USERLOGOP;
		eXPRUSERLOGOP.type = pType;
		eXPRUSERLOGOP.flags = EXPRFLAG.EXF_ASSGOP;
		eXPRUSERLOGOP.TrueFalseCall = pCallTF;
		eXPRUSERLOGOP.OperatorCall = pCallOp;
		eXPRUSERLOGOP.FirstOperandToExamine = eXPR;
		return eXPRUSERLOGOP;
	}

	public EXPRUSERLOGOP CreateUserLogOpError(CType pType, EXPR pCallTF, EXPRCALL pCallOp)
	{
		EXPRUSERLOGOP eXPRUSERLOGOP = CreateUserLogOp(pType, pCallTF, pCallOp);
		eXPRUSERLOGOP.SetError();
		return eXPRUSERLOGOP;
	}

	public EXPRCONCAT CreateConcat(EXPR op1, EXPR op2)
	{
		CType type = op1.type;
		if (!type.isPredefType(PredefinedType.PT_STRING))
		{
			type = op2.type;
		}
		EXPRCONCAT eXPRCONCAT = new EXPRCONCAT();
		eXPRCONCAT.kind = ExpressionKind.EK_CONCAT;
		eXPRCONCAT.type = type;
		eXPRCONCAT.flags = (EXPRFLAG)0;
		eXPRCONCAT.SetFirstArgument(op1);
		eXPRCONCAT.SetSecondArgument(op2);
		return eXPRCONCAT;
	}

	public EXPRCONSTANT CreateStringConstant(string str)
	{
		return CreateConstant(GetTypes().GetReqPredefAgg(PredefinedType.PT_STRING).getThisType(), m_constants.Create(str));
	}

	public EXPRMULTIGET CreateMultiGet(EXPRFLAG nFlags, CType pType, EXPRMULTI pOptionalMulti)
	{
		EXPRMULTIGET eXPRMULTIGET = new EXPRMULTIGET();
		eXPRMULTIGET.kind = ExpressionKind.EK_MULTIGET;
		eXPRMULTIGET.type = pType;
		eXPRMULTIGET.flags = nFlags;
		eXPRMULTIGET.SetOptionalMulti(pOptionalMulti);
		return eXPRMULTIGET;
	}

	public EXPRMULTI CreateMulti(EXPRFLAG nFlags, CType pType, EXPR pLeft, EXPR pOp)
	{
		EXPRMULTI eXPRMULTI = new EXPRMULTI();
		eXPRMULTI.kind = ExpressionKind.EK_MULTI;
		eXPRMULTI.type = pType;
		eXPRMULTI.flags = nFlags;
		eXPRMULTI.SetLeft(pLeft);
		eXPRMULTI.SetOperator(pOp);
		return eXPRMULTI;
	}

	public EXPR CreateZeroInit(CType pType)
	{
		EXPRCLASS pTypeExpr = MakeClass(pType);
		return CreateZeroInit(pTypeExpr);
	}

	public EXPR CreateZeroInit(EXPRTYPEORNAMESPACE pTypeExpr)
	{
		return CreateZeroInit(pTypeExpr, null, isConstructor: false);
	}

	private EXPR CreateZeroInit(EXPRTYPEORNAMESPACE pTypeExpr, EXPR pOptionalOriginalConstructorCall, bool isConstructor)
	{
		CType cType = pTypeExpr.TypeOrNamespace.AsType();
		bool flag = false;
		if (cType.isEnumType())
		{
			ConstValFactory constValFactory = new ConstValFactory();
			return CreateConstant(cType, constValFactory.Create(Activator.CreateInstance(cType.AssociatedSystemType)));
		}
		switch (cType.fundType())
		{
		default:
			flag = true;
			break;
		case FUNDTYPE.FT_PTR:
		{
			CType nullType = GetTypes().GetNullType();
			if (nullType.fundType() == cType.fundType())
			{
				return CreateConstant(cType, ConstValFactory.GetDefaultValue(ConstValKind.IntPtr));
			}
			return CreateCast((EXPRFLAG)0, pTypeExpr, CreateNull());
		}
		case FUNDTYPE.FT_I1:
		case FUNDTYPE.FT_I2:
		case FUNDTYPE.FT_I4:
		case FUNDTYPE.FT_U1:
		case FUNDTYPE.FT_U2:
		case FUNDTYPE.FT_U4:
		case FUNDTYPE.FT_I8:
		case FUNDTYPE.FT_U8:
		case FUNDTYPE.FT_R4:
		case FUNDTYPE.FT_R8:
		case FUNDTYPE.FT_REF:
		{
			EXPRCONSTANT result2 = CreateConstant(cType, ConstValFactory.GetDefaultValue(cType.constValKind()));
			EXPRCONSTANT eXPRCONSTANT2 = CreateConstant(cType, ConstValFactory.GetDefaultValue(cType.constValKind()));
			eXPRCONSTANT2.SetOptionalConstructorCall(pOptionalOriginalConstructorCall);
			return result2;
		}
		case FUNDTYPE.FT_STRUCT:
			if (cType.isPredefType(PredefinedType.PT_DECIMAL))
			{
				EXPRCONSTANT result = CreateConstant(cType, ConstValFactory.GetDefaultValue(cType.constValKind()));
				EXPRCONSTANT eXPRCONSTANT = CreateConstant(cType, ConstValFactory.GetDefaultValue(cType.constValKind()));
				eXPRCONSTANT.SetOptionalConstructorCall(pOptionalOriginalConstructorCall);
				return result;
			}
			break;
		case FUNDTYPE.FT_VAR:
			break;
		}
		EXPRZEROINIT eXPRZEROINIT = new EXPRZEROINIT();
		eXPRZEROINIT.kind = ExpressionKind.EK_ZEROINIT;
		eXPRZEROINIT.type = cType;
		eXPRZEROINIT.flags = (EXPRFLAG)0;
		eXPRZEROINIT.OptionalConstructorCall = pOptionalOriginalConstructorCall;
		eXPRZEROINIT.IsConstructor = isConstructor;
		if (flag)
		{
			eXPRZEROINIT.SetError();
		}
		return eXPRZEROINIT;
	}

	public EXPRCONSTANT CreateConstant(CType pType, CONSTVAL constVal)
	{
		return CreateConstant(pType, constVal, null);
	}

	public EXPRCONSTANT CreateConstant(CType pType, CONSTVAL constVal, EXPR pOriginal)
	{
		EXPRCONSTANT eXPRCONSTANT = CreateConstant(pType);
		eXPRCONSTANT.setVal(constVal);
		return eXPRCONSTANT;
	}

	public EXPRCONSTANT CreateConstant(CType pType)
	{
		EXPRCONSTANT eXPRCONSTANT = new EXPRCONSTANT();
		eXPRCONSTANT.kind = ExpressionKind.EK_CONSTANT;
		eXPRCONSTANT.type = pType;
		eXPRCONSTANT.flags = (EXPRFLAG)0;
		return eXPRCONSTANT;
	}

	public EXPRCONSTANT CreateIntegerConstant(int x)
	{
		return CreateConstant(GetTypes().GetReqPredefAgg(PredefinedType.PT_INT).getThisType(), ConstValFactory.GetInt(x));
	}

	public EXPRCONSTANT CreateBoolConstant(bool b)
	{
		return CreateConstant(GetTypes().GetReqPredefAgg(PredefinedType.PT_BOOL).getThisType(), ConstValFactory.GetBool(b));
	}

	public EXPRBLOCK CreateBlock(EXPRBLOCK pOptionalCurrentBlock, EXPRSTMT pOptionalStatements, Scope pOptionalScope)
	{
		EXPRBLOCK eXPRBLOCK = new EXPRBLOCK();
		eXPRBLOCK.kind = ExpressionKind.EK_BLOCK;
		eXPRBLOCK.type = null;
		eXPRBLOCK.flags = (EXPRFLAG)0;
		eXPRBLOCK.SetOptionalStatements(pOptionalStatements);
		eXPRBLOCK.OptionalScopeSymbol = pOptionalScope;
		return eXPRBLOCK;
	}

	public EXPRQUESTIONMARK CreateQuestionMark(EXPR pTestExpression, EXPRBINOP pConsequence)
	{
		CType type = pConsequence.type;
		if (type == null)
		{
			type = pConsequence.GetOptionalLeftChild().type;
		}
		EXPRQUESTIONMARK eXPRQUESTIONMARK = new EXPRQUESTIONMARK();
		eXPRQUESTIONMARK.kind = ExpressionKind.EK_QUESTIONMARK;
		eXPRQUESTIONMARK.type = type;
		eXPRQUESTIONMARK.flags = (EXPRFLAG)0;
		eXPRQUESTIONMARK.SetTestExpression(pTestExpression);
		eXPRQUESTIONMARK.SetConsequence(pConsequence);
		return eXPRQUESTIONMARK;
	}

	public EXPRARRAYINDEX CreateArrayIndex(EXPR pArray, EXPR pIndex)
	{
		CType cType = pArray.type;
		if (cType != null && cType.IsArrayType())
		{
			cType = cType.AsArrayType().GetElementType();
		}
		else if (cType == null)
		{
			cType = GetTypes().GetReqPredefAgg(PredefinedType.PT_INT).getThisType();
		}
		EXPRARRAYINDEX eXPRARRAYINDEX = new EXPRARRAYINDEX();
		eXPRARRAYINDEX.kind = ExpressionKind.EK_ARRAYINDEX;
		eXPRARRAYINDEX.type = cType;
		eXPRARRAYINDEX.flags = (EXPRFLAG)0;
		eXPRARRAYINDEX.SetArray(pArray);
		eXPRARRAYINDEX.SetIndex(pIndex);
		return eXPRARRAYINDEX;
	}

	public EXPRARRAYLENGTH CreateArrayLength(EXPR pArray)
	{
		EXPRARRAYLENGTH eXPRARRAYLENGTH = new EXPRARRAYLENGTH();
		eXPRARRAYLENGTH.kind = ExpressionKind.EK_ARRAYLENGTH;
		eXPRARRAYLENGTH.type = GetTypes().GetReqPredefAgg(PredefinedType.PT_INT).getThisType();
		eXPRARRAYLENGTH.flags = (EXPRFLAG)0;
		eXPRARRAYLENGTH.SetArray(pArray);
		return eXPRARRAYLENGTH;
	}

	public EXPRBINOP CreateBinop(ExpressionKind exprKind, CType pType, EXPR p1, EXPR p2)
	{
		EXPRBINOP eXPRBINOP = new EXPRBINOP();
		eXPRBINOP.kind = exprKind;
		eXPRBINOP.type = pType;
		eXPRBINOP.flags = EXPRFLAG.EXF_BINOP;
		eXPRBINOP.SetOptionalLeftChild(p1);
		eXPRBINOP.SetOptionalRightChild(p2);
		eXPRBINOP.isLifted = false;
		eXPRBINOP.SetOptionalUserDefinedCall(null);
		eXPRBINOP.SetUserDefinedCallMethod(null);
		return eXPRBINOP;
	}

	public EXPRUNARYOP CreateUnaryOp(ExpressionKind exprKind, CType pType, EXPR pOperand)
	{
		EXPRUNARYOP eXPRUNARYOP = new EXPRUNARYOP();
		eXPRUNARYOP.kind = exprKind;
		eXPRUNARYOP.type = pType;
		eXPRUNARYOP.flags = (EXPRFLAG)0;
		eXPRUNARYOP.Child = pOperand;
		eXPRUNARYOP.OptionalUserDefinedCall = null;
		eXPRUNARYOP.UserDefinedCallMethod = null;
		return eXPRUNARYOP;
	}

	public EXPR CreateOperator(ExpressionKind exprKind, CType pType, EXPR pArg1, EXPR pOptionalArg2)
	{
		EXPR eXPR = null;
		if (exprKind.isUnaryOperator())
		{
			return CreateUnaryOp(exprKind, pType, pArg1);
		}
		return CreateBinop(exprKind, pType, pArg1, pOptionalArg2);
	}

	public EXPRBINOP CreateUserDefinedBinop(ExpressionKind exprKind, CType pType, EXPR p1, EXPR p2, EXPR call, MethPropWithInst pmpwi)
	{
		EXPRBINOP eXPRBINOP = new EXPRBINOP();
		eXPRBINOP.kind = exprKind;
		eXPRBINOP.type = pType;
		eXPRBINOP.flags = EXPRFLAG.EXF_BINOP;
		eXPRBINOP.SetOptionalLeftChild(p1);
		eXPRBINOP.SetOptionalRightChild(p2);
		eXPRBINOP.isLifted = false;
		eXPRBINOP.SetOptionalUserDefinedCall(call);
		eXPRBINOP.SetUserDefinedCallMethod(pmpwi);
		if (call.HasError())
		{
			eXPRBINOP.SetError();
		}
		return eXPRBINOP;
	}

	public EXPRUNARYOP CreateUserDefinedUnaryOperator(ExpressionKind exprKind, CType pType, EXPR pOperand, EXPR call, MethPropWithInst pmpwi)
	{
		EXPRUNARYOP eXPRUNARYOP = new EXPRUNARYOP();
		eXPRUNARYOP.kind = exprKind;
		eXPRUNARYOP.type = pType;
		eXPRUNARYOP.flags = (EXPRFLAG)0;
		eXPRUNARYOP.Child = pOperand;
		eXPRUNARYOP.OptionalUserDefinedCall = call;
		eXPRUNARYOP.UserDefinedCallMethod = pmpwi;
		if (call.HasError())
		{
			eXPRUNARYOP.SetError();
		}
		return eXPRUNARYOP;
	}

	public EXPRUNARYOP CreateNeg(EXPRFLAG nFlags, EXPR pOperand)
	{
		EXPRUNARYOP eXPRUNARYOP = CreateUnaryOp(ExpressionKind.EK_NEG, pOperand.type, pOperand);
		eXPRUNARYOP.flags |= nFlags;
		return eXPRUNARYOP;
	}

	public EXPRBINOP CreateSequence(EXPR p1, EXPR p2)
	{
		return CreateBinop(ExpressionKind.EK_SEQUENCE, p2.type, p1, p2);
	}

	public EXPRBINOP CreateReverseSequence(EXPR p1, EXPR p2)
	{
		return CreateBinop(ExpressionKind.EK_SEQREV, p1.type, p1, p2);
	}

	public EXPRASSIGNMENT CreateAssignment(EXPR pLHS, EXPR pRHS)
	{
		EXPRASSIGNMENT eXPRASSIGNMENT = new EXPRASSIGNMENT();
		eXPRASSIGNMENT.kind = ExpressionKind.EK_ASSIGNMENT;
		eXPRASSIGNMENT.type = pLHS.type;
		eXPRASSIGNMENT.flags = EXPRFLAG.EXF_ASSGOP;
		eXPRASSIGNMENT.SetLHS(pLHS);
		eXPRASSIGNMENT.SetRHS(pRHS);
		return eXPRASSIGNMENT;
	}

	public EXPRNamedArgumentSpecification CreateNamedArgumentSpecification(Name pName, EXPR pValue)
	{
		EXPRNamedArgumentSpecification eXPRNamedArgumentSpecification = new EXPRNamedArgumentSpecification();
		eXPRNamedArgumentSpecification.kind = ExpressionKind.EK_NamedArgumentSpecification;
		eXPRNamedArgumentSpecification.type = pValue.type;
		eXPRNamedArgumentSpecification.flags = (EXPRFLAG)0;
		eXPRNamedArgumentSpecification.Value = pValue;
		eXPRNamedArgumentSpecification.Name = pName;
		return eXPRNamedArgumentSpecification;
	}

	public EXPRWRAP CreateWrap(Scope pCurrentScope, EXPR pOptionalExpression)
	{
		EXPRWRAP eXPRWRAP = new EXPRWRAP();
		eXPRWRAP.kind = ExpressionKind.EK_WRAP;
		eXPRWRAP.type = null;
		eXPRWRAP.flags = (EXPRFLAG)0;
		eXPRWRAP.SetOptionalExpression(pOptionalExpression);
		if (pOptionalExpression != null)
		{
			eXPRWRAP.setType(pOptionalExpression.type);
		}
		eXPRWRAP.flags |= EXPRFLAG.EXF_LVALUE;
		return eXPRWRAP;
	}

	public EXPRWRAP CreateWrapNoAutoFree(Scope pCurrentScope, EXPR pOptionalWrap)
	{
		return CreateWrap(pCurrentScope, pOptionalWrap);
	}

	public EXPRBINOP CreateSave(EXPRWRAP wrap)
	{
		EXPRBINOP eXPRBINOP = CreateBinop(ExpressionKind.EK_SAVE, wrap.type, wrap.GetOptionalExpression(), wrap);
		eXPRBINOP.setAssignment();
		return eXPRBINOP;
	}

	public EXPR CreateNull()
	{
		return CreateConstant(GetTypes().GetNullType(), ConstValFactory.GetNullRef());
	}

	public void AppendItemToList(EXPR newItem, ref EXPR first, ref EXPR last)
	{
		if (newItem != null)
		{
			if (first == null)
			{
				first = newItem;
				last = newItem;
			}
			else if (first.kind != ExpressionKind.EK_LIST)
			{
				first = CreateList(first, newItem);
				last = first;
			}
			else
			{
				last.asLIST().OptionalNextListNode = CreateList(last.asLIST().OptionalNextListNode, newItem);
				last = last.asLIST().OptionalNextListNode;
			}
		}
	}

	public EXPRLIST CreateList(EXPR op1, EXPR op2)
	{
		EXPRLIST eXPRLIST = new EXPRLIST();
		eXPRLIST.kind = ExpressionKind.EK_LIST;
		eXPRLIST.type = null;
		eXPRLIST.flags = (EXPRFLAG)0;
		eXPRLIST.SetOptionalElement(op1);
		eXPRLIST.SetOptionalNextListNode(op2);
		return eXPRLIST;
	}

	public EXPRLIST CreateList(EXPR op1, EXPR op2, EXPR op3)
	{
		return CreateList(op1, CreateList(op2, op3));
	}

	public EXPRLIST CreateList(EXPR op1, EXPR op2, EXPR op3, EXPR op4)
	{
		return CreateList(op1, CreateList(op2, CreateList(op3, op4)));
	}

	public EXPRTYPEARGUMENTS CreateTypeArguments(TypeArray pTypeArray, EXPR pOptionalElements)
	{
		EXPRTYPEARGUMENTS eXPRTYPEARGUMENTS = new EXPRTYPEARGUMENTS();
		eXPRTYPEARGUMENTS.kind = ExpressionKind.EK_TYPEARGUMENTS;
		eXPRTYPEARGUMENTS.type = null;
		eXPRTYPEARGUMENTS.flags = (EXPRFLAG)0;
		eXPRTYPEARGUMENTS.SetOptionalElements(pOptionalElements);
		return eXPRTYPEARGUMENTS;
	}

	public EXPRCLASS CreateClass(CType pType, EXPR pOptionalLHS, EXPRTYPEARGUMENTS pOptionalTypeArguments)
	{
		EXPRCLASS eXPRCLASS = new EXPRCLASS();
		eXPRCLASS.kind = ExpressionKind.EK_CLASS;
		eXPRCLASS.type = pType;
		eXPRCLASS.TypeOrNamespace = pType;
		return eXPRCLASS;
	}

	public EXPRCLASS MakeClass(CType pType)
	{
		return CreateClass(pType, null, null);
	}
}
