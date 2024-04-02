using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using Microsoft.CSharp.RuntimeBinder.Errors;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class TypeManager
{
	private class StdTypeVarColl
	{
		public List<TypeParameterType> prgptvs;

		public StdTypeVarColl()
		{
			prgptvs = new List<TypeParameterType>();
		}

		public TypeParameterType GetTypeVarSym(int iv, TypeManager pTypeManager, bool fMeth)
		{
			TypeParameterType typeParameterType = null;
			if (iv >= prgptvs.Count)
			{
				TypeParameterSymbol typeParameterSymbol = new TypeParameterSymbol();
				typeParameterSymbol.SetIsMethodTypeParameter(fMeth);
				typeParameterSymbol.SetIndexInOwnParameters(iv);
				typeParameterSymbol.SetIndexInTotalParameters(iv);
				typeParameterSymbol.SetAccess(ACCESS.ACC_PRIVATE);
				typeParameterType = pTypeManager.GetTypeParameter(typeParameterSymbol);
				prgptvs.Add(typeParameterType);
			}
			else
			{
				typeParameterType = prgptvs[iv];
			}
			return typeParameterType;
		}
	}

	private BSYMMGR m_BSymmgr;

	private PredefinedTypes m_predefTypes;

	private TypeFactory m_typeFactory;

	private TypeTable m_typeTable;

	private SymbolTable m_symbolTable;

	private VoidType voidType;

	private NullType nullType;

	private OpenTypePlaceholderType typeUnit;

	private BoundLambdaType typeAnonMeth;

	private MethodGroupType typeMethGrp;

	private ArgumentListType argListType;

	private ErrorType errorType;

	private StdTypeVarColl stvcMethod;

	private StdTypeVarColl stvcClass;

	private Dictionary<Tuple<Assembly, Assembly>, bool> internalsVisibleToCalculated = new Dictionary<Tuple<Assembly, Assembly>, bool>();

	public TypeManager()
	{
		m_predefTypes = null;
		m_BSymmgr = null;
		m_typeFactory = new TypeFactory();
		m_typeTable = new TypeTable();
		errorType = m_typeFactory.CreateError(null, null, null, null, null);
		voidType = m_typeFactory.CreateVoid();
		nullType = m_typeFactory.CreateNull();
		typeUnit = m_typeFactory.CreateUnit();
		typeAnonMeth = m_typeFactory.CreateAnonMethod();
		typeMethGrp = m_typeFactory.CreateMethodGroup();
		argListType = m_typeFactory.CreateArgList();
		InitType(errorType);
		errorType.SetErrors(fHasErrors: true);
		InitType(voidType);
		InitType(nullType);
		InitType(typeUnit);
		InitType(typeAnonMeth);
		InitType(typeMethGrp);
		stvcMethod = new StdTypeVarColl();
		stvcClass = new StdTypeVarColl();
	}

	public void InitTypeFactory(SymbolTable table)
	{
		m_symbolTable = table;
	}

	private void InitType(CType at)
	{
	}

	public static bool TypeContainsAnonymousTypes(CType type)
	{
		CType cType = type;
		while (true)
		{
			switch (cType.GetTypeKind())
			{
			default:
				return false;
			case TypeKind.TK_VoidType:
			case TypeKind.TK_NullType:
			case TypeKind.TK_UnboundLambdaType:
			case TypeKind.TK_MethodGroupType:
			case TypeKind.TK_NullableType:
			case TypeKind.TK_TypeParameterType:
				return false;
			case TypeKind.TK_ArrayType:
			case TypeKind.TK_PointerType:
			case TypeKind.TK_ParameterModifierType:
				cType = cType.GetBaseOrParameterOrElementType();
				break;
			case TypeKind.TK_AggregateType:
			{
				if (cType.AsAggregateType().getAggregate().IsAnonymousType())
				{
					return true;
				}
				TypeArray typeArgsAll = cType.AsAggregateType().GetTypeArgsAll();
				for (int i = 0; i < typeArgsAll.size; i++)
				{
					CType type2 = typeArgsAll.Item(i);
					if (TypeContainsAnonymousTypes(type2))
					{
						return true;
					}
				}
				return false;
			}
			case TypeKind.TK_ErrorType:
				if (cType.AsErrorType().HasTypeParent())
				{
					cType = cType.AsErrorType().GetTypeParent();
					break;
				}
				return false;
			}
		}
	}

	public ArrayType GetArray(CType elementType, int args)
	{
		Name name = (((uint)(args - 1) > 1u) ? m_BSymmgr.GetNameManager().Add("[X" + args + 1) : m_BSymmgr.GetNameManager().GetPredefinedName((PredefinedName)(7 + args)));
		ArrayType arrayType = m_typeTable.LookupArray(name, elementType);
		if (arrayType == null)
		{
			arrayType = m_typeFactory.CreateArray(name, elementType, args);
			arrayType.InitFromParent();
			m_typeTable.InsertArray(name, elementType, arrayType);
		}
		return arrayType;
	}

	public AggregateType GetAggregate(AggregateSymbol agg, AggregateType atsOuter, TypeArray typeArgs)
	{
		if (typeArgs == null)
		{
			typeArgs = BSYMMGR.EmptyTypeArray();
		}
		Name nameFromPtrs = m_BSymmgr.GetNameFromPtrs(typeArgs, atsOuter);
		AggregateType aggregateType = m_typeTable.LookupAggregate(nameFromPtrs, agg);
		if (aggregateType == null)
		{
			aggregateType = m_typeFactory.CreateAggregateType(nameFromPtrs, agg, typeArgs, atsOuter);
			aggregateType.SetErrors(aggregateType.GetTypeArgsAll().HasErrors());
			m_typeTable.InsertAggregate(nameFromPtrs, agg, aggregateType);
			if (aggregateType.AssociatedSystemType != null && aggregateType.AssociatedSystemType.BaseType != null)
			{
				AggregateType baseClass = agg.GetBaseClass();
				agg.SetBaseClass(m_symbolTable.GetCTypeFromType(aggregateType.AssociatedSystemType.BaseType).AsAggregateType());
				aggregateType.GetBaseClass();
				agg.SetBaseClass(baseClass);
			}
		}
		return aggregateType;
	}

	public AggregateType GetAggregate(AggregateSymbol agg, TypeArray typeArgsAll)
	{
		if (typeArgsAll.size == 0)
		{
			return agg.getThisType();
		}
		AggregateSymbol outerAgg = agg.GetOuterAgg();
		if (outerAgg == null)
		{
			return GetAggregate(agg, null, typeArgsAll);
		}
		int size = outerAgg.GetTypeVarsAll().Size;
		TypeArray typeArgsAll2 = m_BSymmgr.AllocParams(size, typeArgsAll, 0);
		TypeArray typeArgs = m_BSymmgr.AllocParams(agg.GetTypeVars().Size, typeArgsAll, size);
		AggregateType aggregate = GetAggregate(outerAgg, typeArgsAll2);
		return GetAggregate(agg, aggregate, typeArgs);
	}

	public PointerType GetPointer(CType baseType)
	{
		PointerType pointerType = m_typeTable.LookupPointer(baseType);
		if (pointerType == null)
		{
			Name predefName = m_BSymmgr.GetNameManager().GetPredefName(PredefinedName.PN_PTR);
			pointerType = m_typeFactory.CreatePointer(predefName, baseType);
			pointerType.InitFromParent();
			m_typeTable.InsertPointer(baseType, pointerType);
		}
		return pointerType;
	}

	public NullableType GetNullable(CType pUnderlyingType)
	{
		NullableType nullableType = m_typeTable.LookupNullable(pUnderlyingType);
		if (nullableType == null)
		{
			Name predefName = m_BSymmgr.GetNameManager().GetPredefName(PredefinedName.PN_NUB);
			nullableType = m_typeFactory.CreateNullable(predefName, pUnderlyingType, m_BSymmgr, this);
			nullableType.InitFromParent();
			m_typeTable.InsertNullable(pUnderlyingType, nullableType);
		}
		return nullableType;
	}

	public NullableType GetNubFromNullable(AggregateType ats)
	{
		return GetNullable(ats.GetTypeArgsAll().Item(0));
	}

	public ParameterModifierType GetParameterModifier(CType paramType, bool isOut)
	{
		Name predefName = m_BSymmgr.GetNameManager().GetPredefName(isOut ? PredefinedName.PN_OUTPARAM : PredefinedName.PN_REFPARAM);
		ParameterModifierType parameterModifierType = m_typeTable.LookupParameterModifier(predefName, paramType);
		if (parameterModifierType == null)
		{
			parameterModifierType = m_typeFactory.CreateParameterModifier(predefName, paramType);
			parameterModifierType.isOut = isOut;
			parameterModifierType.InitFromParent();
			m_typeTable.InsertParameterModifier(predefName, paramType, parameterModifierType);
		}
		return parameterModifierType;
	}

	public ErrorType GetErrorType(CType pParentType, AssemblyQualifiedNamespaceSymbol pParentNS, Name nameText, TypeArray typeArgs)
	{
		if (pParentType == null && pParentNS == null)
		{
			pParentNS = m_BSymmgr.GetRootNsAid(KAID.kaidGlobal);
		}
		if (typeArgs == null)
		{
			typeArgs = BSYMMGR.EmptyTypeArray();
		}
		Name nameFromPtrs = m_BSymmgr.GetNameFromPtrs(nameText, typeArgs);
		ErrorType errorType = null;
		errorType = ((pParentType == null) ? m_typeTable.LookupError(nameFromPtrs, pParentNS) : m_typeTable.LookupError(nameFromPtrs, pParentType));
		if (errorType == null)
		{
			errorType = m_typeFactory.CreateError(nameFromPtrs, pParentType, pParentNS, nameText, typeArgs);
			errorType.SetErrors(fHasErrors: true);
			if (pParentType != null)
			{
				m_typeTable.InsertError(nameFromPtrs, pParentType, errorType);
			}
			else
			{
				m_typeTable.InsertError(nameFromPtrs, pParentNS, errorType);
			}
		}
		return errorType;
	}

	public VoidType GetVoid()
	{
		return voidType;
	}

	public NullType GetNullType()
	{
		return nullType;
	}

	public OpenTypePlaceholderType GetUnitType()
	{
		return typeUnit;
	}

	public BoundLambdaType GetAnonMethType()
	{
		return typeAnonMeth;
	}

	public MethodGroupType GetMethGrpType()
	{
		return typeMethGrp;
	}

	public ArgumentListType GetArgListType()
	{
		return argListType;
	}

	public ErrorType GetErrorSym()
	{
		return errorType;
	}

	public AggregateSymbol GetNullable()
	{
		return GetOptPredefAgg(PredefinedType.PT_G_OPTIONAL);
	}

	public CType SubstType(CType typeSrc, TypeArray typeArgsCls, TypeArray typeArgsMeth, SubstTypeFlags grfst)
	{
		if (typeSrc == null)
		{
			return null;
		}
		SubstContext substContext = new SubstContext(typeArgsCls, typeArgsMeth, grfst);
		if (!substContext.FNop())
		{
			return SubstTypeCore(typeSrc, substContext);
		}
		return typeSrc;
	}

	public CType SubstType(CType typeSrc, TypeArray typeArgsCls)
	{
		return SubstType(typeSrc, typeArgsCls, null, SubstTypeFlags.NormNone);
	}

	public CType SubstType(CType typeSrc, TypeArray typeArgsCls, TypeArray typeArgsMeth)
	{
		return SubstType(typeSrc, typeArgsCls, typeArgsMeth, SubstTypeFlags.NormNone);
	}

	public TypeArray SubstTypeArray(TypeArray taSrc, SubstContext pctx)
	{
		if (taSrc == null || taSrc.Size == 0 || pctx == null || pctx.FNop())
		{
			return taSrc;
		}
		CType[] array = new CType[taSrc.Size];
		for (int i = 0; i < taSrc.Size; i++)
		{
			array[i] = SubstTypeCore(taSrc.Item(i), pctx);
		}
		return m_BSymmgr.AllocParams(taSrc.size, array);
	}

	public TypeArray SubstTypeArray(TypeArray taSrc, TypeArray typeArgsCls, TypeArray typeArgsMeth, SubstTypeFlags grfst)
	{
		if (taSrc == null || taSrc.Size == 0)
		{
			return taSrc;
		}
		SubstContext substContext = new SubstContext(typeArgsCls, typeArgsMeth, grfst);
		if (substContext.FNop())
		{
			return taSrc;
		}
		CType[] array = new CType[taSrc.Size];
		for (int i = 0; i < taSrc.Size; i++)
		{
			array[i] = SubstTypeCore(taSrc.Item(i), substContext);
		}
		return m_BSymmgr.AllocParams(taSrc.Size, array);
	}

	public TypeArray SubstTypeArray(TypeArray taSrc, TypeArray typeArgsCls, TypeArray typeArgsMeth)
	{
		return SubstTypeArray(taSrc, typeArgsCls, typeArgsMeth, SubstTypeFlags.NormNone);
	}

	public TypeArray SubstTypeArray(TypeArray taSrc, TypeArray typeArgsCls)
	{
		return SubstTypeArray(taSrc, typeArgsCls, null, SubstTypeFlags.NormNone);
	}

	private CType SubstTypeCore(CType type, SubstContext pctx)
	{
		switch (type.GetTypeKind())
		{
		default:
			return type;
		case TypeKind.TK_VoidType:
		case TypeKind.TK_NullType:
		case TypeKind.TK_OpenTypePlaceholderType:
		case TypeKind.TK_BoundLambdaType:
		case TypeKind.TK_UnboundLambdaType:
		case TypeKind.TK_MethodGroupType:
		case TypeKind.TK_NaturalIntegerType:
		case TypeKind.TK_ArgumentListType:
			return type;
		case TypeKind.TK_ParameterModifierType:
		{
			CType referentType;
			CType cType = SubstTypeCore(referentType = type.AsParameterModifierType().GetParameterType(), pctx);
			if (cType != referentType)
			{
				return GetParameterModifier(cType, type.AsParameterModifierType().isOut);
			}
			return type;
		}
		case TypeKind.TK_ArrayType:
		{
			CType referentType;
			CType cType = SubstTypeCore(referentType = type.AsArrayType().GetElementType(), pctx);
			if (cType != referentType)
			{
				return GetArray(cType, type.AsArrayType().rank);
			}
			return type;
		}
		case TypeKind.TK_PointerType:
		{
			CType referentType;
			CType cType = SubstTypeCore(referentType = type.AsPointerType().GetReferentType(), pctx);
			if (cType != referentType)
			{
				return GetPointer(cType);
			}
			return type;
		}
		case TypeKind.TK_NullableType:
		{
			CType referentType;
			CType cType = SubstTypeCore(referentType = type.AsNullableType().GetUnderlyingType(), pctx);
			if (cType != referentType)
			{
				return GetNullable(cType);
			}
			return type;
		}
		case TypeKind.TK_AggregateType:
			if (type.AsAggregateType().GetTypeArgsAll().size > 0)
			{
				AggregateType aggregateType = type.AsAggregateType();
				TypeArray typeArray = SubstTypeArray(aggregateType.GetTypeArgsAll(), pctx);
				if (aggregateType.GetTypeArgsAll() != typeArray)
				{
					return GetAggregate(aggregateType.getAggregate(), typeArray);
				}
			}
			return type;
		case TypeKind.TK_ErrorType:
			if (type.AsErrorType().HasParent())
			{
				ErrorType errorType = type.AsErrorType();
				CType cType2 = null;
				if (errorType.HasTypeParent())
				{
					cType2 = SubstTypeCore(errorType.GetTypeParent(), pctx);
				}
				TypeArray typeArray2 = SubstTypeArray(errorType.typeArgs, pctx);
				if (typeArray2 != errorType.typeArgs || (errorType.HasTypeParent() && cType2 != errorType.GetTypeParent()))
				{
					return GetErrorType(cType2, errorType.GetNSParent(), errorType.nameText, typeArray2);
				}
			}
			return type;
		case TypeKind.TK_TypeParameterType:
		{
			TypeParameterSymbol typeParameterSymbol = type.AsTypeParameterType().GetTypeParameterSymbol();
			int indexInTotalParameters = typeParameterSymbol.GetIndexInTotalParameters();
			if (typeParameterSymbol.IsMethodTypeParameter())
			{
				if ((pctx.grfst & SubstTypeFlags.DenormMeth) != 0 && typeParameterSymbol.parent != null)
				{
					return type;
				}
				if (indexInTotalParameters < pctx.ctypeMeth)
				{
					return pctx.prgtypeMeth[indexInTotalParameters];
				}
				if ((pctx.grfst & SubstTypeFlags.NormMeth) == 0)
				{
					return type;
				}
				return GetStdMethTypeVar(indexInTotalParameters);
			}
			if ((pctx.grfst & SubstTypeFlags.DenormClass) != 0 && typeParameterSymbol.parent != null)
			{
				return type;
			}
			if (indexInTotalParameters >= pctx.ctypeCls)
			{
				if ((pctx.grfst & SubstTypeFlags.NormClass) == 0)
				{
					return type;
				}
				return GetStdClsTypeVar(indexInTotalParameters);
			}
			return pctx.prgtypeCls[indexInTotalParameters];
		}
		}
	}

	public bool SubstEqualTypes(CType typeDst, CType typeSrc, TypeArray typeArgsCls, TypeArray typeArgsMeth, SubstTypeFlags grfst)
	{
		if (typeDst.Equals(typeSrc))
		{
			return true;
		}
		SubstContext substContext = new SubstContext(typeArgsCls, typeArgsMeth, grfst);
		if (!substContext.FNop())
		{
			return SubstEqualTypesCore(typeDst, typeSrc, substContext);
		}
		return false;
	}

	public bool SubstEqualTypeArrays(TypeArray taDst, TypeArray taSrc, TypeArray typeArgsCls, TypeArray typeArgsMeth, SubstTypeFlags grfst)
	{
		if (taDst == taSrc || (taDst != null && taDst.Equals(taSrc)))
		{
			return true;
		}
		if (taDst.Size != taSrc.Size)
		{
			return false;
		}
		if (taDst.Size == 0)
		{
			return true;
		}
		SubstContext substContext = new SubstContext(typeArgsCls, typeArgsMeth, grfst);
		if (substContext.FNop())
		{
			return false;
		}
		for (int i = 0; i < taDst.size; i++)
		{
			if (!SubstEqualTypesCore(taDst.Item(i), taSrc.Item(i), substContext))
			{
				return false;
			}
		}
		return true;
	}

	public bool SubstEqualTypesCore(CType typeDst, CType typeSrc, SubstContext pctx)
	{
		while (typeDst != typeSrc && !typeDst.Equals(typeSrc))
		{
			switch (typeSrc.GetTypeKind())
			{
			default:
				return false;
			case TypeKind.TK_VoidType:
			case TypeKind.TK_NullType:
			case TypeKind.TK_OpenTypePlaceholderType:
				return false;
			case TypeKind.TK_ArrayType:
				if (typeDst.GetTypeKind() != TypeKind.TK_ArrayType || typeDst.AsArrayType().rank != typeSrc.AsArrayType().rank)
				{
					return false;
				}
				break;
			case TypeKind.TK_ParameterModifierType:
				if (typeDst.GetTypeKind() != TypeKind.TK_ParameterModifierType || ((pctx.grfst & SubstTypeFlags.NoRefOutDifference) == 0 && typeDst.AsParameterModifierType().isOut != typeSrc.AsParameterModifierType().isOut))
				{
					return false;
				}
				break;
			case TypeKind.TK_PointerType:
			case TypeKind.TK_NullableType:
				if (typeDst.GetTypeKind() != typeSrc.GetTypeKind())
				{
					return false;
				}
				break;
			case TypeKind.TK_AggregateType:
			{
				if (typeDst.GetTypeKind() != 0)
				{
					return false;
				}
				AggregateType aggregateType = typeSrc.AsAggregateType();
				AggregateType aggregateType2 = typeDst.AsAggregateType();
				if (aggregateType.getAggregate() != aggregateType2.getAggregate())
				{
					return false;
				}
				for (int i = 0; i < aggregateType.GetTypeArgsAll().Size; i++)
				{
					if (!SubstEqualTypesCore(aggregateType2.GetTypeArgsAll().Item(i), aggregateType.GetTypeArgsAll().Item(i), pctx))
					{
						return false;
					}
				}
				return true;
			}
			case TypeKind.TK_ErrorType:
			{
				if (!typeDst.IsErrorType() || !typeSrc.AsErrorType().HasParent() || !typeDst.AsErrorType().HasParent())
				{
					return false;
				}
				ErrorType errorType = typeSrc.AsErrorType();
				ErrorType errorType2 = typeDst.AsErrorType();
				if (errorType.nameText != errorType2.nameText || errorType.typeArgs.Size != errorType2.typeArgs.Size)
				{
					return false;
				}
				if (errorType.HasTypeParent() != errorType2.HasTypeParent())
				{
					return false;
				}
				if (errorType.HasTypeParent())
				{
					if (errorType.GetTypeParent() != errorType2.GetTypeParent())
					{
						return false;
					}
					if (!SubstEqualTypesCore(errorType2.GetTypeParent(), errorType.GetTypeParent(), pctx))
					{
						return false;
					}
				}
				else if (errorType.GetNSParent() != errorType2.GetNSParent())
				{
					return false;
				}
				for (int j = 0; j < errorType.typeArgs.Size; j++)
				{
					if (!SubstEqualTypesCore(errorType2.typeArgs.Item(j), errorType.typeArgs.Item(j), pctx))
					{
						return false;
					}
				}
				return true;
			}
			case TypeKind.TK_TypeParameterType:
			{
				TypeParameterSymbol typeParameterSymbol = typeSrc.AsTypeParameterType().GetTypeParameterSymbol();
				int indexInTotalParameters = typeParameterSymbol.GetIndexInTotalParameters();
				if (typeParameterSymbol.IsMethodTypeParameter())
				{
					if ((pctx.grfst & SubstTypeFlags.DenormMeth) != 0 && typeParameterSymbol.parent != null)
					{
						return false;
					}
					if (indexInTotalParameters < pctx.ctypeMeth && pctx.prgtypeMeth != null)
					{
						return typeDst == pctx.prgtypeMeth[indexInTotalParameters];
					}
					if ((pctx.grfst & SubstTypeFlags.NormMeth) != 0)
					{
						return typeDst == GetStdMethTypeVar(indexInTotalParameters);
					}
				}
				else
				{
					if ((pctx.grfst & SubstTypeFlags.DenormClass) != 0 && typeParameterSymbol.parent != null)
					{
						return false;
					}
					if (indexInTotalParameters < pctx.ctypeCls)
					{
						return typeDst == pctx.prgtypeCls[indexInTotalParameters];
					}
					if ((pctx.grfst & SubstTypeFlags.NormClass) != 0)
					{
						return typeDst == GetStdClsTypeVar(indexInTotalParameters);
					}
				}
				return false;
			}
			}
			typeSrc = typeSrc.GetBaseOrParameterOrElementType();
			typeDst = typeDst.GetBaseOrParameterOrElementType();
		}
		return true;
	}

	public void ReportMissingPredefTypeError(ErrorHandling errorContext, PredefinedType pt)
	{
		m_predefTypes.ReportMissingPredefTypeError(errorContext, pt);
	}

	public static bool TypeContainsType(CType type, CType typeFind)
	{
		while (type != typeFind && !type.Equals(typeFind))
		{
			switch (type.GetTypeKind())
			{
			default:
				return false;
			case TypeKind.TK_VoidType:
			case TypeKind.TK_NullType:
			case TypeKind.TK_OpenTypePlaceholderType:
				return false;
			case TypeKind.TK_ArrayType:
			case TypeKind.TK_PointerType:
			case TypeKind.TK_ParameterModifierType:
			case TypeKind.TK_NullableType:
				type = type.GetBaseOrParameterOrElementType();
				break;
			case TypeKind.TK_AggregateType:
			{
				AggregateType aggregateType = type.AsAggregateType();
				for (int j = 0; j < aggregateType.GetTypeArgsAll().Size; j++)
				{
					if (TypeContainsType(aggregateType.GetTypeArgsAll().Item(j), typeFind))
					{
						return true;
					}
				}
				return false;
			}
			case TypeKind.TK_ErrorType:
				if (type.AsErrorType().HasParent())
				{
					ErrorType errorType = type.AsErrorType();
					for (int i = 0; i < errorType.typeArgs.Size; i++)
					{
						if (TypeContainsType(errorType.typeArgs.Item(i), typeFind))
						{
							return true;
						}
					}
					if (errorType.HasTypeParent())
					{
						type = errorType.GetTypeParent();
						break;
					}
				}
				return false;
			case TypeKind.TK_TypeParameterType:
				return false;
			}
		}
		return true;
	}

	public static bool TypeContainsTyVars(CType type, TypeArray typeVars)
	{
		while (true)
		{
			switch (type.GetTypeKind())
			{
			default:
				return false;
			case TypeKind.TK_VoidType:
			case TypeKind.TK_NullType:
			case TypeKind.TK_OpenTypePlaceholderType:
			case TypeKind.TK_BoundLambdaType:
			case TypeKind.TK_UnboundLambdaType:
			case TypeKind.TK_MethodGroupType:
				return false;
			case TypeKind.TK_ArrayType:
			case TypeKind.TK_PointerType:
			case TypeKind.TK_ParameterModifierType:
			case TypeKind.TK_NullableType:
				type = type.GetBaseOrParameterOrElementType();
				break;
			case TypeKind.TK_AggregateType:
			{
				AggregateType aggregateType = type.AsAggregateType();
				for (int i = 0; i < aggregateType.GetTypeArgsAll().Size; i++)
				{
					if (TypeContainsTyVars(aggregateType.GetTypeArgsAll().Item(i), typeVars))
					{
						return true;
					}
				}
				return false;
			}
			case TypeKind.TK_ErrorType:
				if (type.AsErrorType().HasParent())
				{
					ErrorType errorType = type.AsErrorType();
					for (int j = 0; j < errorType.typeArgs.Size; j++)
					{
						if (TypeContainsTyVars(errorType.typeArgs.Item(j), typeVars))
						{
							return true;
						}
					}
					if (errorType.HasTypeParent())
					{
						type = errorType.GetTypeParent();
						break;
					}
				}
				return false;
			case TypeKind.TK_TypeParameterType:
				if (typeVars != null && typeVars.Size > 0)
				{
					int indexInTotalParameters = type.AsTypeParameterType().GetIndexInTotalParameters();
					if (indexInTotalParameters < typeVars.Size)
					{
						return type == typeVars.Item(indexInTotalParameters);
					}
					return false;
				}
				return true;
			}
		}
	}

	public static bool ParametersContainTyVar(TypeArray @params, TypeParameterType typeFind)
	{
		for (int i = 0; i < @params.size; i++)
		{
			CType type = @params[i];
			if (TypeContainsType(type, typeFind))
			{
				return true;
			}
		}
		return false;
	}

	public AggregateSymbol GetReqPredefAgg(PredefinedType pt)
	{
		return m_predefTypes.GetReqPredefAgg(pt);
	}

	public AggregateSymbol GetOptPredefAgg(PredefinedType pt)
	{
		return m_predefTypes.GetOptPredefAgg(pt);
	}

	public TypeArray CreateArrayOfUnitTypes(int cSize)
	{
		CType[] array = new CType[cSize];
		for (int i = 0; i < cSize; i++)
		{
			array[i] = GetUnitType();
		}
		return m_BSymmgr.AllocParams(cSize, array);
	}

	public TypeArray ConcatenateTypeArrays(TypeArray pTypeArray1, TypeArray pTypeArray2)
	{
		return m_BSymmgr.ConcatParams(pTypeArray1, pTypeArray2);
	}

	public TypeArray GetStdMethTyVarArray(int cTyVars)
	{
		TypeParameterType[] array = new TypeParameterType[cTyVars];
		for (int i = 0; i < cTyVars; i++)
		{
			array[i] = GetStdMethTypeVar(i);
		}
		BSYMMGR bSymmgr = m_BSymmgr;
		CType[] prgtype = array;
		return bSymmgr.AllocParams(cTyVars, prgtype);
	}

	public CType SubstType(CType typeSrc, SubstContext pctx)
	{
		if (pctx != null && !pctx.FNop())
		{
			return SubstTypeCore(typeSrc, pctx);
		}
		return typeSrc;
	}

	public CType SubstType(CType typeSrc, AggregateType atsCls)
	{
		return SubstType(typeSrc, atsCls, null);
	}

	public CType SubstType(CType typeSrc, AggregateType atsCls, TypeArray typeArgsMeth)
	{
		return SubstType(typeSrc, atsCls?.GetTypeArgsAll(), typeArgsMeth);
	}

	public CType SubstType(CType typeSrc, CType typeCls, TypeArray typeArgsMeth)
	{
		return SubstType(typeSrc, typeCls.IsAggregateType() ? typeCls.AsAggregateType().GetTypeArgsAll() : null, typeArgsMeth);
	}

	public TypeArray SubstTypeArray(TypeArray taSrc, AggregateType atsCls, TypeArray typeArgsMeth)
	{
		return SubstTypeArray(taSrc, atsCls?.GetTypeArgsAll(), typeArgsMeth);
	}

	public TypeArray SubstTypeArray(TypeArray taSrc, AggregateType atsCls)
	{
		return SubstTypeArray(taSrc, atsCls, null);
	}

	public bool SubstEqualTypes(CType typeDst, CType typeSrc, CType typeCls, TypeArray typeArgsMeth)
	{
		return SubstEqualTypes(typeDst, typeSrc, typeCls.IsAggregateType() ? typeCls.AsAggregateType().GetTypeArgsAll() : null, typeArgsMeth, SubstTypeFlags.NormNone);
	}

	public bool SubstEqualTypes(CType typeDst, CType typeSrc, CType typeCls)
	{
		return SubstEqualTypes(typeDst, typeSrc, typeCls, null);
	}

	public TypeParameterType GetStdMethTypeVar(int iv)
	{
		return stvcMethod.GetTypeVarSym(iv, this, fMeth: true);
	}

	public TypeParameterType GetStdClsTypeVar(int iv)
	{
		return stvcClass.GetTypeVarSym(iv, this, fMeth: false);
	}

	public TypeParameterType GetTypeParameter(TypeParameterSymbol pSymbol)
	{
		TypeParameterType typeParameterType = m_typeTable.LookupTypeParameter(pSymbol);
		if (typeParameterType == null)
		{
			typeParameterType = m_typeFactory.CreateTypeParameter(pSymbol);
			m_typeTable.InsertTypeParameter(pSymbol, typeParameterType);
		}
		return typeParameterType;
	}

	internal void Init(BSYMMGR bsymmgr, PredefinedTypes predefTypes)
	{
		m_BSymmgr = bsymmgr;
		m_predefTypes = predefTypes;
	}

	internal bool GetBestAccessibleType(CSemanticChecker semanticChecker, BindingContext bindingContext, CType typeSrc, out CType typeDst)
	{
		typeDst = null;
		if (semanticChecker.CheckTypeAccess(typeSrc, bindingContext.ContextForMemberLookup()))
		{
			typeDst = typeSrc;
			return true;
		}
		if (typeSrc.IsParameterModifierType() || typeSrc.IsPointerType())
		{
			return false;
		}
		if ((typeSrc.isInterfaceType() || typeSrc.isDelegateType()) && TryVarianceAdjustmentToGetAccessibleType(semanticChecker, bindingContext, typeSrc.AsAggregateType(), out var typeDst2))
		{
			typeDst = typeDst2;
			return true;
		}
		if (typeSrc.IsArrayType() && TryArrayVarianceAdjustmentToGetAccessibleType(semanticChecker, bindingContext, typeSrc.AsArrayType(), out typeDst2))
		{
			typeDst = typeDst2;
			return true;
		}
		if (typeSrc.IsNullableType())
		{
			typeDst = GetOptPredefAgg(PredefinedType.PT_VALUE).getThisType();
			return true;
		}
		if (typeSrc.IsArrayType())
		{
			typeDst = GetReqPredefAgg(PredefinedType.PT_ARRAY).getThisType();
			return true;
		}
		if (typeSrc.IsAggregateType())
		{
			AggregateType aggregateType = typeSrc.AsAggregateType();
			AggregateType aggregateType2 = aggregateType.GetBaseClass();
			if (aggregateType2 == null)
			{
				aggregateType2 = GetReqPredefAgg(PredefinedType.PT_OBJECT).getThisType();
			}
			return GetBestAccessibleType(semanticChecker, bindingContext, aggregateType2, out typeDst);
		}
		return false;
	}

	private bool TryVarianceAdjustmentToGetAccessibleType(CSemanticChecker semanticChecker, BindingContext bindingContext, AggregateType typeSrc, out CType typeDst)
	{
		typeDst = null;
		AggregateSymbol owningAggregate = typeSrc.GetOwningAggregate();
		AggregateType thisType = owningAggregate.getThisType();
		if (!semanticChecker.CheckTypeAccess(thisType, bindingContext.ContextForMemberLookup()))
		{
			return false;
		}
		TypeArray typeArgsThis = typeSrc.GetTypeArgsThis();
		TypeArray typeArgsThis2 = thisType.GetTypeArgsThis();
		CType[] array = new CType[typeArgsThis.size];
		for (int i = 0; i < typeArgsThis.size; i++)
		{
			if (semanticChecker.CheckTypeAccess(typeArgsThis.Item(i), bindingContext.ContextForMemberLookup()))
			{
				array[i] = typeArgsThis.Item(i);
				continue;
			}
			if (!typeArgsThis.Item(i).IsRefType() || !typeArgsThis2.Item(i).AsTypeParameterType().Covariant)
			{
				return false;
			}
			if (GetBestAccessibleType(semanticChecker, bindingContext, typeArgsThis.Item(i), out var typeDst2))
			{
				array[i] = typeDst2;
				continue;
			}
			return false;
		}
		TypeArray typeArgs = semanticChecker.getBSymmgr().AllocParams(typeArgsThis.size, array);
		CType aggregate = GetAggregate(owningAggregate, typeSrc.outerType, typeArgs);
		if (!TypeBind.CheckConstraints(semanticChecker, null, aggregate, CheckConstraintsFlags.NoErrors))
		{
			return false;
		}
		typeDst = aggregate;
		return true;
	}

	private bool TryArrayVarianceAdjustmentToGetAccessibleType(CSemanticChecker semanticChecker, BindingContext bindingContext, ArrayType typeSrc, out CType typeDst)
	{
		typeDst = null;
		CType elementType = typeSrc.GetElementType();
		if (!elementType.IsRefType())
		{
			return false;
		}
		if (GetBestAccessibleType(semanticChecker, bindingContext, elementType, out var typeDst2))
		{
			typeDst = GetArray(typeDst2, typeSrc.rank);
			return true;
		}
		return false;
	}

	internal bool InternalsVisibleTo(Assembly assemblyThatDefinesAttribute, Assembly assemblyToCheck)
	{
		Tuple<Assembly, Assembly> key = Tuple.Create(assemblyThatDefinesAttribute, assemblyToCheck);
		if (!internalsVisibleToCalculated.TryGetValue(key, out var value))
		{
			AssemblyName assyName = null;
			try
			{
				assyName = assemblyToCheck.GetName();
			}
			catch (SecurityException)
			{
				value = false;
				goto IL_007a;
			}
			value = (from ivta in assemblyThatDefinesAttribute.GetCustomAttributes(inherit: true).OfType<InternalsVisibleToAttribute>()
				select new AssemblyName(ivta.AssemblyName)).Any((AssemblyName an) => AssemblyName.ReferenceMatchesDefinition(an, assyName));
			goto IL_007a;
		}
		goto IL_0087;
		IL_0087:
		return value;
		IL_007a:
		internalsVisibleToCalculated[key] = value;
		goto IL_0087;
	}
}
