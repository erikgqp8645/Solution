using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CSharp.RuntimeBinder.Errors;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class ExpressionBinder
{
	protected class BinOpArgInfo
	{
		public EXPR arg1;

		public EXPR arg2;

		public PredefinedType pt1;

		public PredefinedType pt2;

		public PredefinedType ptRaw1;

		public PredefinedType ptRaw2;

		public CType type1;

		public CType type2;

		public CType typeRaw1;

		public CType typeRaw2;

		public BinOpKind binopKind;

		public BinOpMask mask;

		public BinOpArgInfo(EXPR op1, EXPR op2)
		{
			arg1 = op1;
			arg2 = op2;
			type1 = arg1.type;
			type2 = arg2.type;
			typeRaw1 = type1.StripNubs();
			typeRaw2 = type2.StripNubs();
			pt1 = (type1.isPredefined() ? type1.getPredefType() : PredefinedType.PT_COUNT);
			pt2 = (type2.isPredefined() ? type2.getPredefType() : PredefinedType.PT_COUNT);
			ptRaw1 = (typeRaw1.isPredefined() ? typeRaw1.getPredefType() : PredefinedType.PT_COUNT);
			ptRaw2 = (typeRaw2.isPredefined() ? typeRaw2.getPredefType() : PredefinedType.PT_COUNT);
		}

		public bool ValidForDelegate()
		{
			return (mask & BinOpMask.Delegate) != 0;
		}

		public bool ValidForEnumAndUnderlyingType()
		{
			return (mask & BinOpMask.EnumUnder) != 0;
		}

		public bool ValidForUnderlyingTypeAndEnum()
		{
			return (mask & BinOpMask.Add) != 0;
		}

		public bool ValidForEnum()
		{
			return (mask & BinOpMask.Enum) != 0;
		}

		public bool ValidForPointer()
		{
			return (mask & BinOpMask.Sub) != 0;
		}

		public bool ValidForVoidPointer()
		{
			return (mask & BinOpMask.VoidPtr) != 0;
		}

		public bool ValidForPointerAndNumber()
		{
			return (mask & BinOpMask.EnumUnder) != 0;
		}

		public bool ValidForNumberAndPointer()
		{
			return (mask & BinOpMask.Add) != 0;
		}
	}

	protected class BinOpSig
	{
		public PredefinedType pt1;

		public PredefinedType pt2;

		public BinOpMask mask;

		public int cbosSkip;

		public PfnBindBinOp pfn;

		public OpSigFlags grfos;

		public BinOpFuncKind fnkind;

		public BinOpSig()
		{
		}

		public BinOpSig(PredefinedType pt1, PredefinedType pt2, BinOpMask mask, int cbosSkip, PfnBindBinOp pfn, OpSigFlags grfos, BinOpFuncKind fnkind)
		{
			this.pt1 = pt1;
			this.pt2 = pt2;
			this.mask = mask;
			this.cbosSkip = cbosSkip;
			this.pfn = pfn;
			this.grfos = grfos;
			this.fnkind = fnkind;
		}

		public bool ConvertOperandsBeforeBinding()
		{
			return (grfos & OpSigFlags.Convert) != 0;
		}

		public bool CanLift()
		{
			return (grfos & OpSigFlags.CanLift) != 0;
		}

		public bool AutoLift()
		{
			return (grfos & OpSigFlags.AutoLift) != 0;
		}
	}

	protected class BinOpFullSig : BinOpSig
	{
		private LiftFlags grflt;

		private CType type1;

		private CType type2;

		public BinOpFullSig(CType type1, CType type2, PfnBindBinOp pfn, OpSigFlags grfos, LiftFlags grflt, BinOpFuncKind fnkind)
		{
			pt1 = PredefinedType.PT_UNDEFINEDINDEX;
			pt2 = PredefinedType.PT_UNDEFINEDINDEX;
			mask = BinOpMask.None;
			cbosSkip = 0;
			base.pfn = pfn;
			base.grfos = grfos;
			this.type1 = type1;
			this.type2 = type2;
			this.grflt = grflt;
			base.fnkind = fnkind;
		}

		public BinOpFullSig(ExpressionBinder fnc, BinOpSig bos)
		{
			pt1 = bos.pt1;
			pt2 = bos.pt2;
			mask = bos.mask;
			cbosSkip = bos.cbosSkip;
			pfn = bos.pfn;
			grfos = bos.grfos;
			fnkind = bos.fnkind;
			type1 = ((pt1 != PredefinedType.PT_UNDEFINEDINDEX) ? fnc.GetOptPDT(pt1) : null);
			type2 = ((pt2 != PredefinedType.PT_UNDEFINEDINDEX) ? fnc.GetOptPDT(pt2) : null);
			grflt = LiftFlags.None;
		}

		public bool FPreDef()
		{
			return pt1 != PredefinedType.PT_UNDEFINEDINDEX;
		}

		public bool isLifted()
		{
			if (grflt == LiftFlags.None)
			{
				return false;
			}
			return true;
		}

		public bool ConvertFirst()
		{
			return (grflt & LiftFlags.Convert1) != 0;
		}

		public bool ConvertSecond()
		{
			return (grflt & LiftFlags.Convert2) != 0;
		}

		public CType Type1()
		{
			return type1;
		}

		public CType Type2()
		{
			return type2;
		}
	}

	private delegate bool ConversionFunc(EXPR pSourceExpr, CType pSourceType, EXPRTYPEORNAMESPACE pDestinationTypeExpr, CType pDestinationTypeForLambdaErrorReporting, bool needsExprDest, out EXPR ppDestinationExpr, CONVERTTYPE flags);

	private class ExplicitConversion
	{
		private ExpressionBinder binder;

		private EXPR exprSrc;

		private CType typeSrc;

		private CType typeDest;

		private EXPRTYPEORNAMESPACE exprTypeDest;

		private CType m_pDestinationTypeForLambdaErrorReporting;

		private EXPR exprDest;

		private bool needsExprDest;

		private CONVERTTYPE flags;

		public EXPR ExprDest => exprDest;

		public ExplicitConversion(ExpressionBinder binder, EXPR exprSrc, CType typeSrc, EXPRTYPEORNAMESPACE typeDest, CType pDestinationTypeForLambdaErrorReporting, bool needsExprDest, CONVERTTYPE flags)
		{
			this.binder = binder;
			this.exprSrc = exprSrc;
			this.typeSrc = typeSrc;
			this.typeDest = typeDest.TypeOrNamespace.AsType();
			m_pDestinationTypeForLambdaErrorReporting = pDestinationTypeForLambdaErrorReporting;
			exprTypeDest = typeDest;
			this.needsExprDest = needsExprDest;
			this.flags = flags;
			exprDest = null;
		}

		public bool Bind()
		{
			if (binder.BindImplicitConversion(exprSrc, typeSrc, exprTypeDest, m_pDestinationTypeForLambdaErrorReporting, needsExprDest, out exprDest, flags | CONVERTTYPE.ISEXPLICIT))
			{
				return true;
			}
			if (typeSrc == null || typeDest == null || typeSrc.IsErrorType() || typeDest.IsErrorType() || typeDest.IsNeverSameType())
			{
				return false;
			}
			if (typeDest.IsNullableType())
			{
				return false;
			}
			if (typeSrc.IsNullableType())
			{
				return bindExplicitConversionFromNub();
			}
			if (bindExplicitConversionFromArrayToIList())
			{
				return true;
			}
			switch (typeDest.GetTypeKind())
			{
			default:
				VSFAIL("Bad type kind");
				return false;
			case TypeKind.TK_VoidType:
				return false;
			case TypeKind.TK_NullType:
				return false;
			case TypeKind.TK_TypeParameterType:
				if (bindExplicitConversionToTypeVar())
				{
					return true;
				}
				break;
			case TypeKind.TK_ArrayType:
				if (bindExplicitConversionToArray(typeDest.AsArrayType()))
				{
					return true;
				}
				break;
			case TypeKind.TK_PointerType:
				if (bindExplicitConversionToPointer())
				{
					return true;
				}
				break;
			case TypeKind.TK_AggregateType:
				switch (bindExplicitConversionToAggregate(typeDest.AsAggregateType()))
				{
				case AggCastResult.Success:
					return true;
				case AggCastResult.Abort:
					return false;
				}
				break;
			}
			if ((flags & CONVERTTYPE.NOUDC) == 0)
			{
				return binder.bindUserDefinedConversion(exprSrc, typeSrc, typeDest, needsExprDest, out exprDest, fImplicitOnly: false);
			}
			return false;
		}

		private bool bindExplicitConversionFromNub()
		{
			if (typeDest.IsValType() && binder.BindExplicitConversion(null, typeSrc.StripNubs(), exprTypeDest, m_pDestinationTypeForLambdaErrorReporting, flags | CONVERTTYPE.NOUDC))
			{
				if (needsExprDest)
				{
					EXPR eXPR = exprSrc;
					while (eXPR.type.IsNullableType())
					{
						eXPR = binder.BindNubValue(eXPR);
					}
					if (!binder.BindExplicitConversion(eXPR, eXPR.type, exprTypeDest, m_pDestinationTypeForLambdaErrorReporting, needsExprDest, out exprDest, flags | CONVERTTYPE.NOUDC))
					{
						VSFAIL("BindExplicitConversion failed unexpectedly");
						return false;
					}
					if (exprDest.kind == ExpressionKind.EK_USERDEFINEDCONVERSION)
					{
						exprDest.asUSERDEFINEDCONVERSION().Argument = exprSrc;
					}
				}
				return true;
			}
			if ((flags & CONVERTTYPE.NOUDC) == 0)
			{
				return binder.bindUserDefinedConversion(exprSrc, typeSrc, typeDest, needsExprDest, out exprDest, fImplicitOnly: false);
			}
			return false;
		}

		private bool bindExplicitConversionFromArrayToIList()
		{
			if (!typeSrc.IsArrayType() || typeSrc.AsArrayType().rank != 1 || !typeDest.isInterfaceType() || typeDest.AsAggregateType().GetTypeArgsAll().Size != 1)
			{
				return false;
			}
			AggregateSymbol optPredefAgg = GetSymbolLoader().GetOptPredefAgg(PredefinedType.PT_G_ILIST);
			AggregateSymbol optPredefAgg2 = GetSymbolLoader().GetOptPredefAgg(PredefinedType.PT_G_IREADONLYLIST);
			if ((optPredefAgg == null || !GetSymbolLoader().IsBaseAggregate(optPredefAgg, typeDest.AsAggregateType().getAggregate())) && (optPredefAgg2 == null || !GetSymbolLoader().IsBaseAggregate(optPredefAgg2, typeDest.AsAggregateType().getAggregate())))
			{
				return false;
			}
			CType elementType = typeSrc.AsArrayType().GetElementType();
			CType typeDst = typeDest.AsAggregateType().GetTypeArgsAll().Item(0);
			if (!CConversions.FExpRefConv(GetSymbolLoader(), elementType, typeDst))
			{
				return false;
			}
			if (needsExprDest)
			{
				binder.bindSimpleCast(exprSrc, exprTypeDest, out exprDest, EXPRFLAG.EXF_OPERATOR);
			}
			return true;
		}

		private bool bindExplicitConversionToTypeVar()
		{
			if (typeSrc.isInterfaceType() || binder.canConvert(typeDest, typeSrc, CONVERTTYPE.NOUDC))
			{
				if (!needsExprDest)
				{
					return true;
				}
				if (typeSrc.IsTypeParameterType())
				{
					EXPRCLASS eXPRCLASS = GetExprFactory().MakeClass(binder.GetReqPDT(PredefinedType.PT_OBJECT));
					binder.bindSimpleCast(exprSrc, eXPRCLASS, out var pexprDest, EXPRFLAG.EXF_UNREALIZEDGOTO);
					exprSrc = pexprDest;
				}
				if (needsExprDest)
				{
					binder.bindSimpleCast(exprSrc, exprTypeDest, out exprDest, EXPRFLAG.EXF_ASFINALLYLEAVE);
				}
				return true;
			}
			return false;
		}

		private bool bindExplicitConversionFromIListToArray(ArrayType arrayDest)
		{
			if (arrayDest.rank != 1 || !typeSrc.isInterfaceType() || typeSrc.AsAggregateType().GetTypeArgsAll().Size != 1)
			{
				return false;
			}
			AggregateSymbol optPredefAgg = GetSymbolLoader().GetOptPredefAgg(PredefinedType.PT_G_ILIST);
			AggregateSymbol optPredefAgg2 = GetSymbolLoader().GetOptPredefAgg(PredefinedType.PT_G_IREADONLYLIST);
			if ((optPredefAgg == null || !GetSymbolLoader().IsBaseAggregate(optPredefAgg, typeSrc.AsAggregateType().getAggregate())) && (optPredefAgg2 == null || !GetSymbolLoader().IsBaseAggregate(optPredefAgg2, typeSrc.AsAggregateType().getAggregate())))
			{
				return false;
			}
			CType elementType = arrayDest.GetElementType();
			CType cType = typeSrc.AsAggregateType().GetTypeArgsAll().Item(0);
			if (elementType != cType && !CConversions.FExpRefConv(GetSymbolLoader(), elementType, cType))
			{
				return false;
			}
			if (needsExprDest)
			{
				binder.bindSimpleCast(exprSrc, exprTypeDest, out exprDest, EXPRFLAG.EXF_OPERATOR);
			}
			return true;
		}

		private bool bindExplicitConversionFromArrayToArray(ArrayType arraySrc, ArrayType arrayDest)
		{
			if (arraySrc.rank != arrayDest.rank)
			{
				return false;
			}
			if (CConversions.FExpRefConv(GetSymbolLoader(), arraySrc.GetElementType(), arrayDest.GetElementType()))
			{
				if (needsExprDest)
				{
					binder.bindSimpleCast(exprSrc, exprTypeDest, out exprDest, EXPRFLAG.EXF_OPERATOR);
				}
				return true;
			}
			return false;
		}

		private bool bindExplicitConversionToArray(ArrayType arrayDest)
		{
			if (typeSrc.IsArrayType())
			{
				return bindExplicitConversionFromArrayToArray(typeSrc.AsArrayType(), arrayDest);
			}
			if (bindExplicitConversionFromIListToArray(arrayDest))
			{
				return true;
			}
			if (binder.canConvert(binder.GetReqPDT(PredefinedType.PT_ARRAY), typeSrc, CONVERTTYPE.NOUDC))
			{
				if (needsExprDest)
				{
					binder.bindSimpleCast(exprSrc, exprTypeDest, out exprDest, EXPRFLAG.EXF_OPERATOR);
				}
				return true;
			}
			return false;
		}

		private bool bindExplicitConversionToPointer()
		{
			if (typeSrc.IsPointerType() || (typeSrc.fundType() <= FUNDTYPE.FT_U8 && typeSrc.isNumericType()))
			{
				if (needsExprDest)
				{
					binder.bindSimpleCast(exprSrc, exprTypeDest, out exprDest);
				}
				return true;
			}
			return false;
		}

		private AggCastResult bindExplicitConversionFromEnumToAggregate(AggregateType aggTypeDest)
		{
			if (!typeSrc.isEnumType())
			{
				return AggCastResult.Failure;
			}
			AggregateSymbol aggregate = aggTypeDest.getAggregate();
			if (aggregate.isPredefAgg(PredefinedType.PT_DECIMAL))
			{
				return bindExplicitConversionFromEnumToDecimal(aggTypeDest);
			}
			if (!aggregate.getThisType().isNumericType() && !aggregate.IsEnum() && (!aggregate.IsPredefined() || aggregate.GetPredefType() != PredefinedType.PT_CHAR))
			{
				return AggCastResult.Failure;
			}
			if (exprSrc.GetConst() != null)
			{
				switch (binder.bindConstantCast(exprSrc, exprTypeDest, needsExprDest, out exprDest, explicitConversion: true))
				{
				case ConstCastResult.Success:
					return AggCastResult.Success;
				case ConstCastResult.CheckFailure:
					return AggCastResult.Abort;
				}
			}
			if (needsExprDest)
			{
				binder.bindSimpleCast(exprSrc, exprTypeDest, out exprDest);
			}
			return AggCastResult.Success;
		}

		private AggCastResult bindExplicitConversionFromDecimalToEnum(AggregateType aggTypeDest)
		{
			if (exprSrc.GetConst() != null)
			{
				switch (binder.bindConstantCast(exprSrc, exprTypeDest, needsExprDest, out exprDest, explicitConversion: true))
				{
				case ConstCastResult.Success:
					return AggCastResult.Success;
				case ConstCastResult.CheckFailure:
					if ((flags & CONVERTTYPE.CHECKOVERFLOW) == 0)
					{
						return AggCastResult.Abort;
					}
					break;
				}
			}
			bool flag = true;
			if (needsExprDest)
			{
				CType typeDst = aggTypeDest.underlyingType();
				flag = binder.bindUserDefinedConversion(exprSrc, typeSrc, typeDst, needsExprDest, out exprDest, fImplicitOnly: false);
				if (flag)
				{
					binder.bindSimpleCast(exprDest, exprTypeDest, out exprDest);
				}
			}
			if (!flag)
			{
				return AggCastResult.Failure;
			}
			return AggCastResult.Success;
		}

		private AggCastResult bindExplicitConversionFromEnumToDecimal(AggregateType aggTypeDest)
		{
			AggregateType pType = typeSrc.underlyingType().AsAggregateType();
			EXPR pexprDest;
			if (exprSrc == null)
			{
				pexprDest = null;
			}
			else
			{
				EXPRCLASS eXPRCLASS = GetExprFactory().MakeClass(pType);
				binder.bindSimpleCast(exprSrc, eXPRCLASS, out pexprDest);
			}
			if (pexprDest.GetConst() != null)
			{
				switch (binder.bindConstantCast(pexprDest, exprTypeDest, needsExprDest, out exprDest, explicitConversion: true))
				{
				case ConstCastResult.Success:
					return AggCastResult.Success;
				case ConstCastResult.CheckFailure:
					if ((flags & CONVERTTYPE.CHECKOVERFLOW) == 0)
					{
						return AggCastResult.Abort;
					}
					break;
				}
			}
			if (needsExprDest)
			{
				bool flag = binder.bindUserDefinedConversion(pexprDest, pType, aggTypeDest, needsExprDest, out exprDest, fImplicitOnly: false);
			}
			return AggCastResult.Success;
		}

		private AggCastResult bindExplicitConversionToEnum(AggregateType aggTypeDest)
		{
			AggregateSymbol aggregate = aggTypeDest.getAggregate();
			if (!aggregate.IsEnum())
			{
				return AggCastResult.Failure;
			}
			if (typeSrc.isPredefType(PredefinedType.PT_DECIMAL))
			{
				return bindExplicitConversionFromDecimalToEnum(aggTypeDest);
			}
			if (typeSrc.isNumericType() || (typeSrc.isPredefined() && typeSrc.getPredefType() == PredefinedType.PT_CHAR))
			{
				if (exprSrc.GetConst() != null)
				{
					switch (binder.bindConstantCast(exprSrc, exprTypeDest, needsExprDest, out exprDest, explicitConversion: true))
					{
					case ConstCastResult.Success:
						return AggCastResult.Success;
					case ConstCastResult.CheckFailure:
						return AggCastResult.Abort;
					}
				}
				if (needsExprDest)
				{
					binder.bindSimpleCast(exprSrc, exprTypeDest, out exprDest);
				}
				return AggCastResult.Success;
			}
			if (typeSrc.isPredefined() && (typeSrc.isPredefType(PredefinedType.PT_OBJECT) || typeSrc.isPredefType(PredefinedType.PT_VALUE) || typeSrc.isPredefType(PredefinedType.PT_ENUM)))
			{
				if (needsExprDest)
				{
					binder.bindSimpleCast(exprSrc, exprTypeDest, out exprDest, EXPRFLAG.EXF_INDEXER);
				}
				return AggCastResult.Success;
			}
			return AggCastResult.Failure;
		}

		private AggCastResult bindExplicitConversionBetweenSimpleTypes(AggregateType aggTypeDest)
		{
			if (!typeSrc.isSimpleType() || !aggTypeDest.isSimpleType())
			{
				return AggCastResult.Failure;
			}
			AggregateSymbol aggregate = aggTypeDest.getAggregate();
			PredefinedType predefType = typeSrc.getPredefType();
			PredefinedType predefType2 = aggregate.GetPredefType();
			ConvKind convKind = GetConvKind(predefType, predefType2);
			if (convKind != ConvKind.Explicit)
			{
				return AggCastResult.Failure;
			}
			if (exprSrc.GetConst() != null)
			{
				switch (binder.bindConstantCast(exprSrc, exprTypeDest, needsExprDest, out exprDest, explicitConversion: true))
				{
				case ConstCastResult.Success:
					return AggCastResult.Success;
				case ConstCastResult.CheckFailure:
					if ((flags & CONVERTTYPE.CHECKOVERFLOW) == 0)
					{
						return AggCastResult.Abort;
					}
					break;
				}
			}
			bool flag = true;
			if (needsExprDest)
			{
				if (isUserDefinedConversion(predefType, predefType2))
				{
					flag = binder.bindUserDefinedConversion(exprSrc, typeSrc, aggTypeDest, needsExprDest, out exprDest, fImplicitOnly: false);
				}
				else
				{
					binder.bindSimpleCast(exprSrc, exprTypeDest, out exprDest, ((flags & CONVERTTYPE.CHECKOVERFLOW) != 0) ? EXPRFLAG.EXF_CHECKOVERFLOW : ((EXPRFLAG)0));
				}
			}
			if (!flag)
			{
				return AggCastResult.Failure;
			}
			return AggCastResult.Success;
		}

		private AggCastResult bindExplicitConversionBetweenAggregates(AggregateType aggTypeDest)
		{
			if (!typeSrc.IsAggregateType())
			{
				return AggCastResult.Failure;
			}
			AggregateSymbol aggregate = typeSrc.AsAggregateType().getAggregate();
			AggregateSymbol aggregate2 = aggTypeDest.getAggregate();
			if (GetSymbolLoader().HasBaseConversion(aggTypeDest, typeSrc.AsAggregateType()))
			{
				if (needsExprDest)
				{
					if (aggregate2.IsValueType() && aggregate.getThisType().fundType() == FUNDTYPE.FT_REF)
					{
						binder.bindSimpleCast(exprSrc, exprTypeDest, out exprDest, EXPRFLAG.EXF_INDEXER);
					}
					else
					{
						binder.bindSimpleCast(exprSrc, exprTypeDest, out exprDest, EXPRFLAG.EXF_OPERATOR | ((exprSrc != null) ? (exprSrc.flags & EXPRFLAG.EXF_CANTBENULL) : ((EXPRFLAG)0)));
					}
				}
				return AggCastResult.Success;
			}
			if ((aggregate.IsClass() && !aggregate.IsSealed() && aggregate2.IsInterface()) || (aggregate.IsInterface() && aggregate2.IsClass() && !aggregate2.IsSealed()) || (aggregate.IsInterface() && aggregate2.IsInterface()) || CConversions.HasGenericDelegateExplicitReferenceConversion(GetSymbolLoader(), typeSrc, aggTypeDest))
			{
				if (needsExprDest)
				{
					binder.bindSimpleCast(exprSrc, exprTypeDest, out exprDest, EXPRFLAG.EXF_OPERATOR | ((exprSrc != null) ? (exprSrc.flags & EXPRFLAG.EXF_CANTBENULL) : ((EXPRFLAG)0)));
				}
				return AggCastResult.Success;
			}
			return AggCastResult.Failure;
		}

		private AggCastResult bindExplicitConversionFromPointerToInt(AggregateType aggTypeDest)
		{
			if (!typeSrc.IsPointerType() || aggTypeDest.fundType() > FUNDTYPE.FT_U8 || !aggTypeDest.isNumericType())
			{
				return AggCastResult.Failure;
			}
			if (needsExprDest)
			{
				binder.bindSimpleCast(exprSrc, exprTypeDest, out exprDest);
			}
			return AggCastResult.Success;
		}

		private AggCastResult bindExplicitConversionFromTypeVarToAggregate(AggregateType aggTypeDest)
		{
			if (!typeSrc.IsTypeParameterType())
			{
				return AggCastResult.Failure;
			}
			if (aggTypeDest.getAggregate().IsInterface())
			{
				if (needsExprDest)
				{
					binder.bindSimpleCast(exprSrc, exprTypeDest, out exprDest, (EXPRFLAG)40);
				}
				return AggCastResult.Success;
			}
			return AggCastResult.Failure;
		}

		private AggCastResult bindExplicitConversionToAggregate(AggregateType aggTypeDest)
		{
			if (typeSrc.isSpecialByRefType())
			{
				return AggCastResult.Abort;
			}
			AggCastResult aggCastResult = bindExplicitConversionFromEnumToAggregate(aggTypeDest);
			if (aggCastResult != AggCastResult.Failure)
			{
				return aggCastResult;
			}
			aggCastResult = bindExplicitConversionToEnum(aggTypeDest);
			if (aggCastResult != AggCastResult.Failure)
			{
				return aggCastResult;
			}
			aggCastResult = bindExplicitConversionBetweenSimpleTypes(aggTypeDest);
			if (aggCastResult != AggCastResult.Failure)
			{
				return aggCastResult;
			}
			aggCastResult = bindExplicitConversionBetweenAggregates(aggTypeDest);
			if (aggCastResult != AggCastResult.Failure)
			{
				return aggCastResult;
			}
			aggCastResult = bindExplicitConversionFromPointerToInt(aggTypeDest);
			if (aggCastResult != AggCastResult.Failure)
			{
				return aggCastResult;
			}
			if (typeSrc.IsVoidType())
			{
				return AggCastResult.Abort;
			}
			aggCastResult = bindExplicitConversionFromTypeVarToAggregate(aggTypeDest);
			if (aggCastResult != AggCastResult.Failure)
			{
				return aggCastResult;
			}
			return AggCastResult.Failure;
		}

		private SymbolLoader GetSymbolLoader()
		{
			return binder.GetSymbolLoader();
		}

		private ExprFactory GetExprFactory()
		{
			return binder.GetExprFactory();
		}
	}

	protected delegate EXPR PfnBindBinOp(ExpressionKind ek, EXPRFLAG flags, EXPR op1, EXPR op2);

	protected delegate EXPR PfnBindUnaOp(ExpressionKind ek, EXPRFLAG flags, EXPR op);

	internal class GroupToArgsBinder
	{
		private enum Result
		{
			Success,
			Failure_SearchForExpanded,
			Failure_NoSearchForExpanded
		}

		private ExpressionBinder m_pExprBinder;

		private bool m_fCandidatesUnsupported;

		private BindingFlag m_fBindFlags;

		private EXPRMEMGRP m_pGroup;

		private ArgInfos m_pArguments;

		private ArgInfos m_pOriginalArguments;

		private bool m_bHasNamedArguments;

		private AggregateType m_pDelegate;

		private AggregateType m_pCurrentType;

		private MethodOrPropertySymbol m_pCurrentSym;

		private TypeArray m_pCurrentTypeArgs;

		private TypeArray m_pCurrentParameters;

		private TypeArray m_pBestParameters;

		private int m_nArgBest;

		private SymWithType[] m_swtWrongCount = new SymWithType[20];

		private int m_nWrongCount;

		private bool m_bIterateToEndOfNsList;

		private bool m_bBindingCollectionAddArgs;

		private GroupToArgsBinderResult m_results;

		private List<CandidateFunctionMember> m_methList;

		private MethPropWithInst m_mpwiParamTypeConstraints;

		private MethPropWithInst m_mpwiBogus;

		private MethPropWithInst m_mpwiCantInferInstArg;

		private MethWithType m_mwtBadArity;

		private Name m_pInvalidSpecifiedName;

		private Name m_pNameUsedInPositionalArgument;

		private Name m_pDuplicateSpecifiedName;

		private List<CType> m_HiddenTypes;

		private bool m_bArgumentsChangedForNamedOrOptionalArguments;

		public GroupToArgsBinder(ExpressionBinder exprBinder, BindingFlag bindFlags, EXPRMEMGRP grp, ArgInfos args, ArgInfos originalArgs, bool bHasNamedArguments, AggregateType atsDelegate)
		{
			m_pExprBinder = exprBinder;
			m_fCandidatesUnsupported = false;
			m_fBindFlags = bindFlags;
			m_pGroup = grp;
			m_pArguments = args;
			m_pOriginalArguments = originalArgs;
			m_bHasNamedArguments = bHasNamedArguments;
			m_pDelegate = atsDelegate;
			m_pCurrentType = null;
			m_pCurrentSym = null;
			m_pCurrentTypeArgs = null;
			m_pCurrentParameters = null;
			m_pBestParameters = null;
			m_nArgBest = -1;
			m_nWrongCount = 0;
			m_bIterateToEndOfNsList = false;
			m_bBindingCollectionAddArgs = false;
			m_results = new GroupToArgsBinderResult();
			m_methList = new List<CandidateFunctionMember>();
			m_mpwiParamTypeConstraints = new MethPropWithInst();
			m_mpwiBogus = new MethPropWithInst();
			m_mpwiCantInferInstArg = new MethPropWithInst();
			m_mwtBadArity = new MethWithType();
			m_HiddenTypes = new List<CType>();
		}

		public bool Bind(bool bReportErrors)
		{
			LookForCandidates();
			if (!GetResultOfBind(bReportErrors))
			{
				if (bReportErrors)
				{
					ReportErrorsOnFailure();
				}
				return false;
			}
			return true;
		}

		public GroupToArgsBinderResult GetResultsOfBind()
		{
			return m_results;
		}

		public bool BindCollectionAddArgs()
		{
			m_bBindingCollectionAddArgs = true;
			return Bind(bReportErrors: true);
		}

		private SymbolLoader GetSymbolLoader()
		{
			return m_pExprBinder.GetSymbolLoader();
		}

		private CSemanticChecker GetSemanticChecker()
		{
			return m_pExprBinder.GetSemanticChecker();
		}

		private ErrorHandling GetErrorContext()
		{
			return m_pExprBinder.GetErrorContext();
		}

		public static CType GetTypeQualifier(EXPRMEMGRP pGroup)
		{
			CType cType = null;
			if ((pGroup.flags & EXPRFLAG.EXF_ASFINALLYLEAVE) != 0)
			{
				return null;
			}
			if ((pGroup.flags & EXPRFLAG.EXF_CTOR) != 0)
			{
				return pGroup.GetParentType();
			}
			if (pGroup.GetOptionalObject() != null)
			{
				return pGroup.GetOptionalObject().type;
			}
			return null;
		}

		private void LookForCandidates()
		{
			bool flag = false;
			bool flag2 = true;
			int num = m_swtWrongCount.Length;
			bool flag3 = true;
			bool flag4 = false;
			symbmask_t mask = (symbmask_t)(1 << (int)m_pGroup.sk);
			CType pObject = ((m_pGroup.GetOptionalObject() != null) ? m_pGroup.GetOptionalObject().type : null);
			CMemberLookupResults.CMethodIterator methodIterator = m_pGroup.GetMemberLookupResults().GetMethodIterator(GetSemanticChecker(), GetSymbolLoader(), pObject, GetTypeQualifier(m_pGroup), m_pExprBinder.ContextForMemberLookup(), allowBogusAndInaccessible: true, allowExtensionMethods: false, m_pGroup.typeArgs.size, m_pGroup.flags, mask);
			while (true)
			{
				bool flag5 = false;
				if (flag2 && !flag)
				{
					flag5 = (flag = ConstructExpandedParameters());
				}
				if (!flag5)
				{
					flag = false;
					if (!GetNextSym(methodIterator))
					{
						break;
					}
					m_pCurrentParameters = m_pCurrentSym.Params;
					flag2 = true;
				}
				if (m_bArgumentsChangedForNamedOrOptionalArguments)
				{
					m_bArgumentsChangedForNamedOrOptionalArguments = false;
					CopyArgInfos(m_pOriginalArguments, m_pArguments);
				}
				if (m_pArguments.fHasExprs)
				{
					if (m_bHasNamedArguments)
					{
						if (!ReOrderArgsForNamedArguments())
						{
							continue;
						}
					}
					else if (HasOptionalParameters() && !AddArgumentsForOptionalParameters())
					{
						continue;
					}
				}
				if (!flag5)
				{
					flag4 = true;
					flag3 &= m_pCurrentSym.getBogus();
					if (m_pCurrentParameters.size != m_pArguments.carg)
					{
						if (m_nWrongCount < num && (!m_pCurrentSym.isParamArray || m_pArguments.carg < m_pCurrentParameters.size - 1))
						{
							m_swtWrongCount[m_nWrongCount++] = new SymWithType(m_pCurrentSym, m_pCurrentType);
						}
						flag2 = true;
						continue;
					}
				}
				if (!methodIterator.CanUseCurrentSymbol())
				{
					continue;
				}
				Result result = DetermineCurrentTypeArgs();
				if (result != 0)
				{
					flag2 = result == Result.Failure_SearchForExpanded;
					continue;
				}
				bool flag6 = !methodIterator.IsCurrentSymbolInaccessible();
				if (!flag6 && (!m_methList.IsEmpty() || (bool)m_results.GetInaccessibleResult()))
				{
					flag2 = false;
					continue;
				}
				bool flag7 = flag6 && methodIterator.IsCurrentSymbolBogus();
				if (flag7 && (!m_methList.IsEmpty() || (bool)m_results.GetInaccessibleResult() || (bool)m_mpwiBogus))
				{
					flag2 = false;
					continue;
				}
				if (!ArgumentsAreConvertible())
				{
					flag2 = true;
					continue;
				}
				if (!flag6)
				{
					m_results.GetInaccessibleResult().Set(m_pCurrentSym, m_pCurrentType, m_pCurrentTypeArgs);
				}
				else if (flag7)
				{
					m_mpwiBogus.Set(m_pCurrentSym, m_pCurrentType, m_pCurrentTypeArgs);
				}
				else
				{
					m_methList.Add(new CandidateFunctionMember(new MethPropWithInst(m_pCurrentSym, m_pCurrentType, m_pCurrentTypeArgs), m_pCurrentParameters, 0, flag));
					if (m_pCurrentType.isInterfaceType())
					{
						TypeArray ifacesAll = m_pCurrentType.GetIfacesAll();
						for (int i = 0; i < ifacesAll.size; i++)
						{
							AggregateType item = ifacesAll.Item(i).AsAggregateType();
							m_HiddenTypes.Add(item);
						}
						AggregateType reqPredefType = GetSymbolLoader().GetReqPredefType(PredefinedType.PT_OBJECT, fEnsureState: true);
						m_HiddenTypes.Add(reqPredefType);
					}
				}
				flag2 = false;
			}
			m_fCandidatesUnsupported = flag3 && flag4;
			if (m_bArgumentsChangedForNamedOrOptionalArguments)
			{
				CopyArgInfos(m_pOriginalArguments, m_pArguments);
			}
		}

		private void CopyArgInfos(ArgInfos src, ArgInfos dst)
		{
			dst.carg = src.carg;
			dst.types = src.types;
			dst.fHasExprs = src.fHasExprs;
			dst.prgexpr.Clear();
			for (int i = 0; i < src.prgexpr.Count; i++)
			{
				dst.prgexpr.Add(src.prgexpr[i]);
			}
		}

		private bool GetResultOfBind(bool bReportErrors)
		{
			if (!m_methList.IsEmpty())
			{
				CandidateFunctionMember candidateFunctionMember;
				if (m_methList.Count == 1)
				{
					candidateFunctionMember = m_methList.Head();
				}
				else
				{
					CandidateFunctionMember methAmbig = null;
					CandidateFunctionMember methAmbig2 = null;
					CType pTypeThrough = ((m_pGroup.GetOptionalObject() != null) ? m_pGroup.GetOptionalObject().type : null);
					candidateFunctionMember = m_pExprBinder.FindBestMethod(m_methList, pTypeThrough, m_pArguments, out methAmbig, out methAmbig2);
					if (candidateFunctionMember == null)
					{
						candidateFunctionMember = methAmbig;
						m_results.AmbiguousResult = methAmbig2.mpwi;
						if (bReportErrors)
						{
							if (methAmbig.@params != methAmbig2.@params || methAmbig.mpwi.MethProp().Params.size != methAmbig2.mpwi.MethProp().Params.size || methAmbig.mpwi.TypeArgs != methAmbig2.mpwi.TypeArgs || methAmbig.mpwi.GetType() != methAmbig2.mpwi.GetType() || methAmbig.mpwi.MethProp().Params == methAmbig2.mpwi.MethProp().Params)
							{
								GetErrorContext().Error(ErrorCode.ERR_AmbigCall, methAmbig.mpwi, methAmbig2.mpwi);
							}
							else
							{
								GetErrorContext().Error(ErrorCode.ERR_AmbigCall, methAmbig.mpwi.MethProp(), methAmbig2.mpwi.MethProp());
							}
						}
					}
				}
				m_results.BestResult = candidateFunctionMember.mpwi;
				if (bReportErrors)
				{
					ReportErrorsOnSuccess();
				}
				return true;
			}
			return false;
		}

		private bool ReOrderArgsForNamedArguments()
		{
			MethodOrPropertySymbol methodOrPropertySymbol = FindMostDerivedMethod(m_pCurrentSym, m_pGroup.GetOptionalObject());
			if (methodOrPropertySymbol == null)
			{
				return false;
			}
			int size = m_pCurrentParameters.size;
			if (size == 0 || size < m_pArguments.carg)
			{
				return false;
			}
			if (!NamedArgumentNamesAppearInParameterList(methodOrPropertySymbol))
			{
				return false;
			}
			m_bArgumentsChangedForNamedOrOptionalArguments = ReOrderArgsForNamedArguments(methodOrPropertySymbol, m_pCurrentParameters, m_pCurrentType, m_pGroup, m_pArguments, m_pExprBinder.GetTypes(), m_pExprBinder.GetExprFactory(), GetSymbolLoader());
			return m_bArgumentsChangedForNamedOrOptionalArguments;
		}

		internal static bool ReOrderArgsForNamedArguments(MethodOrPropertySymbol methprop, TypeArray pCurrentParameters, AggregateType pCurrentType, EXPRMEMGRP pGroup, ArgInfos pArguments, TypeManager typeManager, ExprFactory exprFactory, SymbolLoader symbolLoader)
		{
			int size = pCurrentParameters.size;
			EXPR[] array = new EXPR[size];
			int num = 0;
			EXPR eXPR = null;
			TypeArray typeArray = typeManager.SubstTypeArray(pCurrentParameters, pCurrentType, pGroup.typeArgs);
			foreach (Name parameterName in methprop.ParameterNames)
			{
				if (num >= pCurrentParameters.size)
				{
					break;
				}
				if (methprop.isParamArray && num < pArguments.carg && pArguments.prgexpr[num].isARRINIT() && pArguments.prgexpr[num].asARRINIT().GeneratedForParamArray)
				{
					eXPR = pArguments.prgexpr[num];
				}
				if (num < pArguments.carg && !pArguments.prgexpr[num].isNamedArgumentSpecification() && (!pArguments.prgexpr[num].isARRINIT() || !pArguments.prgexpr[num].asARRINIT().GeneratedForParamArray))
				{
					array[num] = pArguments.prgexpr[num++];
					continue;
				}
				EXPR eXPR2 = FindArgumentWithName(pArguments, parameterName);
				if (eXPR2 == null)
				{
					if (methprop.IsParameterOptional(num))
					{
						eXPR2 = GenerateOptionalArgument(symbolLoader, exprFactory, methprop, typeArray.Item(num), num);
					}
					else
					{
						if (eXPR == null || num != methprop.Params.Count - 1)
						{
							return false;
						}
						eXPR2 = eXPR;
					}
				}
				array[num++] = eXPR2;
			}
			CType[] array2 = new CType[pCurrentParameters.size];
			for (int i = 0; i < size; i++)
			{
				if (i < pArguments.prgexpr.Count)
				{
					pArguments.prgexpr[i] = array[i];
				}
				else
				{
					pArguments.prgexpr.Add(array[i]);
				}
				array2[i] = pArguments.prgexpr[i].type;
			}
			pArguments.carg = pCurrentParameters.size;
			pArguments.types = symbolLoader.getBSymmgr().AllocParams(pCurrentParameters.size, array2);
			return true;
		}

		private static EXPR GenerateOptionalArgument(SymbolLoader symbolLoader, ExprFactory exprFactory, MethodOrPropertySymbol methprop, CType type, int index)
		{
			CType cType = (type.IsNullableType() ? type.AsNullableType().GetUnderlyingType() : type);
			EXPR eXPR = null;
			if (methprop.HasDefaultParameterValue(index))
			{
				CType defaultParameterValueConstValType = methprop.GetDefaultParameterValueConstValType(index);
				CONSTVAL defaultParameterValue = methprop.GetDefaultParameterValue(index);
				if (!defaultParameterValueConstValType.isPredefType(PredefinedType.PT_DATETIME) || (!cType.isPredefType(PredefinedType.PT_DATETIME) && !cType.isPredefType(PredefinedType.PT_OBJECT) && !cType.isPredefType(PredefinedType.PT_VALUE)))
				{
					eXPR = (defaultParameterValueConstValType.isSimpleOrEnumOrString() ? ((!cType.isEnumType() || defaultParameterValueConstValType != cType.underlyingType()) ? exprFactory.CreateConstant(defaultParameterValueConstValType, defaultParameterValue) : exprFactory.CreateConstant(cType, defaultParameterValue)) : (((!type.IsRefType() && !type.IsNullableType()) || !defaultParameterValue.IsNullRef()) ? exprFactory.CreateZeroInit(type) : exprFactory.CreateNull()));
				}
				else
				{
					AggregateType reqPredefType = symbolLoader.GetReqPredefType(PredefinedType.PT_DATETIME);
					eXPR = exprFactory.CreateConstant(reqPredefType, new CONSTVAL(DateTime.FromBinary(defaultParameterValue.longVal)));
				}
			}
			else if (type.isPredefType(PredefinedType.PT_OBJECT))
			{
				if (methprop.MarshalAsObject(index))
				{
					eXPR = exprFactory.CreateNull();
				}
				else if (methprop.IsDispatchConstantParameter(index) || methprop.IsUnknownConstantParameter(index))
				{
					if (methprop.IsUnknownConstantParameter(index))
					{
						AggregateType optPredefType = symbolLoader.GetOptPredefType(PredefinedType.PT_UNKNOWNWRAPPER);
						eXPR = exprFactory.CreateConstant(optPredefType, new CONSTVAL(new UnknownWrapper(null)));
					}
					else
					{
						AggregateType optPredefType2 = symbolLoader.GetOptPredefType(PredefinedType.PT_DISPATCHWRAPPER);
						eXPR = exprFactory.CreateConstant(optPredefType2, new CONSTVAL(new DispatchWrapper(null)));
					}
				}
				else
				{
					AggregateSymbol optPredefAgg = symbolLoader.GetOptPredefAgg(PredefinedType.PT_MISSING);
					Name predefinedName = symbolLoader.GetNameManager().GetPredefinedName(PredefinedName.PN_CAP_VALUE);
					FieldSymbol field = symbolLoader.LookupAggMember(predefinedName, optPredefAgg, symbmask_t.MASK_FieldSymbol).AsFieldSymbol();
					FieldWithType fWT = new FieldWithType(field, optPredefAgg.getThisType());
					EXPRFIELD eXPRFIELD = exprFactory.CreateField((EXPRFLAG)0, optPredefAgg.getThisType(), null, 0u, fWT, null);
					eXPR = ((optPredefAgg.getThisType() == type) ? ((EXPR)eXPRFIELD) : ((EXPR)exprFactory.CreateCast((EXPRFLAG)0, type, eXPRFIELD)));
				}
			}
			else
			{
				eXPR = exprFactory.CreateZeroInit(type);
			}
			eXPR.IsOptionalArgument = true;
			return eXPR;
		}

		private MethodOrPropertySymbol FindMostDerivedMethod(MethodOrPropertySymbol pMethProp, EXPR pObject)
		{
			return FindMostDerivedMethod(GetSymbolLoader(), pMethProp, pObject?.type);
		}

		public static MethodOrPropertySymbol FindMostDerivedMethod(SymbolLoader symbolLoader, MethodOrPropertySymbol pMethProp, CType pType)
		{
			bool flag = false;
			MethodSymbol methodSymbol;
			if (pMethProp.IsMethodSymbol())
			{
				methodSymbol = pMethProp.AsMethodSymbol();
			}
			else
			{
				PropertySymbol propertySymbol = pMethProp.AsPropertySymbol();
				methodSymbol = ((propertySymbol.methGet != null) ? propertySymbol.methGet : propertySymbol.methSet);
				if (methodSymbol == null)
				{
					return null;
				}
				flag = propertySymbol.isIndexer();
			}
			if (!methodSymbol.isVirtual)
			{
				return methodSymbol;
			}
			if (pType == null)
			{
				return methodSymbol;
			}
			if (methodSymbol.swtSlot != null && methodSymbol.swtSlot.Meth() != null)
			{
				methodSymbol = methodSymbol.swtSlot.Meth();
			}
			if (!pType.IsAggregateType())
			{
				return methodSymbol;
			}
			AggregateSymbol aggregateSymbol = pType.AsAggregateType().GetOwningAggregate();
			while (aggregateSymbol != null && aggregateSymbol.GetBaseAgg() != null)
			{
				for (MethodOrPropertySymbol methodOrPropertySymbol = symbolLoader.LookupAggMember(methodSymbol.name, aggregateSymbol, symbmask_t.MASK_MethodSymbol | symbmask_t.MASK_PropertySymbol).AsMethodOrPropertySymbol(); methodOrPropertySymbol != null; methodOrPropertySymbol = symbolLoader.LookupNextSym(methodOrPropertySymbol, aggregateSymbol, symbmask_t.MASK_MethodSymbol | symbmask_t.MASK_PropertySymbol).AsMethodOrPropertySymbol())
				{
					if (methodOrPropertySymbol.isOverride && methodOrPropertySymbol.swtSlot.Sym != null && methodOrPropertySymbol.swtSlot.Sym == methodSymbol)
					{
						if (flag)
						{
							return methodOrPropertySymbol.AsMethodSymbol().getProperty();
						}
						return methodOrPropertySymbol;
					}
				}
				aggregateSymbol = aggregateSymbol.GetBaseAgg();
			}
			return methodSymbol;
		}

		private bool HasOptionalParameters()
		{
			return FindMostDerivedMethod(m_pCurrentSym, m_pGroup.GetOptionalObject())?.HasOptionalParameters() ?? false;
		}

		private bool AddArgumentsForOptionalParameters()
		{
			if (m_pCurrentParameters.size <= m_pArguments.carg)
			{
				return true;
			}
			MethodOrPropertySymbol methodOrPropertySymbol = FindMostDerivedMethod(m_pCurrentSym, m_pGroup.GetOptionalObject());
			if (methodOrPropertySymbol == null)
			{
				return false;
			}
			int num = m_pArguments.carg;
			int num2 = 0;
			TypeArray typeArray = m_pExprBinder.GetTypes().SubstTypeArray(m_pCurrentParameters, m_pCurrentType, m_pGroup.typeArgs);
			EXPR[] array = new EXPR[m_pCurrentParameters.size - num];
			while (num < typeArray.size)
			{
				if (!methodOrPropertySymbol.IsParameterOptional(num))
				{
					return false;
				}
				array[num2] = GenerateOptionalArgument(GetSymbolLoader(), m_pExprBinder.GetExprFactory(), methodOrPropertySymbol, typeArray.Item(num), num);
				num++;
				num2++;
			}
			for (int i = 0; i < num2; i++)
			{
				m_pArguments.prgexpr.Add(array[i]);
			}
			CType[] array2 = new CType[typeArray.size];
			for (int j = 0; j < typeArray.size; j++)
			{
				array2[j] = m_pArguments.prgexpr[j].type;
			}
			m_pArguments.types = GetSymbolLoader().getBSymmgr().AllocParams(typeArray.size, array2);
			m_pArguments.carg = typeArray.size;
			m_bArgumentsChangedForNamedOrOptionalArguments = true;
			return true;
		}

		private static EXPR FindArgumentWithName(ArgInfos pArguments, Name pName)
		{
			for (int i = 0; i < pArguments.carg; i++)
			{
				if (pArguments.prgexpr[i].isNamedArgumentSpecification() && pArguments.prgexpr[i].asNamedArgumentSpecification().Name == pName)
				{
					return pArguments.prgexpr[i];
				}
			}
			return null;
		}

		private bool NamedArgumentNamesAppearInParameterList(MethodOrPropertySymbol methprop)
		{
			List<Name> list = methprop.ParameterNames;
			HashSet<Name> hashSet = new HashSet<Name>();
			for (int i = 0; i < m_pArguments.carg; i++)
			{
				if (!m_pArguments.prgexpr[i].isNamedArgumentSpecification())
				{
					if (!list.IsEmpty())
					{
						list = list.Tail();
					}
					continue;
				}
				Name name = m_pArguments.prgexpr[i].asNamedArgumentSpecification().Name;
				if (!methprop.ParameterNames.Contains(name))
				{
					if (m_pInvalidSpecifiedName == null)
					{
						m_pInvalidSpecifiedName = name;
					}
					return false;
				}
				if (!list.Contains(name))
				{
					if (m_pNameUsedInPositionalArgument == null)
					{
						m_pNameUsedInPositionalArgument = name;
					}
					return false;
				}
				if (hashSet.Contains(name))
				{
					if (m_pDuplicateSpecifiedName == null)
					{
						m_pDuplicateSpecifiedName = name;
					}
					return false;
				}
				hashSet.Add(name);
			}
			return true;
		}

		private bool GetNextSym(CMemberLookupResults.CMethodIterator iterator)
		{
			if (!iterator.MoveNext(m_methList.IsEmpty(), m_bIterateToEndOfNsList))
			{
				return false;
			}
			m_pCurrentSym = iterator.GetCurrentSymbol();
			AggregateType currentType = iterator.GetCurrentType();
			if (m_pCurrentType != currentType && m_pCurrentType != null && !m_methList.IsEmpty() && !m_methList.Head().mpwi.GetType().isInterfaceType() && (!m_methList.Head().mpwi.Sym.IsMethodSymbol() || !m_methList.Head().mpwi.Meth().IsExtension()))
			{
				return false;
			}
			if (m_pCurrentType != currentType && m_pCurrentType != null && !m_methList.IsEmpty() && !m_methList.Head().mpwi.GetType().isInterfaceType() && m_methList.Head().mpwi.Sym.IsMethodSymbol() && m_methList.Head().mpwi.Meth().IsExtension() && m_pGroup.GetOptionalObject() != null)
			{
				m_bIterateToEndOfNsList = true;
			}
			m_pCurrentType = currentType;
			while (m_HiddenTypes.Contains(m_pCurrentType))
			{
				while (iterator.GetCurrentType() == m_pCurrentType)
				{
					iterator.MoveNext(m_methList.IsEmpty(), m_bIterateToEndOfNsList);
				}
				m_pCurrentSym = iterator.GetCurrentSymbol();
				m_pCurrentType = iterator.GetCurrentType();
				if (iterator.AtEnd())
				{
					return false;
				}
			}
			return true;
		}

		private bool ConstructExpandedParameters()
		{
			if (m_pCurrentSym == null || m_pArguments == null || m_pCurrentParameters == null)
			{
				return false;
			}
			if ((m_fBindFlags & BindingFlag.BIND_NOPARAMS) != 0)
			{
				return false;
			}
			if (!m_pCurrentSym.isParamArray)
			{
				return false;
			}
			int num = 0;
			for (int i = m_pArguments.carg; i < m_pCurrentSym.Params.size; i++)
			{
				if (m_pCurrentSym.IsParameterOptional(i))
				{
					num++;
				}
			}
			if (m_pArguments.carg + num < m_pCurrentParameters.size - 1)
			{
				return false;
			}
			return m_pExprBinder.TryGetExpandedParams(m_pCurrentSym.Params, m_pArguments.carg, out m_pCurrentParameters);
		}

		private Result DetermineCurrentTypeArgs()
		{
			TypeArray typeArgs = m_pGroup.typeArgs;
			if (m_pCurrentSym.IsMethodSymbol() && m_pCurrentSym.AsMethodSymbol().typeVars.size != typeArgs.size)
			{
				MethodSymbol methodSymbol = m_pCurrentSym.AsMethodSymbol();
				if (typeArgs.size > 0)
				{
					if (!m_mwtBadArity)
					{
						m_mwtBadArity.Set(methodSymbol, m_pCurrentType);
					}
					return Result.Failure_NoSearchForExpanded;
				}
				if (!MethodTypeInferrer.Infer(m_pExprBinder, GetSymbolLoader(), methodSymbol, m_pCurrentType.GetTypeArgsAll(), m_pCurrentParameters, m_pArguments, out m_pCurrentTypeArgs))
				{
					if (m_results.IsBetterUninferrableResult(m_pCurrentTypeArgs))
					{
						TypeArray typeVars = methodSymbol.typeVars;
						if (typeVars != null && m_pCurrentTypeArgs != null && typeVars.size == m_pCurrentTypeArgs.size)
						{
							m_mpwiCantInferInstArg.Set(m_pCurrentSym.AsMethodSymbol(), m_pCurrentType, m_pCurrentTypeArgs);
						}
						else
						{
							m_mpwiCantInferInstArg.Set(m_pCurrentSym.AsMethodSymbol(), m_pCurrentType, typeVars);
						}
					}
					return Result.Failure_SearchForExpanded;
				}
			}
			else
			{
				m_pCurrentTypeArgs = typeArgs;
			}
			return Result.Success;
		}

		private bool ArgumentsAreConvertible()
		{
			bool flag = false;
			bool flag2 = false;
			if (m_pArguments.carg != 0)
			{
				UpdateArguments();
				for (int i = 0; i < m_pArguments.carg; i++)
				{
					CType cType = m_pCurrentParameters.Item(i);
					if (!TypeBind.CheckConstraints(GetSemanticChecker(), GetErrorContext(), cType, CheckConstraintsFlags.NoErrors) && !DoesTypeArgumentsContainErrorSym(cType))
					{
						m_mpwiParamTypeConstraints.Set(m_pCurrentSym, m_pCurrentType, m_pCurrentTypeArgs);
						return false;
					}
				}
				for (int j = 0; j < m_pArguments.carg; j++)
				{
					CType cType2 = m_pCurrentParameters.Item(j);
					flag |= DoesTypeArgumentsContainErrorSym(cType2);
					bool flag3;
					if (m_pArguments.fHasExprs)
					{
						EXPR expr = m_pArguments.prgexpr[j];
						if (expr.isNamedArgumentSpecification())
						{
							expr = expr.asNamedArgumentSpecification().Value;
						}
						flag3 = m_pExprBinder.canConvert(expr, cType2);
					}
					else
					{
						flag3 = m_pExprBinder.canConvert(m_pArguments.types.Item(j), cType2);
					}
					if (flag3 || flag)
					{
						continue;
					}
					if (j > m_nArgBest)
					{
						m_nArgBest = j;
						if (!m_results.GetBestResult())
						{
							m_results.GetBestResult().Set(m_pCurrentSym, m_pCurrentType, m_pCurrentTypeArgs);
							m_pBestParameters = m_pCurrentParameters;
						}
					}
					else if (j == m_nArgBest && m_pArguments.types.Item(j) != cType2)
					{
						CType cType3 = (m_pArguments.types.Item(j).IsParameterModifierType() ? m_pArguments.types.Item(j).AsParameterModifierType().GetParameterType() : m_pArguments.types.Item(j));
						CType cType4 = (cType2.IsParameterModifierType() ? cType2.AsParameterModifierType().GetParameterType() : cType2);
						if (cType3 == cType4 && !m_results.GetBestResult())
						{
							m_results.GetBestResult().Set(m_pCurrentSym, m_pCurrentType, m_pCurrentTypeArgs);
							m_pBestParameters = m_pCurrentParameters;
						}
					}
					if (m_pCurrentSym.IsMethodSymbol() && (!m_pCurrentSym.AsMethodSymbol().IsExtension() || flag2))
					{
						m_results.AddInconvertibleResult(m_pCurrentSym.AsMethodSymbol(), m_pCurrentType, m_pCurrentTypeArgs);
					}
					return false;
				}
			}
			if (flag)
			{
				if (m_results.IsBetterUninferrableResult(m_pCurrentTypeArgs) && m_pCurrentSym.IsMethodSymbol() && (!m_pCurrentSym.AsMethodSymbol().IsExtension() || m_pCurrentSym.AsMethodSymbol().typeVars.size == 0 || MethodTypeInferrer.CanObjectOfExtensionBeInferred(m_pExprBinder, GetSymbolLoader(), m_pCurrentSym.AsMethodSymbol(), m_pCurrentType.GetTypeArgsAll(), m_pCurrentSym.AsMethodSymbol().Params, m_pArguments)))
				{
					m_results.GetUninferrableResult().Set(m_pCurrentSym.AsMethodSymbol(), m_pCurrentType, m_pCurrentTypeArgs);
				}
			}
			else if (m_pCurrentSym.IsMethodSymbol() && (!m_pCurrentSym.AsMethodSymbol().IsExtension() || flag2))
			{
				m_results.AddInconvertibleResult(m_pCurrentSym.AsMethodSymbol(), m_pCurrentType, m_pCurrentTypeArgs);
			}
			return !flag;
		}

		private void UpdateArguments()
		{
			m_pCurrentParameters = m_pExprBinder.GetTypes().SubstTypeArray(m_pCurrentParameters, m_pCurrentType, m_pCurrentTypeArgs);
			if (m_pArguments.prgexpr == null || m_pArguments.prgexpr.Count == 0)
			{
				return;
			}
			MethodOrPropertySymbol methodOrPropertySymbol = null;
			for (int i = 0; i < m_pCurrentParameters.size; i++)
			{
				EXPR eXPR = m_pArguments.prgexpr[i];
				if (!eXPR.IsOptionalArgument)
				{
					continue;
				}
				CType cType = m_pCurrentParameters.Item(i);
				if (cType != eXPR.type)
				{
					if (methodOrPropertySymbol == null)
					{
						methodOrPropertySymbol = FindMostDerivedMethod(m_pCurrentSym, m_pGroup.GetOptionalObject());
					}
					EXPR value = GenerateOptionalArgument(GetSymbolLoader(), m_pExprBinder.GetExprFactory(), methodOrPropertySymbol, m_pCurrentParameters[i], i);
					m_pArguments.prgexpr[i] = value;
				}
			}
		}

		private bool DoesTypeArgumentsContainErrorSym(CType var)
		{
			if (!var.IsAggregateType())
			{
				return false;
			}
			TypeArray typeArgsAll = var.AsAggregateType().GetTypeArgsAll();
			for (int i = 0; i < typeArgsAll.size; i++)
			{
				CType cType = typeArgsAll.Item(i);
				if (cType.IsErrorType())
				{
					return true;
				}
				if (cType.IsAggregateType() && DoesTypeArgumentsContainErrorSym(cType))
				{
					return true;
				}
			}
			return false;
		}

		private void ReportErrorsOnSuccess()
		{
			if (m_results.GetBestResult().MethProp().name == GetSymbolLoader().GetNameManager().GetPredefName(PredefinedName.PN_DTOR) && m_results.GetBestResult().MethProp().getClass()
				.isPredefAgg(PredefinedType.PT_OBJECT))
			{
				if ((m_pGroup.flags & EXPRFLAG.EXF_ASFINALLYLEAVE) != 0)
				{
					GetErrorContext().Error(ErrorCode.ERR_CallingBaseFinalizeDeprecated);
				}
				else
				{
					GetErrorContext().Error(ErrorCode.ERR_CallingFinalizeDepracated);
				}
			}
			if (m_pGroup.sk == SYMKIND.SK_MethodSymbol && m_results.GetBestResult().TypeArgs.size > 0)
			{
				TypeBind.CheckMethConstraints(GetSemanticChecker(), GetErrorContext(), new MethWithInst(m_results.GetBestResult()));
			}
		}

		private void ReportErrorsOnFailure()
		{
			if (m_pDuplicateSpecifiedName != null)
			{
				GetErrorContext().Error(ErrorCode.ERR_DuplicateNamedArgument, m_pDuplicateSpecifiedName);
				return;
			}
			if ((bool)m_results.GetInaccessibleResult())
			{
				GetSemanticChecker().ReportAccessError(m_results.GetInaccessibleResult(), m_pExprBinder.ContextForMemberLookup(), GetTypeQualifier(m_pGroup));
				return;
			}
			if ((bool)m_mpwiBogus)
			{
				GetErrorContext().ErrorRef(ErrorCode.ERR_BindToBogus, m_mpwiBogus);
				return;
			}
			bool flag = false;
			Name name = m_pGroup.name;
			if (m_pGroup.GetOptionalObject() != null && m_pGroup.GetOptionalObject().type != null && m_pGroup.GetOptionalObject().type.isDelegateType() && m_pGroup.name == GetSymbolLoader().GetNameManager().GetPredefName(PredefinedName.PN_INVOKE))
			{
				flag = true;
				name = m_pGroup.GetOptionalObject().type.getAggregate().name;
			}
			if ((bool)m_results.GetBestResult())
			{
				ReportErrorsForBestMatching(flag, name);
				return;
			}
			if ((bool)m_results.GetUninferrableResult() || (bool)m_mpwiCantInferInstArg)
			{
				if (!m_results.GetUninferrableResult())
				{
					m_results.GetUninferrableResult().Set(m_mpwiCantInferInstArg.Sym.AsMethodSymbol(), m_mpwiCantInferInstArg.GetType(), m_mpwiCantInferInstArg.TypeArgs);
				}
				MethodSymbol methodSymbol = m_results.GetUninferrableResult().Meth();
				TypeArray @params = methodSymbol.Params;
				CType cType = null;
				if (m_pGroup.GetOptionalObject() != null)
				{
					cType = m_pGroup.GetOptionalObject().type;
				}
				else if (m_pGroup.GetOptionalLHS() != null)
				{
					cType = m_pGroup.GetOptionalLHS().type;
				}
				MethWithType methWithType = new MethWithType();
				methWithType.Set(m_results.GetUninferrableResult().Meth(), m_results.GetUninferrableResult().GetType());
				GetErrorContext().Error(ErrorCode.ERR_CantInferMethTypeArgs, methWithType);
				return;
			}
			if ((bool)m_mwtBadArity)
			{
				int size = m_mwtBadArity.Meth().typeVars.size;
				GetErrorContext().ErrorRef((size > 0) ? ErrorCode.ERR_BadArity : ErrorCode.ERR_HasNoTypeVars, m_mwtBadArity, new ErrArgSymKind(m_mwtBadArity.Meth()), m_pArguments.carg);
				return;
			}
			if ((bool)m_mpwiParamTypeConstraints)
			{
				TypeBind.CheckMethConstraints(GetSemanticChecker(), GetErrorContext(), new MethWithInst(m_mpwiParamTypeConstraints));
				return;
			}
			if (m_pInvalidSpecifiedName != null)
			{
				if (m_pGroup.GetOptionalObject() != null && m_pGroup.GetOptionalObject().type.IsAggregateType() && m_pGroup.GetOptionalObject().type.AsAggregateType().GetOwningAggregate().IsDelegate())
				{
					GetErrorContext().Error(ErrorCode.ERR_BadNamedArgumentForDelegateInvoke, m_pGroup.GetOptionalObject().type.AsAggregateType().GetOwningAggregate().name, m_pInvalidSpecifiedName);
				}
				else
				{
					GetErrorContext().Error(ErrorCode.ERR_BadNamedArgument, m_pGroup.name, m_pInvalidSpecifiedName);
				}
				return;
			}
			if (m_pNameUsedInPositionalArgument != null)
			{
				GetErrorContext().Error(ErrorCode.ERR_NamedArgumentUsedInPositional, m_pNameUsedInPositionalArgument);
				return;
			}
			CParameterizedError error;
			if (m_pDelegate != null)
			{
				GetErrorContext().MakeError(out error, ErrorCode.ERR_MethDelegateMismatch, name, m_pDelegate);
				GetErrorContext().AddRelatedTypeLoc(error, m_pDelegate);
			}
			else if (m_fCandidatesUnsupported)
			{
				GetErrorContext().MakeError(out error, ErrorCode.ERR_BindToBogus, name);
			}
			else if (flag)
			{
				GetErrorContext().MakeError(out error, ErrorCode.ERR_BadDelArgCount, name, m_pArguments.carg);
			}
			else if ((m_pGroup.flags & EXPRFLAG.EXF_CTOR) != 0)
			{
				GetErrorContext().MakeError(out error, ErrorCode.ERR_BadCtorArgCount, m_pGroup.GetParentType(), m_pArguments.carg);
			}
			else
			{
				GetErrorContext().MakeError(out error, ErrorCode.ERR_BadArgCount, name, m_pArguments.carg);
			}
			for (int i = 0; i < m_nWrongCount; i++)
			{
				if (GetSemanticChecker().CheckAccess(m_swtWrongCount[i].Sym, m_swtWrongCount[i].GetType(), m_pExprBinder.ContextForMemberLookup(), GetTypeQualifier(m_pGroup)))
				{
					GetErrorContext().AddRelatedSymLoc(error, m_swtWrongCount[i].Sym);
				}
			}
			GetErrorContext().SubmitError(error);
		}

		private void ReportErrorsForBestMatching(bool bUseDelegateErrors, Name nameErr)
		{
			if (m_pDelegate != null)
			{
				GetErrorContext().ErrorRef(ErrorCode.ERR_MethDelegateMismatch, nameErr, m_pDelegate, m_results.GetBestResult());
			}
			else
			{
				if (m_bBindingCollectionAddArgs && ReportErrorsForCollectionAdd())
				{
					return;
				}
				if (bUseDelegateErrors)
				{
					GetErrorContext().Error(ErrorCode.ERR_BadDelArgTypes, m_results.GetBestResult().GetType());
				}
				else if (m_results.GetBestResult().Sym.IsMethodSymbol() && m_results.GetBestResult().Sym.AsMethodSymbol().IsExtension() && m_pGroup.GetOptionalObject() != null)
				{
					GetErrorContext().Error(ErrorCode.ERR_BadExtensionArgTypes, m_pGroup.GetOptionalObject().type, m_pGroup.name, m_results.GetBestResult().Sym);
				}
				else if (m_bBindingCollectionAddArgs)
				{
					GetErrorContext().Error(ErrorCode.ERR_BadArgTypesForCollectionAdd, m_results.GetBestResult());
				}
				else
				{
					GetErrorContext().Error(ErrorCode.ERR_BadArgTypes, m_results.GetBestResult());
				}
				for (int i = 0; i < m_pArguments.carg; i++)
				{
					CType cType = m_pBestParameters.Item(i);
					if (m_pExprBinder.canConvert(m_pArguments.prgexpr[i], cType))
					{
						continue;
					}
					CType cType2 = (m_pArguments.types.Item(i).IsParameterModifierType() ? m_pArguments.types.Item(i).AsParameterModifierType().GetParameterType() : m_pArguments.types.Item(i));
					CType cType3 = (cType.IsParameterModifierType() ? cType.AsParameterModifierType().GetParameterType() : cType);
					if (cType2 == cType3)
					{
						if (cType3 != cType)
						{
							GetErrorContext().Error(ErrorCode.ERR_BadArgRef, i + 1, (cType.IsParameterModifierType() && cType.AsParameterModifierType().isOut) ? "out" : "ref");
						}
						else
						{
							CType cType4 = m_pArguments.types.Item(i);
							GetErrorContext().Error(ErrorCode.ERR_BadArgExtraRef, i + 1, (cType4.IsParameterModifierType() && cType4.AsParameterModifierType().isOut) ? "out" : "ref");
						}
						continue;
					}
					Symbol sym = m_results.GetBestResult().Sym;
					if (i == 0 && sym.IsMethodSymbol() && sym.AsMethodSymbol().IsExtension() && m_pGroup.GetOptionalObject() != null && !m_pExprBinder.canConvertInstanceParamForExtension(m_pGroup.GetOptionalObject(), sym.AsMethodSymbol().Params.Item(0)))
					{
						if (!m_pGroup.GetOptionalObject().type.getBogus())
						{
							GetErrorContext().Error(ErrorCode.ERR_BadInstanceArgType, m_pGroup.GetOptionalObject().type, cType);
						}
					}
					else
					{
						GetErrorContext().Error(ErrorCode.ERR_BadArgType, i + 1, new ErrArg(m_pArguments.types.Item(i), ErrArgFlags.Unique), new ErrArg(cType, ErrArgFlags.Unique));
					}
				}
			}
		}

		private bool ReportErrorsForCollectionAdd()
		{
			for (int i = 0; i < m_pArguments.carg; i++)
			{
				CType cType = m_pBestParameters.Item(i);
				if (cType.IsParameterModifierType())
				{
					GetErrorContext().ErrorRef(ErrorCode.ERR_InitializerAddHasParamModifiers, m_results.GetBestResult());
					return true;
				}
			}
			return false;
		}
	}

	internal class GroupToArgsBinderResult
	{
		public MethPropWithInst BestResult;

		public MethPropWithInst AmbiguousResult;

		public MethPropWithInst InaccessibleResult;

		public MethPropWithInst UninferrableResult;

		public MethPropWithInst InconvertibleResult;

		private List<MethPropWithInst> m_inconvertibleResults;

		public MethPropWithInst GetBestResult()
		{
			return BestResult;
		}

		public MethPropWithInst GetAmbiguousResult()
		{
			return AmbiguousResult;
		}

		public MethPropWithInst GetInaccessibleResult()
		{
			return InaccessibleResult;
		}

		public MethPropWithInst GetUninferrableResult()
		{
			return UninferrableResult;
		}

		public GroupToArgsBinderResult()
		{
			BestResult = new MethPropWithInst();
			AmbiguousResult = new MethPropWithInst();
			InaccessibleResult = new MethPropWithInst();
			UninferrableResult = new MethPropWithInst();
			InconvertibleResult = new MethPropWithInst();
			m_inconvertibleResults = new List<MethPropWithInst>();
		}

		public void AddInconvertibleResult(MethodSymbol method, AggregateType currentType, TypeArray currentTypeArgs)
		{
			if (InconvertibleResult.Sym == null)
			{
				InconvertibleResult.Set(method, currentType, currentTypeArgs);
			}
			m_inconvertibleResults.Add(new MethPropWithInst(method, currentType, currentTypeArgs));
		}

		private static int NumberOfErrorTypes(TypeArray pTypeArgs)
		{
			int num = 0;
			for (int i = 0; i < pTypeArgs.Size; i++)
			{
				if (pTypeArgs.Item(i).IsErrorType())
				{
					num++;
				}
			}
			return num;
		}

		private static bool IsBetterThanCurrent(TypeArray pTypeArgs1, TypeArray pTypeArgs2)
		{
			int num = NumberOfErrorTypes(pTypeArgs1);
			int num2 = NumberOfErrorTypes(pTypeArgs2);
			if (num == num2)
			{
				int num3 = ((pTypeArgs1.Size > pTypeArgs2.Size) ? pTypeArgs2.Size : pTypeArgs1.Size);
				for (int i = 0; i < num3; i++)
				{
					if (pTypeArgs1.Item(i).IsAggregateType())
					{
						num += NumberOfErrorTypes(pTypeArgs1.Item(i).AsAggregateType().GetTypeArgsAll());
					}
					if (pTypeArgs2.Item(i).IsAggregateType())
					{
						num2 += NumberOfErrorTypes(pTypeArgs2.Item(i).AsAggregateType().GetTypeArgsAll());
					}
				}
			}
			return num2 < num;
		}

		public bool IsBetterUninferrableResult(TypeArray pTypeArguments)
		{
			if (UninferrableResult.Sym == null)
			{
				return true;
			}
			if (pTypeArguments == null)
			{
				return false;
			}
			return IsBetterThanCurrent(UninferrableResult.TypeArgs, pTypeArguments);
		}
	}

	private class ImplicitConversion
	{
		private EXPR exprDest;

		private ExpressionBinder binder;

		private EXPR exprSrc;

		private CType typeSrc;

		private CType typeDest;

		private EXPRTYPEORNAMESPACE exprTypeDest;

		private bool needsExprDest;

		private CONVERTTYPE flags;

		public EXPR ExprDest => exprDest;

		public ImplicitConversion(ExpressionBinder binder, EXPR exprSrc, CType typeSrc, EXPRTYPEORNAMESPACE typeDest, bool needsExprDest, CONVERTTYPE flags)
		{
			this.binder = binder;
			this.exprSrc = exprSrc;
			this.typeSrc = typeSrc;
			this.typeDest = typeDest.TypeOrNamespace.AsType();
			exprTypeDest = typeDest;
			this.needsExprDest = needsExprDest;
			this.flags = flags;
			exprDest = null;
		}

		public bool Bind()
		{
			if (typeSrc == null || typeDest == null || typeDest.IsNeverSameType())
			{
				return false;
			}
			switch (typeDest.GetTypeKind())
			{
			case TypeKind.TK_ErrorType:
				if (typeSrc != typeDest)
				{
					return false;
				}
				if (needsExprDest)
				{
					exprDest = exprSrc;
				}
				return true;
			case TypeKind.TK_NullType:
				if (!typeSrc.IsNullType())
				{
					return false;
				}
				if (needsExprDest)
				{
					exprDest = exprSrc;
				}
				return true;
			case TypeKind.TK_MethodGroupType:
				VSFAIL("Something is wrong with Type.IsNeverSameType()");
				return false;
			case TypeKind.TK_NaturalIntegerType:
			case TypeKind.TK_ArgumentListType:
				return typeSrc == typeDest;
			case TypeKind.TK_VoidType:
				return false;
			default:
			{
				if (typeSrc.IsErrorType())
				{
					return false;
				}
				if (typeSrc == typeDest && ((flags & CONVERTTYPE.ISEXPLICIT) == 0 || (!typeSrc.isPredefType(PredefinedType.PT_FLOAT) && !typeSrc.isPredefType(PredefinedType.PT_DOUBLE))))
				{
					if (needsExprDest)
					{
						exprDest = exprSrc;
					}
					return true;
				}
				if (typeDest.IsNullableType())
				{
					return BindNubConversion(typeDest.AsNullableType());
				}
				if (typeSrc.IsNullableType())
				{
					return bindImplicitConversionFromNullable(typeSrc.AsNullableType());
				}
				if ((flags & CONVERTTYPE.ISEXPLICIT) != 0)
				{
					flags |= CONVERTTYPE.NOUDC;
				}
				FUNDTYPE fUNDTYPE = typeDest.fundType();
				switch (typeSrc.GetTypeKind())
				{
				default:
					VSFAIL("Bad type symbol kind");
					break;
				case TypeKind.TK_MethodGroupType:
					if (exprSrc.isMEMGRP())
					{
						EXPRCALL pexprDst;
						bool result = binder.BindGrpConversion(exprSrc.asMEMGRP(), typeDest, needsExprDest, out pexprDst, fReportErrors: false);
						exprDest = pexprDst;
						return result;
					}
					return false;
				case TypeKind.TK_VoidType:
				case TypeKind.TK_ErrorType:
				case TypeKind.TK_ArgumentListType:
				case TypeKind.TK_ParameterModifierType:
					return false;
				case TypeKind.TK_NullType:
					if (bindImplicitConversionFromNull())
					{
						return true;
					}
					break;
				case TypeKind.TK_ArrayType:
					if (bindImplicitConversionFromArray())
					{
						return true;
					}
					break;
				case TypeKind.TK_PointerType:
					if (bindImplicitConversionFromPointer())
					{
						return true;
					}
					break;
				case TypeKind.TK_TypeParameterType:
					if (bindImplicitConversionFromTypeVar(typeSrc.AsTypeParameterType()))
					{
						return true;
					}
					break;
				case TypeKind.TK_AggregateType:
					if (typeSrc.isSpecialByRefType())
					{
						return false;
					}
					if (bindImplicitConversionFromAgg(typeSrc.AsAggregateType()))
					{
						return true;
					}
					break;
				}
				if (exprSrc != null && exprSrc.RuntimeObject != null && typeDest.AssociatedSystemType.IsInstanceOfType(exprSrc.RuntimeObject) && binder.GetSemanticChecker().CheckTypeAccess(typeDest, binder.Context.ContextForMemberLookup()))
				{
					if (needsExprDest)
					{
						binder.bindSimpleCast(exprSrc, exprTypeDest, out exprDest, exprSrc.flags & EXPRFLAG.EXF_CANTBENULL);
					}
					return true;
				}
				if ((flags & CONVERTTYPE.NOUDC) == 0)
				{
					return binder.bindUserDefinedConversion(exprSrc, typeSrc, typeDest, needsExprDest, out exprDest, fImplicitOnly: true);
				}
				return false;
			}
			}
		}

		private bool BindNubConversion(NullableType nubDst)
		{
			AggregateType ats = nubDst.GetAts(GetErrorContext());
			if (ats == null)
			{
				return false;
			}
			if (GetSymbolLoader().HasBaseConversion(nubDst.GetUnderlyingType(), typeSrc) && !CConversions.FWrappingConv(typeSrc, nubDst))
			{
				if ((flags & CONVERTTYPE.ISEXPLICIT) == 0)
				{
					return false;
				}
				if (needsExprDest)
				{
					binder.bindSimpleCast(exprSrc, exprTypeDest, out exprDest, EXPRFLAG.EXF_INDEXER);
				}
				return true;
			}
			int pcnub;
			CType cType = nubDst.StripNubs(out pcnub);
			EXPRCLASS pDestinationTypeExpr = GetExprFactory().MakeClass(cType);
			int pcnub2;
			CType cType2 = typeSrc.StripNubs(out pcnub2);
			ConversionFunc conversionFunc = (((flags & CONVERTTYPE.ISEXPLICIT) != 0) ? new ConversionFunc(binder.BindExplicitConversion) : new ConversionFunc(binder.BindImplicitConversion));
			if (pcnub2 == 0)
			{
				if (typeSrc.IsNullType())
				{
					if (needsExprDest)
					{
						if (exprSrc.isCONSTANT_OK())
						{
							exprDest = GetExprFactory().CreateZeroInit(nubDst);
						}
						else
						{
							exprDest = GetExprFactory().CreateCast((EXPRFLAG)0, typeDest, exprSrc);
						}
					}
					return true;
				}
				EXPR ppDestinationExpr = exprSrc;
				if (typeSrc == cType || conversionFunc(exprSrc, typeSrc, pDestinationTypeExpr, nubDst, needsExprDest, out ppDestinationExpr, flags | CONVERTTYPE.NOUDC))
				{
					if (needsExprDest)
					{
						EXPRUSERDEFINEDCONVERSION eXPRUSERDEFINEDCONVERSION = ((ppDestinationExpr.kind == ExpressionKind.EK_USERDEFINEDCONVERSION) ? ppDestinationExpr.asUSERDEFINEDCONVERSION() : null);
						if (eXPRUSERDEFINEDCONVERSION != null)
						{
							ppDestinationExpr = eXPRUSERDEFINEDCONVERSION.UserDefinedCall;
						}
						for (int i = 0; i < pcnub; i++)
						{
							ppDestinationExpr = binder.BindNubNew(ppDestinationExpr);
							ppDestinationExpr.asCALL().nubLiftKind = NullableCallLiftKind.NullableConversionConstructor;
						}
						if (eXPRUSERDEFINEDCONVERSION != null)
						{
							eXPRUSERDEFINEDCONVERSION.UserDefinedCall = ppDestinationExpr;
							eXPRUSERDEFINEDCONVERSION.setType(ppDestinationExpr.type);
							ppDestinationExpr = eXPRUSERDEFINEDCONVERSION;
						}
						exprDest = ppDestinationExpr;
					}
					return true;
				}
				if ((flags & CONVERTTYPE.NOUDC) == 0)
				{
					return binder.bindUserDefinedConversion(exprSrc, typeSrc, nubDst, needsExprDest, out exprDest, (flags & CONVERTTYPE.ISEXPLICIT) == 0);
				}
				return false;
			}
			if (cType2 != cType && !conversionFunc(null, cType2, pDestinationTypeExpr, nubDst, needsExprDest: false, out exprDest, flags | CONVERTTYPE.NOUDC))
			{
				if ((flags & CONVERTTYPE.NOUDC) == 0)
				{
					return binder.bindUserDefinedConversion(exprSrc, typeSrc, nubDst, needsExprDest, out exprDest, (flags & CONVERTTYPE.ISEXPLICIT) == 0);
				}
				return false;
			}
			if (needsExprDest)
			{
				MethWithInst mwi = new MethWithInst(null, null);
				EXPRMEMGRP pMemberGroup = GetExprFactory().CreateMemGroup(null, mwi);
				EXPRCALL eXPRCALL = GetExprFactory().CreateCall((EXPRFLAG)0, nubDst, exprSrc, pMemberGroup, null);
				EXPR ppDestinationExpr2 = binder.mustCast(exprSrc, cType2);
				EXPRCLASS pDestinationTypeExpr2 = GetExprFactory().MakeClass(cType);
				if (!(((flags & CONVERTTYPE.ISEXPLICIT) == 0) ? binder.BindImplicitConversion(ppDestinationExpr2, ppDestinationExpr2.type, pDestinationTypeExpr2, cType, out ppDestinationExpr2, flags | CONVERTTYPE.NOUDC) : binder.BindExplicitConversion(ppDestinationExpr2, ppDestinationExpr2.type, pDestinationTypeExpr2, cType, out ppDestinationExpr2, flags | CONVERTTYPE.NOUDC)))
				{
					VSFAIL("bind(Im|Ex)plicitConversion failed unexpectedly");
					return false;
				}
				eXPRCALL.castOfNonLiftedResultToLiftedType = binder.mustCast(ppDestinationExpr2, nubDst, (CONVERTTYPE)0);
				eXPRCALL.nubLiftKind = NullableCallLiftKind.NullableConversion;
				eXPRCALL.pConversions = eXPRCALL.castOfNonLiftedResultToLiftedType;
				exprDest = eXPRCALL;
			}
			return true;
		}

		private bool bindImplicitConversionFromNull()
		{
			FUNDTYPE fUNDTYPE = typeDest.fundType();
			if (fUNDTYPE != FUNDTYPE.FT_REF && fUNDTYPE != FUNDTYPE.FT_PTR && (fUNDTYPE != FUNDTYPE.FT_VAR || !typeDest.AsTypeParameterType().IsReferenceType()) && !typeDest.isPredefType(PredefinedType.PT_G_OPTIONAL))
			{
				return false;
			}
			if (needsExprDest)
			{
				if (exprSrc.isCONSTANT_OK())
				{
					exprDest = GetExprFactory().CreateZeroInit(typeDest);
				}
				else
				{
					exprDest = GetExprFactory().CreateCast((EXPRFLAG)0, typeDest, exprSrc);
				}
			}
			return true;
		}

		private bool bindImplicitConversionFromNullable(NullableType nubSrc)
		{
			AggregateType ats = nubSrc.GetAts(GetErrorContext());
			if (ats == null)
			{
				return false;
			}
			if (ats == typeDest)
			{
				if (needsExprDest)
				{
					exprDest = exprSrc;
				}
				return true;
			}
			if (GetSymbolLoader().HasBaseConversion(nubSrc.GetUnderlyingType(), typeDest) && !CConversions.FUnwrappingConv(nubSrc, typeDest))
			{
				if (needsExprDest)
				{
					binder.bindSimpleCast(exprSrc, exprTypeDest, out exprDest, EXPRFLAG.EXF_CTOR);
					if (!typeDest.isPredefType(PredefinedType.PT_OBJECT))
					{
						binder.bindSimpleCast(exprDest, exprTypeDest, out exprDest, EXPRFLAG.EXF_ASFINALLYLEAVE);
					}
				}
				return true;
			}
			if ((flags & CONVERTTYPE.NOUDC) == 0)
			{
				return binder.bindUserDefinedConversion(exprSrc, nubSrc, typeDest, needsExprDest, out exprDest, fImplicitOnly: true);
			}
			return false;
		}

		private bool bindImplicitConversionFromArray()
		{
			if (!GetSymbolLoader().HasBaseConversion(typeSrc, typeDest))
			{
				return false;
			}
			EXPRFLAG exprFlags = (EXPRFLAG)0;
			if ((typeDest.IsArrayType() || (typeDest.isInterfaceType() && typeDest.AsAggregateType().GetTypeArgsAll().Size == 1 && (typeDest.AsAggregateType().GetTypeArgsAll().Item(0) != typeSrc.AsArrayType().GetElementType() || (flags & CONVERTTYPE.FORCECAST) != 0))) && ((flags & CONVERTTYPE.FORCECAST) != 0 || TypeManager.TypeContainsTyVars(typeSrc, null) || TypeManager.TypeContainsTyVars(typeDest, null)))
			{
				exprFlags = EXPRFLAG.EXF_OPERATOR;
			}
			if (needsExprDest)
			{
				binder.bindSimpleCast(exprSrc, exprTypeDest, out exprDest, exprFlags);
			}
			return true;
		}

		private bool bindImplicitConversionFromPointer()
		{
			if (typeDest.IsPointerType() && typeDest.AsPointerType().GetReferentType() == binder.getVoidType())
			{
				if (needsExprDest)
				{
					binder.bindSimpleCast(exprSrc, exprTypeDest, out exprDest);
				}
				return true;
			}
			return false;
		}

		private bool bindImplicitConversionFromAgg(AggregateType aggTypeSrc)
		{
			AggregateSymbol aggregate = aggTypeSrc.getAggregate();
			if (aggregate.IsEnum())
			{
				return bindImplicitConversionFromEnum(aggTypeSrc);
			}
			if (typeDest.isEnumType())
			{
				if (bindImplicitConversionToEnum(aggTypeSrc))
				{
					return true;
				}
			}
			else if (aggregate.getThisType().isSimpleType() && typeDest.isSimpleType() && bindImplicitConversionBetweenSimpleTypes(aggTypeSrc))
			{
				return true;
			}
			return bindImplicitConversionToBase(aggTypeSrc);
		}

		private bool bindImplicitConversionToBase(AggregateType pSource)
		{
			if (!typeDest.IsAggregateType() || !GetSymbolLoader().HasBaseConversion(pSource, typeDest))
			{
				return false;
			}
			EXPRFLAG exprFlags = (EXPRFLAG)0;
			if (pSource.getAggregate().IsStruct() && typeDest.fundType() == FUNDTYPE.FT_REF)
			{
				exprFlags = (EXPRFLAG)131074;
			}
			else if (exprSrc != null)
			{
				exprFlags = exprSrc.flags & EXPRFLAG.EXF_CANTBENULL;
			}
			if (needsExprDest)
			{
				binder.bindSimpleCast(exprSrc, exprTypeDest, out exprDest, exprFlags);
			}
			return true;
		}

		private bool bindImplicitConversionFromEnum(AggregateType aggTypeSrc)
		{
			if (typeDest.IsAggregateType() && GetSymbolLoader().HasBaseConversion(aggTypeSrc, typeDest.AsAggregateType()))
			{
				if (needsExprDest)
				{
					binder.bindSimpleCast(exprSrc, exprTypeDest, out exprDest, (EXPRFLAG)131074);
				}
				return true;
			}
			return false;
		}

		private bool bindImplicitConversionToEnum(AggregateType aggTypeSrc)
		{
			if (aggTypeSrc.getAggregate().GetPredefType() != PredefinedType.PT_BOOL && exprSrc != null && exprSrc.isZero() && exprSrc.type.isNumericType() && (flags & CONVERTTYPE.STANDARD) == 0)
			{
				if (needsExprDest)
				{
					exprDest = GetExprFactory().CreateConstant(typeDest, ConstValFactory.GetDefaultValue(typeDest.constValKind()));
				}
				return true;
			}
			return false;
		}

		private bool bindImplicitConversionBetweenSimpleTypes(AggregateType aggTypeSrc)
		{
			AggregateSymbol aggregate = aggTypeSrc.getAggregate();
			PredefinedType predefType = aggregate.GetPredefType();
			PredefinedType predefType2 = typeDest.getPredefType();
			bool flag = false;
			ConvKind convKind;
			if (exprSrc == null || !exprSrc.isCONSTANT_OK() || ((predefType != PredefinedType.PT_INT || predefType2 == PredefinedType.PT_BOOL || predefType2 == PredefinedType.PT_CHAR) && (predefType != PredefinedType.PT_LONG || predefType2 != PredefinedType.PT_ULONG)) || !isConstantInRange(exprSrc.asCONSTANT(), typeDest))
			{
				convKind = ((predefType != predefType2) ? GetConvKind(predefType, predefType2) : ConvKind.Implicit);
			}
			else
			{
				convKind = ConvKind.Implicit;
				flag = needsExprDest && GetConvKind(predefType, predefType2) != ConvKind.Implicit;
			}
			if (convKind != ConvKind.Implicit)
			{
				return false;
			}
			if (exprSrc.GetConst() != null && binder.bindConstantCast(exprSrc, exprTypeDest, needsExprDest, out exprDest, explicitConversion: false) == ConstCastResult.Success)
			{
				return true;
			}
			if (isUserDefinedConversion(predefType, predefType2))
			{
				if (!needsExprDest)
				{
					return true;
				}
				return binder.bindUserDefinedConversion(exprSrc, aggTypeSrc, typeDest, needsExprDest, out exprDest, fImplicitOnly: true);
			}
			if (needsExprDest)
			{
				binder.bindSimpleCast(exprSrc, exprTypeDest, out exprDest);
			}
			return true;
		}

		private bool bindImplicitConversionFromTypeVar(TypeParameterType tyVarSrc)
		{
			CType cType = tyVarSrc.GetEffectiveBaseClass();
			TypeArray bounds = tyVarSrc.GetBounds();
			int num = -1;
			while (!binder.canConvert(cType, typeDest, flags | CONVERTTYPE.NOUDC))
			{
				do
				{
					if (++num >= bounds.Size)
					{
						return false;
					}
					cType = bounds.Item(num);
				}
				while (!cType.isInterfaceType() && !cType.IsTypeParameterType());
			}
			if (!needsExprDest)
			{
				return true;
			}
			if (typeDest.IsTypeParameterType())
			{
				EXPRCLASS eXPRCLASS = GetExprFactory().MakeClass(binder.GetReqPDT(PredefinedType.PT_OBJECT));
				binder.bindSimpleCast(exprSrc, eXPRCLASS, out var pexprDest, EXPRFLAG.EXF_UNREALIZEDGOTO);
				binder.bindSimpleCast(pexprDest, exprTypeDest, out exprDest, EXPRFLAG.EXF_ASFINALLYLEAVE);
			}
			else
			{
				binder.bindSimpleCast(exprSrc, exprTypeDest, out exprDest, EXPRFLAG.EXF_UNREALIZEDGOTO);
			}
			return true;
		}

		private SymbolLoader GetSymbolLoader()
		{
			return binder.GetSymbolLoader();
		}

		private ExprFactory GetExprFactory()
		{
			return binder.GetExprFactory();
		}

		private ErrorHandling GetErrorContext()
		{
			return binder.GetErrorContext();
		}
	}

	protected class UnaOpSig
	{
		public PredefinedType pt;

		public UnaOpMask grfuom;

		public int cuosSkip;

		public PfnBindUnaOp pfn;

		public UnaOpFuncKind fnkind;

		public UnaOpSig()
		{
		}

		public UnaOpSig(PredefinedType pt, UnaOpMask grfuom, int cuosSkip, PfnBindUnaOp pfn, UnaOpFuncKind fnkind)
		{
			this.pt = pt;
			this.grfuom = grfuom;
			this.cuosSkip = cuosSkip;
			this.pfn = pfn;
			this.fnkind = fnkind;
		}
	}

	protected class UnaOpFullSig : UnaOpSig
	{
		private LiftFlags grflt;

		private CType type;

		public UnaOpFullSig(CType type, PfnBindUnaOp pfn, LiftFlags grflt, UnaOpFuncKind fnkind)
		{
			pt = PredefinedType.PT_UNDEFINEDINDEX;
			grfuom = UnaOpMask.None;
			cuosSkip = 0;
			base.pfn = pfn;
			this.type = type;
			this.grflt = grflt;
			base.fnkind = fnkind;
		}

		public UnaOpFullSig(ExpressionBinder fnc, UnaOpSig uos)
		{
			pt = uos.pt;
			grfuom = uos.grfuom;
			cuosSkip = uos.cuosSkip;
			pfn = uos.pfn;
			fnkind = uos.fnkind;
			type = ((pt != PredefinedType.PT_UNDEFINEDINDEX) ? fnc.GetOptPDT(pt) : null);
			grflt = LiftFlags.None;
		}

		public bool FPreDef()
		{
			return pt != PredefinedType.PT_UNDEFINEDINDEX;
		}

		public bool isLifted()
		{
			if (grflt == LiftFlags.None)
			{
				return false;
			}
			return true;
		}

		public bool Convert()
		{
			return (grflt & LiftFlags.Convert1) != 0;
		}

		public new CType GetType()
		{
			return type;
		}
	}

	private const byte ID = 1;

	private const byte IMP = 2;

	private const byte EXP = 3;

	private const byte NO = 5;

	private const byte CONV_KIND_MASK = 15;

	private const byte UDC = 64;

	private const byte XUD = 67;

	private const byte IUD = 66;

	private static readonly byte[,] simpleTypeConversions = new byte[13, 13]
	{
		{
			1, 2, 2, 2, 2, 2, 66, 3, 5, 3,
			2, 2, 2
		},
		{
			3, 1, 2, 2, 2, 2, 66, 3, 5, 3,
			3, 3, 3
		},
		{
			3, 3, 1, 2, 2, 2, 66, 3, 5, 3,
			3, 3, 3
		},
		{
			3, 3, 3, 1, 2, 2, 66, 3, 5, 3,
			3, 3, 3
		},
		{
			3, 3, 3, 3, 1, 2, 67, 3, 5, 3,
			3, 3, 3
		},
		{
			3, 3, 3, 3, 3, 1, 67, 3, 5, 3,
			3, 3, 3
		},
		{
			67, 67, 67, 67, 67, 67, 1, 67, 5, 67,
			67, 67, 67
		},
		{
			3, 3, 2, 2, 2, 2, 66, 1, 5, 3,
			2, 2, 2
		},
		{
			5, 5, 5, 5, 5, 5, 5, 5, 1, 5,
			5, 5, 5
		},
		{
			3, 2, 2, 2, 2, 2, 66, 3, 5, 1,
			3, 3, 3
		},
		{
			3, 3, 2, 2, 2, 2, 66, 3, 5, 3,
			1, 2, 2
		},
		{
			3, 3, 3, 2, 2, 2, 66, 3, 5, 3,
			3, 1, 2
		},
		{
			3, 3, 3, 3, 2, 2, 66, 3, 5, 3,
			3, 3, 1
		}
	};

	private const int NUM_SIMPLE_TYPES = 13;

	private const int NUM_EXT_TYPES = 16;

	private const byte same = 0;

	private const byte left = 1;

	private const byte right = 2;

	private const byte neither = 3;

	private static readonly byte[,] simpleTypeBetter = new byte[16, 16]
	{
		{
			0, 1, 1, 1, 1, 1, 1, 3, 3, 2,
			1, 1, 1, 3, 3, 1
		},
		{
			2, 0, 1, 1, 1, 1, 1, 3, 3, 2,
			1, 1, 1, 3, 3, 1
		},
		{
			2, 2, 0, 1, 1, 1, 1, 2, 3, 2,
			2, 1, 1, 3, 3, 1
		},
		{
			2, 2, 2, 0, 1, 1, 1, 2, 3, 2,
			2, 2, 1, 3, 3, 1
		},
		{
			2, 2, 2, 2, 0, 1, 3, 2, 3, 2,
			2, 2, 2, 3, 3, 1
		},
		{
			2, 2, 2, 2, 2, 0, 3, 2, 3, 2,
			2, 2, 2, 3, 3, 1
		},
		{
			2, 2, 2, 2, 3, 3, 0, 2, 3, 2,
			2, 2, 2, 3, 3, 1
		},
		{
			3, 3, 1, 1, 1, 1, 1, 0, 3, 3,
			1, 1, 1, 3, 3, 1
		},
		{
			3, 3, 3, 3, 3, 3, 3, 3, 0, 3,
			3, 3, 3, 3, 3, 1
		},
		{
			1, 1, 1, 1, 1, 1, 1, 3, 3, 0,
			1, 1, 1, 3, 3, 1
		},
		{
			2, 2, 1, 1, 1, 1, 1, 2, 3, 2,
			0, 1, 1, 3, 3, 1
		},
		{
			2, 2, 2, 1, 1, 1, 1, 2, 3, 2,
			2, 0, 1, 3, 3, 1
		},
		{
			2, 2, 2, 2, 1, 1, 1, 2, 3, 2,
			2, 2, 0, 3, 3, 1
		},
		{
			3, 3, 3, 3, 3, 3, 3, 3, 3, 3,
			3, 3, 3, 0, 3, 1
		},
		{
			3, 3, 3, 3, 3, 3, 3, 3, 3, 3,
			3, 3, 3, 3, 0, 1
		},
		{
			2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
			2, 2, 2, 2, 2, 0
		}
	};

	protected BindingContext Context;

	protected CNullable m_nullable;

	private static readonly PredefinedType[] rgptIntOp = new PredefinedType[4]
	{
		PredefinedType.PT_INT,
		PredefinedType.PT_UINT,
		PredefinedType.PT_LONG,
		PredefinedType.PT_ULONG
	};

	private static readonly PredefinedName[] EK2NAME = new PredefinedName[26]
	{
		PredefinedName.PN_OPEQUALS,
		PredefinedName.PN_OPCOMPARE,
		PredefinedName.PN_OPTRUE,
		PredefinedName.PN_OPFALSE,
		PredefinedName.PN_OPINCREMENT,
		PredefinedName.PN_OPDECREMENT,
		PredefinedName.PN_OPNEGATION,
		PredefinedName.PN_OPEQUALITY,
		PredefinedName.PN_OPINEQUALITY,
		PredefinedName.PN_OPLESSTHAN,
		PredefinedName.PN_OPLESSTHANOREQUAL,
		PredefinedName.PN_OPGREATERTHAN,
		PredefinedName.PN_OPGREATERTHANOREQUAL,
		PredefinedName.PN_OPPLUS,
		PredefinedName.PN_OPMINUS,
		PredefinedName.PN_OPMULTIPLY,
		PredefinedName.PN_OPDIVISION,
		PredefinedName.PN_OPMODULUS,
		PredefinedName.PN_OPUNARYMINUS,
		PredefinedName.PN_OPUNARYPLUS,
		PredefinedName.PN_OPBITWISEAND,
		PredefinedName.PN_OPBITWISEOR,
		PredefinedName.PN_OPXOR,
		PredefinedName.PN_OPCOMPLEMENT,
		PredefinedName.PN_OPLEFTSHIFT,
		PredefinedName.PN_OPRIGHTSHIFT
	};

	protected readonly BinOpSig[] g_binopSignatures;

	protected readonly UnaOpSig[] g_rguos;

	private static readonly byte[,] betterConversionTable = new byte[16, 16]
	{
		{
			0, 0, 0, 0, 0, 0, 0, 0, 0, 2,
			0, 0, 0, 0, 0, 0
		},
		{
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			1, 1, 1, 0, 0, 0
		},
		{
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			0, 1, 1, 0, 0, 0
		},
		{
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			0, 0, 1, 0, 0, 0
		},
		{
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			0, 0, 0, 0, 0, 0
		},
		{
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			0, 0, 0, 0, 0, 0
		},
		{
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			0, 0, 0, 0, 0, 0
		},
		{
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			0, 0, 0, 0, 0, 0
		},
		{
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			0, 0, 0, 0, 0, 0
		},
		{
			1, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			1, 1, 1, 0, 0, 0
		},
		{
			0, 2, 0, 0, 0, 0, 0, 0, 0, 2,
			0, 0, 0, 0, 0, 0
		},
		{
			0, 2, 2, 0, 0, 0, 0, 0, 0, 2,
			0, 0, 0, 0, 0, 0
		},
		{
			0, 2, 2, 2, 0, 0, 0, 0, 0, 2,
			0, 0, 0, 0, 0, 0
		},
		{
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			0, 0, 0, 0, 0, 0
		},
		{
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			0, 0, 0, 0, 0, 0
		},
		{
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			0, 0, 0, 0, 0, 0
		}
	};

	private static readonly ErrorCode[] ReadOnlyLocalErrors = new ErrorCode[2]
	{
		ErrorCode.ERR_RefReadonlyLocal,
		ErrorCode.ERR_AssgReadonlyLocal
	};

	private static readonly ErrorCode[] ReadOnlyErrors = new ErrorCode[8]
	{
		ErrorCode.ERR_RefReadonly,
		ErrorCode.ERR_AssgReadonly,
		ErrorCode.ERR_RefReadonlyStatic,
		ErrorCode.ERR_AssgReadonlyStatic,
		ErrorCode.ERR_RefReadonly2,
		ErrorCode.ERR_AssgReadonly2,
		ErrorCode.ERR_RefReadonlyStatic2,
		ErrorCode.ERR_AssgReadonlyStatic2
	};

	protected SymbolLoader SymbolLoader => Context.SymbolLoader;

	protected CSemanticChecker SemanticChecker => Context.SemanticChecker;

	private ErrorHandling ErrorContext => SymbolLoader.ErrorContext;

	protected TypeManager TypeManager => SymbolLoader.TypeManager;

	private ExprFactory ExprFactory => Context.GetExprFactory();

	protected CType VoidType => GetSymbolLoader().GetTypeManager().GetVoid();

	private static void RoundToFloat(double d, out float f)
	{
		f = (float)d;
	}

	private static long I64(long x)
	{
		return x;
	}

	private static long I64(ulong x)
	{
		return (long)x;
	}

	private static void RETAILVERIFY(bool b)
	{
		if (!b)
		{
			throw Error.InternalCompilerError();
		}
	}

	private static ConvKind GetConvKind(PredefinedType ptSrc, PredefinedType ptDst)
	{
		if ((int)ptSrc < 13 && (int)ptDst < 13)
		{
			return (ConvKind)(simpleTypeConversions[(uint)ptSrc, (uint)ptDst] & 0xF);
		}
		if (ptSrc == ptDst || (ptDst == PredefinedType.PT_OBJECT && ptSrc < PredefinedType.PT_COUNT))
		{
			return ConvKind.Implicit;
		}
		if (ptSrc == PredefinedType.PT_OBJECT && ptDst < PredefinedType.PT_COUNT)
		{
			return ConvKind.Explicit;
		}
		return ConvKind.Unknown;
	}

	private static bool isUserDefinedConversion(PredefinedType ptSrc, PredefinedType ptDst)
	{
		if ((int)ptSrc < 13 && (int)ptDst < 13)
		{
			return (simpleTypeConversions[(uint)ptSrc, (uint)ptDst] & 0x40) != 0;
		}
		return false;
	}

	private BetterType WhichSimpleConversionIsBetter(PredefinedType pt1, PredefinedType pt2)
	{
		RETAILVERIFY((int)pt1 < 16);
		RETAILVERIFY((int)pt2 < 16);
		return (BetterType)simpleTypeBetter[(uint)pt1, (uint)pt2];
	}

	private BetterType WhichTypeIsBetter(PredefinedType pt1, PredefinedType pt2, CType typeGiven)
	{
		if (pt1 == pt2)
		{
			return BetterType.Same;
		}
		if (typeGiven.isPredefType(pt1))
		{
			return BetterType.Left;
		}
		if (typeGiven.isPredefType(pt2))
		{
			return BetterType.Right;
		}
		if ((int)pt1 <= 16 && (int)pt2 <= 16)
		{
			return WhichSimpleConversionIsBetter(pt1, pt2);
		}
		if (pt2 == PredefinedType.PT_OBJECT && pt1 < PredefinedType.PT_COUNT)
		{
			return BetterType.Left;
		}
		if (pt1 == PredefinedType.PT_OBJECT && pt2 < PredefinedType.PT_COUNT)
		{
			return BetterType.Right;
		}
		return WhichTypeIsBetter(GetOptPDT(pt1), GetOptPDT(pt2), typeGiven);
	}

	private BetterType WhichTypeIsBetter(CType type1, CType type2, CType typeGiven)
	{
		if (type1 == type2)
		{
			return BetterType.Same;
		}
		if (typeGiven == type1)
		{
			return BetterType.Left;
		}
		if (typeGiven == type2)
		{
			return BetterType.Right;
		}
		bool flag = canConvert(type1, type2);
		bool flag2 = canConvert(type2, type1);
		if (flag != flag2)
		{
			if (!flag)
			{
				return BetterType.Right;
			}
			return BetterType.Left;
		}
		if (!type1.IsNullableType() || !type2.IsNullableType() || !type1.AsNullableType().UnderlyingType.isPredefined() || !type2.AsNullableType().UnderlyingType.isPredefined())
		{
			return BetterType.Neither;
		}
		PredefinedType predefType = (type1 as NullableType).UnderlyingType.getPredefType();
		PredefinedType predefType2 = (type2 as NullableType).UnderlyingType.getPredefType();
		if ((int)predefType <= 16 && (int)predefType2 <= 16)
		{
			return WhichSimpleConversionIsBetter(predefType, predefType2);
		}
		return BetterType.Neither;
	}

	public bool canConvert(CType src, CType dest, CONVERTTYPE flags)
	{
		EXPRCLASS pDestinationTypeExpr = ExprFactory.MakeClass(dest);
		return BindImplicitConversion(null, src, pDestinationTypeExpr, dest, flags);
	}

	public bool canConvert(CType src, CType dest)
	{
		return canConvert(src, dest, (CONVERTTYPE)0);
	}

	public bool canConvert(EXPR expr, CType dest)
	{
		return canConvert(expr, dest, (CONVERTTYPE)0);
	}

	public bool canConvert(EXPR expr, CType dest, CONVERTTYPE flags)
	{
		EXPRCLASS pDestinationTypeExpr = ExprFactory.MakeClass(dest);
		return BindImplicitConversion(expr, expr.type, pDestinationTypeExpr, dest, flags);
	}

	public EXPR mustConvertCore(EXPR expr, EXPRTYPEORNAMESPACE destExpr)
	{
		return mustConvertCore(expr, destExpr, (CONVERTTYPE)0);
	}

	public EXPR mustConvertCore(EXPR expr, EXPRTYPEORNAMESPACE destExpr, CONVERTTYPE flags)
	{
		CType cType = destExpr.TypeOrNamespace as CType;
		if (BindImplicitConversion(expr, expr.type, destExpr, cType, out var ppDestinationExpr, flags))
		{
			checkUnsafe(expr.type);
			checkUnsafe(cType);
			return ppDestinationExpr;
		}
		if (expr.isOK() && !cType.IsErrorType())
		{
			FUNDTYPE fUNDTYPE = expr.type.fundType();
			FUNDTYPE fUNDTYPE2 = cType.fundType();
			if (expr.isCONSTANT_OK() && expr.type.isSimpleType() && cType.isSimpleType())
			{
				if ((fUNDTYPE == FUNDTYPE.FT_I4 && (fUNDTYPE2 <= FUNDTYPE.FT_U4 || fUNDTYPE2 == FUNDTYPE.FT_U8)) || (fUNDTYPE == FUNDTYPE.FT_I8 && fUNDTYPE2 == FUNDTYPE.FT_U8))
				{
					string text = expr.asCONSTANT().I64Value.ToString(CultureInfo.InvariantCulture);
					ErrorContext.Error(ErrorCode.ERR_ConstOutOfRange, text, cType);
					ppDestinationExpr = ExprFactory.CreateCast((EXPRFLAG)0, destExpr, expr);
					ppDestinationExpr.SetError();
					return ppDestinationExpr;
				}
				if (fUNDTYPE == FUNDTYPE.FT_R8 && (expr.flags & EXPRFLAG.EXF_LITERALCONST) != 0 && (cType.isPredefType(PredefinedType.PT_FLOAT) || cType.isPredefType(PredefinedType.PT_DECIMAL)))
				{
					ErrorContext.Error(ErrorCode.ERR_LiteralDoubleCast, cType.isPredefType(PredefinedType.PT_DECIMAL) ? "M" : "F", cType);
					ppDestinationExpr = ExprFactory.CreateCast((EXPRFLAG)0, destExpr, expr);
					ppDestinationExpr.SetError();
					return ppDestinationExpr;
				}
			}
			if (expr.type is NullType && cType.fundType() != FUNDTYPE.FT_REF)
			{
				ErrorContext.Error((cType is TypeParameterType) ? ErrorCode.ERR_TypeVarCantBeNull : ErrorCode.ERR_ValueCantBeNull, cType);
			}
			else if (expr.isMEMGRP())
			{
				BindGrpConversion(expr.asMEMGRP(), cType, fReportErrors: true);
			}
			else if (!TypeManager.TypeContainsAnonymousTypes(cType) && canCast(expr.type, cType, flags))
			{
				ErrorContext.Error(ErrorCode.ERR_NoImplicitConvCast, new ErrArg(expr.type, ErrArgFlags.Unique), new ErrArg(cType, ErrArgFlags.Unique));
			}
			else
			{
				ErrorContext.Error(ErrorCode.ERR_NoImplicitConv, new ErrArg(expr.type, ErrArgFlags.Unique), new ErrArg(cType, ErrArgFlags.Unique));
			}
		}
		ppDestinationExpr = ExprFactory.CreateCast((EXPRFLAG)0, destExpr, expr);
		ppDestinationExpr.SetError();
		return ppDestinationExpr;
	}

	public EXPR tryConvert(EXPR expr, CType dest)
	{
		return tryConvert(expr, dest, (CONVERTTYPE)0);
	}

	public EXPR tryConvert(EXPR expr, CType dest, CONVERTTYPE flags)
	{
		EXPRCLASS pDestinationTypeExpr = ExprFactory.MakeClass(dest);
		if (BindImplicitConversion(expr, expr.type, pDestinationTypeExpr, dest, out var ppDestinationExpr, flags))
		{
			checkUnsafe(expr.type);
			checkUnsafe(dest);
			return ppDestinationExpr;
		}
		return null;
	}

	public EXPR mustConvert(EXPR expr, CType dest)
	{
		return mustConvert(expr, dest, (CONVERTTYPE)0);
	}

	public EXPR mustConvert(EXPR expr, CType dest, CONVERTTYPE flags)
	{
		EXPRCLASS dest2 = ExprFactory.MakeClass(dest);
		return mustConvert(expr, dest2, flags);
	}

	public EXPR mustConvert(EXPR expr, EXPRTYPEORNAMESPACE dest, CONVERTTYPE flags)
	{
		return mustConvertCore(expr, dest, flags);
	}

	private EXPR mustCastCore(EXPR expr, EXPRTYPEORNAMESPACE destExpr, CONVERTTYPE flags)
	{
		CType cType = destExpr.TypeOrNamespace as CType;
		SemanticChecker.CheckForStaticClass(null, cType, ErrorCode.ERR_ConvertToStaticClass);
		EXPR ppDestinationExpr;
		if (expr.isOK())
		{
			if (BindExplicitConversion(expr, expr.type, destExpr, cType, out ppDestinationExpr, flags))
			{
				checkUnsafe(expr.type);
				checkUnsafe(cType);
				return ppDestinationExpr;
			}
			if (cType != null && !(cType is ErrorType))
			{
				string text = "";
				EXPR @const = expr.GetConst();
				FUNDTYPE fUNDTYPE = expr.type.fundType();
				bool flag = @const != null && expr.type.isSimpleOrEnum() && cType.isSimpleOrEnum();
				if (flag && fUNDTYPE == FUNDTYPE.FT_STRUCT)
				{
					ErrorContext.Error(ErrorCode.ERR_ConstOutOfRange, @const.asCONSTANT().Val.decVal.ToString(CultureInfo.InvariantCulture), cType);
				}
				else if (flag && Context.CheckedConstant)
				{
					if (!canExplicitConversionBeBoundInUncheckedContext(expr, expr.type, destExpr, flags | CONVERTTYPE.NOUDC))
					{
						CantConvert(expr, cType);
					}
					else
					{
						if (fUNDTYPE <= FUNDTYPE.FT_U8)
						{
							text = ((!expr.type.isUnsigned()) ? @const.asCONSTANT().I64Value.ToString(CultureInfo.InvariantCulture) : ((ulong)@const.asCONSTANT().I64Value).ToString(CultureInfo.InvariantCulture));
						}
						else if (fUNDTYPE <= FUNDTYPE.FT_R8)
						{
							text = @const.asCONSTANT().Val.doubleVal.ToString(CultureInfo.InvariantCulture);
						}
						ErrorContext.Error(ErrorCode.ERR_ConstOutOfRangeChecked, text, cType);
					}
				}
				else if (expr.type is NullType && cType.fundType() != FUNDTYPE.FT_REF)
				{
					ErrorContext.Error(ErrorCode.ERR_ValueCantBeNull, cType);
				}
				else if (expr.isMEMGRP())
				{
					BindGrpConversion(expr.asMEMGRP(), cType, fReportErrors: true);
				}
				else
				{
					CantConvert(expr, cType);
				}
			}
		}
		ppDestinationExpr = ExprFactory.CreateCast((EXPRFLAG)0, destExpr, expr);
		ppDestinationExpr.SetError();
		return ppDestinationExpr;
	}

	private void CantConvert(EXPR expr, CType dest)
	{
		if (expr.type != null && !(expr.type is ErrorType))
		{
			ErrorContext.Error(ErrorCode.ERR_NoExplicitConv, new ErrArg(expr.type, ErrArgFlags.Unique), new ErrArg(dest, ErrArgFlags.Unique));
		}
	}

	public EXPR mustCast(EXPR expr, CType dest)
	{
		return mustCast(expr, dest, (CONVERTTYPE)0);
	}

	public EXPR mustCast(EXPR expr, CType dest, CONVERTTYPE flags)
	{
		EXPRCLASS destExpr = ExprFactory.MakeClass(dest);
		return mustCastCore(expr, destExpr, flags);
	}

	private EXPR mustCastInUncheckedContext(EXPR expr, CType dest, CONVERTTYPE flags)
	{
		CheckedContext context = CheckedContext.CreateInstance(Context, checkedNormal: false, checkedConstant: false);
		return new ExpressionBinder(context).mustCast(expr, dest, flags);
	}

	private bool canCast(CType src, CType dest, CONVERTTYPE flags)
	{
		EXPRCLASS pDestinationTypeExpr = ExprFactory.MakeClass(dest);
		return BindExplicitConversion(null, src, pDestinationTypeExpr, dest, flags);
	}

	public bool BindGrpConversion(EXPRMEMGRP grp, CType typeDst, bool fReportErrors)
	{
		EXPRCALL pexprDst;
		return BindGrpConversion(grp, typeDst, needDest: false, out pexprDst, fReportErrors);
	}

	public bool BindGrpConversion(EXPRMEMGRP grp, CType typeDst, bool needDest, out EXPRCALL pexprDst, bool fReportErrors)
	{
		pexprDst = null;
		if (!typeDst.isDelegateType())
		{
			if (fReportErrors)
			{
				ErrorContext.Error(ErrorCode.ERR_MethGrpToNonDel, grp.name, typeDst);
			}
			return false;
		}
		AggregateType aggregateType = typeDst.AsAggregateType();
		MethodSymbol methodSymbol = SymbolLoader.PredefinedMembers.FindDelegateConstructor(aggregateType.getAggregate(), fReportErrors);
		if (methodSymbol == null)
		{
			return false;
		}
		MethodSymbol methodSymbol2 = SymbolLoader.LookupInvokeMeth(aggregateType.getAggregate());
		TypeArray args = GetTypes().SubstTypeArray(methodSymbol2.Params, aggregateType);
		CType cType = GetTypes().SubstType(methodSymbol2.RetType, aggregateType);
		if (!BindGrpConversionCore(out var pmpwi, BindingFlag.BIND_NOPARAMS, grp, ref args, aggregateType, fReportErrors, out var pmpwiAmbig))
		{
			return false;
		}
		MethWithInst pMWI = new MethWithInst(pmpwi);
		MethWithInst methWithInst = new MethWithInst(pmpwiAmbig);
		bool flag = false;
		if (methodSymbol2.Params.Size < args.Size && pMWI.Meth().IsExtension())
		{
			flag = true;
			TypeArray typeArray = GetTypes().SubstTypeArray(pMWI.Meth().Params, pMWI.GetType());
			if (typeArray.Item(0).IsTypeParameterType() ? (!args.Item(0).IsRefType()) : (!typeArray.Item(0).IsRefType()))
			{
				ErrorContext.Error(ErrorCode.ERR_ValueTypeExtDelegate, pMWI, typeArray.Item(0).IsTypeParameterType() ? args.Item(0) : typeArray.Item(0));
			}
		}
		if (!fReportErrors && !needDest)
		{
			return true;
		}
		bool flag2 = methWithInst;
		if ((bool)methWithInst && !fReportErrors)
		{
			ErrorContext.Error(ErrorCode.ERR_AmbigCall, pMWI, methWithInst);
		}
		CType cType2 = GetTypes().SubstType(pMWI.Meth().RetType, pMWI.Ats, pMWI.TypeArgs);
		if (cType != cType2 && !CConversions.FImpRefConv(GetSymbolLoader(), cType2, cType))
		{
			ErrorContext.ErrorRef(ErrorCode.ERR_BadRetType, pMWI, cType2);
			flag2 = true;
		}
		TypeArray typeArray2 = GetTypes().SubstTypeArray(pMWI.Meth().Params, pMWI.Ats, pMWI.TypeArgs);
		if (typeArray2 != args)
		{
			for (int i = 0; i < typeArray2.Size; i++)
			{
				CType cType3 = args.Item(i);
				CType cType4 = typeArray2.Item(i);
				if (cType3 != cType4 && !CConversions.FImpRefConv(GetSymbolLoader(), cType3, cType4))
				{
					ErrorContext.ErrorRef(ErrorCode.ERR_MethDelegateMismatch, pMWI, typeDst);
					flag2 = true;
					break;
				}
			}
		}
		EXPR pObject = ((!flag) ? grp.GetOptionalObject() : null);
		PostBindMethod((grp.flags & EXPRFLAG.EXF_ASFINALLYLEAVE) != 0, ref pMWI, pObject);
		pObject = AdjustMemberObject(pMWI, pObject, out var _, out var pIsMatchingStatic);
		if (!pIsMatchingStatic)
		{
			grp.SetMismatchedStaticBit();
		}
		pObject = (flag ? grp.GetOptionalObject() : pObject);
		if (pMWI.TypeArgs.Size > 0)
		{
			TypeBind.CheckMethConstraints(GetSemanticChecker(), GetErrorContext(), pMWI);
		}
		if (pMWI.Meth().MethKind() == MethodKindEnum.Latent)
		{
			ErrorContext.ErrorRef(ErrorCode.ERR_PartialMethodToDelegate, pMWI);
		}
		if (!needDest)
		{
			return true;
		}
		EXPRFUNCPTR eXPRFUNCPTR = ExprFactory.CreateFunctionPointer(grp.flags & EXPRFLAG.EXF_ASFINALLYLEAVE, getVoidType(), null, pMWI);
		if (!pMWI.Meth().isStatic || flag)
		{
			if (pMWI.Meth().getClass().isPredefAgg(PredefinedType.PT_G_OPTIONAL))
			{
				ErrorContext.Error(ErrorCode.ERR_DelegateOnNullable, pMWI);
			}
			eXPRFUNCPTR.SetOptionalObject(pObject);
			if (pObject != null && pObject.type.fundType() != FUNDTYPE.FT_REF)
			{
				pObject = mustConvert(pObject, GetReqPDT(PredefinedType.PT_OBJECT));
			}
		}
		else
		{
			eXPRFUNCPTR.SetOptionalObject(null);
			pObject = ExprFactory.CreateNull();
		}
		MethWithInst mWI = new MethWithInst(methodSymbol, aggregateType);
		grp.SetOptionalObject(null);
		EXPRCALL eXPRCALL = ExprFactory.CreateCall((EXPRFLAG)131088, aggregateType, ExprFactory.CreateList(pObject, eXPRFUNCPTR), grp, mWI);
		pexprDst = eXPRCALL;
		return true;
	}

	private bool BindGrpConversionCore(out MethPropWithInst pmpwi, BindingFlag bindFlags, EXPRMEMGRP grp, ref TypeArray args, AggregateType atsDelegate, bool fReportErrors, out MethPropWithInst pmpwiAmbig)
	{
		bool flag = false;
		int size = args.Size;
		ArgInfos argInfos = new ArgInfos();
		argInfos.carg = args.Size;
		argInfos.types = args;
		argInfos.fHasExprs = false;
		GroupToArgsBinder groupToArgsBinder = new GroupToArgsBinder(this, bindFlags, grp, argInfos, null, bHasNamedArguments: false, atsDelegate);
		flag = groupToArgsBinder.Bind(fReportErrors);
		GroupToArgsBinderResult resultsOfBind = groupToArgsBinder.GetResultsOfBind();
		pmpwi = resultsOfBind.GetBestResult();
		pmpwiAmbig = resultsOfBind.GetAmbiguousResult();
		return flag;
	}

	private bool canConvertInstanceParamForExtension(EXPR exprSrc, CType typeDest)
	{
		if (exprSrc == null || exprSrc.type == null)
		{
			return false;
		}
		return canConvertInstanceParamForExtension(exprSrc.type, typeDest);
	}

	private bool canConvertInstanceParamForExtension(CType typeSrc, CType typeDest)
	{
		if (!CConversions.FIsSameType(typeSrc, typeDest) && !CConversions.FImpRefConv(GetSymbolLoader(), typeSrc, typeDest))
		{
			return CConversions.FBoxingConv(GetSymbolLoader(), typeSrc, typeDest);
		}
		return true;
	}

	private bool BindImplicitConversion(EXPR pSourceExpr, CType pSourceType, EXPRTYPEORNAMESPACE pDestinationTypeExpr, CType pDestinationTypeForLambdaErrorReporting, CONVERTTYPE flags)
	{
		ImplicitConversion implicitConversion = new ImplicitConversion(this, pSourceExpr, pSourceType, pDestinationTypeExpr, needsExprDest: false, flags);
		return implicitConversion.Bind();
	}

	private bool BindImplicitConversion(EXPR pSourceExpr, CType pSourceType, EXPRTYPEORNAMESPACE pDestinationTypeExpr, CType pDestinationTypeForLambdaErrorReporting, out EXPR ppDestinationExpr, CONVERTTYPE flags)
	{
		ImplicitConversion implicitConversion = new ImplicitConversion(this, pSourceExpr, pSourceType, pDestinationTypeExpr, needsExprDest: true, flags);
		bool result = implicitConversion.Bind();
		ppDestinationExpr = implicitConversion.ExprDest;
		return result;
	}

	private bool BindImplicitConversion(EXPR pSourceExpr, CType pSourceType, EXPRTYPEORNAMESPACE pDestinationTypeExpr, CType pDestinationTypeForLambdaErrorReporting, bool needsExprDest, out EXPR ppDestinationExpr, CONVERTTYPE flags)
	{
		ImplicitConversion implicitConversion = new ImplicitConversion(this, pSourceExpr, pSourceType, pDestinationTypeExpr, needsExprDest, flags);
		bool result = implicitConversion.Bind();
		ppDestinationExpr = (needsExprDest ? implicitConversion.ExprDest : null);
		return result;
	}

	private bool BindExplicitConversion(EXPR pSourceExpr, CType pSourceType, EXPRTYPEORNAMESPACE pDestinationTypeExpr, CType pDestinationTypeForLambdaErrorReporting, bool needsExprDest, out EXPR ppDestinationExpr, CONVERTTYPE flags)
	{
		ExplicitConversion explicitConversion = new ExplicitConversion(this, pSourceExpr, pSourceType, pDestinationTypeExpr, pDestinationTypeForLambdaErrorReporting, needsExprDest, flags);
		bool result = explicitConversion.Bind();
		ppDestinationExpr = (needsExprDest ? explicitConversion.ExprDest : null);
		return result;
	}

	private bool BindExplicitConversion(EXPR pSourceExpr, CType pSourceType, EXPRTYPEORNAMESPACE pDestinationTypeExpr, CType pDestinationTypeForLambdaErrorReporting, out EXPR ppDestinationExpr, CONVERTTYPE flags)
	{
		ExplicitConversion explicitConversion = new ExplicitConversion(this, pSourceExpr, pSourceType, pDestinationTypeExpr, pDestinationTypeForLambdaErrorReporting, needsExprDest: true, flags);
		bool result = explicitConversion.Bind();
		ppDestinationExpr = explicitConversion.ExprDest;
		return result;
	}

	private bool BindExplicitConversion(EXPR pSourceExpr, CType pSourceType, EXPRTYPEORNAMESPACE pDestinationTypeExpr, CType pDestinationTypeForLambdaErrorReporting, CONVERTTYPE flags)
	{
		ExplicitConversion explicitConversion = new ExplicitConversion(this, pSourceExpr, pSourceType, pDestinationTypeExpr, pDestinationTypeForLambdaErrorReporting, needsExprDest: false, flags);
		return explicitConversion.Bind();
	}

	private bool bindUserDefinedConversion(EXPR exprSrc, CType typeSrc, CType typeDst, bool needExprDest, out EXPR pexprDst, bool fImplicitOnly)
	{
		pexprDst = null;
		if (typeSrc == null || typeDst == null || typeSrc.isInterfaceType() || typeDst.isInterfaceType())
		{
			return false;
		}
		CType cType = typeSrc.StripNubs();
		CType cType2 = typeDst.StripNubs();
		bool flag = cType != typeSrc;
		bool flag2 = cType2 != typeDst;
		bool flag3 = flag2 || typeDst.IsRefType() || typeDst.IsPointerType();
		AggregateType[] array = new AggregateType[2];
		int num = 0;
		bool flag4 = fImplicitOnly;
		bool flag5 = false;
		if (cType.IsTypeParameterType())
		{
			AggregateType effectiveBaseClass = cType.AsTypeParameterType().GetEffectiveBaseClass();
			if (effectiveBaseClass != null && effectiveBaseClass.getAggregate().HasConversion(GetSymbolLoader()))
			{
				array[num++] = effectiveBaseClass;
			}
			flag4 = true;
		}
		else if (cType.IsAggregateType() && cType.getAggregate().HasConversion(GetSymbolLoader()))
		{
			array[num++] = cType.AsAggregateType();
			flag5 = cType.isPredefType(PredefinedType.PT_INTPTR) || cType.isPredefType(PredefinedType.PT_UINTPTR);
		}
		if (cType2.IsTypeParameterType())
		{
			AggregateType effectiveBaseClass2;
			if (!fImplicitOnly && (effectiveBaseClass2 = cType2.AsTypeParameterType().GetEffectiveBaseClass()).getAggregate().HasConversion(GetSymbolLoader()))
			{
				array[num++] = effectiveBaseClass2;
			}
		}
		else if (cType2.IsAggregateType())
		{
			if (cType2.getAggregate().HasConversion(GetSymbolLoader()))
			{
				array[num++] = cType2.AsAggregateType();
			}
			if (flag5 && !cType2.isPredefType(PredefinedType.PT_LONG) && !cType2.isPredefType(PredefinedType.PT_ULONG))
			{
				flag5 = false;
			}
		}
		else
		{
			flag5 = false;
		}
		if (num == 0)
		{
			return false;
		}
		List<UdConvInfo> list = new List<UdConvInfo>();
		CType cType3 = null;
		CType cType4 = null;
		bool flag6 = false;
		bool flag7 = false;
		int num2 = -1;
		int num3 = -1;
		CType cType5;
		CType cType6;
		for (int i = 0; i < num; i++)
		{
			AggregateType aggregateType = array[i];
			while (aggregateType != null && aggregateType.getAggregate().HasConversion(GetSymbolLoader()))
			{
				AggregateSymbol aggregate = aggregateType.getAggregate();
				PredefinedType predefType = aggregate.GetPredefType();
				bool flag8 = aggregate.IsPredefined() && (predefType == PredefinedType.PT_INTPTR || predefType == PredefinedType.PT_UINTPTR || predefType == PredefinedType.PT_DECIMAL);
				for (MethodSymbol methodSymbol = aggregate.GetFirstUDConversion(); methodSymbol != null; methodSymbol = methodSymbol.ConvNext())
				{
					if (methodSymbol.Params.Size != 1 || (fImplicitOnly && !methodSymbol.isImplicit()) || GetSemanticChecker().CheckBogus(methodSymbol))
					{
						continue;
					}
					cType5 = GetTypes().SubstType(methodSymbol.Params.Item(0), aggregateType);
					cType6 = GetTypes().SubstType(methodSymbol.RetType, aggregateType);
					bool flag9 = fImplicitOnly;
					if (flag4 && !flag9 && cType5.StripNubs() != cType)
					{
						if (!methodSymbol.isImplicit())
						{
							continue;
						}
						flag9 = true;
					}
					FUNDTYPE fUNDTYPE;
					FUNDTYPE fUNDTYPE2;
					if (((fUNDTYPE = cType6.fundType()) <= FUNDTYPE.FT_R8 && fUNDTYPE > FUNDTYPE.FT_NONE && (fUNDTYPE2 = cType5.fundType()) <= FUNDTYPE.FT_R8 && fUNDTYPE2 > FUNDTYPE.FT_NONE) || (flag5 && (cType6.isPredefType(PredefinedType.PT_INT) || cType6.isPredefType(PredefinedType.PT_UINT))))
					{
						continue;
					}
					if (flag && (flag3 || !flag9) && cType5.IsNonNubValType())
					{
						cType5 = GetTypes().GetNullable(cType5);
					}
					if (flag2 && cType6.IsNonNubValType())
					{
						cType6 = GetTypes().GetNullable(cType6);
					}
					bool flag10 = ((exprSrc != null) ? canConvert(exprSrc, cType5, CONVERTTYPE.STANDARDANDNOUDC) : canConvert(typeSrc, cType5, CONVERTTYPE.STANDARDANDNOUDC));
					if (!flag10 && (flag9 || (!canConvert(cType5, typeSrc, CONVERTTYPE.STANDARDANDNOUDC) && (!flag8 || typeSrc.IsPointerType() || cType5.IsPointerType() || !canCast(typeSrc, cType5, CONVERTTYPE.NOUDC)))))
					{
						continue;
					}
					bool flag11 = canConvert(cType6, typeDst, CONVERTTYPE.STANDARDANDNOUDC);
					if ((!flag11 && (flag9 || (!canConvert(typeDst, cType6, CONVERTTYPE.STANDARDANDNOUDC) && (!flag8 || typeDst.IsPointerType() || cType6.IsPointerType() || !canCast(cType6, typeDst, CONVERTTYPE.NOUDC))))) || isConvInTable(list, methodSymbol, aggregateType, flag10, flag11))
					{
						continue;
					}
					list.Add(new UdConvInfo());
					list[list.Count - 1].mwt = new MethWithType();
					list[list.Count - 1].mwt.Set(methodSymbol, aggregateType);
					list[list.Count - 1].fSrcImplicit = flag10;
					list[list.Count - 1].fDstImplicit = flag11;
					if (!flag6)
					{
						if (cType5 == typeSrc)
						{
							cType3 = cType5;
							num2 = list.Count - 1;
							flag6 = true;
						}
						else if (cType3 == null)
						{
							cType3 = cType5;
							num2 = list.Count - 1;
						}
						else if (cType3 != cType5)
						{
							int num4 = CompareSrcTypesBased(cType3, list[num2].fSrcImplicit, cType5, flag10);
							if (num4 > 0)
							{
								cType3 = cType5;
								num2 = list.Count - 1;
							}
						}
					}
					if (flag7)
					{
						continue;
					}
					if (cType6 == typeDst)
					{
						cType4 = cType6;
						num3 = list.Count - 1;
						flag7 = true;
					}
					else if (cType4 == null)
					{
						cType4 = cType6;
						num3 = list.Count - 1;
					}
					else if (cType4 != cType6)
					{
						int num5 = CompareDstTypesBased(cType4, list[num3].fDstImplicit, cType6, flag11);
						if (num5 > 0)
						{
							cType4 = cType6;
							num3 = list.Count - 1;
						}
					}
				}
				aggregateType = aggregateType.GetBaseClass();
			}
		}
		if (cType3 == null)
		{
			return false;
		}
		int num6 = 3;
		int num7 = -1;
		int num8 = -1;
		for (int j = 0; j < list.Count; j++)
		{
			UdConvInfo udConvInfo = list[j];
			cType5 = GetTypes().SubstType(udConvInfo.mwt.Meth().Params.Item(0), udConvInfo.mwt.GetType());
			cType6 = GetTypes().SubstType(udConvInfo.mwt.Meth().RetType, udConvInfo.mwt.GetType());
			int num9 = 0;
			if (flag && cType5.IsNonNubValType())
			{
				cType5 = GetTypes().GetNullable(cType5);
				num9++;
			}
			if (flag2 && cType6.IsNonNubValType())
			{
				cType6 = GetTypes().GetNullable(cType6);
				num9++;
			}
			if (cType5 == cType3 && cType6 == cType4)
			{
				if (num6 > num9)
				{
					num7 = j;
					num8 = -1;
					num6 = num9;
				}
				else if (num6 >= num9 && num8 < 0)
				{
					num8 = j;
					if (num9 == 0)
					{
						break;
					}
				}
				continue;
			}
			if (!flag6 && cType5 != cType3)
			{
				int num10 = CompareSrcTypesBased(cType3, list[num2].fSrcImplicit, cType5, udConvInfo.fSrcImplicit);
				if (num10 >= 0)
				{
					if (!needExprDest)
					{
						return true;
					}
					num3 = j;
					pexprDst = HandleAmbiguity(exprSrc, typeSrc, typeDst, list, num2, num3);
					return true;
				}
			}
			if (flag7 || cType6 == cType4)
			{
				continue;
			}
			int num11 = CompareDstTypesBased(cType4, list[num3].fDstImplicit, cType6, udConvInfo.fDstImplicit);
			if (num11 >= 0)
			{
				if (!needExprDest)
				{
					return true;
				}
				num3 = j;
				pexprDst = HandleAmbiguity(exprSrc, typeSrc, typeDst, list, num2, num3);
				return true;
			}
		}
		if (!needExprDest)
		{
			return true;
		}
		if (num7 < 0)
		{
			pexprDst = HandleAmbiguity(exprSrc, typeSrc, typeDst, list, num2, num3);
			return true;
		}
		if (num8 >= 0)
		{
			num2 = num7;
			num3 = num8;
			pexprDst = HandleAmbiguity(exprSrc, typeSrc, typeDst, list, num2, num3);
			return true;
		}
		MethWithInst methWithInst = new MethWithInst(list[num7].mwt.Meth(), list[num7].mwt.GetType(), null);
		cType5 = GetTypes().SubstType(methWithInst.Meth().Params.Item(0), methWithInst.GetType());
		cType6 = GetTypes().SubstType(methWithInst.Meth().RetType, methWithInst.GetType());
		EXPR ppTransformedArgument = exprSrc;
		EXPR eXPR;
		if (num6 > 0 && !cType5.IsNullableType() && flag3)
		{
			EXPRMEMGRP pMemberGroup = ExprFactory.CreateMemGroup(null, methWithInst);
			eXPR = ExprFactory.CreateCall((EXPRFLAG)0, typeDst, exprSrc, pMemberGroup, methWithInst);
			EXPR eXPR2 = mustCast(exprSrc, cType5);
			MarkAsIntermediateConversion(eXPR2);
			EXPR expr = BindUDConversionCore(eXPR2, cType5, cType6, typeDst, methWithInst);
			EXPRCALL eXPRCALL = eXPR.asCALL();
			eXPRCALL.castOfNonLiftedResultToLiftedType = mustCast(expr, typeDst);
			eXPRCALL.nubLiftKind = NullableCallLiftKind.UserDefinedConversion;
			if (flag)
			{
				EXPR eXPR3 = null;
				if (cType5 == cType)
				{
					eXPR3 = ((!cType6.IsNullableType()) ? exprSrc : mustCast(exprSrc, cType5));
				}
				else
				{
					NullableType nullable = SymbolLoader.GetTypeManager().GetNullable(cType5);
					eXPR3 = mustCast(exprSrc, nullable);
					MarkAsIntermediateConversion(eXPR3);
				}
				EXPR eXPR4 = ExprFactory.CreateCall((EXPRFLAG)0, typeDst, eXPR3, pMemberGroup, methWithInst);
				eXPR4.asCALL().nubLiftKind = NullableCallLiftKind.NotLiftedIntermediateConversion;
				eXPRCALL.pConversions = eXPR4;
			}
			else
			{
				EXPR eXPR5 = BindUDConversionCore(eXPR2, cType5, cType6, typeDst, methWithInst);
				MarkAsIntermediateConversion(eXPR5);
				eXPRCALL.pConversions = eXPR5;
			}
		}
		else
		{
			eXPR = BindUDConversionCore(exprSrc, cType5, cType6, typeDst, methWithInst, out ppTransformedArgument);
		}
		pexprDst = ExprFactory.CreateUserDefinedConversion(ppTransformedArgument, eXPR, methWithInst);
		return true;
	}

	private EXPR HandleAmbiguity(EXPR exprSrc, CType typeSrc, CType typeDst, List<UdConvInfo> prguci, int iuciBestSrc, int iuciBestDst)
	{
		ErrorContext.Error(ErrorCode.ERR_AmbigUDConv, prguci[iuciBestSrc].mwt, prguci[iuciBestDst].mwt, typeSrc, typeDst);
		EXPRCLASS pType = ExprFactory.MakeClass(typeDst);
		EXPR eXPR = ExprFactory.CreateCast((EXPRFLAG)0, pType, exprSrc);
		eXPR.SetError();
		return eXPR;
	}

	private void MarkAsIntermediateConversion(EXPR pExpr)
	{
		if (pExpr.isCALL())
		{
			switch (pExpr.asCALL().nubLiftKind)
			{
			case NullableCallLiftKind.NotLifted:
				pExpr.asCALL().nubLiftKind = NullableCallLiftKind.NotLiftedIntermediateConversion;
				break;
			case NullableCallLiftKind.NullableConversion:
				pExpr.asCALL().nubLiftKind = NullableCallLiftKind.NullableIntermediateConversion;
				break;
			case NullableCallLiftKind.NullableConversionConstructor:
				MarkAsIntermediateConversion(pExpr.asCALL().GetOptionalArguments());
				break;
			}
		}
		else if (pExpr.isUSERDEFINEDCONVERSION())
		{
			MarkAsIntermediateConversion(pExpr.asUSERDEFINEDCONVERSION().UserDefinedCall);
		}
	}

	private EXPR BindUDConversionCore(EXPR pFrom, CType pTypeFrom, CType pTypeTo, CType pTypeDestination, MethWithInst mwiBest)
	{
		EXPR ppTransformedArgument;
		return BindUDConversionCore(pFrom, pTypeFrom, pTypeTo, pTypeDestination, mwiBest, out ppTransformedArgument);
	}

	private EXPR BindUDConversionCore(EXPR pFrom, CType pTypeFrom, CType pTypeTo, CType pTypeDestination, MethWithInst mwiBest, out EXPR ppTransformedArgument)
	{
		EXPRCLASS destExpr = ExprFactory.MakeClass(pTypeFrom);
		EXPR eXPR = mustCastCore(pFrom, destExpr, CONVERTTYPE.NOUDC);
		EXPRMEMGRP pMemberGroup = ExprFactory.CreateMemGroup(null, mwiBest);
		EXPRCALL expr = ExprFactory.CreateCall((EXPRFLAG)0, pTypeTo, eXPR, pMemberGroup, mwiBest);
		EXPRCLASS destExpr2 = ExprFactory.MakeClass(pTypeDestination);
		EXPR result = mustCastCore(expr, destExpr2, CONVERTTYPE.NOUDC);
		ppTransformedArgument = eXPR;
		return result;
	}

	private ConstCastResult bindConstantCast(EXPR exprSrc, EXPRTYPEORNAMESPACE exprTypeDest, bool needExprDest, out EXPR pexprDest, bool explicitConversion)
	{
		pexprDest = null;
		long num = 0L;
		double num2 = 0.0;
		CType cType = exprTypeDest.TypeOrNamespace.AsType();
		FUNDTYPE fUNDTYPE = exprSrc.type.fundType();
		FUNDTYPE fUNDTYPE2 = cType.fundType();
		bool flag = fUNDTYPE <= FUNDTYPE.FT_U8;
		bool flag2 = fUNDTYPE <= FUNDTYPE.FT_R8;
		EXPRCONSTANT eXPRCONSTANT = exprSrc.GetConst().asCONSTANT();
		if (fUNDTYPE == FUNDTYPE.FT_STRUCT || fUNDTYPE2 == FUNDTYPE.FT_STRUCT)
		{
			EXPR eXPR = bindDecimalConstCast(exprTypeDest, exprSrc.type, eXPRCONSTANT);
			if (eXPR == null)
			{
				if (explicitConversion)
				{
					return ConstCastResult.CheckFailure;
				}
				return ConstCastResult.Failure;
			}
			if (needExprDest)
			{
				pexprDest = eXPR;
			}
			return ConstCastResult.Success;
		}
		if (explicitConversion && Context.CheckedConstant && !isConstantInRange(eXPRCONSTANT, cType, realsOk: true))
		{
			return ConstCastResult.CheckFailure;
		}
		if (!needExprDest)
		{
			return ConstCastResult.Success;
		}
		if (flag)
		{
			if (eXPRCONSTANT.type.fundType() == FUNDTYPE.FT_U8)
			{
				if (fUNDTYPE2 == FUNDTYPE.FT_U8)
				{
					CONSTVAL constVal = GetExprConstants().Create(eXPRCONSTANT.getU64Value());
					pexprDest = ExprFactory.CreateConstant(cType, constVal);
					return ConstCastResult.Success;
				}
				num = (long)eXPRCONSTANT.getU64Value() & -1L;
			}
			else
			{
				num = eXPRCONSTANT.getI64Value();
			}
		}
		else
		{
			if (!flag2)
			{
				return ConstCastResult.Failure;
			}
			num2 = eXPRCONSTANT.getVal().doubleVal;
		}
		switch (fUNDTYPE2)
		{
		case FUNDTYPE.FT_I1:
			if (!flag)
			{
				num = (long)num2;
			}
			num = (sbyte)(num & 0xFF);
			break;
		case FUNDTYPE.FT_I2:
			if (!flag)
			{
				num = (long)num2;
			}
			num = (short)(num & 0xFFFF);
			break;
		case FUNDTYPE.FT_I4:
			if (!flag)
			{
				num = (long)num2;
			}
			num = (int)(num & 0xFFFFFFFFu);
			break;
		case FUNDTYPE.FT_I8:
			if (!flag)
			{
				num = (long)num2;
			}
			break;
		case FUNDTYPE.FT_U1:
			if (!flag)
			{
				num = (long)num2;
			}
			num &= 0xFF;
			break;
		case FUNDTYPE.FT_U2:
			if (!flag)
			{
				num = (long)num2;
			}
			num &= 0xFFFF;
			break;
		case FUNDTYPE.FT_U4:
			if (!flag)
			{
				num = (long)num2;
			}
			num &= 0xFFFFFFFFu;
			break;
		case FUNDTYPE.FT_U8:
			if (!flag)
			{
				num = (long)(ulong)num2;
				num = ((!(num2 < 9.223372036854776E+18)) ? ((long)(num2 - 9.223372036854776E+18) + I64(9223372036854775808uL)) : ((long)num2));
			}
			break;
		case FUNDTYPE.FT_R4:
		case FUNDTYPE.FT_R8:
			if (flag)
			{
				num2 = ((fUNDTYPE != FUNDTYPE.FT_U8) ? ((double)num) : ((double)(ulong)num));
			}
			if (fUNDTYPE2 == FUNDTYPE.FT_R4)
			{
				RoundToFloat(num2, out var f);
				num2 = f;
			}
			break;
		}
		CONSTVAL cONSTVAL = new CONSTVAL();
		if (fUNDTYPE2 == FUNDTYPE.FT_U4)
		{
			cONSTVAL.uiVal = (uint)num;
		}
		else if (fUNDTYPE2 > FUNDTYPE.FT_U4)
		{
			cONSTVAL = ((fUNDTYPE2 > FUNDTYPE.FT_U8) ? GetExprConstants().Create(num2) : GetExprConstants().Create(num));
		}
		else
		{
			cONSTVAL.iVal = (int)num;
		}
		EXPRCONSTANT eXPRCONSTANT2 = ExprFactory.CreateConstant(cType, cONSTVAL);
		pexprDest = eXPRCONSTANT2;
		return ConstCastResult.Success;
	}

	private int CompareSrcTypesBased(CType type1, bool fImplicit1, CType type2, bool fImplicit2)
	{
		if (fImplicit1 != fImplicit2)
		{
			if (!fImplicit1)
			{
				return 1;
			}
			return -1;
		}
		bool flag = canConvert(type1, type2, CONVERTTYPE.NOUDC);
		bool flag2 = canConvert(type2, type1, CONVERTTYPE.NOUDC);
		if (flag == flag2)
		{
			return 0;
		}
		if (fImplicit1 != flag)
		{
			return 1;
		}
		return -1;
	}

	private int CompareDstTypesBased(CType type1, bool fImplicit1, CType type2, bool fImplicit2)
	{
		if (fImplicit1 != fImplicit2)
		{
			if (!fImplicit1)
			{
				return 1;
			}
			return -1;
		}
		bool flag = canConvert(type1, type2, CONVERTTYPE.NOUDC);
		bool flag2 = canConvert(type2, type1, CONVERTTYPE.NOUDC);
		if (flag == flag2)
		{
			return 0;
		}
		if (fImplicit1 != flag)
		{
			return -1;
		}
		return 1;
	}

	private EXPR bindDecimalConstCast(EXPRTYPEORNAMESPACE exprDestType, CType srcType, EXPRCONSTANT src)
	{
		CType cType = exprDestType.TypeOrNamespace.AsType();
		CType optPredefType = SymbolLoader.GetOptPredefType(PredefinedType.PT_DECIMAL);
		CONSTVAL cONSTVAL = new CONSTVAL();
		if (optPredefType == null)
		{
			return null;
		}
		if (cType == optPredefType)
		{
			decimal value;
			switch (srcType.fundType())
			{
			case FUNDTYPE.FT_I1:
			case FUNDTYPE.FT_I2:
			case FUNDTYPE.FT_I4:
				value = Convert.ToDecimal(src.getVal().iVal);
				break;
			case FUNDTYPE.FT_U1:
			case FUNDTYPE.FT_U2:
			case FUNDTYPE.FT_U4:
				value = Convert.ToDecimal(src.getVal().uiVal);
				break;
			case FUNDTYPE.FT_R4:
				value = Convert.ToDecimal((float)src.getVal().doubleVal);
				break;
			case FUNDTYPE.FT_R8:
				value = Convert.ToDecimal(src.getVal().doubleVal);
				break;
			case FUNDTYPE.FT_U8:
				value = Convert.ToDecimal((ulong)src.getVal().longVal);
				break;
			case FUNDTYPE.FT_I8:
				value = Convert.ToDecimal(src.getVal().longVal);
				break;
			default:
				return null;
			}
			cONSTVAL = GetExprConstants().Create(value);
			return ExprFactory.CreateConstant(optPredefType, cONSTVAL);
		}
		if (srcType == optPredefType)
		{
			decimal value2 = default(decimal);
			FUNDTYPE fUNDTYPE = cType.fundType();
			try
			{
				if (fUNDTYPE != FUNDTYPE.FT_R4 && fUNDTYPE != FUNDTYPE.FT_R8)
				{
					value2 = decimal.Truncate(src.getVal().decVal);
				}
				switch (fUNDTYPE)
				{
				case FUNDTYPE.FT_I1:
					cONSTVAL.iVal = Convert.ToSByte(value2);
					break;
				case FUNDTYPE.FT_U1:
					cONSTVAL.uiVal = Convert.ToByte(value2);
					break;
				case FUNDTYPE.FT_I2:
					cONSTVAL.iVal = Convert.ToInt16(value2);
					break;
				case FUNDTYPE.FT_U2:
					cONSTVAL.uiVal = Convert.ToUInt16(value2);
					break;
				case FUNDTYPE.FT_I4:
					cONSTVAL.iVal = Convert.ToInt32(value2);
					break;
				case FUNDTYPE.FT_U4:
					cONSTVAL.uiVal = Convert.ToUInt32(value2);
					break;
				case FUNDTYPE.FT_I8:
					cONSTVAL = GetExprConstants().Create(Convert.ToInt64(value2));
					break;
				case FUNDTYPE.FT_U8:
					cONSTVAL = GetExprConstants().Create(Convert.ToUInt64(value2));
					break;
				case FUNDTYPE.FT_R4:
					cONSTVAL = GetExprConstants().Create(Convert.ToSingle(src.getVal().decVal));
					break;
				case FUNDTYPE.FT_R8:
					cONSTVAL = GetExprConstants().Create(Convert.ToDouble(src.getVal().decVal));
					break;
				default:
					return null;
				}
			}
			catch (OverflowException)
			{
				return null;
			}
			return ExprFactory.CreateConstant(cType, cONSTVAL);
		}
		return null;
	}

	private bool canExplicitConversionBeBoundInUncheckedContext(EXPR exprSrc, CType typeSrc, EXPRTYPEORNAMESPACE typeDest, CONVERTTYPE flags)
	{
		CheckedContext context = CheckedContext.CreateInstance(Context, checkedNormal: false, checkedConstant: false);
		return new ExpressionBinder(context).BindExplicitConversion(exprSrc, typeSrc, typeDest, typeDest.TypeOrNamespace.AsType(), flags);
	}

	public BindingContext GetContext()
	{
		return Context;
	}

	private static void VSFAIL(string s)
	{
	}

	public ExpressionBinder(BindingContext context)
	{
		Context = context;
		m_nullable = new CNullable(GetSymbolLoader(), GetErrorContext(), GetExprFactory());
		g_binopSignatures = new BinOpSig[20]
		{
			new BinOpSig(PredefinedType.PT_INT, PredefinedType.PT_INT, BinOpMask.Integer, 8, BindIntBinOp, OpSigFlags.Value, BinOpFuncKind.IntBinOp),
			new BinOpSig(PredefinedType.PT_UINT, PredefinedType.PT_UINT, BinOpMask.Integer, 7, BindIntBinOp, OpSigFlags.Value, BinOpFuncKind.IntBinOp),
			new BinOpSig(PredefinedType.PT_LONG, PredefinedType.PT_LONG, BinOpMask.Integer, 6, BindIntBinOp, OpSigFlags.Value, BinOpFuncKind.IntBinOp),
			new BinOpSig(PredefinedType.PT_ULONG, PredefinedType.PT_ULONG, BinOpMask.Integer, 5, BindIntBinOp, OpSigFlags.Value, BinOpFuncKind.IntBinOp),
			new BinOpSig(PredefinedType.PT_ULONG, PredefinedType.PT_LONG, BinOpMask.Integer, 4, null, OpSigFlags.Value, BinOpFuncKind.None),
			new BinOpSig(PredefinedType.PT_LONG, PredefinedType.PT_ULONG, BinOpMask.Integer, 3, null, OpSigFlags.Value, BinOpFuncKind.None),
			new BinOpSig(PredefinedType.PT_FLOAT, PredefinedType.PT_FLOAT, BinOpMask.Real, 1, BindRealBinOp, OpSigFlags.Value, BinOpFuncKind.RealBinOp),
			new BinOpSig(PredefinedType.PT_DOUBLE, PredefinedType.PT_DOUBLE, BinOpMask.Real, 0, BindRealBinOp, OpSigFlags.Value, BinOpFuncKind.RealBinOp),
			new BinOpSig(PredefinedType.PT_DECIMAL, PredefinedType.PT_DECIMAL, BinOpMask.Real, 0, BindDecBinOp, OpSigFlags.Value, BinOpFuncKind.DecBinOp),
			new BinOpSig(PredefinedType.PT_STRING, PredefinedType.PT_STRING, BinOpMask.Equal, 0, BindStrCmpOp, OpSigFlags.Convert, BinOpFuncKind.StrCmpOp),
			new BinOpSig(PredefinedType.PT_STRING, PredefinedType.PT_STRING, BinOpMask.Add, 2, BindStrBinOp, OpSigFlags.Convert, BinOpFuncKind.StrBinOp),
			new BinOpSig(PredefinedType.PT_STRING, PredefinedType.PT_OBJECT, BinOpMask.Add, 1, BindStrBinOp, OpSigFlags.Convert, BinOpFuncKind.StrBinOp),
			new BinOpSig(PredefinedType.PT_OBJECT, PredefinedType.PT_STRING, BinOpMask.Add, 0, BindStrBinOp, OpSigFlags.Convert, BinOpFuncKind.StrBinOp),
			new BinOpSig(PredefinedType.PT_INT, PredefinedType.PT_INT, BinOpMask.Shift, 3, BindShiftOp, OpSigFlags.Value, BinOpFuncKind.ShiftOp),
			new BinOpSig(PredefinedType.PT_UINT, PredefinedType.PT_INT, BinOpMask.Shift, 2, BindShiftOp, OpSigFlags.Value, BinOpFuncKind.ShiftOp),
			new BinOpSig(PredefinedType.PT_LONG, PredefinedType.PT_INT, BinOpMask.Shift, 1, BindShiftOp, OpSigFlags.Value, BinOpFuncKind.ShiftOp),
			new BinOpSig(PredefinedType.PT_ULONG, PredefinedType.PT_INT, BinOpMask.Shift, 0, BindShiftOp, OpSigFlags.Value, BinOpFuncKind.ShiftOp),
			new BinOpSig(PredefinedType.PT_BOOL, PredefinedType.PT_BOOL, BinOpMask.BoolNorm, 0, BindBoolBinOp, OpSigFlags.Value, BinOpFuncKind.BoolBinOp),
			new BinOpSig(PredefinedType.PT_BOOL, PredefinedType.PT_BOOL, BinOpMask.Logical, 0, BindBoolBinOp, OpSigFlags.BoolBit, BinOpFuncKind.BoolBinOp),
			new BinOpSig(PredefinedType.PT_BOOL, PredefinedType.PT_BOOL, BinOpMask.Bitwise, 0, BindLiftedBoolBitwiseOp, OpSigFlags.BoolBit, BinOpFuncKind.BoolBitwiseOp)
		};
		g_rguos = new UnaOpSig[16]
		{
			new UnaOpSig(PredefinedType.PT_INT, UnaOpMask.Signed, 7, BindIntUnaOp, UnaOpFuncKind.IntUnaOp),
			new UnaOpSig(PredefinedType.PT_UINT, UnaOpMask.Unsigned, 6, BindIntUnaOp, UnaOpFuncKind.IntUnaOp),
			new UnaOpSig(PredefinedType.PT_LONG, UnaOpMask.Signed, 5, BindIntUnaOp, UnaOpFuncKind.IntUnaOp),
			new UnaOpSig(PredefinedType.PT_ULONG, UnaOpMask.Unsigned, 4, BindIntUnaOp, UnaOpFuncKind.IntUnaOp),
			new UnaOpSig(PredefinedType.PT_ULONG, UnaOpMask.Minus, 3, null, UnaOpFuncKind.None),
			new UnaOpSig(PredefinedType.PT_FLOAT, UnaOpMask.Real, 1, BindRealUnaOp, UnaOpFuncKind.RealUnaOp),
			new UnaOpSig(PredefinedType.PT_DOUBLE, UnaOpMask.Real, 0, BindRealUnaOp, UnaOpFuncKind.RealUnaOp),
			new UnaOpSig(PredefinedType.PT_DECIMAL, UnaOpMask.Real, 0, BindDecUnaOp, UnaOpFuncKind.DecUnaOp),
			new UnaOpSig(PredefinedType.PT_BOOL, UnaOpMask.Bang, 0, BindBoolUnaOp, UnaOpFuncKind.BoolUnaOp),
			new UnaOpSig(PredefinedType.PT_INT, UnaOpMask.IncDec, 6, null, UnaOpFuncKind.None),
			new UnaOpSig(PredefinedType.PT_UINT, UnaOpMask.IncDec, 5, null, UnaOpFuncKind.None),
			new UnaOpSig(PredefinedType.PT_LONG, UnaOpMask.IncDec, 4, null, UnaOpFuncKind.None),
			new UnaOpSig(PredefinedType.PT_ULONG, UnaOpMask.IncDec, 3, null, UnaOpFuncKind.None),
			new UnaOpSig(PredefinedType.PT_FLOAT, UnaOpMask.IncDec, 1, null, UnaOpFuncKind.None),
			new UnaOpSig(PredefinedType.PT_DOUBLE, UnaOpMask.IncDec, 0, null, UnaOpFuncKind.None),
			new UnaOpSig(PredefinedType.PT_DECIMAL, UnaOpMask.IncDec, 0, null, UnaOpFuncKind.None)
		};
	}

	protected SymbolLoader GetSymbolLoader()
	{
		return SymbolLoader;
	}

	public CSemanticChecker GetSemanticChecker()
	{
		return SemanticChecker;
	}

	private ErrorHandling GetErrorContext()
	{
		return ErrorContext;
	}

	protected BSYMMGR GetGlobalSymbols()
	{
		return GetSymbolLoader().getBSymmgr();
	}

	protected TypeManager GetTypes()
	{
		return TypeManager;
	}

	private ExprFactory GetExprFactory()
	{
		return ExprFactory;
	}

	private ConstValFactory GetExprConstants()
	{
		return GetExprFactory().GetExprConstants();
	}

	protected AggregateType GetReqPDT(PredefinedType pt)
	{
		return GetReqPDT(pt, GetSymbolLoader());
	}

	protected static AggregateType GetReqPDT(PredefinedType pt, SymbolLoader symbolLoader)
	{
		return symbolLoader.GetReqPredefType(pt, fEnsureState: true);
	}

	protected AggregateType GetOptPDT(PredefinedType pt)
	{
		return GetOptPDT(pt, WarnIfNotFound: true);
	}

	protected AggregateType GetOptPDT(PredefinedType pt, bool WarnIfNotFound)
	{
		if (WarnIfNotFound)
		{
			return GetSymbolLoader().GetOptPredefTypeErr(pt, fEnsureState: true);
		}
		return GetSymbolLoader().GetOptPredefType(pt, fEnsureState: true);
	}

	protected CType getVoidType()
	{
		return VoidType;
	}

	public EXPR GenerateAssignmentConversion(EXPR op1, EXPR op2, bool allowExplicit)
	{
		if (allowExplicit)
		{
			return mustCastCore(op2, GetExprFactory().MakeClass(op1.type), (CONVERTTYPE)0);
		}
		return mustConvertCore(op2, GetExprFactory().MakeClass(op1.type));
	}

	public EXPR bindAssignment(EXPR op1, EXPR op2, bool allowExplicit)
	{
		bool flag = false;
		bool flag2 = false;
		if (!op1.isANYLOCAL_OK())
		{
			if (!checkLvalue(op1, CheckLvalueKind.Assignment))
			{
				EXPR eXPR = GetExprFactory().CreateAssignment(op1, op2);
				eXPR.SetError();
				return eXPR;
			}
		}
		else
		{
			if (op2.type.IsArrayType())
			{
				return BindPtrToArray(op1.asANYLOCAL(), op2);
			}
			if (op2.type == GetReqPDT(PredefinedType.PT_STRING))
			{
				op2 = bindPtrToString(op2);
			}
			else if (op2.kind == ExpressionKind.EK_ADDR)
			{
				op2.flags |= EXPRFLAG.EXF_ASFINALLYLEAVE;
			}
			else if (op2.isOK())
			{
				flag = true;
				flag2 = op2.isCAST();
			}
		}
		op2 = GenerateAssignmentConversion(op1, op2, allowExplicit);
		if (op2.isOK() && flag)
		{
			if (flag2)
			{
				ErrorContext.Error(ErrorCode.ERR_BadCastInFixed);
			}
			else
			{
				ErrorContext.Error(ErrorCode.ERR_FixedNotNeeded);
			}
		}
		return GenerateOptimizedAssignment(op1, op2);
	}

	internal EXPR BindArrayIndexCore(BindingFlag bindFlags, EXPR pOp1, EXPR pOp2)
	{
		bool flag = false;
		if (!pOp1.isOK() || !pOp2.isOK())
		{
			flag = true;
		}
		CType pIntType = GetReqPDT(PredefinedType.PT_INT);
		EXPR eXPR;
		if (!pOp1.type.IsArrayType())
		{
			eXPR = bindIndexer(pOp1, pOp2, bindFlags);
			if (flag)
			{
				eXPR.SetError();
			}
			return eXPR;
		}
		ArrayType arrayType = pOp1.type.AsArrayType();
		checkUnsafe(arrayType.GetElementType());
		CType pDestType = chooseArrayIndexType(pOp2);
		if (pDestType == null)
		{
			pDestType = pIntType;
		}
		int rank = arrayType.rank;
		int cIndices = 0;
		EXPR pIndex = pOp2.Map(GetExprFactory(), delegate(EXPR x)
		{
			cIndices++;
			EXPR eXPR2 = mustConvert(x, pDestType);
			if (pDestType == pIntType)
			{
				return eXPR2;
			}
			EXPRFLAG nFlags = EXPRFLAG.EXF_LITERALCONST;
			EXPRCLASS pType = GetExprFactory().MakeClass(pDestType);
			return GetExprFactory().CreateCast(nFlags, pType, eXPR2);
		});
		if (cIndices != rank)
		{
			ErrorContext.Error(ErrorCode.ERR_BadIndexCount, rank);
			eXPR = GetExprFactory().CreateArrayIndex(pOp1, pIndex);
			eXPR.SetError();
			return eXPR;
		}
		eXPR = GetExprFactory().CreateArrayIndex(pOp1, pIndex);
		eXPR.flags |= (EXPRFLAG)6291456;
		if (flag)
		{
			eXPR.SetError();
		}
		return eXPR;
	}

	protected EXPRUNARYOP bindPtrToString(EXPR @string)
	{
		CType pointer = GetTypes().GetPointer(GetReqPDT(PredefinedType.PT_CHAR));
		return GetExprFactory().CreateUnaryOp(ExpressionKind.EK_ADDR, pointer, @string);
	}

	protected EXPRQUESTIONMARK BindPtrToArray(EXPRLOCAL exprLoc, EXPR array)
	{
		CType elementType = array.type.AsArrayType().GetElementType();
		CType pointer = GetTypes().GetPointer(elementType);
		if (GetSymbolLoader().isManagedType(elementType))
		{
			ErrorContext.Error(ErrorCode.ERR_ManagedAddr, elementType);
		}
		SetExternalRef(elementType);
		EXPR eXPR = null;
		EXPRWRAP eXPRWRAP = WrapShortLivedExpression(array).asWRAP();
		EXPR p = GetExprFactory().CreateSave(eXPRWRAP);
		EXPR p2 = GetExprFactory().CreateBinop(ExpressionKind.EK_NE, GetReqPDT(PredefinedType.PT_BOOL), p, GetExprFactory().CreateConstant(eXPRWRAP.type, ConstValFactory.GetInt(0)));
		EXPR p4;
		if (array.type.AsArrayType().rank == 1)
		{
			EXPR p3 = GetExprFactory().CreateArrayLength(eXPRWRAP);
			p4 = GetExprFactory().CreateBinop(ExpressionKind.EK_NE, GetReqPDT(PredefinedType.PT_BOOL), p3, GetExprFactory().CreateConstant(GetReqPDT(PredefinedType.PT_INT), ConstValFactory.GetInt(0)));
		}
		else
		{
			EXPRCALL p5 = BindPredefMethToArgs(PREDEFMETH.PM_ARRAY_GETLENGTH, eXPRWRAP, null, null, null);
			p4 = GetExprFactory().CreateBinop(ExpressionKind.EK_NE, GetReqPDT(PredefinedType.PT_BOOL), p5, GetExprFactory().CreateConstant(GetReqPDT(PredefinedType.PT_INT), ConstValFactory.GetInt(0)));
		}
		eXPR = GetExprFactory().CreateBinop(ExpressionKind.EK_LOGAND, GetReqPDT(PredefinedType.PT_BOOL), p2, p4);
		EXPR eXPR2 = null;
		EXPR first = eXPR2;
		EXPR last = null;
		for (int i = 0; i < array.type.AsArrayType().rank; i++)
		{
			GetExprFactory().AppendItemToList(GetExprFactory().CreateConstant(GetReqPDT(PredefinedType.PT_INT), ConstValFactory.GetInt(0)), ref first, ref last);
		}
		EXPR eXPR3 = GetExprFactory().CreateUnaryOp(ExpressionKind.EK_ADDR, pointer, GetExprFactory().CreateArrayIndex(eXPRWRAP, eXPR2));
		eXPR3.flags |= EXPRFLAG.EXF_ASFINALLYLEAVE;
		eXPR3 = mustConvert(eXPR3, exprLoc.type, CONVERTTYPE.NOUDC);
		eXPR3 = GetExprFactory().CreateAssignment(exprLoc, eXPR3);
		eXPR3.flags |= EXPRFLAG.EXF_ASSGOP;
		eXPR3 = GetExprFactory().CreateBinop(ExpressionKind.EK_SEQREV, exprLoc.type, eXPR3, WrapShortLivedExpression(eXPRWRAP));
		EXPR pRHS = GetExprFactory().CreateZeroInit(exprLoc.type);
		pRHS = GetExprFactory().CreateAssignment(exprLoc, pRHS);
		pRHS.flags |= EXPRFLAG.EXF_ASSGOP;
		EXPRBINOP pConsequence = GetExprFactory().CreateBinop(ExpressionKind.EK_BINOP, eXPR3.type, eXPR3, pRHS);
		return GetExprFactory().CreateQuestionMark(eXPR, pConsequence);
	}

	protected EXPR bindIndexer(EXPR pObject, EXPR args, BindingFlag bindFlags)
	{
		CType type = pObject.type;
		if (!type.IsAggregateType() && !type.IsTypeParameterType())
		{
			ErrorContext.Error(ErrorCode.ERR_BadIndexLHS, type);
			MethWithInst mwi = new MethWithInst(null, null);
			EXPRMEMGRP pMemberGroup = GetExprFactory().CreateMemGroup(pObject, mwi);
			EXPRCALL eXPRCALL = GetExprFactory().CreateCall((EXPRFLAG)0, type, args, pMemberGroup, null);
			eXPRCALL.SetError();
			return eXPRCALL;
		}
		Name predefName = GetSymbolLoader().GetNameManager().GetPredefName(PredefinedName.PN_INDEXERINTERNAL);
		MemberLookup memberLookup = new MemberLookup();
		if (!memberLookup.Lookup(GetSemanticChecker(), type, pObject, ContextForMemberLookup(), predefName, 0, ((bindFlags & BindingFlag.BIND_BASECALL) != 0) ? ((MemLookFlags)68u) : MemLookFlags.Indexer))
		{
			memberLookup.ReportErrors();
			type = GetTypes().GetErrorSym();
			Symbol symbol = null;
			if (memberLookup.SwtInaccessible().Sym != null)
			{
				type = memberLookup.SwtInaccessible().MethProp().RetType;
				symbol = memberLookup.SwtInaccessible().Sym;
			}
			EXPRMEMGRP eXPRMEMGRP = null;
			if (symbol != null)
			{
				eXPRMEMGRP = GetExprFactory().CreateMemGroup((EXPRFLAG)memberLookup.GetFlags(), predefName, BSYMMGR.EmptyTypeArray(), symbol.getKind(), memberLookup.GetSourceType(), null, memberLookup.GetObject(), memberLookup.GetResults());
				eXPRMEMGRP.SetInaccessibleBit();
			}
			else
			{
				MethWithInst mwi2 = new MethWithInst(null, null);
				eXPRMEMGRP = GetExprFactory().CreateMemGroup(memberLookup.GetObject(), mwi2);
			}
			EXPRCALL eXPRCALL2 = GetExprFactory().CreateCall((EXPRFLAG)0, type, args, eXPRMEMGRP, null);
			eXPRCALL2.SetError();
			return eXPRCALL2;
		}
		EXPRMEMGRP grp = GetExprFactory().CreateMemGroup((EXPRFLAG)memberLookup.GetFlags(), predefName, BSYMMGR.EmptyTypeArray(), memberLookup.SymFirst().getKind(), memberLookup.GetSourceType(), null, memberLookup.GetObject(), memberLookup.GetResults());
		EXPR eXPR = BindMethodGroupToArguments(bindFlags, grp, args);
		if (eXPR.getObject() == null)
		{
			eXPR.SetObject(pObject);
			eXPR.SetError();
		}
		return eXPR;
	}

	public void bindSimpleCast(EXPR exprSrc, EXPRTYPEORNAMESPACE typeDest, out EXPR pexprDest)
	{
		bindSimpleCast(exprSrc, typeDest, out pexprDest, (EXPRFLAG)0);
	}

	public void bindSimpleCast(EXPR exprSrc, EXPRTYPEORNAMESPACE exprTypeDest, out EXPR pexprDest, EXPRFLAG exprFlags)
	{
		CType cType = exprTypeDest.TypeOrNamespace.AsType();
		pexprDest = null;
		EXPR @const = exprSrc.GetConst();
		EXPRCAST eXPRCAST = GetExprFactory().CreateCast(exprFlags, exprTypeDest, exprSrc);
		if (Context.CheckedNormal)
		{
			eXPRCAST.flags |= EXPRFLAG.EXF_CHECKOVERFLOW;
		}
		if (@const != null && exprFlags == (EXPRFLAG)0 && exprSrc.type.fundType() == cType.fundType() && (!exprSrc.type.isPredefType(PredefinedType.PT_STRING) || @const.asCONSTANT().getVal().IsNullRef()))
		{
			EXPRCONSTANT eXPRCONSTANT = GetExprFactory().CreateConstant(cType, @const.asCONSTANT().getVal());
			pexprDest = eXPRCONSTANT;
		}
		else
		{
			pexprDest = eXPRCAST;
		}
	}

	internal EXPRCALL BindToMethod(MethWithInst mwi, EXPR pArguments, EXPRMEMGRP pMemGroup, MemLookFlags flags)
	{
		EXPR optionalObject = pMemGroup.GetOptionalObject();
		CType callingObjectType = optionalObject?.type;
		PostBindMethod((flags & MemLookFlags.BaseCall) != 0, ref mwi, optionalObject);
		optionalObject = AdjustMemberObject(mwi, optionalObject, out var pfConstrained, out var pIsMatchingStatic);
		pMemGroup.SetOptionalObject(optionalObject);
		CType cType = null;
		cType = (((flags & (MemLookFlags)18u) != (MemLookFlags)18u) ? GetTypes().SubstType(mwi.Meth().RetType, mwi.GetType(), mwi.TypeArgs) : mwi.Ats);
		EXPRCALL eXPRCALL = GetExprFactory().CreateCall((EXPRFLAG)0, cType, pArguments, pMemGroup, mwi);
		if (!pIsMatchingStatic)
		{
			eXPRCALL.SetMismatchedStaticBit();
		}
		if (!eXPRCALL.isOK())
		{
			return eXPRCALL;
		}
		if ((flags & MemLookFlags.Ctor) != 0 && (flags & MemLookFlags.NewObj) != 0)
		{
			eXPRCALL.flags |= (EXPRFLAG)131088;
		}
		if ((flags & MemLookFlags.BaseCall) != 0)
		{
			eXPRCALL.flags |= EXPRFLAG.EXF_ASFINALLYLEAVE;
		}
		else if (pfConstrained && optionalObject != null)
		{
			eXPRCALL.flags |= EXPRFLAG.EXF_UNREALIZEDGOTO;
		}
		verifyMethodArgs(eXPRCALL, callingObjectType);
		return eXPRCALL;
	}

	internal EXPR BindToField(EXPR pObject, FieldWithType fwt, BindingFlag bindFlags)
	{
		return BindToField(pObject, fwt, bindFlags, null);
	}

	internal EXPR BindToField(EXPR pOptionalObject, FieldWithType fwt, BindingFlag bindFlags, EXPR pOptionalLHS)
	{
		CType cType = GetTypes().SubstType(fwt.Field().GetType(), fwt.GetType());
		if (pOptionalObject != null && !pOptionalObject.isOK())
		{
			EXPRFIELD eXPRFIELD = GetExprFactory().CreateField((EXPRFLAG)0, cType, pOptionalObject, 0u, fwt, pOptionalLHS);
			eXPRFIELD.SetError();
			return eXPRFIELD;
		}
		EXPR eXPR = pOptionalObject;
		pOptionalObject = AdjustMemberObject(fwt, pOptionalObject, out var _, out var pIsMatchingStatic);
		checkUnsafe(cType);
		bool flag = false;
		if ((pOptionalObject != null && pOptionalObject.type.IsPointerType()) || objectIsLvalue(pOptionalObject))
		{
			flag = true;
		}
		if (RespectReadonly() && fwt.Field().isReadOnly && (ContainingAgg() == null || !InMethod() || !InConstructor() || fwt.Field().getClass() != ContainingAgg() || InStaticMethod() != fwt.Field().isStatic || (pOptionalObject != null && !isThisPointer(pOptionalObject)) || InAnonymousMethod()))
		{
			flag = false;
		}
		EXPRFIELD eXPRFIELD2 = GetExprFactory().CreateField(flag ? EXPRFLAG.EXF_LVALUE : ((EXPRFLAG)0), cType, pOptionalObject, 0u, fwt, pOptionalLHS);
		if (!pIsMatchingStatic)
		{
			eXPRFIELD2.SetMismatchedStaticBit();
		}
		if (cType.IsErrorType())
		{
			eXPRFIELD2.SetError();
		}
		eXPRFIELD2.flags |= (EXPRFLAG)(bindFlags & BindingFlag.BIND_MEMBERSET);
		if (eXPRFIELD2.isFIELD() && fwt.Field().isEvent && fwt.Field().getEvent(GetSymbolLoader()) != null && fwt.Field().getEvent(GetSymbolLoader()).IsWindowsRuntimeEvent)
		{
			CType type = fwt.Field().GetType();
			if (type.IsAggregateType())
			{
				eXPRFIELD2.setType(GetTypes().GetParameterModifier(eXPRFIELD2.type, isOut: false));
				Name predefName = GetSymbolLoader().GetNameManager().GetPredefName(PredefinedName.PN_GETORCREATEEVENTREGISTRATIONTOKENTABLE);
				GetSymbolLoader().RuntimeBinderSymbolTable.PopulateSymbolTableWithName(predefName.Text, null, type.AssociatedSystemType);
				MethodSymbol mps = GetSymbolLoader().LookupAggMember(predefName, type.getAggregate(), symbmask_t.MASK_MethodSymbol).AsMethodSymbol();
				MethPropWithInst methPropWithInst = new MethPropWithInst(mps, type.AsAggregateType());
				EXPRMEMGRP pMemGroup = GetExprFactory().CreateMemGroup(null, methPropWithInst);
				EXPR pObject = BindToMethod(new MethWithInst(methPropWithInst), eXPRFIELD2, pMemGroup, MemLookFlags.None);
				AggregateSymbol owningAggregate = type.AsAggregateType().GetOwningAggregate();
				Name predefName2 = GetSymbolLoader().GetNameManager().GetPredefName(PredefinedName.PN_INVOCATIONLIST);
				GetSymbolLoader().RuntimeBinderSymbolTable.PopulateSymbolTableWithName(predefName2.Text, null, type.AssociatedSystemType);
				PropertySymbol propertySymbol = GetSymbolLoader().LookupAggMember(predefName2, owningAggregate, symbmask_t.MASK_PropertySymbol).AsPropertySymbol();
				MethPropWithInst mwi = new MethPropWithInst(propertySymbol, type.AsAggregateType());
				EXPRMEMGRP pMemGroup2 = GetExprFactory().CreateMemGroup(pObject, mwi);
				PropWithType pwt = new PropWithType(propertySymbol, type.AsAggregateType());
				return BindToProperty(pObject, pwt, bindFlags, null, null, pMemGroup2);
			}
		}
		return eXPRFIELD2;
	}

	internal EXPR BindToProperty(EXPR pObject, PropWithType pwt, BindingFlag bindFlags, EXPR args, AggregateType pOtherType, EXPRMEMGRP pMemGroup)
	{
		EXPR eXPR = null;
		if ((bindFlags & BindingFlag.BIND_BASECALL) == 0)
		{
			eXPR = pObject;
		}
		PostBindProperty((bindFlags & BindingFlag.BIND_BASECALL) != 0, pwt, pObject, out var pmwtGet, out var pmwtSet);
		pObject = (((bool)pmwtGet && (!pmwtSet || pmwtSet.GetType() == pmwtGet.GetType() || GetSymbolLoader().HasBaseConversion(pmwtGet.GetType(), pmwtSet.GetType()))) ? AdjustMemberObject(pmwtGet, pObject, out var pfConstrained, out var pIsMatchingStatic) : ((!pmwtSet) ? AdjustMemberObject(pwt, pObject, out pfConstrained, out pIsMatchingStatic) : AdjustMemberObject(pmwtSet, pObject, out pfConstrained, out pIsMatchingStatic)));
		pMemGroup.SetOptionalObject(pObject);
		CType pType = GetTypes().SubstType(pwt.Prop().RetType, pwt.GetType());
		if (pObject != null && !pObject.isOK())
		{
			EXPRPROP eXPRPROP = GetExprFactory().CreateProperty(pType, eXPR, args, pMemGroup, pwt, null, null);
			if (!pIsMatchingStatic)
			{
				eXPRPROP.SetMismatchedStaticBit();
			}
			eXPRPROP.SetError();
			return eXPRPROP;
		}
		if ((bindFlags & BindingFlag.BIND_RVALUEREQUIRED) != 0)
		{
			if (!pmwtGet)
			{
				if (pOtherType != null)
				{
					return GetExprFactory().MakeClass(pOtherType);
				}
				ErrorContext.ErrorRef(ErrorCode.ERR_PropertyLacksGet, pwt);
			}
			else if ((bindFlags & BindingFlag.BIND_BASECALL) != 0 && pmwtGet.Meth().isAbstract)
			{
				if (pOtherType != null)
				{
					return GetExprFactory().MakeClass(pOtherType);
				}
				ErrorContext.Error(ErrorCode.ERR_AbstractBaseCall, pwt);
			}
			else
			{
				CType cType = null;
				if (eXPR != null)
				{
					cType = eXPR.type;
				}
				ACCESSERROR aCCESSERROR = SemanticChecker.CheckAccess2(pmwtGet.Meth(), pmwtGet.GetType(), ContextForMemberLookup(), cType);
				if (aCCESSERROR != ACCESSERROR.ACCESSERROR_NOERROR)
				{
					if (pOtherType != null)
					{
						return GetExprFactory().MakeClass(pOtherType);
					}
					if (aCCESSERROR == ACCESSERROR.ACCESSERROR_NOACCESSTHRU)
					{
						ErrorContext.Error(ErrorCode.ERR_BadProtectedAccess, pwt, cType, ContextForMemberLookup());
					}
					else
					{
						ErrorContext.ErrorRef(ErrorCode.ERR_InaccessibleGetter, pwt);
					}
				}
			}
		}
		EXPRPROP eXPRPROP2 = GetExprFactory().CreateProperty(pType, eXPR, args, pMemGroup, pwt, pmwtGet, pmwtSet);
		if (!pIsMatchingStatic)
		{
			eXPRPROP2.SetMismatchedStaticBit();
		}
		if ((BindingFlag.BIND_BASECALL & bindFlags) != 0)
		{
			eXPRPROP2.flags |= EXPRFLAG.EXF_ASFINALLYLEAVE;
		}
		else if (pfConstrained && pObject != null)
		{
			eXPRPROP2.flags |= EXPRFLAG.EXF_UNREALIZEDGOTO;
		}
		if (eXPRPROP2.GetOptionalArguments() != null)
		{
			verifyMethodArgs(eXPRPROP2, eXPR?.type);
		}
		if ((bool)pmwtSet && objectIsLvalue(eXPRPROP2.GetMemberGroup().GetOptionalObject()))
		{
			eXPRPROP2.flags |= EXPRFLAG.EXF_LVALUE;
		}
		if (pOtherType != null)
		{
			eXPRPROP2.flags |= EXPRFLAG.EXF_SAMENAMETYPE;
		}
		return eXPRPROP2;
	}

	internal EXPR bindUDUnop(ExpressionKind ek, EXPR arg)
	{
		Name name = ekName(ek);
		CType cType = arg.type;
		while (true)
		{
			switch (cType.GetTypeKind())
			{
			case TypeKind.TK_NullableType:
				cType = cType.StripNubs();
				break;
			case TypeKind.TK_TypeParameterType:
				cType = cType.AsTypeParameterType().GetEffectiveBaseClass();
				break;
			case TypeKind.TK_AggregateType:
			{
				if ((!cType.isClassType() && !cType.isStructType()) || cType.AsAggregateType().getAggregate().IsSkipUDOps())
				{
					return null;
				}
				ArgInfos argInfos = new ArgInfos();
				argInfos.carg = 1;
				FillInArgInfoFromArgList(argInfos, arg);
				List<CandidateFunctionMember> list = new List<CandidateFunctionMember>();
				MethodSymbol methodSymbol = null;
				AggregateType aggregateType = cType.AsAggregateType();
				while (true)
				{
					methodSymbol = ((methodSymbol == null) ? GetSymbolLoader().LookupAggMember(name, aggregateType.getAggregate(), symbmask_t.MASK_MethodSymbol).AsMethodSymbol() : GetSymbolLoader().LookupNextSym(methodSymbol, aggregateType.getAggregate(), symbmask_t.MASK_MethodSymbol).AsMethodSymbol());
					if (methodSymbol == null)
					{
						if (!list.IsEmpty())
						{
							break;
						}
						aggregateType = aggregateType.GetBaseClass();
						if (aggregateType == null)
						{
							break;
						}
					}
					else if (methodSymbol.isOperator && methodSymbol.Params.size == 1)
					{
						TypeArray typeArray = GetTypes().SubstTypeArray(methodSymbol.Params, aggregateType);
						CType cType2 = typeArray.Item(0);
						NullableType nullable;
						if (canConvert(arg, cType2))
						{
							list.Add(new CandidateFunctionMember(new MethPropWithInst(methodSymbol, aggregateType, BSYMMGR.EmptyTypeArray()), typeArray, 0, fExpanded: false));
						}
						else if (GetSymbolLoader().FCanLift() && cType2.IsNonNubValType() && GetTypes().SubstType(methodSymbol.RetType, aggregateType).IsNonNubValType() && canConvert(arg, nullable = GetTypes().GetNullable(cType2)))
						{
							list.Add(new CandidateFunctionMember(new MethPropWithInst(methodSymbol, aggregateType, BSYMMGR.EmptyTypeArray()), GetGlobalSymbols().AllocParams(1, new CType[1] { nullable }), 1, fExpanded: false));
						}
					}
				}
				if (list.IsEmpty())
				{
					return null;
				}
				CandidateFunctionMember methAmbig;
				CandidateFunctionMember methAmbig2;
				CandidateFunctionMember candidateFunctionMember = FindBestMethod(list, null, argInfos, out methAmbig, out methAmbig2);
				if (candidateFunctionMember == null)
				{
					ErrorContext.Error(ErrorCode.ERR_AmbigCall, methAmbig.mpwi, methAmbig2.mpwi);
					EXPRMEMGRP pMemberGroup = GetExprFactory().CreateMemGroup(null, methAmbig.mpwi);
					EXPRCALL eXPRCALL = GetExprFactory().CreateCall((EXPRFLAG)0, null, arg, pMemberGroup, null);
					eXPRCALL.SetError();
					return eXPRCALL;
				}
				if (SemanticChecker.CheckBogus(candidateFunctionMember.mpwi.Meth()))
				{
					ErrorContext.ErrorRef(ErrorCode.ERR_BindToBogus, candidateFunctionMember.mpwi);
					EXPRMEMGRP pMemberGroup2 = GetExprFactory().CreateMemGroup(null, candidateFunctionMember.mpwi);
					EXPRCALL eXPRCALL2 = GetExprFactory().CreateCall((EXPRFLAG)0, null, arg, pMemberGroup2, null);
					eXPRCALL2.SetError();
					return eXPRCALL2;
				}
				EXPRCALL eXPRCALL3 = ((candidateFunctionMember.ctypeLift == 0) ? BindUDUnopCall(arg, candidateFunctionMember.@params.Item(0), candidateFunctionMember.mpwi) : BindLiftedUDUnop(arg, candidateFunctionMember.@params.Item(0), candidateFunctionMember.mpwi));
				return GetExprFactory().CreateUserDefinedUnaryOperator(ek, eXPRCALL3.type, arg, eXPRCALL3, candidateFunctionMember.mpwi);
			}
			default:
				return null;
			}
		}
	}

	private EXPRCALL BindLiftedUDUnop(EXPR arg, CType typeArg, MethPropWithInst mpwi)
	{
		CType cType = typeArg.StripNubs();
		if (!arg.type.IsNullableType() || !canConvert(arg.type.StripNubs(), cType, CONVERTTYPE.NOUDC))
		{
			arg = mustConvert(arg, typeArg);
		}
		CType cType2 = GetTypes().SubstType(mpwi.Meth().RetType, mpwi.GetType());
		if (!cType2.IsNullableType())
		{
			cType2 = GetTypes().GetNullable(cType2);
		}
		EXPR arg2 = mustCast(arg, cType);
		EXPRCALL expr = BindUDUnopCall(arg2, cType, mpwi);
		EXPRMEMGRP pMemberGroup = GetExprFactory().CreateMemGroup(null, mpwi);
		EXPRCALL eXPRCALL = GetExprFactory().CreateCall((EXPRFLAG)0, cType2, arg, pMemberGroup, null);
		eXPRCALL.mwi = new MethWithInst(mpwi);
		eXPRCALL.castOfNonLiftedResultToLiftedType = mustCast(expr, cType2, (CONVERTTYPE)0);
		eXPRCALL.nubLiftKind = NullableCallLiftKind.Operator;
		return eXPRCALL;
	}

	private EXPRCALL BindUDUnopCall(EXPR arg, CType typeArg, MethPropWithInst mpwi)
	{
		CType cType = GetTypes().SubstType(mpwi.Meth().RetType, mpwi.GetType());
		checkUnsafe(cType);
		EXPRMEMGRP pMemberGroup = GetExprFactory().CreateMemGroup(null, mpwi);
		EXPRCALL eXPRCALL = GetExprFactory().CreateCall((EXPRFLAG)0, cType, mustConvert(arg, typeArg), pMemberGroup, null);
		eXPRCALL.mwi = new MethWithInst(mpwi);
		verifyMethodArgs(eXPRCALL, mpwi.GetType());
		return eXPRCALL;
	}

	private bool BindMethodGroupToArgumentsCore(out GroupToArgsBinderResult pResults, BindingFlag bindFlags, EXPRMEMGRP grp, ref EXPR args, int carg, bool bindingCollectionAdd, bool bHasNamedArgumentSpecifiers)
	{
		bool flag = false;
		ArgInfos argInfos = new ArgInfos();
		argInfos.carg = carg;
		FillInArgInfoFromArgList(argInfos, args);
		ArgInfos argInfos2 = new ArgInfos();
		argInfos2.carg = carg;
		FillInArgInfoFromArgList(argInfos2, args);
		GroupToArgsBinder groupToArgsBinder = new GroupToArgsBinder(this, bindFlags, grp, argInfos, argInfos2, bHasNamedArgumentSpecifiers, null);
		flag = ((!bindingCollectionAdd) ? groupToArgsBinder.Bind(bReportErrors: true) : groupToArgsBinder.BindCollectionAddArgs());
		pResults = groupToArgsBinder.GetResultsOfBind();
		return flag;
	}

	internal EXPR BindMethodGroupToArguments(BindingFlag bindFlags, EXPRMEMGRP grp, EXPR args)
	{
		bool typeErrors;
		int carg = CountArguments(args, out typeErrors);
		EXPR optionalObject = grp.GetOptionalObject();
		if (grp.name == null)
		{
			EXPRCALL eXPRCALL = GetExprFactory().CreateCall((EXPRFLAG)0, GetTypes().GetErrorSym(), args, grp, null);
			eXPRCALL.SetError();
			return eXPRCALL;
		}
		bool seenNamed = false;
		if (!VerifyNamedArgumentsAfterFixed(args, out seenNamed))
		{
			EXPRCALL eXPRCALL2 = GetExprFactory().CreateCall((EXPRFLAG)0, GetTypes().GetErrorSym(), args, grp, null);
			eXPRCALL2.SetError();
			return eXPRCALL2;
		}
		if (!BindMethodGroupToArgumentsCore(out var pResults, bindFlags, grp, ref args, carg, bindingCollectionAdd: false, seenNamed))
		{
			return null;
		}
		MethPropWithInst bestResult = pResults.GetBestResult();
		if (grp.sk == SYMKIND.SK_PropertySymbol)
		{
			return BindToProperty(grp.GetOptionalObject(), new PropWithType(bestResult), (BindingFlag)((int)bindFlags | (int)(grp.flags & EXPRFLAG.EXF_ASFINALLYLEAVE)), args, null, grp);
		}
		return BindToMethod(new MethWithInst(bestResult), args, grp, (MemLookFlags)grp.flags);
	}

	private bool VerifyNamedArgumentsAfterFixed(EXPR args, out bool seenNamed)
	{
		EXPR eXPR = args;
		seenNamed = false;
		while (eXPR != null)
		{
			EXPR expr;
			if (eXPR.isLIST())
			{
				expr = eXPR.asLIST().GetOptionalElement();
				eXPR = eXPR.asLIST().GetOptionalNextListNode();
			}
			else
			{
				expr = eXPR;
				eXPR = null;
			}
			if (expr.isNamedArgumentSpecification())
			{
				seenNamed = true;
			}
			else if (seenNamed)
			{
				GetErrorContext().Error(ErrorCode.ERR_NamedArgumentSpecificationBeforeFixedArgument);
				return false;
			}
		}
		return true;
	}

	internal EXPRCALL BindPredefMethToArgs(PREDEFMETH predefMethod, EXPR obj, EXPR args, TypeArray clsTypeArgs, TypeArray methTypeArgs)
	{
		MethodSymbol method = GetSymbolLoader().getPredefinedMembers().GetMethod(predefMethod);
		if (method == null)
		{
			MethWithInst mwi = new MethWithInst(null, null);
			EXPRMEMGRP pMemberGroup = GetExprFactory().CreateMemGroup(obj, mwi);
			EXPRCALL eXPRCALL = GetExprFactory().CreateCall((EXPRFLAG)0, null, args, pMemberGroup, null);
			eXPRCALL.SetError();
			return eXPRCALL;
		}
		AggregateSymbol @class = method.getClass();
		if (clsTypeArgs == null)
		{
			clsTypeArgs = BSYMMGR.EmptyTypeArray();
		}
		AggregateType aggregate = GetTypes().GetAggregate(@class, clsTypeArgs);
		MethPropWithInst methPropWithInst = new MethPropWithInst(method, aggregate, methTypeArgs);
		EXPRMEMGRP pMemGroup = GetExprFactory().CreateMemGroup(obj, methPropWithInst);
		return BindToMethod(new MethWithInst(methPropWithInst), args, pMemGroup, MemLookFlags.None);
	}

	protected EXPR BadOperatorTypesError(ExpressionKind ek, EXPR pOperand1, EXPR pOperand2)
	{
		return BadOperatorTypesError(ek, pOperand1, pOperand2, null);
	}

	protected EXPR BadOperatorTypesError(ExpressionKind ek, EXPR pOperand1, EXPR pOperand2, CType pTypeErr)
	{
		string errorString = pOperand1.errorString;
		pOperand1 = UnwrapExpression(pOperand1);
		if (pOperand1 != null)
		{
			if (pOperand2 != null)
			{
				pOperand2 = UnwrapExpression(pOperand2);
				if (pOperand1.type != null && !pOperand1.type.IsErrorType() && pOperand2.type != null && !pOperand2.type.IsErrorType())
				{
					ErrorContext.Error(ErrorCode.ERR_BadBinaryOps, errorString, pOperand1.type, pOperand2.type);
				}
			}
			else if (pOperand1.type != null && !pOperand1.type.IsErrorType())
			{
				ErrorContext.Error(ErrorCode.ERR_BadUnaryOp, errorString, pOperand1.type);
			}
		}
		if (pTypeErr == null)
		{
			pTypeErr = GetReqPDT(PredefinedType.PT_OBJECT);
		}
		EXPR eXPR = GetExprFactory().CreateOperator(ek, pTypeErr, pOperand1, pOperand2);
		eXPR.SetError();
		return eXPR;
	}

	protected EXPR UnwrapExpression(EXPR pExpression)
	{
		EXPR eXPR = pExpression;
		while (eXPR != null && eXPR.isWRAP() && eXPR.asWRAP().GetOptionalExpression() != null)
		{
			eXPR = eXPR.asWRAP().GetOptionalExpression();
		}
		return eXPR;
	}

	private static ErrorCode GetStandardLvalueError(CheckLvalueKind kind)
	{
		switch (kind)
		{
		default:
			VSFAIL("bad kind");
			return ErrorCode.ERR_AssgLvalueExpected;
		case CheckLvalueKind.Assignment:
			return ErrorCode.ERR_AssgLvalueExpected;
		case CheckLvalueKind.OutParameter:
			return ErrorCode.ERR_RefLvalueExpected;
		case CheckLvalueKind.Increment:
			return ErrorCode.ERR_IncrementLvalueExpected;
		}
	}

	protected void CheckLvalueProp(EXPRPROP prop)
	{
		if (prop.isBaseCall() && prop.mwtSet.Meth().isAbstract)
		{
			ErrorContext.Error(ErrorCode.ERR_AbstractBaseCall, prop.mwtSet);
			return;
		}
		CType type = null;
		if (prop.GetOptionalObjectThrough() != null)
		{
			type = prop.GetOptionalObjectThrough().type;
		}
		CheckPropertyAccess(prop.mwtSet, prop.pwtSlot, type);
	}

	protected bool CheckPropertyAccess(MethWithType mwt, PropWithType pwtSlot, CType type)
	{
		switch (SemanticChecker.CheckAccess2(mwt.Meth(), mwt.GetType(), ContextForMemberLookup(), type))
		{
		case ACCESSERROR.ACCESSERROR_NOACCESSTHRU:
			ErrorContext.Error(ErrorCode.ERR_BadProtectedAccess, pwtSlot, type, ContextForMemberLookup());
			return false;
		case ACCESSERROR.ACCESSERROR_NOACCESS:
			ErrorContext.Error(mwt.Meth().isSetAccessor() ? ErrorCode.ERR_InaccessibleSetter : ErrorCode.ERR_InaccessibleGetter, pwtSlot);
			return false;
		default:
			return true;
		}
	}

	internal bool checkLvalue(EXPR expr, CheckLvalueKind kind)
	{
		if (!expr.isOK())
		{
			return false;
		}
		if (expr.isLvalue())
		{
			if (expr.isPROP())
			{
				CheckLvalueProp(expr.asPROP());
			}
			markFieldAssigned(expr);
			return true;
		}
		switch (expr.kind)
		{
		case ExpressionKind.EK_PROP:
			if (kind == CheckLvalueKind.OutParameter)
			{
				ErrorContext.Error(ErrorCode.ERR_RefProperty);
				return true;
			}
			if (!expr.asPROP().mwtSet)
			{
				ErrorContext.Error(ErrorCode.ERR_AssgReadonlyProp, expr.asPROP().pwtSlot);
				return true;
			}
			break;
		case ExpressionKind.EK_ARRAYLENGTH:
			if (kind == CheckLvalueKind.OutParameter)
			{
				ErrorContext.Error(ErrorCode.ERR_RefProperty);
			}
			else
			{
				ErrorContext.Error(ErrorCode.ERR_AssgReadonlyProp, GetSymbolLoader().getPredefinedMembers().GetProperty(PREDEFPROP.PP_ARRAY_LENGTH));
			}
			return true;
		case ExpressionKind.EK_CONSTANT:
		case ExpressionKind.EK_BOUNDLAMBDA:
		case ExpressionKind.EK_UNBOUNDLAMBDA:
			ErrorContext.Error(GetStandardLvalueError(kind));
			return false;
		case ExpressionKind.EK_MEMGRP:
		{
			ErrorCode id = ((kind == CheckLvalueKind.OutParameter) ? ErrorCode.ERR_RefReadonlyLocalCause : ErrorCode.ERR_AssgReadonlyLocalCause);
			ErrorContext.Error(id, expr.asMEMGRP().name, new ErrArgIds(MessageID.MethodGroup));
			return false;
		}
		}
		return !TryReportLvalueFailure(expr, kind);
	}

	internal void PostBindMethod(bool fBaseCall, ref MethWithInst pMWI, EXPR pObject)
	{
		MethWithInst methWithInst = pMWI;
		if (pObject != null && (fBaseCall || pObject.type.isSimpleType() || pObject.type.isSpecialByRefType()))
		{
			RemapToOverride(GetSymbolLoader(), pMWI, pObject.type);
		}
		if (fBaseCall && pMWI.Meth().isAbstract)
		{
			ErrorContext.Error(ErrorCode.ERR_AbstractBaseCall, pMWI);
		}
		if (pMWI.Meth().RetType == null)
		{
			return;
		}
		checkUnsafe(pMWI.Meth().RetType);
		bool flag = false;
		if (pMWI.Meth().isExternal)
		{
			flag = true;
			SetExternalRef(pMWI.Meth().RetType);
		}
		TypeArray @params = pMWI.Meth().Params;
		for (int i = 0; i < @params.size; i++)
		{
			CType cType = @params.Item(i);
			if (cType.isUnsafe())
			{
				checkUnsafe(cType);
			}
			if (flag && cType.IsParameterModifierType())
			{
				SetExternalRef(cType);
			}
		}
	}

	protected void PostBindProperty(bool fBaseCall, PropWithType pwt, EXPR pObject, out MethWithType pmwtGet, out MethWithType pmwtSet)
	{
		pmwtGet = new MethWithType();
		pmwtSet = new MethWithType();
		if (pwt.Prop().methGet != null)
		{
			pmwtGet.Set(pwt.Prop().methGet, pwt.GetType());
		}
		else
		{
			pmwtGet.Clear();
		}
		if (pwt.Prop().methSet != null)
		{
			pmwtSet.Set(pwt.Prop().methSet, pwt.GetType());
		}
		else
		{
			pmwtSet.Clear();
		}
		if (fBaseCall && pObject != null)
		{
			if ((bool)pmwtGet)
			{
				RemapToOverride(GetSymbolLoader(), pmwtGet, pObject.type);
			}
			if ((bool)pmwtSet)
			{
				RemapToOverride(GetSymbolLoader(), pmwtSet, pObject.type);
			}
		}
		if (pwt.Prop().RetType != null)
		{
			checkUnsafe(pwt.Prop().RetType);
		}
	}

	private EXPR AdjustMemberObject(SymWithType swt, EXPR pObject, out bool pfConstrained, out bool pIsMatchingStatic)
	{
		bool flag = (pIsMatchingStatic = IsMatchingStatic(swt, pObject));
		pfConstrained = false;
		bool isStatic = swt.Sym.isStatic;
		if (!flag)
		{
			if (isStatic)
			{
				if ((pObject.flags & EXPRFLAG.EXF_UNREALIZEDGOTO) != 0)
				{
					pIsMatchingStatic = true;
					return null;
				}
				ErrorContext.ErrorRef(ErrorCode.ERR_ObjectProhibited, swt);
				return null;
			}
			ErrorContext.ErrorRef(ErrorCode.ERR_ObjectRequired, swt);
			return pObject;
		}
		if (isStatic)
		{
			return null;
		}
		if (swt.Sym.IsMethodSymbol() && swt.Meth().IsConstructor())
		{
			return pObject;
		}
		if (pObject == null)
		{
			if (InFieldInitializer() && !InStaticMethod() && ContainingAgg() == swt.Sym.parent)
			{
				ErrorContext.ErrorRef(ErrorCode.ERR_FieldInitRefNonstatic, swt);
			}
			else
			{
				if (!InAnonymousMethod() || InStaticMethod() || ContainingAgg() != swt.Sym.parent || !ContainingAgg().IsStruct())
				{
					return null;
				}
				ErrorContext.Error(ErrorCode.ERR_ThisStructNotInAnonMeth);
			}
			EXPRTHISPOINTER eXPRTHISPOINTER = GetExprFactory().CreateThis(Context.GetThisPointer(), fImplicit: true);
			eXPRTHISPOINTER.SetMismatchedStaticBit();
			if (eXPRTHISPOINTER.type == null)
			{
				eXPRTHISPOINTER.setType(GetTypes().GetErrorSym());
			}
			return eXPRTHISPOINTER;
		}
		CType cType = pObject.type;
		CType ats;
		if (cType.IsNullableType() && (ats = cType.AsNullableType().GetAts(GetErrorContext())) != null && ats != swt.GetType())
		{
			cType = ats;
		}
		if (cType.IsTypeParameterType() || cType.IsAggregateType())
		{
			AggregateSymbol aggregateSymbol = null;
			aggregateSymbol = swt.Sym.parent.AsAggregateSymbol();
			if (pObject.isFIELD() && !pObject.asFIELD().fwt.Field().isAssigned && !swt.Sym.IsFieldSymbol() && cType.isStructType() && !cType.isPredefined())
			{
				pObject.asFIELD().fwt.Field().isAssigned = true;
			}
			if (pfConstrained && (cType.IsTypeParameterType() || (cType.isStructType() && swt.GetType().IsRefType() && swt.Sym.IsVirtual())))
			{
				pfConstrained = true;
			}
			EXPR eXPR = tryConvert(pObject, swt.GetType(), CONVERTTYPE.NOUDC);
			if (eXPR == null)
			{
				if (!pObject.type.isSpecialByRefType())
				{
					ErrorContext.Error(ErrorCode.ERR_WrongNestedThis, swt.GetType(), pObject.type);
				}
				else
				{
					ErrorContext.Error(ErrorCode.ERR_NoImplicitConv, pObject.type, swt.GetType());
				}
			}
			pObject = eXPR;
		}
		return pObject;
	}

	private bool IsMatchingStatic(SymWithType swt, EXPR pObject)
	{
		Symbol sym = swt.Sym;
		if (sym.IsMethodSymbol() && sym.AsMethodSymbol().IsConstructor())
		{
			return !sym.AsMethodSymbol().isStatic;
		}
		if (swt.Sym.isStatic)
		{
			if (pObject == null || (pObject.flags & EXPRFLAG.EXF_IMPLICITTHIS) != 0)
			{
				return true;
			}
			if ((pObject.flags & EXPRFLAG.EXF_SAMENAMETYPE) == 0)
			{
				return false;
			}
		}
		else if (pObject == null)
		{
			bool flag = InFieldInitializer() && !InStaticMethod() && ContainingAgg() == swt.Sym.parent;
			bool flag2 = InAnonymousMethod() && !InStaticMethod() && ContainingAgg() == swt.Sym.parent && ContainingAgg().IsStruct();
			if (!flag && !flag2)
			{
				return false;
			}
		}
		return true;
	}

	private bool objectIsLvalue(EXPR pObject)
	{
		if (pObject != null && !isThisPointer(pObject) && ((pObject.flags & EXPRFLAG.EXF_LVALUE) == 0 || pObject.kind == ExpressionKind.EK_PROP))
		{
			return !pObject.type.isStructOrEnum();
		}
		return true;
	}

	public static void RemapToOverride(SymbolLoader symbolLoader, SymWithType pswt, CType typeObj)
	{
		if (typeObj.IsNullableType())
		{
			typeObj = typeObj.AsNullableType().GetAts(symbolLoader.GetErrorContext());
			if (typeObj == null)
			{
				VSFAIL("Why did GetAts return null?");
				return;
			}
		}
		if (!typeObj.IsAggregateType() || typeObj.isInterfaceType() || !pswt.Sym.IsVirtual())
		{
			return;
		}
		symbmask_t symbmask_t2 = pswt.Sym.mask();
		AggregateType aggregateType = typeObj.AsAggregateType();
		while (aggregateType != null && aggregateType.getAggregate() != pswt.Sym.parent)
		{
			for (Symbol symbol = symbolLoader.LookupAggMember(pswt.Sym.name, aggregateType.getAggregate(), symbmask_t2); symbol != null; symbol = symbolLoader.LookupNextSym(symbol, aggregateType.getAggregate(), symbmask_t2))
			{
				if (symbol.IsOverride() && (symbol.SymBaseVirtual() == pswt.Sym || symbol.SymBaseVirtual() == pswt.Sym.SymBaseVirtual()))
				{
					pswt.Set(symbol, aggregateType);
					return;
				}
			}
			aggregateType = aggregateType.GetBaseClass();
		}
	}

	protected void verifyMethodArgs(EXPR call, CType callingObjectType)
	{
		EXPR args = call.getArgs();
		SymWithType symWithType = call.GetSymWithType();
		MethodOrPropertySymbol mp = symWithType.Sym.AsMethodOrPropertySymbol();
		TypeArray pTypeArgs = (call.isCALL() ? call.asCALL().mwi.TypeArgs : null);
		AdjustCallArgumentsForParams(callingObjectType, symWithType.GetType(), mp, pTypeArgs, args, out var newArgs);
		call.setArgs(newArgs);
	}

	protected void AdjustCallArgumentsForParams(CType callingObjectType, CType type, MethodOrPropertySymbol mp, TypeArray pTypeArgs, EXPR argsPtr, out EXPR newArgs)
	{
		newArgs = null;
		EXPR last = null;
		MethodOrPropertySymbol methodOrPropertySymbol = GroupToArgsBinder.FindMostDerivedMethod(GetSymbolLoader(), mp, callingObjectType);
		int num = mp.Params.size;
		TypeArray @params = mp.Params;
		int num2 = 0;
		bool flag = mp.IsFMETHSYM() && mp.AsFMETHSYM().isExternal;
		int num3 = ExpressionIterator.Count(argsPtr);
		if (mp.IsFMETHSYM() && mp.AsFMETHSYM().isVarargs)
		{
			num--;
		}
		bool flag2 = false;
		EXPR eXPR = null;
		ExpressionIterator expressionIterator = new ExpressionIterator(argsPtr);
		if (argsPtr == null)
		{
			if (!mp.isParamArray)
			{
				return;
			}
		}
		else
		{
			while (true)
			{
				if (expressionIterator.AtEnd())
				{
					return;
				}
				eXPR = expressionIterator.Current();
				if (eXPR.type.IsParameterModifierType())
				{
					if (num != 0)
					{
						num--;
					}
					if (flag)
					{
						SetExternalRef(eXPR.type);
					}
					GetExprFactory().AppendItemToList(eXPR, ref newArgs, ref last);
				}
				else if (num != 0)
				{
					if (num == 1 && mp.isParamArray && num3 > mp.Params.size)
					{
						break;
					}
					EXPR eXPR2 = eXPR;
					EXPR eXPR3;
					if (eXPR2.isNamedArgumentSpecification())
					{
						int num4 = 0;
						foreach (Name parameterName in methodOrPropertySymbol.ParameterNames)
						{
							if (parameterName == eXPR2.asNamedArgumentSpecification().Name)
							{
								break;
							}
							num4++;
						}
						CType dest = GetTypes().SubstType(@params.Item(num4), type, pTypeArgs);
						if (!canConvert(eXPR2.asNamedArgumentSpecification().Value, dest) && mp.isParamArray && num4 == mp.Params.size - 1)
						{
							CType cType = GetTypes().SubstType(mp.Params.Item(mp.Params.size - 1), type, pTypeArgs);
							CType elementType = cType.AsArrayType().GetElementType();
							EXPRARRINIT eXPRARRINIT = GetExprFactory().CreateArrayInit((EXPRFLAG)0, cType, null, null, null);
							eXPRARRINIT.GeneratedForParamArray = true;
							eXPRARRINIT.dimSizes = new int[1] { eXPRARRINIT.dimSize };
							eXPRARRINIT.dimSize = 1;
							eXPRARRINIT.SetOptionalArguments(eXPR2.asNamedArgumentSpecification().Value);
							eXPR2.asNamedArgumentSpecification().Value = eXPRARRINIT;
							flag2 = true;
						}
						else
						{
							eXPR2.asNamedArgumentSpecification().Value = tryConvert(eXPR2.asNamedArgumentSpecification().Value, dest);
						}
						eXPR3 = eXPR2;
					}
					else
					{
						CType dest2 = GetTypes().SubstType(@params.Item(num2), type, pTypeArgs);
						eXPR3 = tryConvert(eXPR, dest2);
					}
					if (eXPR3 == null)
					{
						if (mp.isParamArray && num == 1 && num3 >= mp.Params.size)
						{
							break;
						}
						return;
					}
					eXPR = eXPR3;
					GetExprFactory().AppendItemToList(eXPR3, ref newArgs, ref last);
					num--;
				}
				num2++;
				if (num != 0 && mp.isParamArray && num2 == num3)
				{
					eXPR = null;
					expressionIterator.MoveNext();
					break;
				}
				expressionIterator.MoveNext();
			}
		}
		if (flag2)
		{
			return;
		}
		CType cType2 = GetTypes().SubstType(mp.Params.Item(mp.Params.size - 1), type, pTypeArgs);
		if (!cType2.IsArrayType() || cType2.AsArrayType().rank != 1)
		{
			return;
		}
		CType elementType2 = cType2.AsArrayType().GetElementType();
		EXPRARRINIT eXPRARRINIT2 = GetExprFactory().CreateArrayInit((EXPRFLAG)0, cType2, null, null, null);
		eXPRARRINIT2.GeneratedForParamArray = true;
		eXPRARRINIT2.dimSizes = new int[1] { eXPRARRINIT2.dimSize };
		if (expressionIterator.AtEnd())
		{
			eXPRARRINIT2.dimSize = 0;
			eXPRARRINIT2.dimSizes[0] = 0;
			eXPRARRINIT2.SetOptionalArguments(null);
			argsPtr = ((argsPtr != null) ? ((EXPR)GetExprFactory().CreateList(argsPtr, eXPRARRINIT2)) : ((EXPR)eXPRARRINIT2));
			GetExprFactory().AppendItemToList(eXPRARRINIT2, ref newArgs, ref last);
			return;
		}
		EXPR first = null;
		EXPR last2 = null;
		int num5 = 0;
		while (!expressionIterator.AtEnd())
		{
			EXPR eXPR4 = expressionIterator.Current();
			num5++;
			if (eXPR4.isNamedArgumentSpecification())
			{
				eXPR4.asNamedArgumentSpecification().Value = tryConvert(eXPR4.asNamedArgumentSpecification().Value, elementType2);
			}
			else
			{
				eXPR4 = tryConvert(eXPR4, elementType2);
			}
			GetExprFactory().AppendItemToList(eXPR4, ref first, ref last2);
			expressionIterator.MoveNext();
		}
		eXPRARRINIT2.dimSize = num5;
		eXPRARRINIT2.dimSizes[0] = num5;
		eXPRARRINIT2.SetOptionalArguments(first);
		GetExprFactory().AppendItemToList(eXPRARRINIT2, ref newArgs, ref last);
	}

	protected void markFieldAssigned(EXPR expr)
	{
		if (expr.isFIELD() && (expr.flags & EXPRFLAG.EXF_LVALUE) != 0)
		{
			EXPRFIELD eXPRFIELD;
			do
			{
				eXPRFIELD = expr.asFIELD();
				eXPRFIELD.fwt.Field().isAssigned = true;
				expr = eXPRFIELD.GetOptionalObject();
			}
			while (eXPRFIELD.fwt.Field().getClass().IsStruct() && !eXPRFIELD.fwt.Field().isStatic && expr != null && expr.isFIELD());
		}
	}

	protected void SetExternalRef(CType type)
	{
		AggregateSymbol nakedAgg = type.GetNakedAgg();
		if (nakedAgg == null || nakedAgg.HasExternReference())
		{
			return;
		}
		nakedAgg.SetHasExternReference(hasExternReference: true);
		foreach (Symbol item in nakedAgg.Children())
		{
			if (item.IsFieldSymbol())
			{
				SetExternalRef(item.AsFieldSymbol().GetType());
			}
		}
	}

	internal CType chooseArrayIndexType(EXPR args)
	{
		for (int i = 0; i < rgptIntOp.Length; i++)
		{
			CType reqPDT = GetReqPDT(rgptIntOp[i]);
			using IEnumerator<EXPR> enumerator = args.ToEnumerable().GetEnumerator();
			EXPR current;
			do
			{
				if (enumerator.MoveNext())
				{
					current = enumerator.Current;
					continue;
				}
				return reqPDT;
			}
			while (canConvert(current, reqPDT));
		}
		return null;
	}

	internal void FillInArgInfoFromArgList(ArgInfos argInfo, EXPR args)
	{
		CType[] array = new CType[argInfo.carg];
		argInfo.fHasExprs = true;
		argInfo.prgexpr = new List<EXPR>();
		int num = 0;
		EXPR eXPR = args;
		while (eXPR != null)
		{
			EXPR eXPR2;
			if (eXPR.isLIST())
			{
				eXPR2 = eXPR.asLIST().GetOptionalElement();
				eXPR = eXPR.asLIST().GetOptionalNextListNode();
			}
			else
			{
				eXPR2 = eXPR;
				eXPR = null;
			}
			if (eXPR2.type != null)
			{
				array[num] = eXPR2.type;
			}
			else
			{
				array[num] = GetTypes().GetErrorSym();
			}
			argInfo.prgexpr.Add(eXPR2);
			num++;
		}
		argInfo.types = GetGlobalSymbols().AllocParams(num, array);
	}

	protected bool TryGetExpandedParams(TypeArray @params, int count, out TypeArray ppExpandedParams)
	{
		CType[] array;
		if (count < @params.size - 1)
		{
			array = new CType[@params.size - 1];
			@params.CopyItems(0, @params.size - 1, array);
			ppExpandedParams = GetGlobalSymbols().AllocParams(@params.size - 1, array);
			return true;
		}
		array = new CType[count];
		@params.CopyItems(0, @params.size - 1, array);
		CType cType = @params.Item(@params.size - 1);
		CType cType2 = null;
		if (!cType.IsArrayType())
		{
			ppExpandedParams = null;
			return false;
		}
		cType2 = cType.AsArrayType().GetElementType();
		for (int i = @params.size - 1; i < count; i++)
		{
			array[i] = cType2;
		}
		ppExpandedParams = GetGlobalSymbols().AllocParams(array);
		return true;
	}

	public static bool IsMethPropCallable(MethodOrPropertySymbol sym, bool requireUC)
	{
		if (!sym.isOverride || sym.isHideByName)
		{
			if (requireUC)
			{
				return sym.isUserCallable();
			}
			return true;
		}
		return false;
	}

	private bool isConvInTable(List<UdConvInfo> convTable, MethodSymbol meth, AggregateType ats, bool fSrc, bool fDst)
	{
		foreach (UdConvInfo item in convTable)
		{
			if (item.mwt.Meth() == meth && item.mwt.GetType() == ats && item.fSrcImplicit == fSrc && item.fDstImplicit == fDst)
			{
				return true;
			}
		}
		return false;
	}

	public static bool isConstantInRange(EXPRCONSTANT exprSrc, CType typeDest)
	{
		return isConstantInRange(exprSrc, typeDest, realsOk: false);
	}

	public static bool isConstantInRange(EXPRCONSTANT exprSrc, CType typeDest, bool realsOk)
	{
		FUNDTYPE fUNDTYPE = exprSrc.type.fundType();
		FUNDTYPE fUNDTYPE2 = typeDest.fundType();
		if (fUNDTYPE > FUNDTYPE.FT_U8 || fUNDTYPE2 > FUNDTYPE.FT_U8)
		{
			if (!realsOk)
			{
				return false;
			}
			if (fUNDTYPE > FUNDTYPE.FT_R8 || fUNDTYPE2 > FUNDTYPE.FT_R8)
			{
				return false;
			}
		}
		if (fUNDTYPE2 > FUNDTYPE.FT_U8)
		{
			return true;
		}
		if (fUNDTYPE > FUNDTYPE.FT_U8)
		{
			double doubleVal = exprSrc.asCONSTANT().getVal().doubleVal;
			switch (fUNDTYPE2)
			{
			case FUNDTYPE.FT_I1:
				if (doubleVal > -129.0 && doubleVal < 128.0)
				{
					return true;
				}
				break;
			case FUNDTYPE.FT_I2:
				if (doubleVal > -32769.0 && doubleVal < 32768.0)
				{
					return true;
				}
				break;
			case FUNDTYPE.FT_I4:
				if (doubleVal > (double)I64(-2147483649L) && doubleVal < (double)I64(2147483648L))
				{
					return true;
				}
				break;
			case FUNDTYPE.FT_I8:
				if (doubleVal >= -9.223372036854776E+18 && doubleVal < 9.223372036854776E+18)
				{
					return true;
				}
				break;
			case FUNDTYPE.FT_U1:
				if (doubleVal > -1.0 && doubleVal < 256.0)
				{
					return true;
				}
				break;
			case FUNDTYPE.FT_U2:
				if (doubleVal > -1.0 && doubleVal < 65536.0)
				{
					return true;
				}
				break;
			case FUNDTYPE.FT_U4:
				if (doubleVal > -1.0 && doubleVal < (double)I64(4294967296L))
				{
					return true;
				}
				break;
			case FUNDTYPE.FT_U8:
				if (doubleVal > -1.0 && doubleVal < 1.8446744073709552E+19)
				{
					return true;
				}
				break;
			}
			return false;
		}
		if (fUNDTYPE == FUNDTYPE.FT_U8)
		{
			ulong u64Value = exprSrc.asCONSTANT().getU64Value();
			switch (fUNDTYPE2)
			{
			case FUNDTYPE.FT_I1:
				if (u64Value <= 127)
				{
					return true;
				}
				break;
			case FUNDTYPE.FT_I2:
				if (u64Value <= 32767)
				{
					return true;
				}
				break;
			case FUNDTYPE.FT_I4:
				if (u64Value <= int.MaxValue)
				{
					return true;
				}
				break;
			case FUNDTYPE.FT_I8:
				if (u64Value <= long.MaxValue)
				{
					return true;
				}
				break;
			case FUNDTYPE.FT_U1:
				if (u64Value <= 255)
				{
					return true;
				}
				break;
			case FUNDTYPE.FT_U2:
				if (u64Value <= 65535)
				{
					return true;
				}
				break;
			case FUNDTYPE.FT_U4:
				if (u64Value <= uint.MaxValue)
				{
					return true;
				}
				break;
			case FUNDTYPE.FT_U8:
				return true;
			}
		}
		else
		{
			long i64Value = exprSrc.asCONSTANT().getI64Value();
			switch (fUNDTYPE2)
			{
			case FUNDTYPE.FT_I1:
				if (i64Value >= -128 && i64Value <= 127)
				{
					return true;
				}
				break;
			case FUNDTYPE.FT_I2:
				if (i64Value >= -32768 && i64Value <= 32767)
				{
					return true;
				}
				break;
			case FUNDTYPE.FT_I4:
				if (i64Value >= I64(-2147483648L) && i64Value <= I64(2147483647L))
				{
					return true;
				}
				break;
			case FUNDTYPE.FT_I8:
				return true;
			case FUNDTYPE.FT_U1:
				if (i64Value >= 0 && i64Value <= 255)
				{
					return true;
				}
				break;
			case FUNDTYPE.FT_U2:
				if (i64Value >= 0 && i64Value <= 65535)
				{
					return true;
				}
				break;
			case FUNDTYPE.FT_U4:
				if (i64Value >= 0 && i64Value <= I64(4294967295L))
				{
					return true;
				}
				break;
			case FUNDTYPE.FT_U8:
				if (i64Value >= 0)
				{
					return true;
				}
				break;
			}
		}
		return false;
	}

	protected Name ekName(ExpressionKind ek)
	{
		return GetSymbolLoader().GetNameManager().GetPredefName(EK2NAME[(int)(ek - 42)]);
	}

	public void checkUnsafe(CType type)
	{
		checkUnsafe(type, ErrorCode.ERR_UnsafeNeeded, null);
	}

	public void checkUnsafe(CType type, ErrorCode errCode, ErrArg pArg)
	{
		if (type != null && !type.isUnsafe())
		{
			return;
		}
		if (!isUnsafeContext() && ReportUnsafeErrors())
		{
			if (pArg != null)
			{
				ErrorContext.Error(errCode, pArg);
			}
			else
			{
				ErrorContext.Error(errCode);
			}
		}
		RecordUnsafeUsage();
	}

	protected bool InMethod()
	{
		return Context.InMethod();
	}

	protected bool InStaticMethod()
	{
		return Context.InStaticMethod();
	}

	protected bool InConstructor()
	{
		return Context.InConstructor();
	}

	protected bool InAnonymousMethod()
	{
		return Context.InAnonymousMethod();
	}

	protected bool InFieldInitializer()
	{
		return Context.InFieldInitializer();
	}

	private Declaration ContextForMemberLookup()
	{
		return Context.ContextForMemberLookup();
	}

	protected AggregateSymbol ContainingAgg()
	{
		return Context.ContainingAgg();
	}

	protected bool isThisPointer(EXPR expr)
	{
		return Context.IsThisPointer(expr);
	}

	protected bool RespectReadonly()
	{
		return Context.RespectReadonly();
	}

	protected bool isUnsafeContext()
	{
		return Context.IsUnsafeContext();
	}

	protected bool ReportUnsafeErrors()
	{
		return Context.ReportUnsafeErrors();
	}

	protected virtual void RecordUnsafeUsage()
	{
		RecordUnsafeUsage(Context);
	}

	protected virtual EXPR WrapShortLivedExpression(EXPR expr)
	{
		return GetExprFactory().CreateWrap(null, expr);
	}

	protected virtual EXPR GenerateOptimizedAssignment(EXPR op1, EXPR op2)
	{
		return GetExprFactory().CreateAssignment(op1, op2);
	}

	public static void RecordUnsafeUsage(BindingContext context)
	{
		if (context.GetUnsafeState() != 0 && !context.GetOutputContext().m_bUnsafeErrorGiven)
		{
			context.GetOutputContext().m_bUnsafeErrorGiven = true;
		}
	}

	internal static int CountArguments(EXPR args, out bool typeErrors)
	{
		int num = 0;
		typeErrors = false;
		EXPR eXPR = args;
		while (eXPR != null)
		{
			EXPR eXPR2;
			if (eXPR.isLIST())
			{
				eXPR2 = eXPR.asLIST().GetOptionalElement();
				eXPR = eXPR.asLIST().GetOptionalNextListNode();
			}
			else
			{
				eXPR2 = eXPR;
				eXPR = null;
			}
			if (eXPR2.type == null || eXPR2.type.IsErrorType())
			{
				typeErrors = true;
			}
			num++;
		}
		return num;
	}

	internal EXPR BindNubValue(EXPR exprSrc)
	{
		return m_nullable.BindValue(exprSrc);
	}

	private EXPRCALL BindNubNew(EXPR exprSrc)
	{
		return m_nullable.BindNew(exprSrc);
	}

	protected EXPR bindUserDefinedBinOp(ExpressionKind ek, BinOpArgInfo info)
	{
		MethPropWithInst ppmpwi = null;
		if (info.pt1 <= PredefinedType.PT_ULONG && info.pt2 <= PredefinedType.PT_ULONG)
		{
			return null;
		}
		EXPR eXPR = null;
		BinOpKind binopKind = info.binopKind;
		if (binopKind == BinOpKind.Logical)
		{
			EXPRCALL eXPRCALL = BindUDBinop(ek - 68 + 62, info.arg1, info.arg2, fDontLift: true, out ppmpwi);
			if (eXPRCALL != null)
			{
				eXPR = ((!eXPRCALL.isOK()) ? eXPRCALL : BindUserBoolOp(ek, eXPRCALL));
			}
		}
		else
		{
			eXPR = BindUDBinop(ek, info.arg1, info.arg2, fDontLift: false, out ppmpwi);
		}
		if (eXPR == null)
		{
			return null;
		}
		return GetExprFactory().CreateUserDefinedBinop(ek, eXPR.type, info.arg1, info.arg2, eXPR, ppmpwi);
	}

	protected bool GetSpecialBinopSignatures(List<BinOpFullSig> prgbofs, BinOpArgInfo info)
	{
		if (info.pt1 <= PredefinedType.PT_ULONG && info.pt2 <= PredefinedType.PT_ULONG)
		{
			return false;
		}
		if (!GetDelBinOpSigs(prgbofs, info) && !GetEnumBinOpSigs(prgbofs, info) && !GetPtrBinOpSigs(prgbofs, info))
		{
			return GetRefEqualSigs(prgbofs, info);
		}
		return true;
	}

	protected bool GetStandardAndLiftedBinopSignatures(List<BinOpFullSig> rgbofs, BinOpArgInfo info)
	{
		int num = ((!GetSymbolLoader().FCanLift()) ? g_binopSignatures.Length : 0);
		for (int i = 0; i < g_binopSignatures.Length; i++)
		{
			BinOpSig binOpSig = g_binopSignatures[i];
			if ((binOpSig.mask & info.mask) == 0)
			{
				continue;
			}
			CType cType = GetOptPDT(binOpSig.pt1, PredefinedTypes.isRequired(binOpSig.pt1));
			CType cType2 = GetOptPDT(binOpSig.pt2, PredefinedTypes.isRequired(binOpSig.pt2));
			if (cType == null || cType2 == null)
			{
				continue;
			}
			ConvKind convKind = GetConvKind(info.pt1, binOpSig.pt1);
			ConvKind convKind2 = GetConvKind(info.pt2, binOpSig.pt2);
			LiftFlags liftFlags = LiftFlags.None;
			switch (convKind)
			{
			default:
				VSFAIL("Shouldn't happen!");
				continue;
			case ConvKind.Explicit:
				if (!info.arg1.isCONSTANT_OK())
				{
					continue;
				}
				if (!canConvert(info.arg1, cType))
				{
					if (i < num || !binOpSig.CanLift())
					{
						continue;
					}
					cType = GetSymbolLoader().GetTypeManager().GetNullable(cType);
					if (!canConvert(info.arg1, cType))
					{
						continue;
					}
					ConvKind convKind4 = GetConvKind(info.ptRaw1, binOpSig.pt1);
					liftFlags = (((uint)(convKind4 - 1) <= 1u) ? (liftFlags | LiftFlags.Lift1) : (liftFlags | LiftFlags.Convert1));
				}
				break;
			case ConvKind.Unknown:
				if (!canConvert(info.arg1, cType))
				{
					if (i < num || !binOpSig.CanLift())
					{
						continue;
					}
					cType = GetSymbolLoader().GetTypeManager().GetNullable(cType);
					if (!canConvert(info.arg1, cType))
					{
						continue;
					}
					ConvKind convKind3 = GetConvKind(info.ptRaw1, binOpSig.pt1);
					liftFlags = (((uint)(convKind3 - 1) <= 1u) ? (liftFlags | LiftFlags.Lift1) : (liftFlags | LiftFlags.Convert1));
				}
				break;
			case ConvKind.Identity:
				if (convKind2 == ConvKind.Identity)
				{
					BinOpFullSig binOpFullSig = new BinOpFullSig(this, binOpSig);
					if (binOpFullSig.Type1() != null && binOpFullSig.Type2() != null)
					{
						rgbofs.Add(binOpFullSig);
						return true;
					}
				}
				break;
			case ConvKind.Implicit:
				break;
			case ConvKind.None:
				continue;
			}
			switch (convKind2)
			{
			default:
				VSFAIL("Shouldn't happen!");
				continue;
			case ConvKind.Explicit:
				if (!info.arg2.isCONSTANT_OK())
				{
					continue;
				}
				if (!canConvert(info.arg2, cType2))
				{
					if (i < num || !binOpSig.CanLift())
					{
						continue;
					}
					cType2 = GetSymbolLoader().GetTypeManager().GetNullable(cType2);
					if (!canConvert(info.arg2, cType2))
					{
						continue;
					}
					ConvKind convKind6 = GetConvKind(info.ptRaw2, binOpSig.pt2);
					liftFlags = (((uint)(convKind6 - 1) <= 1u) ? (liftFlags | LiftFlags.Lift2) : (liftFlags | LiftFlags.Convert2));
				}
				break;
			case ConvKind.Unknown:
				if (!canConvert(info.arg2, cType2))
				{
					if (i < num || !binOpSig.CanLift())
					{
						continue;
					}
					cType2 = GetSymbolLoader().GetTypeManager().GetNullable(cType2);
					if (!canConvert(info.arg2, cType2))
					{
						continue;
					}
					ConvKind convKind5 = GetConvKind(info.ptRaw2, binOpSig.pt2);
					liftFlags = (((uint)(convKind5 - 1) <= 1u) ? (liftFlags | LiftFlags.Lift2) : (liftFlags | LiftFlags.Convert2));
				}
				break;
			case ConvKind.Identity:
			case ConvKind.Implicit:
				break;
			case ConvKind.None:
				continue;
			}
			if (liftFlags != 0)
			{
				rgbofs.Add(new BinOpFullSig(cType, cType2, binOpSig.pfn, binOpSig.grfos, liftFlags, binOpSig.fnkind));
				num = i + binOpSig.cbosSkip + 1;
			}
			else
			{
				rgbofs.Add(new BinOpFullSig(this, binOpSig));
				i += binOpSig.cbosSkip;
			}
		}
		return false;
	}

	protected int FindBestSignatureInList(List<BinOpFullSig> binopSignatures, BinOpArgInfo info)
	{
		if (binopSignatures.Count == 1)
		{
			return 0;
		}
		int num = 0;
		for (int i = 1; i < binopSignatures.Count; i++)
		{
			if (num < 0)
			{
				num = i;
				continue;
			}
			int num2 = WhichBofsIsBetter(binopSignatures[num], binopSignatures[i], info.type1, info.type2);
			if (num2 == 0)
			{
				num = -1;
			}
			else if (num2 > 0)
			{
				num = i;
			}
		}
		if (num == -1)
		{
			return -1;
		}
		for (int i = 0; i < binopSignatures.Count; i++)
		{
			if (i != num && WhichBofsIsBetter(binopSignatures[num], binopSignatures[i], info.type1, info.type2) >= 0)
			{
				return -1;
			}
		}
		return num;
	}

	protected EXPRBINOP bindNullEqualityComparison(ExpressionKind ek, BinOpArgInfo info)
	{
		EXPR arg = info.arg1;
		EXPR p = info.arg2;
		if (info.binopKind == BinOpKind.Equal)
		{
			CType reqPDT = GetReqPDT(PredefinedType.PT_BOOL);
			EXPRBINOP eXPRBINOP = null;
			if (info.type1.IsNullableType() && info.type2.IsNullType())
			{
				p = GetExprFactory().CreateZeroInit(info.type1);
				eXPRBINOP = GetExprFactory().CreateBinop(ek, reqPDT, arg, p);
			}
			if (info.type1.IsNullType() && info.type2.IsNullableType())
			{
				arg = GetExprFactory().CreateZeroInit(info.type2);
				eXPRBINOP = GetExprFactory().CreateBinop(ek, reqPDT, arg, p);
			}
			if (eXPRBINOP != null)
			{
				eXPRBINOP.isLifted = true;
				return eXPRBINOP;
			}
		}
		EXPR expr = BadOperatorTypesError(ek, info.arg1, info.arg2, GetTypes().GetErrorSym());
		return expr.asBIN();
	}

	public EXPR BindStandardBinop(ExpressionKind ek, EXPR arg1, EXPR arg2)
	{
		EXPRFLAG flags = (EXPRFLAG)0;
		BinOpArgInfo binOpArgInfo = new BinOpArgInfo(arg1, arg2);
		if (!GetBinopKindAndFlags(ek, out binOpArgInfo.binopKind, out flags))
		{
			return BadOperatorTypesError(ek, arg1, arg2);
		}
		binOpArgInfo.mask = (BinOpMask)(1 << (int)binOpArgInfo.binopKind);
		List<BinOpFullSig> list = new List<BinOpFullSig>();
		int num = -1;
		EXPR eXPR = bindUserDefinedBinOp(ek, binOpArgInfo);
		if (eXPR != null)
		{
			return eXPR;
		}
		bool flag = GetSpecialBinopSignatures(list, binOpArgInfo);
		if (!flag)
		{
			flag = GetStandardAndLiftedBinopSignatures(list, binOpArgInfo);
		}
		if (flag)
		{
			num = list.Count - 1;
		}
		else
		{
			if (list.Count == 0)
			{
				return bindNullEqualityComparison(ek, binOpArgInfo);
			}
			num = FindBestSignatureInList(list, binOpArgInfo);
			if (num < 0)
			{
				return ambiguousOperatorError(ek, arg1, arg2);
			}
		}
		return BindStandardBinopCore(binOpArgInfo, list[num], ek, flags);
	}

	protected EXPR BindStandardBinopCore(BinOpArgInfo info, BinOpFullSig bofs, ExpressionKind ek, EXPRFLAG flags)
	{
		if (bofs.pfn == null)
		{
			return BadOperatorTypesError(ek, info.arg1, info.arg2);
		}
		if (!bofs.isLifted() || !bofs.AutoLift())
		{
			EXPR eXPR = info.arg1;
			EXPR eXPR2 = info.arg2;
			if (bofs.ConvertOperandsBeforeBinding())
			{
				eXPR = mustConvert(eXPR, bofs.Type1());
				eXPR2 = mustConvert(eXPR2, bofs.Type2());
			}
			if (bofs.fnkind == BinOpFuncKind.BoolBitwiseOp)
			{
				return BindBoolBitwiseOp(ek, flags, eXPR, eXPR2, bofs);
			}
			return bofs.pfn(ek, flags, eXPR, eXPR2);
		}
		return BindLiftedStandardBinOp(info, bofs, ek, flags);
	}

	private EXPR BindLiftedStandardBinOp(BinOpArgInfo info, BinOpFullSig bofs, ExpressionKind ek, EXPRFLAG flags)
	{
		EXPR arg = info.arg1;
		EXPR arg2 = info.arg2;
		EXPR ppLiftedArgument = null;
		EXPR ppLiftedArgument2 = null;
		EXPR ppNonLiftedArgument = null;
		EXPR ppNonLiftedArgument2 = null;
		EXPR expr = null;
		CType cType = null;
		LiftArgument(arg, bofs.Type1(), bofs.ConvertFirst(), out ppLiftedArgument, out ppNonLiftedArgument);
		LiftArgument(arg2, bofs.Type2(), bofs.ConvertSecond(), out ppLiftedArgument2, out ppNonLiftedArgument2);
		if (!ppNonLiftedArgument.isNull() && !ppNonLiftedArgument2.isNull())
		{
			expr = bofs.pfn(ek, flags, ppNonLiftedArgument, ppNonLiftedArgument2);
		}
		if (info.binopKind == BinOpKind.Compare || info.binopKind == BinOpKind.Equal)
		{
			cType = GetReqPDT(PredefinedType.PT_BOOL);
		}
		else
		{
			cType = ((bofs.fnkind != BinOpFuncKind.EnumBinOp) ? ppLiftedArgument.type : GetEnumBinOpType(ek, ppNonLiftedArgument.type, ppNonLiftedArgument2.type, out var _));
			cType = (cType.IsNullableType() ? cType : GetSymbolLoader().GetTypeManager().GetNullable(cType));
		}
		EXPRBINOP eXPRBINOP = GetExprFactory().CreateBinop(ek, cType, ppLiftedArgument, ppLiftedArgument2);
		mustCast(expr, cType, (CONVERTTYPE)0);
		eXPRBINOP.isLifted = true;
		eXPRBINOP.flags |= flags;
		return eXPRBINOP;
	}

	private void LiftArgument(EXPR pArgument, CType pParameterType, bool bConvertBeforeLift, out EXPR ppLiftedArgument, out EXPR ppNonLiftedArgument)
	{
		EXPR eXPR = mustConvert(pArgument, pParameterType);
		if (eXPR != pArgument)
		{
			MarkAsIntermediateConversion(eXPR);
		}
		EXPR expr = pArgument;
		if (pParameterType.IsNullableType())
		{
			if (expr.isNull())
			{
				expr = mustCast(expr, pParameterType);
			}
			expr = mustCast(expr, pParameterType.AsNullableType().GetUnderlyingType());
			if (bConvertBeforeLift)
			{
				MarkAsIntermediateConversion(expr);
			}
		}
		else
		{
			expr = eXPR;
		}
		ppLiftedArgument = eXPR;
		ppNonLiftedArgument = expr;
	}

	protected bool GetDelBinOpSigs(List<BinOpFullSig> prgbofs, BinOpArgInfo info)
	{
		if (!info.ValidForDelegate())
		{
			return false;
		}
		if (!info.type1.isDelegateType() && !info.type2.isDelegateType())
		{
			return false;
		}
		if ((info.mask & BinOpMask.Equal) != 0 && (info.type1.IsBoundLambdaType() || info.type2.IsBoundLambdaType()))
		{
			return false;
		}
		if (info.type1 == info.type2)
		{
			prgbofs.Add(new BinOpFullSig(info.type1, info.type2, BindDelBinOp, OpSigFlags.Convert, LiftFlags.None, BinOpFuncKind.DelBinOp));
			return true;
		}
		bool flag = info.type2.isDelegateType() && canConvert(info.arg1, info.type2);
		bool flag2 = info.type1.isDelegateType() && canConvert(info.arg2, info.type1);
		if (flag)
		{
			prgbofs.Add(new BinOpFullSig(info.type2, info.type2, BindDelBinOp, OpSigFlags.Convert, LiftFlags.None, BinOpFuncKind.DelBinOp));
		}
		if (flag2)
		{
			prgbofs.Add(new BinOpFullSig(info.type1, info.type1, BindDelBinOp, OpSigFlags.Convert, LiftFlags.None, BinOpFuncKind.DelBinOp));
		}
		return false;
	}

	private bool CanConvertArg1(BinOpArgInfo info, CType typeDst, out LiftFlags pgrflt, out CType ptypeSig1, out CType ptypeSig2)
	{
		ptypeSig1 = null;
		ptypeSig2 = null;
		if (canConvert(info.arg1, typeDst))
		{
			pgrflt = LiftFlags.None;
		}
		else
		{
			pgrflt = LiftFlags.None;
			if (!GetSymbolLoader().FCanLift())
			{
				return false;
			}
			typeDst = GetSymbolLoader().GetTypeManager().GetNullable(typeDst);
			if (!canConvert(info.arg1, typeDst))
			{
				return false;
			}
			pgrflt = LiftFlags.Convert1;
		}
		ptypeSig1 = typeDst;
		if (info.type2.IsNullableType())
		{
			pgrflt |= LiftFlags.Lift2;
			ptypeSig2 = GetSymbolLoader().GetTypeManager().GetNullable(info.typeRaw2);
		}
		else
		{
			ptypeSig2 = info.typeRaw2;
		}
		return true;
	}

	private bool CanConvertArg2(BinOpArgInfo info, CType typeDst, out LiftFlags pgrflt, out CType ptypeSig1, out CType ptypeSig2)
	{
		ptypeSig1 = null;
		ptypeSig2 = null;
		if (canConvert(info.arg2, typeDst))
		{
			pgrflt = LiftFlags.None;
		}
		else
		{
			pgrflt = LiftFlags.None;
			if (!GetSymbolLoader().FCanLift())
			{
				return false;
			}
			typeDst = GetSymbolLoader().GetTypeManager().GetNullable(typeDst);
			if (!canConvert(info.arg2, typeDst))
			{
				return false;
			}
			pgrflt = LiftFlags.Convert2;
		}
		ptypeSig2 = typeDst;
		if (info.type1.IsNullableType())
		{
			pgrflt |= LiftFlags.Lift1;
			ptypeSig1 = GetSymbolLoader().GetTypeManager().GetNullable(info.typeRaw1);
		}
		else
		{
			ptypeSig1 = info.typeRaw1;
		}
		return true;
	}

	private void RecordBinOpSigFromArgs(List<BinOpFullSig> prgbofs, BinOpArgInfo info)
	{
		LiftFlags liftFlags = LiftFlags.None;
		CType type;
		if (info.type1 != info.typeRaw1)
		{
			liftFlags |= LiftFlags.Lift1;
			type = GetSymbolLoader().GetTypeManager().GetNullable(info.typeRaw1);
		}
		else
		{
			type = info.typeRaw1;
		}
		CType type2;
		if (info.type2 != info.typeRaw2)
		{
			liftFlags |= LiftFlags.Lift2;
			type2 = GetSymbolLoader().GetTypeManager().GetNullable(info.typeRaw2);
		}
		else
		{
			type2 = info.typeRaw2;
		}
		prgbofs.Add(new BinOpFullSig(type, type2, BindEnumBinOp, OpSigFlags.Value, liftFlags, BinOpFuncKind.EnumBinOp));
	}

	protected bool GetEnumBinOpSigs(List<BinOpFullSig> prgbofs, BinOpArgInfo info)
	{
		if (!info.typeRaw1.isEnumType() && !info.typeRaw2.isEnumType())
		{
			return false;
		}
		CType ptypeSig = null;
		CType ptypeSig2 = null;
		LiftFlags pgrflt = LiftFlags.None;
		if (info.typeRaw1 == info.typeRaw2)
		{
			if (!info.ValidForEnum())
			{
				return false;
			}
			RecordBinOpSigFromArgs(prgbofs, info);
			return true;
		}
		if ((!info.typeRaw1.isEnumType()) ? (info.typeRaw1 == info.typeRaw2.underlyingEnumType() && info.ValidForUnderlyingTypeAndEnum()) : (info.typeRaw2 == info.typeRaw1.underlyingEnumType() && info.ValidForEnumAndUnderlyingType()))
		{
			RecordBinOpSigFromArgs(prgbofs, info);
			return true;
		}
		if ((!info.typeRaw1.isEnumType()) ? ((info.ValidForEnum() && CanConvertArg1(info, info.typeRaw2, out pgrflt, out ptypeSig, out ptypeSig2)) || (info.ValidForEnumAndUnderlyingType() && CanConvertArg1(info, info.typeRaw2.underlyingEnumType(), out pgrflt, out ptypeSig, out ptypeSig2))) : ((info.ValidForEnum() && CanConvertArg2(info, info.typeRaw1, out pgrflt, out ptypeSig, out ptypeSig2)) || (info.ValidForEnumAndUnderlyingType() && CanConvertArg2(info, info.typeRaw1.underlyingEnumType(), out pgrflt, out ptypeSig, out ptypeSig2))))
		{
			prgbofs.Add(new BinOpFullSig(ptypeSig, ptypeSig2, BindEnumBinOp, OpSigFlags.Value, pgrflt, BinOpFuncKind.EnumBinOp));
		}
		return false;
	}

	protected bool GetPtrBinOpSigs(List<BinOpFullSig> prgbofs, BinOpArgInfo info)
	{
		if (!info.type1.IsPointerType() && !info.type2.IsPointerType())
		{
			return false;
		}
		if (info.type1.IsPointerType() && info.type2.IsPointerType())
		{
			if (info.ValidForVoidPointer())
			{
				prgbofs.Add(new BinOpFullSig(info.type1, info.type2, BindPtrCmpOp, OpSigFlags.None, LiftFlags.None, BinOpFuncKind.PtrCmpOp));
				return true;
			}
			if (info.type1 == info.type2 && info.ValidForPointer())
			{
				prgbofs.Add(new BinOpFullSig(info.type1, info.type2, BindPtrBinOp, OpSigFlags.None, LiftFlags.None, BinOpFuncKind.PtrBinOp));
				return true;
			}
			return false;
		}
		if (info.type1.IsPointerType())
		{
			if (info.type2.IsNullType())
			{
				if (!info.ValidForVoidPointer())
				{
					return false;
				}
				prgbofs.Add(new BinOpFullSig(info.type1, info.type1, BindPtrCmpOp, OpSigFlags.Convert, LiftFlags.None, BinOpFuncKind.PtrCmpOp));
				return true;
			}
			if (!info.ValidForPointerAndNumber())
			{
				return false;
			}
			for (uint num = 0u; num < rgptIntOp.Length; num++)
			{
				CType reqPDT;
				if (canConvert(info.arg2, reqPDT = GetReqPDT(rgptIntOp[num])))
				{
					prgbofs.Add(new BinOpFullSig(info.type1, reqPDT, BindPtrBinOp, OpSigFlags.Convert, LiftFlags.None, BinOpFuncKind.PtrBinOp));
					return true;
				}
			}
			return false;
		}
		if (info.type1.IsNullType())
		{
			if (!info.ValidForVoidPointer())
			{
				return false;
			}
			prgbofs.Add(new BinOpFullSig(info.type2, info.type2, BindPtrCmpOp, OpSigFlags.Convert, LiftFlags.None, BinOpFuncKind.PtrCmpOp));
			return true;
		}
		if (!info.ValidForNumberAndPointer())
		{
			return false;
		}
		for (uint num2 = 0u; num2 < rgptIntOp.Length; num2++)
		{
			CType reqPDT;
			if (canConvert(info.arg1, reqPDT = GetReqPDT(rgptIntOp[num2])))
			{
				prgbofs.Add(new BinOpFullSig(reqPDT, info.type2, BindPtrBinOp, OpSigFlags.Convert, LiftFlags.None, BinOpFuncKind.PtrBinOp));
				return true;
			}
		}
		return false;
	}

	protected bool GetRefEqualSigs(List<BinOpFullSig> prgbofs, BinOpArgInfo info)
	{
		if (info.mask != BinOpMask.Equal)
		{
			return false;
		}
		if (info.type1 != info.typeRaw1 || info.type2 != info.typeRaw2)
		{
			return false;
		}
		bool result = false;
		CType cType = info.type1;
		CType cType2 = info.type2;
		CType reqPDT = GetReqPDT(PredefinedType.PT_OBJECT);
		CType cType3 = null;
		if (cType.IsNullType() && cType2.IsNullType())
		{
			cType3 = reqPDT;
			result = true;
		}
		else
		{
			CType reqPDT2 = GetReqPDT(PredefinedType.PT_DELEGATE);
			if (canConvert(info.arg1, reqPDT2) && canConvert(info.arg2, reqPDT2) && !cType.isDelegateType() && !cType2.isDelegateType())
			{
				prgbofs.Add(new BinOpFullSig(reqPDT2, reqPDT2, BindDelBinOp, OpSigFlags.Convert, LiftFlags.None, BinOpFuncKind.DelBinOp));
			}
			FUNDTYPE fUNDTYPE = cType.fundType();
			FUNDTYPE fUNDTYPE2 = cType2.fundType();
			switch (fUNDTYPE)
			{
			default:
				return false;
			case FUNDTYPE.FT_VAR:
				if (cType.AsTypeParameterType().IsValueType() || (!cType.AsTypeParameterType().IsReferenceType() && !cType2.IsNullType()))
				{
					return false;
				}
				cType = cType.AsTypeParameterType().GetEffectiveBaseClass();
				break;
			case FUNDTYPE.FT_REF:
				break;
			}
			if (cType2.IsNullType())
			{
				result = true;
				cType3 = reqPDT;
			}
			else
			{
				switch (fUNDTYPE2)
				{
				default:
					return false;
				case FUNDTYPE.FT_VAR:
					if (cType2.AsTypeParameterType().IsValueType() || (!cType2.AsTypeParameterType().IsReferenceType() && !cType.IsNullType()))
					{
						return false;
					}
					cType2 = cType2.AsTypeParameterType().GetEffectiveBaseClass();
					break;
				case FUNDTYPE.FT_REF:
					break;
				}
				if (cType.IsNullType())
				{
					result = true;
					cType3 = reqPDT;
				}
				else
				{
					if (!canCast(cType, cType2, CONVERTTYPE.NOUDC) && !canCast(cType2, cType, CONVERTTYPE.NOUDC))
					{
						return false;
					}
					if (cType.isInterfaceType() || cType.isPredefType(PredefinedType.PT_STRING) || GetSymbolLoader().HasBaseConversion(cType, reqPDT2))
					{
						cType = reqPDT;
					}
					else if (cType.IsArrayType())
					{
						cType = GetReqPDT(PredefinedType.PT_ARRAY);
					}
					else if (!cType.isClassType())
					{
						return false;
					}
					if (cType2.isInterfaceType() || cType2.isPredefType(PredefinedType.PT_STRING) || GetSymbolLoader().HasBaseConversion(cType2, reqPDT2))
					{
						cType2 = reqPDT;
					}
					else if (cType2.IsArrayType())
					{
						cType2 = GetReqPDT(PredefinedType.PT_ARRAY);
					}
					else if (!cType2.isClassType())
					{
						return false;
					}
					if (GetSymbolLoader().HasBaseConversion(cType2, cType))
					{
						cType3 = cType;
					}
					else if (GetSymbolLoader().HasBaseConversion(cType, cType2))
					{
						cType3 = cType2;
					}
				}
			}
		}
		prgbofs.Add(new BinOpFullSig(cType3, cType3, BindRefCmpOp, OpSigFlags.None, LiftFlags.None, BinOpFuncKind.RefCmpOp));
		return result;
	}

	private int WhichBofsIsBetter(BinOpFullSig bofs1, BinOpFullSig bofs2, CType type1, CType type2)
	{
		BetterType betterType;
		BetterType betterType2;
		if (bofs1.FPreDef() && bofs2.FPreDef())
		{
			betterType = WhichTypeIsBetter(bofs1.pt1, bofs2.pt1, type1);
			betterType2 = WhichTypeIsBetter(bofs1.pt2, bofs2.pt2, type2);
		}
		else
		{
			betterType = WhichTypeIsBetter(bofs1.Type1(), bofs2.Type1(), type1);
			betterType2 = WhichTypeIsBetter(bofs1.Type2(), bofs2.Type2(), type2);
		}
		int num = 0;
		switch (betterType)
		{
		default:
			VSFAIL("Shouldn't happen");
			break;
		case BetterType.Left:
			num--;
			break;
		case BetterType.Right:
			num++;
			break;
		case BetterType.Same:
		case BetterType.Neither:
			break;
		}
		switch (betterType2)
		{
		default:
			VSFAIL("Shouldn't happen");
			break;
		case BetterType.Left:
			num--;
			break;
		case BetterType.Right:
			num++;
			break;
		case BetterType.Same:
		case BetterType.Neither:
			break;
		}
		return num;
	}

	private static bool CalculateExprAndUnaryOpKinds(OperatorKind op, bool bChecked, out ExpressionKind ek, out UnaOpKind uok, out EXPRFLAG flags)
	{
		flags = (EXPRFLAG)0;
		ek = ExpressionKind.EK_BLOCK;
		uok = UnaOpKind.Plus;
		switch (op)
		{
		case OperatorKind.OP_UPLUS:
			uok = UnaOpKind.Plus;
			ek = ExpressionKind.EK_UPLUS;
			break;
		case OperatorKind.OP_NEG:
			if (bChecked)
			{
				flags |= EXPRFLAG.EXF_CHECKOVERFLOW;
			}
			uok = UnaOpKind.Minus;
			ek = ExpressionKind.EK_NEG;
			break;
		case OperatorKind.OP_BITNOT:
			uok = UnaOpKind.Tilde;
			ek = ExpressionKind.EK_BITNOT;
			break;
		case OperatorKind.OP_LOGNOT:
			uok = UnaOpKind.Bang;
			ek = ExpressionKind.EK_LOGNOT;
			break;
		case OperatorKind.OP_POSTINC:
			flags |= EXPRFLAG.EXF_OPERATOR;
			if (bChecked)
			{
				flags |= EXPRFLAG.EXF_CHECKOVERFLOW;
			}
			uok = UnaOpKind.IncDec;
			ek = ExpressionKind.EK_ADD;
			break;
		case OperatorKind.OP_PREINC:
			if (bChecked)
			{
				flags |= EXPRFLAG.EXF_CHECKOVERFLOW;
			}
			uok = UnaOpKind.IncDec;
			ek = ExpressionKind.EK_ADD;
			break;
		case OperatorKind.OP_POSTDEC:
			flags |= EXPRFLAG.EXF_OPERATOR;
			if (bChecked)
			{
				flags |= EXPRFLAG.EXF_CHECKOVERFLOW;
			}
			uok = UnaOpKind.IncDec;
			ek = ExpressionKind.EK_SUB;
			break;
		case OperatorKind.OP_PREDEC:
			if (bChecked)
			{
				flags |= EXPRFLAG.EXF_CHECKOVERFLOW;
			}
			uok = UnaOpKind.IncDec;
			ek = ExpressionKind.EK_SUB;
			break;
		default:
			VSFAIL("Bad op");
			return false;
		}
		return true;
	}

	public EXPR BindStandardUnaryOperator(OperatorKind op, EXPR pArgument)
	{
		RETAILVERIFY(pArgument != null);
		if (pArgument.type == null || !CalculateExprAndUnaryOpKinds(op, Context.CheckedNormal, out var ek, out var uok, out var flags))
		{
			return BadOperatorTypesError(ExpressionKind.EK_UNARYOP, pArgument, null);
		}
		UnaOpMask unaryOpMask = (UnaOpMask)(1 << (int)uok);
		CType type = pArgument.type;
		List<UnaOpFullSig> list = new List<UnaOpFullSig>();
		EXPR ppResult = null;
		UnaryOperatorSignatureFindResult unaryOperatorSignatureFindResult = PopulateSignatureList(pArgument, uok, unaryOpMask, ek, flags, list, out ppResult);
		int num = list.Count - 1;
		switch (unaryOperatorSignatureFindResult)
		{
		case UnaryOperatorSignatureFindResult.Return:
			return ppResult;
		default:
			if (!FindApplicableSignatures(pArgument, unaryOpMask, list))
			{
				if (list.Count == 0)
				{
					return BadOperatorTypesError(ek, pArgument, null);
				}
				num = 0;
				if (list.Count == 1)
				{
					break;
				}
				for (int i = 1; i < list.Count; i++)
				{
					if (num < 0)
					{
						num = i;
						continue;
					}
					int num2 = WhichUofsIsBetter(list[num], list[i], type);
					if (num2 == 0)
					{
						num = -1;
					}
					else if (num2 > 0)
					{
						num = i;
					}
				}
				if (num < 0)
				{
					return ambiguousOperatorError(ek, pArgument, null);
				}
				for (int j = 0; j < list.Count; j++)
				{
					if (j != num && WhichUofsIsBetter(list[num], list[j], type) >= 0)
					{
						return ambiguousOperatorError(ek, pArgument, null);
					}
				}
			}
			else
			{
				num = list.Count - 1;
			}
			break;
		case UnaryOperatorSignatureFindResult.Match:
			break;
		}
		RETAILVERIFY(num < list.Count);
		UnaOpFullSig unaOpFullSig = list[num];
		if (unaOpFullSig.pfn == null)
		{
			if (uok == UnaOpKind.IncDec)
			{
				return BindIncOp(ek, flags, pArgument, unaOpFullSig);
			}
			return BadOperatorTypesError(ek, pArgument, null);
		}
		if (unaOpFullSig.isLifted())
		{
			return BindLiftedStandardUnop(ek, flags, pArgument, unaOpFullSig);
		}
		EXPR eXPR = tryConvert(pArgument, unaOpFullSig.GetType());
		if (eXPR == null)
		{
			eXPR = mustCast(pArgument, unaOpFullSig.GetType(), CONVERTTYPE.NOUDC);
		}
		return unaOpFullSig.pfn(ek, flags, eXPR);
	}

	private UnaryOperatorSignatureFindResult PopulateSignatureList(EXPR pArgument, UnaOpKind unaryOpKind, UnaOpMask unaryOpMask, ExpressionKind exprKind, EXPRFLAG flags, List<UnaOpFullSig> pSignatures, out EXPR ppResult)
	{
		ppResult = null;
		CType type = pArgument.type;
		CType cType = type.StripNubs();
		PredefinedType predefinedType = (cType.isPredefined() ? cType.getPredefType() : PredefinedType.PT_COUNT);
		if (predefinedType > PredefinedType.PT_ULONG)
		{
			if (cType.isEnumType())
			{
				if ((unaryOpMask & (UnaOpMask)20) != 0)
				{
					LiftFlags grflt = LiftFlags.None;
					CType cType2 = type;
					if (cType2.IsNullableType())
					{
						if (cType2.AsNullableType().GetUnderlyingType() != cType)
						{
							cType2 = GetSymbolLoader().GetTypeManager().GetNullable(cType);
						}
						grflt = LiftFlags.Lift1;
					}
					if (unaryOpKind == UnaOpKind.Tilde)
					{
						pSignatures.Add(new UnaOpFullSig(cType2.getAggregate().GetUnderlyingType(), BindEnumUnaOp, grflt, UnaOpFuncKind.EnumUnaOp));
					}
					else
					{
						pSignatures.Add(new UnaOpFullSig(cType2.getAggregate().GetUnderlyingType(), null, grflt, UnaOpFuncKind.None));
					}
					return UnaryOperatorSignatureFindResult.Match;
				}
			}
			else if (unaryOpKind == UnaOpKind.IncDec)
			{
				if (type.IsPointerType())
				{
					pSignatures.Add(new UnaOpFullSig(type, null, LiftFlags.None, UnaOpFuncKind.None));
					return UnaryOperatorSignatureFindResult.Match;
				}
				EXPRMULTIGET eXPRMULTIGET = GetExprFactory().CreateMultiGet((EXPRFLAG)0, type, null);
				EXPR eXPR = bindUDUnop(exprKind - 55 + 46, eXPRMULTIGET);
				if (eXPR != null)
				{
					if (eXPR.type != null && !eXPR.type.IsErrorType() && eXPR.type != type)
					{
						eXPR = mustConvert(eXPR, type);
					}
					EXPRMULTI eXPRMULTI = GetExprFactory().CreateMulti(EXPRFLAG.EXF_ASSGOP | flags, type, pArgument, eXPR);
					eXPRMULTIGET.SetOptionalMulti(eXPRMULTI);
					if (!checkLvalue(pArgument, CheckLvalueKind.Increment))
					{
						eXPRMULTI.SetError();
					}
					ppResult = eXPRMULTI;
					return UnaryOperatorSignatureFindResult.Return;
				}
			}
			else
			{
				EXPR eXPR2 = bindUDUnop(exprKind, pArgument);
				if (eXPR2 != null)
				{
					ppResult = eXPR2;
					return UnaryOperatorSignatureFindResult.Return;
				}
			}
		}
		return UnaryOperatorSignatureFindResult.Continue;
	}

	private bool FindApplicableSignatures(EXPR pArgument, UnaOpMask unaryOpMask, List<UnaOpFullSig> pSignatures)
	{
		long num = ((!GetSymbolLoader().FCanLift()) ? g_rguos.Length : 0);
		CType type = pArgument.type;
		CType cType = type.StripNubs();
		PredefinedType ptSrc = (type.isPredefined() ? type.getPredefType() : PredefinedType.PT_COUNT);
		PredefinedType ptSrc2 = (cType.isPredefined() ? cType.getPredefType() : PredefinedType.PT_COUNT);
		for (int i = 0; i < g_rguos.Length; i++)
		{
			UnaOpSig unaOpSig = g_rguos[i];
			if ((unaOpSig.grfuom & unaryOpMask) == 0)
			{
				continue;
			}
			ConvKind convKind = GetConvKind(ptSrc, g_rguos[i].pt);
			CType cType2 = null;
			switch (convKind)
			{
			default:
				VSFAIL("Shouldn't happen!");
				continue;
			case ConvKind.Explicit:
				if (!pArgument.isCONSTANT_OK())
				{
					continue;
				}
				if (!canConvert(pArgument, cType2 = GetOptPDT(unaOpSig.pt)))
				{
					if (i < num)
					{
						continue;
					}
					cType2 = GetSymbolLoader().GetTypeManager().GetNullable(cType2);
					if (!canConvert(pArgument, cType2))
					{
						continue;
					}
				}
				break;
			case ConvKind.Unknown:
				if (!canConvert(pArgument, cType2 = GetOptPDT(unaOpSig.pt)))
				{
					if (i < num)
					{
						continue;
					}
					cType2 = GetSymbolLoader().GetTypeManager().GetNullable(cType2);
					if (!canConvert(pArgument, cType2))
					{
						continue;
					}
				}
				break;
			case ConvKind.Identity:
			{
				UnaOpFullSig unaOpFullSig = new UnaOpFullSig(this, unaOpSig);
				if (unaOpFullSig.GetType() != null)
				{
					pSignatures.Add(unaOpFullSig);
					return true;
				}
				break;
			}
			case ConvKind.Implicit:
				break;
			case ConvKind.None:
				continue;
			}
			if (cType2 != null && cType2.IsNullableType())
			{
				LiftFlags liftFlags = LiftFlags.None;
				ConvKind convKind2 = GetConvKind(ptSrc2, unaOpSig.pt);
				liftFlags = (((uint)(convKind2 - 1) <= 1u) ? (liftFlags | LiftFlags.Lift1) : (liftFlags | LiftFlags.Convert1));
				pSignatures.Add(new UnaOpFullSig(cType2, unaOpSig.pfn, liftFlags, unaOpSig.fnkind));
				num = i + unaOpSig.cuosSkip + 1;
			}
			else
			{
				UnaOpFullSig unaOpFullSig2 = new UnaOpFullSig(this, unaOpSig);
				if (unaOpFullSig2.GetType() != null)
				{
					pSignatures.Add(unaOpFullSig2);
				}
				i += unaOpSig.cuosSkip;
			}
		}
		return false;
	}

	private EXPR BindLiftedStandardUnop(ExpressionKind ek, EXPRFLAG flags, EXPR arg, UnaOpFullSig uofs)
	{
		NullableType nullableType = uofs.GetType().AsNullableType();
		if (arg.type.IsNullType())
		{
			return BadOperatorTypesError(ek, arg, null, nullableType);
		}
		EXPR ppLiftedArgument = null;
		EXPR ppNonLiftedArgument = null;
		LiftArgument(arg, uofs.GetType(), uofs.Convert(), out ppLiftedArgument, out ppNonLiftedArgument);
		EXPR expr = uofs.pfn(ek, flags, ppNonLiftedArgument);
		EXPRUNARYOP eXPRUNARYOP = GetExprFactory().CreateUnaryOp(ek, nullableType, ppLiftedArgument);
		mustCast(expr, nullableType, (CONVERTTYPE)0);
		eXPRUNARYOP.flags |= flags;
		return eXPRUNARYOP;
	}

	private int WhichUofsIsBetter(UnaOpFullSig uofs1, UnaOpFullSig uofs2, CType typeArg)
	{
		switch ((!uofs1.FPreDef() || !uofs2.FPreDef()) ? WhichTypeIsBetter(uofs1.GetType(), uofs2.GetType(), typeArg) : WhichTypeIsBetter(uofs1.pt, uofs2.pt, typeArg))
		{
		default:
			VSFAIL("Shouldn't happen");
			return 0;
		case BetterType.Same:
		case BetterType.Neither:
			return 0;
		case BetterType.Left:
			return -1;
		case BetterType.Right:
			return 1;
		}
	}

	private EXPR BindIntBinOp(ExpressionKind ek, EXPRFLAG flags, EXPR arg1, EXPR arg2)
	{
		return BindIntOp(ek, flags, arg1, arg2, arg1.type.getPredefType());
	}

	private EXPR BindIntUnaOp(ExpressionKind ek, EXPRFLAG flags, EXPR arg)
	{
		return BindIntOp(ek, flags, arg, null, arg.type.getPredefType());
	}

	private EXPR BindRealBinOp(ExpressionKind ek, EXPRFLAG flags, EXPR arg1, EXPR arg2)
	{
		return bindFloatOp(ek, flags, arg1, arg2);
	}

	private EXPR BindRealUnaOp(ExpressionKind ek, EXPRFLAG flags, EXPR arg)
	{
		return bindFloatOp(ek, flags, arg, null);
	}

	private EXPR BindIncOp(ExpressionKind ek, EXPRFLAG flags, EXPR arg, UnaOpFullSig uofs)
	{
		if (!checkLvalue(arg, CheckLvalueKind.Increment))
		{
			EXPR eXPR = GetExprFactory().CreateBinop(ek, arg.type, arg, null);
			eXPR.SetError();
			return eXPR;
		}
		CType cType = uofs.GetType().StripNubs();
		FUNDTYPE fUNDTYPE = cType.fundType();
		if (fUNDTYPE == FUNDTYPE.FT_R8 || fUNDTYPE == FUNDTYPE.FT_R4)
		{
			flags = (EXPRFLAG)(-262145);
		}
		if (uofs.isLifted())
		{
			return BindLiftedIncOp(ek, flags, arg, uofs);
		}
		return BindNonliftedIncOp(ek, flags, arg, uofs);
	}

	private EXPR BindIncOpCore(ExpressionKind ek, EXPRFLAG flags, EXPR exprVal, CType type)
	{
		CONSTVAL cONSTVAL = new CONSTVAL();
		EXPR pExprResult = null;
		if (type.isEnumType() && type.fundType() > FUNDTYPE.FT_U8)
		{
			type = GetReqPDT(PredefinedType.PT_INT);
		}
		FUNDTYPE fUNDTYPE = type.fundType();
		CType typeTmp = type;
		switch (fUNDTYPE)
		{
		default:
		{
			ek = ((ek == ExpressionKind.EK_ADD) ? ExpressionKind.EK_DECIMALINC : ExpressionKind.EK_DECIMALDEC);
			PREDEFMETH predefMeth = ((ek == ExpressionKind.EK_DECIMALINC) ? PREDEFMETH.PM_DECIMAL_OPINCREMENT : PREDEFMETH.PM_DECIMAL_OPDECREMENT);
			return CreateUnaryOpForPredefMethodCall(ek, predefMeth, type, exprVal);
		}
		case FUNDTYPE.FT_PTR:
			cONSTVAL.iVal = 1;
			return BindPtrBinOp(ek, flags, exprVal, GetExprFactory().CreateConstant(GetReqPDT(PredefinedType.PT_INT), cONSTVAL));
		case FUNDTYPE.FT_I1:
		case FUNDTYPE.FT_I2:
		case FUNDTYPE.FT_U1:
		case FUNDTYPE.FT_U2:
			typeTmp = GetReqPDT(PredefinedType.PT_INT);
			cONSTVAL.iVal = 1;
			return LScalar(ek, flags, exprVal, type, cONSTVAL, pExprResult, typeTmp);
		case FUNDTYPE.FT_I4:
		case FUNDTYPE.FT_U4:
			cONSTVAL.iVal = 1;
			return LScalar(ek, flags, exprVal, type, cONSTVAL, pExprResult, typeTmp);
		case FUNDTYPE.FT_I8:
		case FUNDTYPE.FT_U8:
			cONSTVAL = GetExprConstants().Create(1L);
			return LScalar(ek, flags, exprVal, type, cONSTVAL, pExprResult, typeTmp);
		case FUNDTYPE.FT_R4:
		case FUNDTYPE.FT_R8:
			cONSTVAL = GetExprConstants().Create(1.0);
			return LScalar(ek, flags, exprVal, type, cONSTVAL, pExprResult, typeTmp);
		}
	}

	private EXPR LScalar(ExpressionKind ek, EXPRFLAG flags, EXPR exprVal, CType type, CONSTVAL cv, EXPR pExprResult, CType typeTmp)
	{
		CType cType = type;
		if (cType.isEnumType())
		{
			cType = cType.underlyingEnumType();
		}
		pExprResult = GetExprFactory().CreateBinop(ek, typeTmp, exprVal, GetExprFactory().CreateConstant(cType, cv));
		pExprResult.flags |= flags;
		if (typeTmp != type)
		{
			pExprResult = mustCast(pExprResult, type, CONVERTTYPE.NOUDC);
		}
		return pExprResult;
	}

	private EXPRMULTI BindNonliftedIncOp(ExpressionKind ek, EXPRFLAG flags, EXPR arg, UnaOpFullSig uofs)
	{
		EXPRMULTIGET eXPRMULTIGET = GetExprFactory().CreateMultiGet(EXPRFLAG.EXF_ASSGOP, arg.type, null);
		EXPR expr = eXPRMULTIGET;
		CType type = uofs.GetType();
		expr = mustCast(expr, type);
		expr = BindIncOpCore(ek, flags, expr, type);
		EXPR pOp = mustCast(expr, arg.type, CONVERTTYPE.NOUDC);
		EXPRMULTI eXPRMULTI = GetExprFactory().CreateMulti(EXPRFLAG.EXF_ASSGOP | flags, arg.type, arg, pOp);
		eXPRMULTIGET.SetOptionalMulti(eXPRMULTI);
		return eXPRMULTI;
	}

	private EXPRMULTI BindLiftedIncOp(ExpressionKind ek, EXPRFLAG flags, EXPR arg, UnaOpFullSig uofs)
	{
		NullableType nullableType = uofs.GetType().AsNullableType();
		EXPRMULTIGET eXPRMULTIGET = GetExprFactory().CreateMultiGet(EXPRFLAG.EXF_ASSGOP, arg.type, null);
		EXPR eXPR = eXPRMULTIGET;
		EXPR eXPR2 = null;
		EXPR expr = eXPR;
		expr = mustCast(expr, nullableType.GetUnderlyingType());
		eXPR2 = BindIncOpCore(ek, flags, expr, nullableType.GetUnderlyingType());
		eXPR = mustCast(eXPR, nullableType);
		EXPRUNARYOP eXPRUNARYOP = GetExprFactory().CreateUnaryOp((ek == ExpressionKind.EK_ADD) ? ExpressionKind.EK_INC : ExpressionKind.EK_DEC, arg.type, eXPR);
		mustCast(mustCast(eXPR2, nullableType), arg.type);
		eXPRUNARYOP.flags |= flags;
		EXPRMULTI eXPRMULTI = GetExprFactory().CreateMulti(EXPRFLAG.EXF_ASSGOP | flags, arg.type, arg, eXPRUNARYOP);
		eXPRMULTIGET.SetOptionalMulti(eXPRMULTI);
		return eXPRMULTI;
	}

	private EXPR BindDecBinOp(ExpressionKind ek, EXPRFLAG flags, EXPR arg1, EXPR arg2)
	{
		CType optPDT = GetOptPDT(PredefinedType.PT_DECIMAL);
		EXPR @const = arg1.GetConst();
		EXPR const2 = arg2.GetConst();
		CType pType = null;
		switch (ek)
		{
		default:
			VSFAIL("Bad kind");
			break;
		case ExpressionKind.EK_ADD:
		case ExpressionKind.EK_SUB:
		case ExpressionKind.EK_MUL:
		case ExpressionKind.EK_DIV:
		case ExpressionKind.EK_MOD:
			pType = optPDT;
			break;
		case ExpressionKind.EK_EQ:
		case ExpressionKind.EK_NE:
		case ExpressionKind.EK_LT:
		case ExpressionKind.EK_LE:
		case ExpressionKind.EK_GT:
		case ExpressionKind.EK_GE:
			pType = GetReqPDT(PredefinedType.PT_BOOL);
			break;
		}
		if (const2 == null || @const == null)
		{
			return GetExprFactory().CreateBinop(ek, pType, arg1, arg2);
		}
		decimal value = default(decimal);
		bool value2 = false;
		bool flag = false;
		decimal decVal = @const.asCONSTANT().getVal().decVal;
		decimal decVal2 = const2.asCONSTANT().getVal().decVal;
		switch (ek)
		{
		case ExpressionKind.EK_ADD:
			value = decVal + decVal2;
			break;
		case ExpressionKind.EK_SUB:
			value = decVal - decVal2;
			break;
		case ExpressionKind.EK_MUL:
			value = decVal * decVal2;
			break;
		case ExpressionKind.EK_DIV:
			if (decVal2 == 0m)
			{
				GetErrorContext().Error(ErrorCode.ERR_IntDivByZero);
				EXPR eXPR2 = GetExprFactory().CreateBinop(ek, optPDT, arg1, arg2);
				eXPR2.SetError();
				return eXPR2;
			}
			value = decVal / decVal2;
			break;
		case ExpressionKind.EK_MOD:
		{
			if (decVal2 == 0m)
			{
				GetErrorContext().Error(ErrorCode.ERR_IntDivByZero);
				EXPR eXPR = GetExprFactory().CreateBinop(ek, optPDT, arg1, arg2);
				eXPR.SetError();
				return eXPR;
			}
			decimal num = decVal % decVal2;
			break;
		}
		default:
			flag = true;
			switch (ek)
			{
			default:
				VSFAIL("Bad ek");
				break;
			case ExpressionKind.EK_EQ:
				value2 = decVal == decVal2;
				break;
			case ExpressionKind.EK_NE:
				value2 = decVal != decVal2;
				break;
			case ExpressionKind.EK_LE:
				value2 = decVal <= decVal2;
				break;
			case ExpressionKind.EK_LT:
				value2 = decVal < decVal2;
				break;
			case ExpressionKind.EK_GE:
				value2 = decVal >= decVal2;
				break;
			case ExpressionKind.EK_GT:
				value2 = decVal > decVal2;
				break;
			}
			break;
		}
		CONSTVAL @bool;
		if (flag)
		{
			@bool = ConstValFactory.GetBool(value2);
			return GetExprFactory().CreateConstant(GetReqPDT(PredefinedType.PT_BOOL), @bool);
		}
		@bool = GetExprConstants().Create(value);
		return GetExprFactory().CreateConstant(optPDT, @bool);
	}

	private EXPR BindDecUnaOp(ExpressionKind ek, EXPRFLAG flags, EXPR arg)
	{
		CType optPDT = GetOptPDT(PredefinedType.PT_DECIMAL);
		ek = ((ek == ExpressionKind.EK_NEG) ? ExpressionKind.EK_DECIMALNEG : ExpressionKind.EK_UPLUS);
		EXPR @const = arg.GetConst();
		if (@const == null)
		{
			if (ek == ExpressionKind.EK_DECIMALNEG)
			{
				PREDEFMETH predefMeth = PREDEFMETH.PM_DECIMAL_OPUNARYMINUS;
				return CreateUnaryOpForPredefMethodCall(ek, predefMeth, optPDT, arg);
			}
			return GetExprFactory().CreateUnaryOp(ek, optPDT, arg);
		}
		if (ek == ExpressionKind.EK_UPLUS)
		{
			return arg;
		}
		decimal decVal = @const.asCONSTANT().getVal().decVal;
		decVal *= -1m;
		CONSTVAL constVal = GetExprConstants().Create(decVal);
		return GetExprFactory().CreateConstant(optPDT, constVal);
	}

	private EXPR BindStrBinOp(ExpressionKind ek, EXPRFLAG flags, EXPR arg1, EXPR arg2)
	{
		return bindStringConcat(arg1, arg2);
	}

	private EXPR BindShiftOp(ExpressionKind ek, EXPRFLAG flags, EXPR arg1, EXPR arg2)
	{
		PredefinedType predefType = arg1.type.getPredefType();
		EXPR @const = arg1.GetConst();
		EXPR const2 = arg2.GetConst();
		if (@const == null || const2 == null)
		{
			return GetExprFactory().CreateBinop(ek, arg1.type, arg1, arg2);
		}
		CONSTVAL cONSTVAL = new CONSTVAL();
		int num = ((predefType == PredefinedType.PT_LONG || predefType == PredefinedType.PT_ULONG) ? 63 : 31);
		cONSTVAL.iVal = const2.asCONSTANT().getVal().iVal & num;
		num = cONSTVAL.iVal;
		if (predefType == PredefinedType.PT_LONG || predefType == PredefinedType.PT_ULONG)
		{
			ulong ulongVal = @const.asCONSTANT().getVal().ulongVal;
			ulong value;
			switch (ek)
			{
			case ExpressionKind.EK_LSHIFT:
				value = ulongVal << num;
				break;
			case ExpressionKind.EK_RSHIFT:
				value = ((predefType == PredefinedType.PT_LONG) ? ((ulong)((long)ulongVal >> num)) : (ulongVal >> num));
				break;
			default:
				VSFAIL("Unknown op");
				value = 0uL;
				break;
			}
			cONSTVAL = GetExprConstants().Create(value);
		}
		else
		{
			uint uiVal = @const.asCONSTANT().getVal().uiVal;
			switch (ek)
			{
			case ExpressionKind.EK_LSHIFT:
				cONSTVAL.uiVal = uiVal << num;
				break;
			case ExpressionKind.EK_RSHIFT:
				cONSTVAL.uiVal = ((predefType == PredefinedType.PT_INT) ? ((uint)((int)uiVal >> num)) : (uiVal >> num));
				break;
			default:
				VSFAIL("Unknown op");
				cONSTVAL.uiVal = 0u;
				break;
			}
		}
		return GetExprFactory().CreateConstant(GetReqPDT(predefType), cONSTVAL);
	}

	private EXPR BindBoolBinOp(ExpressionKind ek, EXPRFLAG flags, EXPR arg1, EXPR arg2)
	{
		return GetExprFactory().CreateBinop(ek, GetReqPDT(PredefinedType.PT_BOOL), arg1, arg2);
	}

	private EXPR BindBoolBitwiseOp(ExpressionKind ek, EXPRFLAG flags, EXPR expr1, EXPR expr2, BinOpFullSig bofs)
	{
		if (expr1.type.IsNullableType() || expr2.type.IsNullableType())
		{
			CType reqPDT = GetReqPDT(PredefinedType.PT_BOOL);
			CType nullable = GetSymbolLoader().GetTypeManager().GetNullable(reqPDT);
			EXPR eXPR = CNullable.StripNullableConstructor(expr1);
			EXPR eXPR2 = CNullable.StripNullableConstructor(expr2);
			EXPR eXPR3 = null;
			if (!eXPR.type.IsNullableType() && !eXPR2.type.IsNullableType())
			{
				eXPR3 = BindBoolBinOp(ek, flags, eXPR, eXPR2);
			}
			EXPRBINOP eXPRBINOP = GetExprFactory().CreateBinop(ek, nullable, expr1, expr2);
			if (eXPR3 != null)
			{
				mustCast(eXPR3, nullable, (CONVERTTYPE)0);
			}
			eXPRBINOP.isLifted = true;
			eXPRBINOP.flags |= flags;
			return eXPRBINOP;
		}
		return BindBoolBinOp(ek, flags, expr1, expr2);
	}

	private EXPR BindLiftedBoolBitwiseOp(ExpressionKind ek, EXPRFLAG flags, EXPR expr1, EXPR expr2)
	{
		return null;
	}

	private EXPR BindBoolUnaOp(ExpressionKind ek, EXPRFLAG flags, EXPR arg)
	{
		CType reqPDT = GetReqPDT(PredefinedType.PT_BOOL);
		EXPR @const = arg.GetConst();
		if (@const == null)
		{
			return GetExprFactory().CreateUnaryOp(ExpressionKind.EK_LOGNOT, reqPDT, arg);
		}
		bool flag = @const.asCONSTANT().getVal().iVal != 0;
		return GetExprFactory().CreateConstant(reqPDT, ConstValFactory.GetBool(!flag));
	}

	private EXPR BindStrCmpOp(ExpressionKind ek, EXPRFLAG flags, EXPR arg1, EXPR arg2)
	{
		PREDEFMETH predefMeth = ((ek == ExpressionKind.EK_EQ) ? PREDEFMETH.PM_STRING_OPEQUALITY : PREDEFMETH.PM_STRING_OPINEQUALITY);
		ek = ((ek == ExpressionKind.EK_EQ) ? ExpressionKind.EK_STRINGEQ : ExpressionKind.EK_STRINGNE);
		return CreateBinopForPredefMethodCall(ek, predefMeth, GetReqPDT(PredefinedType.PT_BOOL), arg1, arg2);
	}

	private EXPR BindRefCmpOp(ExpressionKind ek, EXPRFLAG flags, EXPR arg1, EXPR arg2)
	{
		arg1 = mustConvert(arg1, GetReqPDT(PredefinedType.PT_OBJECT), CONVERTTYPE.NOUDC);
		arg2 = mustConvert(arg2, GetReqPDT(PredefinedType.PT_OBJECT), CONVERTTYPE.NOUDC);
		return GetExprFactory().CreateBinop(ek, GetReqPDT(PredefinedType.PT_BOOL), arg1, arg2);
	}

	private EXPR BindDelBinOp(ExpressionKind ek, EXPRFLAG flags, EXPR arg1, EXPR arg2)
	{
		PREDEFMETH predefMeth = PREDEFMETH.PM_FIRST;
		CType retType = null;
		switch (ek)
		{
		case ExpressionKind.EK_ADD:
			predefMeth = PREDEFMETH.PM_DELEGATE_COMBINE;
			retType = arg1.type;
			ek = ExpressionKind.EK_DELEGATEADD;
			break;
		case ExpressionKind.EK_SUB:
			predefMeth = PREDEFMETH.PM_DELEGATE_REMOVE;
			retType = arg1.type;
			ek = ExpressionKind.EK_DELEGATESUB;
			break;
		case ExpressionKind.EK_EQ:
			predefMeth = PREDEFMETH.PM_DELEGATE_OPEQUALITY;
			retType = GetReqPDT(PredefinedType.PT_BOOL);
			ek = ExpressionKind.EK_DELEGATEEQ;
			break;
		case ExpressionKind.EK_NE:
			predefMeth = PREDEFMETH.PM_DELEGATE_OPINEQUALITY;
			retType = GetReqPDT(PredefinedType.PT_BOOL);
			ek = ExpressionKind.EK_DELEGATENE;
			break;
		}
		return CreateBinopForPredefMethodCall(ek, predefMeth, retType, arg1, arg2);
	}

	private EXPR BindEnumBinOp(ExpressionKind ek, EXPRFLAG flags, EXPR arg1, EXPR arg2)
	{
		AggregateType ppEnumType = null;
		AggregateType enumBinOpType = GetEnumBinOpType(ek, arg1.type, arg2.type, out ppEnumType);
		PredefinedType predefinedType = ppEnumType.fundType() switch
		{
			FUNDTYPE.FT_U4 => PredefinedType.PT_UINT, 
			FUNDTYPE.FT_I8 => PredefinedType.PT_LONG, 
			FUNDTYPE.FT_U8 => PredefinedType.PT_ULONG, 
			_ => PredefinedType.PT_INT, 
		};
		CType reqPDT = GetReqPDT(predefinedType);
		arg1 = mustCast(arg1, reqPDT, CONVERTTYPE.NOUDC);
		arg2 = mustCast(arg2, reqPDT, CONVERTTYPE.NOUDC);
		EXPR eXPR = BindIntOp(ek, flags, arg1, arg2, predefinedType);
		if (!eXPR.isOK())
		{
			return eXPR;
		}
		if (eXPR.type != enumBinOpType)
		{
			eXPR = mustCast(eXPR, enumBinOpType, CONVERTTYPE.NOUDC);
		}
		return eXPR;
	}

	private EXPR BindEnumUnaOp(ExpressionKind ek, EXPRFLAG flags, EXPR arg)
	{
		CType type = arg.asCAST().GetArgument().type;
		PredefinedType predefinedType = type.fundType() switch
		{
			FUNDTYPE.FT_U4 => PredefinedType.PT_UINT, 
			FUNDTYPE.FT_I8 => PredefinedType.PT_LONG, 
			FUNDTYPE.FT_U8 => PredefinedType.PT_ULONG, 
			_ => PredefinedType.PT_INT, 
		};
		CType reqPDT = GetReqPDT(predefinedType);
		arg = mustCast(arg, reqPDT, CONVERTTYPE.NOUDC);
		EXPR eXPR = BindIntOp(ek, flags, arg, null, predefinedType);
		if (!eXPR.isOK())
		{
			return eXPR;
		}
		return mustCastInUncheckedContext(eXPR, type, CONVERTTYPE.NOUDC);
	}

	private EXPR BindPtrBinOp(ExpressionKind ek, EXPRFLAG flags, EXPR arg1, EXPR arg2)
	{
		return null;
	}

	private EXPR BindPtrCmpOp(ExpressionKind ek, EXPRFLAG flags, EXPR arg1, EXPR arg2)
	{
		return null;
	}

	private bool GetBinopKindAndFlags(ExpressionKind ek, out BinOpKind pBinopKind, out EXPRFLAG flags)
	{
		flags = (EXPRFLAG)0;
		switch (ek)
		{
		case ExpressionKind.EK_ADD:
			if (Context.CheckedNormal)
			{
				flags |= EXPRFLAG.EXF_CHECKOVERFLOW;
			}
			pBinopKind = BinOpKind.Add;
			break;
		case ExpressionKind.EK_SUB:
			if (Context.CheckedNormal)
			{
				flags |= EXPRFLAG.EXF_CHECKOVERFLOW;
			}
			pBinopKind = BinOpKind.Sub;
			break;
		case ExpressionKind.EK_DIV:
		case ExpressionKind.EK_MOD:
			flags |= EXPRFLAG.EXF_ASSGOP;
			if (Context.CheckedNormal)
			{
				flags |= EXPRFLAG.EXF_CHECKOVERFLOW;
			}
			pBinopKind = BinOpKind.Mul;
			break;
		case ExpressionKind.EK_MUL:
			if (Context.CheckedNormal)
			{
				flags |= EXPRFLAG.EXF_CHECKOVERFLOW;
			}
			pBinopKind = BinOpKind.Mul;
			break;
		case ExpressionKind.EK_BITAND:
		case ExpressionKind.EK_BITOR:
			pBinopKind = BinOpKind.Bitwise;
			break;
		case ExpressionKind.EK_BITXOR:
			pBinopKind = BinOpKind.BitXor;
			break;
		case ExpressionKind.EK_LSHIFT:
		case ExpressionKind.EK_RSHIFT:
			pBinopKind = BinOpKind.Shift;
			break;
		case ExpressionKind.EK_LOGAND:
		case ExpressionKind.EK_LOGOR:
			pBinopKind = BinOpKind.Logical;
			break;
		case ExpressionKind.EK_LT:
		case ExpressionKind.EK_LE:
		case ExpressionKind.EK_GT:
		case ExpressionKind.EK_GE:
			pBinopKind = BinOpKind.Compare;
			break;
		case ExpressionKind.EK_EQ:
		case ExpressionKind.EK_NE:
			pBinopKind = BinOpKind.Equal;
			break;
		default:
			VSFAIL("Bad ek");
			pBinopKind = BinOpKind.Add;
			return false;
		}
		return true;
	}

	private static bool isDivByZero(ExpressionKind kind, EXPR op2)
	{
		return false;
	}

	private EXPR FoldIntegerConstants(ExpressionKind kind, EXPRFLAG flags, EXPR op1, EXPR op2, PredefinedType ptOp)
	{
		CType reqPDT = GetReqPDT(ptOp);
		EXPRCONSTANT eXPRCONSTANT = op1.GetConst().asCONSTANT();
		EXPRCONSTANT eXPRCONSTANT2 = op2?.GetConst().asCONSTANT();
		if (eXPRCONSTANT != null && (op2 == null || eXPRCONSTANT2 != null))
		{
			if (ptOp == PredefinedType.PT_LONG || ptOp == PredefinedType.PT_ULONG)
			{
				return FoldConstI8Op(kind, op1, eXPRCONSTANT, op2, eXPRCONSTANT2, ptOp);
			}
			return FoldConstI4Op(kind, op1, eXPRCONSTANT, op2, eXPRCONSTANT2, ptOp);
		}
		return null;
	}

	private EXPR BindIntOp(ExpressionKind kind, EXPRFLAG flags, EXPR op1, EXPR op2, PredefinedType ptOp)
	{
		CType reqPDT = GetReqPDT(ptOp);
		if (isDivByZero(kind, op2))
		{
			GetErrorContext().Error(ErrorCode.ERR_IntDivByZero);
			EXPR eXPR = GetExprFactory().CreateBinop(kind, reqPDT, op1, op2);
			eXPR.SetError();
			return eXPR;
		}
		EXPR eXPR2 = FoldIntegerConstants(kind, flags, op1, op2, ptOp);
		if (eXPR2 != null)
		{
			return eXPR2;
		}
		if (kind == ExpressionKind.EK_NEG)
		{
			return BindIntegerNeg(flags, op1, ptOp);
		}
		CType pType = (kind.isRelational() ? GetReqPDT(PredefinedType.PT_BOOL) : reqPDT);
		EXPR eXPR3 = GetExprFactory().CreateOperator(kind, pType, op1, op2);
		eXPR3.flags |= flags;
		return eXPR3;
	}

	private EXPR BindIntegerNeg(EXPRFLAG flags, EXPR op, PredefinedType ptOp)
	{
		CType reqPDT = GetReqPDT(ptOp);
		switch (ptOp)
		{
		case PredefinedType.PT_ULONG:
			return BadOperatorTypesError(ExpressionKind.EK_NEG, op, null);
		case PredefinedType.PT_UINT:
			if (op.type.fundType() == FUNDTYPE.FT_U4)
			{
				EXPRCLASS destExpr = GetExprFactory().MakeClass(GetReqPDT(PredefinedType.PT_LONG));
				op = mustConvertCore(op, destExpr, CONVERTTYPE.NOUDC);
			}
			break;
		}
		return GetExprFactory().CreateNeg(flags, op);
	}

	private EXPR FoldConstI4Op(ExpressionKind kind, EXPR op1, EXPRCONSTANT opConst1, EXPR op2, EXPRCONSTANT opConst2, PredefinedType ptOp)
	{
		bool flag = ptOp == PredefinedType.PT_INT;
		uint uiVal = opConst1.asCONSTANT().getVal().uiVal;
		uint num = opConst2?.asCONSTANT().getVal().uiVal ?? 0;
		uint num2 = 2147483648u;
		uint num3;
		switch (kind)
		{
		case ExpressionKind.EK_ADD:
			num3 = uiVal + num;
			if (flag)
			{
				EnsureChecked((((uiVal ^ num) | (uiVal ^ num3 ^ num2)) & num2) != 0);
			}
			else
			{
				EnsureChecked(num3 >= uiVal);
			}
			break;
		case ExpressionKind.EK_SUB:
			num3 = uiVal - num;
			if (flag)
			{
				EnsureChecked((((uiVal ^ num ^ num2) | (uiVal ^ num3 ^ num2)) & num2) != 0);
			}
			else
			{
				EnsureChecked(num3 <= uiVal);
			}
			break;
		case ExpressionKind.EK_MUL:
			num3 = uiVal * num;
			if (uiVal != 0 && num != 0)
			{
				if (flag)
				{
					EnsureChecked((num != num3 || uiVal == 1) && (int)num3 / (int)uiVal == (int)num);
				}
				else
				{
					EnsureChecked(num3 / uiVal == num);
				}
			}
			break;
		case ExpressionKind.EK_DIV:
			if (!flag)
			{
				num3 = uiVal / num;
				break;
			}
			if (num != 0)
			{
				num3 = (uint)((int)uiVal / (int)num);
				break;
			}
			num3 = 0 - uiVal;
			EnsureChecked(uiVal != num2);
			break;
		case ExpressionKind.EK_MOD:
			num3 = (flag ? ((num != 0) ? ((uint)((int)uiVal % (int)num)) : 0u) : (uiVal % num));
			break;
		case ExpressionKind.EK_NEG:
			if (!flag)
			{
				CONSTVAL constVal = GetExprConstants().Create(0L - (long)uiVal);
				return GetExprFactory().CreateConstant(GetReqPDT(PredefinedType.PT_LONG), constVal);
			}
			num3 = 0 - uiVal;
			EnsureChecked(uiVal != num2);
			break;
		case ExpressionKind.EK_UPLUS:
			num3 = uiVal;
			break;
		case ExpressionKind.EK_BITAND:
			num3 = uiVal & num;
			break;
		case ExpressionKind.EK_BITOR:
			num3 = uiVal | num;
			break;
		case ExpressionKind.EK_BITXOR:
			num3 = uiVal ^ num;
			break;
		case ExpressionKind.EK_BITNOT:
			num3 = ~uiVal;
			break;
		case ExpressionKind.EK_EQ:
			num3 = ((uiVal == num) ? 1u : 0u);
			break;
		case ExpressionKind.EK_NE:
			num3 = ((uiVal != num) ? 1u : 0u);
			break;
		case ExpressionKind.EK_LE:
			num3 = ((flag ? ((int)uiVal <= (int)num) : (uiVal <= num)) ? 1u : 0u);
			break;
		case ExpressionKind.EK_LT:
			num3 = ((flag ? ((int)uiVal < (int)num) : (uiVal < num)) ? 1u : 0u);
			break;
		case ExpressionKind.EK_GE:
			num3 = ((flag ? ((int)uiVal >= (int)num) : (uiVal >= num)) ? 1u : 0u);
			break;
		case ExpressionKind.EK_GT:
			num3 = ((flag ? ((int)uiVal > (int)num) : (uiVal > num)) ? 1u : 0u);
			break;
		default:
			VSFAIL("Unknown op");
			num3 = 0u;
			break;
		}
		CType optPDT = GetOptPDT(kind.isRelational() ? PredefinedType.PT_BOOL : ptOp);
		return GetExprFactory().CreateConstant(optPDT, ConstValFactory.GetUInt(num3));
	}

	private void EnsureChecked(bool b)
	{
		if (!b && Context.CheckedConstant)
		{
			GetErrorContext().Error(ErrorCode.ERR_CheckedOverflow);
		}
	}

	private EXPR FoldConstI8Op(ExpressionKind kind, EXPR op1, EXPRCONSTANT opConst1, EXPR op2, EXPRCONSTANT opConst2, PredefinedType ptOp)
	{
		bool flag = ptOp == PredefinedType.PT_LONG;
		bool flag2 = false;
		CONSTVAL cONSTVAL = new CONSTVAL();
		CType pType;
		if (flag)
		{
			long longVal = opConst1.asCONSTANT().getVal().longVal;
			long num = opConst2?.asCONSTANT().getVal().longVal ?? 0;
			long value = 0L;
			switch (kind)
			{
			case ExpressionKind.EK_ADD:
				value = longVal + num;
				break;
			case ExpressionKind.EK_SUB:
				value = longVal - num;
				break;
			case ExpressionKind.EK_MUL:
				value = longVal * num;
				break;
			case ExpressionKind.EK_DIV:
				value = longVal / num;
				break;
			case ExpressionKind.EK_MOD:
				value = longVal % num;
				break;
			case ExpressionKind.EK_NEG:
				value = -longVal;
				break;
			case ExpressionKind.EK_UPLUS:
				value = longVal;
				break;
			case ExpressionKind.EK_BITAND:
				value = longVal & num;
				break;
			case ExpressionKind.EK_BITOR:
				value = longVal | num;
				break;
			case ExpressionKind.EK_BITXOR:
				value = longVal ^ num;
				break;
			case ExpressionKind.EK_BITNOT:
				value = ~longVal;
				break;
			case ExpressionKind.EK_EQ:
				flag2 = longVal == num;
				break;
			case ExpressionKind.EK_NE:
				flag2 = longVal != num;
				break;
			case ExpressionKind.EK_LE:
				flag2 = longVal <= num;
				break;
			case ExpressionKind.EK_LT:
				flag2 = longVal < num;
				break;
			case ExpressionKind.EK_GE:
				flag2 = longVal >= num;
				break;
			case ExpressionKind.EK_GT:
				flag2 = longVal > num;
				break;
			default:
				VSFAIL("Unknown op");
				value = 0L;
				break;
			}
			if (kind.isRelational())
			{
				cONSTVAL.iVal = (flag2 ? 1 : 0);
				pType = GetReqPDT(PredefinedType.PT_BOOL);
			}
			else
			{
				cONSTVAL = GetExprConstants().Create(value);
				pType = GetOptPDT(ptOp);
			}
		}
		else
		{
			ulong ulongVal = opConst1.asCONSTANT().getVal().ulongVal;
			ulong num2 = opConst2?.asCONSTANT().getVal().ulongVal ?? 0;
			ulong value2 = 0uL;
			switch (kind)
			{
			case ExpressionKind.EK_ADD:
				value2 = ulongVal + num2;
				break;
			case ExpressionKind.EK_SUB:
				value2 = ulongVal - num2;
				break;
			case ExpressionKind.EK_MUL:
				value2 = ulongVal * num2;
				break;
			case ExpressionKind.EK_DIV:
				value2 = ulongVal / num2;
				break;
			case ExpressionKind.EK_MOD:
				value2 = ulongVal % num2;
				break;
			case ExpressionKind.EK_NEG:
				return BadOperatorTypesError(kind, op1, op2);
			case ExpressionKind.EK_UPLUS:
				value2 = ulongVal;
				break;
			case ExpressionKind.EK_BITAND:
				value2 = ulongVal & num2;
				break;
			case ExpressionKind.EK_BITOR:
				value2 = ulongVal | num2;
				break;
			case ExpressionKind.EK_BITXOR:
				value2 = ulongVal ^ num2;
				break;
			case ExpressionKind.EK_BITNOT:
				value2 = ~ulongVal;
				break;
			case ExpressionKind.EK_EQ:
				flag2 = ulongVal == num2;
				break;
			case ExpressionKind.EK_NE:
				flag2 = ulongVal != num2;
				break;
			case ExpressionKind.EK_LE:
				flag2 = ulongVal <= num2;
				break;
			case ExpressionKind.EK_LT:
				flag2 = ulongVal < num2;
				break;
			case ExpressionKind.EK_GE:
				flag2 = ulongVal >= num2;
				break;
			case ExpressionKind.EK_GT:
				flag2 = ulongVal > num2;
				break;
			default:
				VSFAIL("Unknown op");
				value2 = 0uL;
				break;
			}
			if (kind.isRelational())
			{
				cONSTVAL.iVal = (flag2 ? 1 : 0);
				pType = GetReqPDT(PredefinedType.PT_BOOL);
			}
			else
			{
				cONSTVAL = GetExprConstants().Create(value2);
				pType = GetOptPDT(ptOp);
			}
		}
		return GetExprFactory().CreateConstant(pType, cONSTVAL);
	}

	private EXPR bindFloatOp(ExpressionKind kind, EXPRFLAG flags, EXPR op1, EXPR op2)
	{
		EXPR @const = op1.GetConst();
		EXPR eXPR = op2?.GetConst();
		EXPR eXPR2;
		if (@const != null && (op2 == null || eXPR != null))
		{
			double doubleVal = @const.asCONSTANT().getVal().doubleVal;
			double num = eXPR?.asCONSTANT().getVal().doubleVal ?? 0.0;
			double value = 0.0;
			bool flag = false;
			switch (kind)
			{
			case ExpressionKind.EK_ADD:
				value = doubleVal + num;
				break;
			case ExpressionKind.EK_SUB:
				value = doubleVal - num;
				break;
			case ExpressionKind.EK_MUL:
				value = doubleVal * num;
				break;
			case ExpressionKind.EK_DIV:
				value = doubleVal / num;
				break;
			case ExpressionKind.EK_NEG:
				value = 0.0 - doubleVal;
				break;
			case ExpressionKind.EK_UPLUS:
				value = doubleVal;
				break;
			case ExpressionKind.EK_MOD:
				value = doubleVal % num;
				break;
			case ExpressionKind.EK_EQ:
				flag = doubleVal == num;
				break;
			case ExpressionKind.EK_NE:
				flag = doubleVal != num;
				break;
			case ExpressionKind.EK_LE:
				flag = doubleVal <= num;
				break;
			case ExpressionKind.EK_LT:
				flag = doubleVal < num;
				break;
			case ExpressionKind.EK_GE:
				flag = doubleVal >= num;
				break;
			case ExpressionKind.EK_GT:
				flag = doubleVal > num;
				break;
			default:
				value = 0.0;
				break;
			}
			CONSTVAL cONSTVAL = new CONSTVAL();
			CType pType;
			if (kind.isRelational())
			{
				cONSTVAL.iVal = (flag ? 1 : 0);
				pType = GetReqPDT(PredefinedType.PT_BOOL);
			}
			else
			{
				cONSTVAL = GetExprConstants().Create(value);
				pType = op1.type;
			}
			eXPR2 = GetExprFactory().CreateConstant(pType, cONSTVAL);
		}
		else
		{
			CType pType2 = (kind.isRelational() ? GetReqPDT(PredefinedType.PT_BOOL) : op1.type);
			eXPR2 = GetExprFactory().CreateOperator(kind, pType2, op1, op2);
			flags = (EXPRFLAG)(-262145);
			eXPR2.flags |= flags;
		}
		return eXPR2;
	}

	private EXPR bindStringConcat(EXPR op1, EXPR op2)
	{
		return GetExprFactory().CreateConcat(op1, op2);
	}

	private EXPR ambiguousOperatorError(ExpressionKind ek, EXPR op1, EXPR op2)
	{
		RETAILVERIFY(op1 != null);
		string errorString = op1.errorString;
		if (op2 != null)
		{
			GetErrorContext().Error(ErrorCode.ERR_AmbigBinaryOps, errorString, op1.type, op2.type);
		}
		else
		{
			GetErrorContext().Error(ErrorCode.ERR_AmbigUnaryOp, errorString, op1.type);
		}
		EXPR eXPR = GetExprFactory().CreateOperator(ek, null, op1, op2);
		eXPR.SetError();
		return eXPR;
	}

	private EXPR BindUserBoolOp(ExpressionKind kind, EXPRCALL pCall)
	{
		RETAILVERIFY(pCall != null);
		RETAILVERIFY(pCall.mwi.Meth() != null);
		RETAILVERIFY(pCall.GetOptionalArguments() != null);
		CType type = pCall.type;
		if (!GetTypes().SubstEqualTypes(type, pCall.mwi.Meth().Params.Item(0), type) || !GetTypes().SubstEqualTypes(type, pCall.mwi.Meth().Params.Item(1), type))
		{
			MethWithInst mwi = new MethWithInst(null, null);
			EXPRMEMGRP pMemberGroup = GetExprFactory().CreateMemGroup(null, mwi);
			EXPRCALL eXPRCALL = GetExprFactory().CreateCall((EXPRFLAG)0, null, null, pMemberGroup, null);
			eXPRCALL.SetError();
			GetErrorContext().Error(ErrorCode.ERR_BadBoolOp, pCall.mwi);
			return GetExprFactory().CreateUserLogOpError(type, eXPRCALL, pCall);
		}
		EXPR optionalElement = pCall.GetOptionalArguments().asLIST().GetOptionalElement();
		EXPR eXPR = WrapShortLivedExpression(optionalElement);
		pCall.GetOptionalArguments().asLIST().SetOptionalElement(eXPR);
		SymbolLoader.RuntimeBinderSymbolTable.PopulateSymbolTableWithName("op_True", null, eXPR.type.AssociatedSystemType);
		SymbolLoader.RuntimeBinderSymbolTable.PopulateSymbolTableWithName("op_False", null, eXPR.type.AssociatedSystemType);
		EXPR eXPR2 = bindUDUnop(ExpressionKind.EK_TRUE, eXPR);
		EXPR eXPR3 = bindUDUnop(ExpressionKind.EK_FALSE, eXPR);
		if (eXPR2 == null || eXPR3 == null)
		{
			EXPR eXPR4 = ((eXPR2 != null) ? eXPR2 : eXPR3);
			if (eXPR4 == null)
			{
				MethWithInst mwi2 = new MethWithInst(null, null);
				EXPRMEMGRP pMemberGroup2 = GetExprFactory().CreateMemGroup(null, mwi2);
				eXPR4 = GetExprFactory().CreateCall((EXPRFLAG)0, null, eXPR, pMemberGroup2, null);
				pCall.SetError();
			}
			GetErrorContext().Error(ErrorCode.ERR_MustHaveOpTF, type);
			return GetExprFactory().CreateUserLogOpError(type, eXPR4, pCall);
		}
		eXPR2 = mustConvert(eXPR2, GetReqPDT(PredefinedType.PT_BOOL));
		eXPR3 = mustConvert(eXPR3, GetReqPDT(PredefinedType.PT_BOOL));
		return GetExprFactory().CreateUserLogOp(type, (kind == ExpressionKind.EK_LOGAND) ? eXPR3 : eXPR2, pCall);
	}

	private AggregateType GetUserDefinedBinopArgumentType(CType type)
	{
		while (true)
		{
			switch (type.GetTypeKind())
			{
			case TypeKind.TK_NullableType:
				type = type.StripNubs();
				break;
			case TypeKind.TK_TypeParameterType:
				type = type.AsTypeParameterType().GetEffectiveBaseClass();
				break;
			case TypeKind.TK_AggregateType:
				if ((type.isClassType() || type.isStructType()) && !type.AsAggregateType().getAggregate().IsSkipUDOps())
				{
					return type.AsAggregateType();
				}
				return null;
			default:
				return null;
			}
		}
	}

	private int GetUserDefinedBinopArgumentTypes(CType type1, CType type2, AggregateType[] rgats)
	{
		int num = 0;
		rgats[0] = GetUserDefinedBinopArgumentType(type1);
		if (rgats[0] != null)
		{
			num++;
		}
		rgats[num] = GetUserDefinedBinopArgumentType(type2);
		if (rgats[num] != null)
		{
			num++;
		}
		if (num == 2 && rgats[0] == rgats[1])
		{
			num = 1;
		}
		return num;
	}

	private bool UserDefinedBinaryOperatorCanBeLifted(ExpressionKind ek, MethodSymbol method, AggregateType ats, TypeArray Params)
	{
		if (!Params.Item(0).IsNonNubValType())
		{
			return false;
		}
		if (!Params.Item(1).IsNonNubValType())
		{
			return false;
		}
		CType cType = GetTypes().SubstType(method.RetType, ats);
		if (!cType.IsNonNubValType())
		{
			return false;
		}
		switch (ek)
		{
		case ExpressionKind.EK_EQ:
		case ExpressionKind.EK_NE:
			if (!cType.isPredefType(PredefinedType.PT_BOOL))
			{
				return false;
			}
			if (Params.Item(0) != Params.Item(1))
			{
				return false;
			}
			return true;
		case ExpressionKind.EK_LT:
		case ExpressionKind.EK_LE:
		case ExpressionKind.EK_GT:
		case ExpressionKind.EK_GE:
			if (!cType.isPredefType(PredefinedType.PT_BOOL))
			{
				return false;
			}
			return true;
		default:
			return true;
		}
	}

	private bool UserDefinedBinaryOperatorIsApplicable(List<CandidateFunctionMember> candidateList, ExpressionKind ek, MethodSymbol method, AggregateType ats, EXPR arg1, EXPR arg2, bool fDontLift)
	{
		if (!method.isOperator || method.Params.size != 2)
		{
			return false;
		}
		TypeArray typeArray = GetTypes().SubstTypeArray(method.Params, ats);
		if (canConvert(arg1, typeArray.Item(0)) && canConvert(arg2, typeArray.Item(1)))
		{
			candidateList.Add(new CandidateFunctionMember(new MethPropWithInst(method, ats, BSYMMGR.EmptyTypeArray()), typeArray, 0, fExpanded: false));
			return true;
		}
		if (fDontLift || !GetSymbolLoader().FCanLift() || !UserDefinedBinaryOperatorCanBeLifted(ek, method, ats, typeArray))
		{
			return false;
		}
		CType[] array = new CType[2]
		{
			GetTypes().GetNullable(typeArray.Item(0)),
			GetTypes().GetNullable(typeArray.Item(1))
		};
		if (!canConvert(arg1, array[0]) || !canConvert(arg2, array[1]))
		{
			return false;
		}
		candidateList.Add(new CandidateFunctionMember(new MethPropWithInst(method, ats, BSYMMGR.EmptyTypeArray()), GetGlobalSymbols().AllocParams(2, array), 2, fExpanded: false));
		return true;
	}

	private bool GetApplicableUserDefinedBinaryOperatorCandidates(List<CandidateFunctionMember> candidateList, ExpressionKind ek, AggregateType type, EXPR arg1, EXPR arg2, bool fDontLift)
	{
		Name name = ekName(ek);
		bool result = false;
		for (MethodSymbol methodSymbol = GetSymbolLoader().LookupAggMember(name, type.getAggregate(), symbmask_t.MASK_MethodSymbol).AsMethodSymbol(); methodSymbol != null; methodSymbol = GetSymbolLoader().LookupNextSym(methodSymbol, type.getAggregate(), symbmask_t.MASK_MethodSymbol).AsMethodSymbol())
		{
			if (UserDefinedBinaryOperatorIsApplicable(candidateList, ek, methodSymbol, type, arg1, arg2, fDontLift))
			{
				result = true;
			}
		}
		return result;
	}

	private AggregateType GetApplicableUserDefinedBinaryOperatorCandidatesInBaseTypes(List<CandidateFunctionMember> candidateList, ExpressionKind ek, AggregateType type, EXPR arg1, EXPR arg2, bool fDontLift, AggregateType atsStop)
	{
		AggregateType aggregateType = type;
		while (aggregateType != null && aggregateType != atsStop)
		{
			if (GetApplicableUserDefinedBinaryOperatorCandidates(candidateList, ek, aggregateType, arg1, arg2, fDontLift))
			{
				return aggregateType;
			}
			aggregateType = aggregateType.GetBaseClass();
		}
		return null;
	}

	private EXPRCALL BindUDBinop(ExpressionKind ek, EXPR arg1, EXPR arg2, bool fDontLift, out MethPropWithInst ppmpwi)
	{
		List<CandidateFunctionMember> list = new List<CandidateFunctionMember>();
		ppmpwi = null;
		AggregateType[] array = new AggregateType[2];
		switch (GetUserDefinedBinopArgumentTypes(arg1.type, arg2.type, array))
		{
		case 0:
			return null;
		case 1:
			GetApplicableUserDefinedBinaryOperatorCandidatesInBaseTypes(list, ek, array[0], arg1, arg2, fDontLift, null);
			break;
		default:
		{
			AggregateType applicableUserDefinedBinaryOperatorCandidatesInBaseTypes = GetApplicableUserDefinedBinaryOperatorCandidatesInBaseTypes(list, ek, array[0], arg1, arg2, fDontLift, null);
			GetApplicableUserDefinedBinaryOperatorCandidatesInBaseTypes(list, ek, array[1], arg1, arg2, fDontLift, applicableUserDefinedBinaryOperatorCandidatesInBaseTypes);
			break;
		}
		}
		if (list.IsEmpty())
		{
			return null;
		}
		EXPRLIST args = GetExprFactory().CreateList(arg1, arg2);
		ArgInfos argInfos = new ArgInfos();
		argInfos.carg = 2;
		FillInArgInfoFromArgList(argInfos, args);
		CandidateFunctionMember methAmbig;
		CandidateFunctionMember methAmbig2;
		CandidateFunctionMember candidateFunctionMember = FindBestMethod(list, null, argInfos, out methAmbig, out methAmbig2);
		if (candidateFunctionMember == null)
		{
			GetErrorContext().Error(ErrorCode.ERR_AmbigCall, methAmbig.mpwi, methAmbig2.mpwi);
			EXPRMEMGRP pMemberGroup = GetExprFactory().CreateMemGroup(null, methAmbig.mpwi);
			EXPRCALL eXPRCALL = GetExprFactory().CreateCall((EXPRFLAG)0, null, GetExprFactory().CreateList(arg1, arg2), pMemberGroup, null);
			eXPRCALL.SetError();
			return eXPRCALL;
		}
		if (GetSemanticChecker().CheckBogus(candidateFunctionMember.mpwi.Meth()))
		{
			GetErrorContext().ErrorRef(ErrorCode.ERR_BindToBogus, candidateFunctionMember.mpwi);
			EXPRMEMGRP pMemberGroup2 = GetExprFactory().CreateMemGroup(null, candidateFunctionMember.mpwi);
			EXPRCALL eXPRCALL2 = GetExprFactory().CreateCall((EXPRFLAG)0, null, GetExprFactory().CreateList(arg1, arg2), pMemberGroup2, null);
			eXPRCALL2.SetError();
			return eXPRCALL2;
		}
		ppmpwi = candidateFunctionMember.mpwi;
		if (candidateFunctionMember.ctypeLift != 0)
		{
			return BindLiftedUDBinop(ek, arg1, arg2, candidateFunctionMember.@params, candidateFunctionMember.mpwi);
		}
		CType typeRet = GetTypes().SubstType(candidateFunctionMember.mpwi.Meth().RetType, candidateFunctionMember.mpwi.GetType());
		return BindUDBinopCall(arg1, arg2, candidateFunctionMember.@params, typeRet, candidateFunctionMember.mpwi);
	}

	private EXPRCALL BindUDBinopCall(EXPR arg1, EXPR arg2, TypeArray Params, CType typeRet, MethPropWithInst mpwi)
	{
		arg1 = mustConvert(arg1, Params.Item(0));
		arg2 = mustConvert(arg2, Params.Item(1));
		EXPRLIST pOptionalArguments = GetExprFactory().CreateList(arg1, arg2);
		checkUnsafe(arg1.type);
		checkUnsafe(arg2.type);
		checkUnsafe(typeRet);
		EXPRMEMGRP pMemberGroup = GetExprFactory().CreateMemGroup(null, mpwi);
		EXPRCALL eXPRCALL = GetExprFactory().CreateCall((EXPRFLAG)0, typeRet, pOptionalArguments, pMemberGroup, null);
		eXPRCALL.mwi = new MethWithInst(mpwi);
		verifyMethodArgs(eXPRCALL, mpwi.GetType());
		return eXPRCALL;
	}

	private EXPRCALL BindLiftedUDBinop(ExpressionKind ek, EXPR arg1, EXPR arg2, TypeArray Params, MethPropWithInst mpwi)
	{
		EXPR eXPR = arg1;
		EXPR eXPR2 = arg2;
		CType cType = GetTypes().SubstType(mpwi.Meth().RetType, mpwi.GetType());
		TypeArray typeArray = GetTypes().SubstTypeArray(mpwi.Meth().Params, mpwi.GetType());
		if (!canConvert(arg1.type.StripNubs(), typeArray.Item(0), CONVERTTYPE.NOUDC))
		{
			eXPR = mustConvert(arg1, Params.Item(0));
		}
		if (!canConvert(arg2.type.StripNubs(), typeArray.Item(1), CONVERTTYPE.NOUDC))
		{
			eXPR2 = mustConvert(arg2, Params.Item(1));
		}
		EXPR arg3 = mustCast(eXPR, typeArray.Item(0));
		EXPR arg4 = mustCast(eXPR2, typeArray.Item(1));
		CType cType2;
		switch (ek)
		{
		default:
			cType2 = GetTypes().GetNullable(cType);
			break;
		case ExpressionKind.EK_EQ:
		case ExpressionKind.EK_NE:
			cType2 = cType;
			break;
		case ExpressionKind.EK_LT:
		case ExpressionKind.EK_LE:
		case ExpressionKind.EK_GT:
		case ExpressionKind.EK_GE:
			cType2 = cType;
			break;
		}
		EXPRCALL expr = BindUDBinopCall(arg3, arg4, typeArray, cType, mpwi);
		EXPRLIST pOptionalArguments = GetExprFactory().CreateList(eXPR, eXPR2);
		EXPRMEMGRP pMemberGroup = GetExprFactory().CreateMemGroup(null, mpwi);
		EXPRCALL eXPRCALL = GetExprFactory().CreateCall((EXPRFLAG)0, cType2, pOptionalArguments, pMemberGroup, null);
		eXPRCALL.mwi = new MethWithInst(mpwi);
		switch (ek)
		{
		case ExpressionKind.EK_EQ:
			eXPRCALL.nubLiftKind = NullableCallLiftKind.EqualityOperator;
			break;
		case ExpressionKind.EK_NE:
			eXPRCALL.nubLiftKind = NullableCallLiftKind.InequalityOperator;
			break;
		default:
			eXPRCALL.nubLiftKind = NullableCallLiftKind.Operator;
			break;
		}
		eXPRCALL.castOfNonLiftedResultToLiftedType = mustCast(expr, cType2, (CONVERTTYPE)0);
		return eXPRCALL;
	}

	private AggregateType GetEnumBinOpType(ExpressionKind ek, CType argType1, CType argType2, out AggregateType ppEnumType)
	{
		AggregateType aggregateType = argType1.AsAggregateType();
		AggregateType aggregateType2 = argType2.AsAggregateType();
		AggregateType aggregateType3 = (aggregateType.isEnumType() ? aggregateType : aggregateType2);
		AggregateType result = aggregateType3;
		switch (ek)
		{
		case ExpressionKind.EK_SUB:
			if (aggregateType == aggregateType2)
			{
				result = aggregateType3.underlyingEnumType();
			}
			break;
		default:
			result = GetReqPDT(PredefinedType.PT_BOOL);
			break;
		case ExpressionKind.EK_ADD:
		case ExpressionKind.EK_BITAND:
		case ExpressionKind.EK_BITOR:
		case ExpressionKind.EK_BITXOR:
			break;
		}
		ppEnumType = aggregateType3;
		return result;
	}

	private EXPRBINOP CreateBinopForPredefMethodCall(ExpressionKind ek, PREDEFMETH predefMeth, CType RetType, EXPR arg1, EXPR arg2)
	{
		MethodSymbol method = GetSymbolLoader().getPredefinedMembers().GetMethod(predefMeth);
		EXPRBINOP eXPRBINOP = GetExprFactory().CreateBinop(ek, RetType, arg1, arg2);
		if (method != null)
		{
			AggregateSymbol @class = method.getClass();
			AggregateType aggregate = GetTypes().GetAggregate(@class, BSYMMGR.EmptyTypeArray());
			eXPRBINOP.predefinedMethodToCall = new MethWithInst(method, aggregate, null);
			eXPRBINOP.SetUserDefinedCallMethod(eXPRBINOP.predefinedMethodToCall);
		}
		else
		{
			eXPRBINOP.SetError();
		}
		return eXPRBINOP;
	}

	private EXPRUNARYOP CreateUnaryOpForPredefMethodCall(ExpressionKind ek, PREDEFMETH predefMeth, CType pRetType, EXPR pArg)
	{
		MethodSymbol method = GetSymbolLoader().getPredefinedMembers().GetMethod(predefMeth);
		EXPRUNARYOP eXPRUNARYOP = GetExprFactory().CreateUnaryOp(ek, pRetType, pArg);
		if (method != null)
		{
			AggregateSymbol @class = method.getClass();
			AggregateType aggregate = GetTypes().GetAggregate(@class, BSYMMGR.EmptyTypeArray());
			eXPRUNARYOP.predefinedMethodToCall = new MethWithInst(method, aggregate, null);
			eXPRUNARYOP.UserDefinedCallMethod = eXPRUNARYOP.predefinedMethodToCall;
		}
		else
		{
			eXPRUNARYOP.SetError();
		}
		return eXPRUNARYOP;
	}

	protected BetterType WhichMethodIsBetterTieBreaker(CandidateFunctionMember node1, CandidateFunctionMember node2, CType pTypeThrough, ArgInfos args)
	{
		MethPropWithInst mpwi = node1.mpwi;
		MethPropWithInst mpwi2 = node2.mpwi;
		if (node1.ctypeLift != node2.ctypeLift)
		{
			if (node1.ctypeLift >= node2.ctypeLift)
			{
				return BetterType.Right;
			}
			return BetterType.Left;
		}
		if (mpwi.TypeArgs.size != 0)
		{
			if (mpwi2.TypeArgs.size == 0)
			{
				return BetterType.Right;
			}
		}
		else if (mpwi2.TypeArgs.size != 0)
		{
			return BetterType.Left;
		}
		if (node1.fExpanded)
		{
			if (!node2.fExpanded)
			{
				return BetterType.Right;
			}
		}
		else if (node2.fExpanded)
		{
			return BetterType.Left;
		}
		BetterType betterType = GetGlobalSymbols().CompareTypes(RearrangeNamedArguments(mpwi.MethProp().Params, mpwi, pTypeThrough, args), RearrangeNamedArguments(mpwi2.MethProp().Params, mpwi2, pTypeThrough, args));
		if (betterType == BetterType.Left || betterType == BetterType.Right)
		{
			return betterType;
		}
		if (mpwi.MethProp().modOptCount != mpwi2.MethProp().modOptCount)
		{
			if (mpwi.MethProp().modOptCount >= mpwi2.MethProp().modOptCount)
			{
				return BetterType.Right;
			}
			return BetterType.Left;
		}
		return BetterType.Neither;
	}

	private static int FindName(List<Name> names, Name name)
	{
		return names.IndexOf(name);
	}

	private TypeArray RearrangeNamedArguments(TypeArray pta, MethPropWithInst mpwi, CType pTypeThrough, ArgInfos args)
	{
		if (!args.fHasExprs)
		{
			return pta;
		}
		CType pType = ((pTypeThrough != null) ? pTypeThrough : mpwi.GetType());
		CType[] array = new CType[pta.size];
		MethodOrPropertySymbol methodOrPropertySymbol = GroupToArgsBinder.FindMostDerivedMethod(GetSymbolLoader(), mpwi.MethProp(), pType);
		for (int i = 0; i < pta.size; i++)
		{
			array[i] = pta.Item(i);
		}
		for (int j = 0; j < args.carg; j++)
		{
			EXPR expr = args.prgexpr[j];
			if (expr.isNamedArgumentSpecification())
			{
				int num = FindName(methodOrPropertySymbol.ParameterNames, expr.asNamedArgumentSpecification().Name);
				CType cType = pta.Item(num);
				for (int k = j; k < num; k++)
				{
					array[k + 1] = array[k];
				}
				array[j] = cType;
			}
		}
		return GetSymbolLoader().getBSymmgr().AllocParams(pta.size, array);
	}

	protected BetterType WhichMethodIsBetter(CandidateFunctionMember node1, CandidateFunctionMember node2, CType pTypeThrough, ArgInfos args)
	{
		MethPropWithInst mpwi = node1.mpwi;
		MethPropWithInst mpwi2 = node2.mpwi;
		TypeArray typeArray = RearrangeNamedArguments(node1.@params, mpwi, pTypeThrough, args);
		TypeArray typeArray2 = RearrangeNamedArguments(node2.@params, mpwi2, pTypeThrough, args);
		if (typeArray == typeArray2)
		{
			return WhichMethodIsBetterTieBreaker(node1, node2, pTypeThrough, args);
		}
		BetterType betterType = BetterType.Neither;
		CType pType = ((pTypeThrough != null) ? pTypeThrough : mpwi.GetType());
		CType pType2 = ((pTypeThrough != null) ? pTypeThrough : mpwi2.GetType());
		MethodOrPropertySymbol methodOrPropertySymbol = GroupToArgsBinder.FindMostDerivedMethod(GetSymbolLoader(), mpwi.MethProp(), pType);
		MethodOrPropertySymbol methodOrPropertySymbol2 = GroupToArgsBinder.FindMostDerivedMethod(GetSymbolLoader(), mpwi2.MethProp(), pType2);
		List<Name> parameterNames = methodOrPropertySymbol.ParameterNames;
		List<Name> parameterNames2 = methodOrPropertySymbol2.ParameterNames;
		for (int i = 0; i < args.carg; i++)
		{
			EXPR eXPR = (args.fHasExprs ? args.prgexpr[i] : null);
			CType argType = args.types.Item(i);
			CType p = typeArray.Item(i);
			CType p2 = typeArray2.Item(i);
			if (eXPR.RuntimeObjectActualType != null)
			{
				argType = eXPR.RuntimeObjectActualType;
			}
			BetterType betterType2 = WhichConversionIsBetter(eXPR, argType, p, p2);
			if (betterType == BetterType.Right && betterType2 == BetterType.Left)
			{
				betterType = BetterType.Neither;
				break;
			}
			if (betterType == BetterType.Left && betterType2 == BetterType.Right)
			{
				betterType = BetterType.Neither;
				break;
			}
			if (betterType == BetterType.Neither && (betterType2 == BetterType.Right || betterType2 == BetterType.Left))
			{
				betterType = betterType2;
			}
		}
		if (typeArray.size != typeArray2.size && betterType == BetterType.Neither)
		{
			if (node1.fExpanded && !node2.fExpanded)
			{
				return BetterType.Right;
			}
			if (node2.fExpanded && !node1.fExpanded)
			{
				return BetterType.Left;
			}
			if (typeArray.size == args.carg)
			{
				return BetterType.Left;
			}
			if (typeArray2.size == args.carg)
			{
				return BetterType.Right;
			}
			return BetterType.Neither;
		}
		return betterType;
	}

	protected BetterType WhichConversionIsBetter(EXPR arg, CType argType, CType p1, CType p2)
	{
		if (p1 == p2)
		{
			return BetterType.Same;
		}
		return WhichConversionIsBetter(argType, p1, p2);
	}

	public BetterType WhichConversionIsBetter(CType argType, CType p1, CType p2)
	{
		if (p1 == p2)
		{
			return BetterType.Same;
		}
		if (argType == p1)
		{
			return BetterType.Left;
		}
		if (argType == p2)
		{
			return BetterType.Right;
		}
		bool flag = canConvert(p1, p2);
		bool flag2 = canConvert(p2, p1);
		if (flag && !flag2)
		{
			return BetterType.Left;
		}
		if (flag2 && !flag)
		{
			return BetterType.Right;
		}
		if (p1.isPredefined() && p2.isPredefined() && p1.getPredefType() <= PredefinedType.PT_OBJECT && p2.getPredefType() <= PredefinedType.PT_OBJECT)
		{
			switch (betterConversionTable[(uint)p1.getPredefType(), (uint)p2.getPredefType()])
			{
			case 1:
				return BetterType.Left;
			case 2:
				return BetterType.Right;
			}
		}
		return BetterType.Neither;
	}

	protected CandidateFunctionMember FindBestMethod(List<CandidateFunctionMember> list, CType pTypeThrough, ArgInfos args, out CandidateFunctionMember methAmbig1, out CandidateFunctionMember methAmbig2)
	{
		CandidateFunctionMember candidateFunctionMember = null;
		CandidateFunctionMember candidateFunctionMember2 = null;
		bool flag = false;
		CandidateFunctionMember candidateFunctionMember3 = list[0];
		for (int i = 1; i < list.Count; i++)
		{
			CandidateFunctionMember candidateFunctionMember4 = list[i];
			switch (WhichMethodIsBetter(candidateFunctionMember3, candidateFunctionMember4, pTypeThrough, args))
			{
			case BetterType.Left:
				flag = false;
				continue;
			case BetterType.Right:
				flag = false;
				candidateFunctionMember3 = candidateFunctionMember4;
				continue;
			}
			candidateFunctionMember = candidateFunctionMember3;
			candidateFunctionMember2 = candidateFunctionMember4;
			i++;
			if (i < list.Count)
			{
				candidateFunctionMember4 = list[i];
				candidateFunctionMember3 = candidateFunctionMember4;
			}
			else
			{
				flag = true;
			}
		}
		if (!flag)
		{
			foreach (CandidateFunctionMember item in list)
			{
				if (item == candidateFunctionMember3)
				{
					methAmbig1 = null;
					methAmbig2 = null;
					return candidateFunctionMember3;
				}
				switch (WhichMethodIsBetter(item, candidateFunctionMember3, pTypeThrough, args))
				{
				case BetterType.Same:
				case BetterType.Neither:
					candidateFunctionMember = candidateFunctionMember3;
					candidateFunctionMember2 = item;
					goto end_IL_00bf;
				case BetterType.Right:
					break;
				default:
					goto end_IL_00bf;
				}
				continue;
				end_IL_00bf:
				break;
			}
		}
		if (candidateFunctionMember != null && candidateFunctionMember2 != null)
		{
			methAmbig1 = candidateFunctionMember;
			methAmbig2 = candidateFunctionMember2;
		}
		else
		{
			methAmbig1 = list.First();
			methAmbig2 = list.Skip(1).First();
		}
		return null;
	}

	protected void ReportLocalError(LocalVariableSymbol local, CheckLvalueKind kind, bool isNested)
	{
		int num = ((kind != CheckLvalueKind.OutParameter) ? 1 : 0);
		ErrorCode id = ReadOnlyLocalErrors[num];
		ErrorContext.Error(id, local.name);
	}

	protected void ReportReadOnlyError(EXPRFIELD field, CheckLvalueKind kind, bool isNested)
	{
		bool isStatic = field.fwt.Field().isStatic;
		int num = (isNested ? 4 : 0) + (isStatic ? 2 : 0) + ((kind != CheckLvalueKind.OutParameter) ? 1 : 0);
		ErrorCode id = ReadOnlyErrors[num];
		if (isNested)
		{
			ErrorContext.Error(id, field.fwt);
		}
		else
		{
			ErrorContext.Error(id);
		}
	}

	protected bool TryReportLvalueFailure(EXPR expr, CheckLvalueKind kind)
	{
		bool flag = false;
		EXPR expr2 = expr;
		while (true)
		{
			if (expr2.isANYLOCAL_OK())
			{
				ReportLocalError(expr2.asANYLOCAL().local, kind, flag);
				return true;
			}
			EXPR eXPR = null;
			if (expr2.isPROP())
			{
				eXPR = expr2.asPROP().GetMemberGroup().GetOptionalObject();
			}
			else if (expr2.isFIELD())
			{
				EXPRFIELD eXPRFIELD = expr2.asFIELD();
				if (eXPRFIELD.fwt.Field().isReadOnly)
				{
					ReportReadOnlyError(eXPRFIELD, kind, flag);
					return true;
				}
				if (!eXPRFIELD.fwt.Field().isStatic)
				{
					eXPR = eXPRFIELD.GetOptionalObject();
				}
			}
			if (eXPR != null && eXPR.type.isStructOrEnum())
			{
				if (eXPR.isCALL() || eXPR.isPROP())
				{
					ErrorContext.Error(ErrorCode.ERR_ReturnNotLValue, eXPR.GetSymWithType());
					return true;
				}
				if (eXPR.isCAST())
				{
					eXPR.flags |= EXPRFLAG.EXF_USERCALLABLE;
					return false;
				}
			}
			if (eXPR == null || eXPR.isLvalue() || (!expr2.isFIELD() && (flag || !expr2.isPROP())))
			{
				break;
			}
			expr2 = eXPR;
			flag = true;
		}
		ErrorContext.Error(GetStandardLvalueError(kind));
		return true;
	}

	public static void ReportTypeArgsNotAllowedError(SymbolLoader symbolLoader, int arity, ErrArgRef argName, ErrArgRef argKind)
	{
		symbolLoader.ErrorContext.ErrorRef(ErrorCode.ERR_TypeArgsNotAllowed, argName, argKind);
	}
}
