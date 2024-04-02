using Microsoft.CSharp.RuntimeBinder.Errors;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class SymbolLoader
{
	private NameManager m_nameManager;

	public PredefinedMembers PredefinedMembers { get; private set; }

	public GlobalSymbolContext GlobalSymbolContext { get; private set; }

	public ErrorHandling ErrorContext { get; private set; }

	public SymbolTable RuntimeBinderSymbolTable { get; private set; }

	public TypeManager TypeManager => GlobalSymbolContext.TypeManager;

	public SymbolLoader(GlobalSymbolContext globalSymbols, UserStringBuilder userStringBuilder, ErrorHandling errorContext)
	{
		m_nameManager = globalSymbols.GetNameManager();
		PredefinedMembers = new PredefinedMembers(this);
		ErrorContext = errorContext;
		GlobalSymbolContext = globalSymbols;
	}

	public ErrorHandling GetErrorContext()
	{
		return ErrorContext;
	}

	public GlobalSymbolContext GetGlobalSymbolContext()
	{
		return GlobalSymbolContext;
	}

	public MethodSymbol LookupInvokeMeth(AggregateSymbol pAggDel)
	{
		for (Symbol symbol = LookupAggMember(GetNameManager().GetPredefName(PredefinedName.PN_INVOKE), pAggDel, symbmask_t.MASK_ALL); symbol != null; symbol = LookupNextSym(symbol, pAggDel, symbmask_t.MASK_ALL))
		{
			if (symbol.IsMethodSymbol() && symbol.AsMethodSymbol().isInvoke())
			{
				return symbol.AsMethodSymbol();
			}
		}
		return null;
	}

	public NameManager GetNameManager()
	{
		return m_nameManager;
	}

	public PredefinedTypes getPredefTypes()
	{
		return GlobalSymbolContext.GetPredefTypes();
	}

	public TypeManager GetTypeManager()
	{
		return TypeManager;
	}

	public PredefinedMembers getPredefinedMembers()
	{
		return PredefinedMembers;
	}

	public BSYMMGR getBSymmgr()
	{
		return GlobalSymbolContext.GetGlobalSymbols();
	}

	public SymFactory GetGlobalSymbolFactory()
	{
		return GlobalSymbolContext.GetGlobalSymbolFactory();
	}

	public MiscSymFactory GetGlobalMiscSymFactory()
	{
		return GlobalSymbolContext.GetGlobalMiscSymFactory();
	}

	public AggregateType GetReqPredefType(PredefinedType pt)
	{
		return GetReqPredefType(pt, fEnsureState: true);
	}

	public AggregateType GetReqPredefType(PredefinedType pt, bool fEnsureState)
	{
		return GetTypeManager().GetReqPredefAgg(pt)?.getThisType();
	}

	public AggregateSymbol GetOptPredefAgg(PredefinedType pt)
	{
		return GetOptPredefAgg(pt, fEnsureState: true);
	}

	public AggregateSymbol GetOptPredefAgg(PredefinedType pt, bool fEnsureState)
	{
		return GetTypeManager().GetOptPredefAgg(pt);
	}

	public AggregateType GetOptPredefType(PredefinedType pt)
	{
		return GetOptPredefType(pt, fEnsureState: true);
	}

	public AggregateType GetOptPredefType(PredefinedType pt, bool fEnsureState)
	{
		return GetTypeManager().GetOptPredefAgg(pt)?.getThisType();
	}

	public AggregateType GetOptPredefTypeErr(PredefinedType pt, bool fEnsureState)
	{
		AggregateSymbol optPredefAgg = GetTypeManager().GetOptPredefAgg(pt);
		if (optPredefAgg == null)
		{
			getPredefTypes().ReportMissingPredefTypeError(ErrorContext, pt);
			return null;
		}
		return optPredefAgg.getThisType();
	}

	public Symbol LookupAggMember(Name name, AggregateSymbol agg, symbmask_t mask)
	{
		return getBSymmgr().LookupAggMember(name, agg, mask);
	}

	public Symbol LookupNextSym(Symbol sym, ParentSymbol parent, symbmask_t kindmask)
	{
		return BSYMMGR.LookupNextSym(sym, parent, kindmask);
	}

	public bool isManagedType(CType type)
	{
		return type.computeManagedType(this);
	}

	public AggregateType GetAggTypeSym(CType typeSym)
	{
		return typeSym.GetTypeKind() switch
		{
			TypeKind.TK_AggregateType => typeSym.AsAggregateType(), 
			TypeKind.TK_ArrayType => GetReqPredefType(PredefinedType.PT_ARRAY), 
			TypeKind.TK_TypeParameterType => typeSym.AsTypeParameterType().GetEffectiveBaseClass(), 
			TypeKind.TK_NullableType => typeSym.AsNullableType().GetAts(ErrorContext), 
			_ => null, 
		};
	}

	public bool IsBaseInterface(CType pDerived, CType pBase)
	{
		if (!pBase.isInterfaceType())
		{
			return false;
		}
		if (!pDerived.IsAggregateType())
		{
			return false;
		}
		for (AggregateType aggregateType = pDerived.AsAggregateType(); aggregateType != null; aggregateType = aggregateType.GetBaseClass())
		{
			TypeArray ifacesAll = aggregateType.GetIfacesAll();
			for (int i = 0; i < ifacesAll.Size; i++)
			{
				if (AreTypesEqualForConversion(ifacesAll.Item(i), pBase))
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool IsBaseClassOfClass(CType pDerived, CType pBase)
	{
		if (!pDerived.isClassType())
		{
			return false;
		}
		return IsBaseClass(pDerived, pBase);
	}

	public bool IsBaseClass(CType pDerived, CType pBase)
	{
		if (!pBase.isClassType())
		{
			return false;
		}
		if (pDerived.IsNullableType())
		{
			pDerived = pDerived.AsNullableType().GetAts(ErrorContext);
			if (pDerived == null)
			{
				return false;
			}
		}
		if (!pDerived.IsAggregateType())
		{
			return false;
		}
		AggregateType aggregateType = pDerived.AsAggregateType();
		AggregateType aggregateType2 = pBase.AsAggregateType();
		for (AggregateType baseClass = aggregateType.GetBaseClass(); baseClass != null; baseClass = baseClass.GetBaseClass())
		{
			if (baseClass == aggregateType2)
			{
				return true;
			}
		}
		return false;
	}

	private bool HasCovariantArrayConversion(ArrayType pSource, ArrayType pDest)
	{
		if (pSource.rank == pDest.rank)
		{
			return HasImplicitReferenceConversion(pSource.GetElementType(), pDest.GetElementType());
		}
		return false;
	}

	public bool HasIdentityOrImplicitReferenceConversion(CType pSource, CType pDest)
	{
		if (AreTypesEqualForConversion(pSource, pDest))
		{
			return true;
		}
		return HasImplicitReferenceConversion(pSource, pDest);
	}

	protected bool AreTypesEqualForConversion(CType pType1, CType pType2)
	{
		return pType1.Equals(pType2);
	}

	private bool HasArrayConversionToInterface(ArrayType pSource, CType pDest)
	{
		if (pSource.rank != 1)
		{
			return false;
		}
		if (!pDest.isInterfaceType())
		{
			return false;
		}
		if (pDest.isPredefType(PredefinedType.PT_IENUMERABLE))
		{
			return true;
		}
		AggregateType aggregateType = pDest.AsAggregateType();
		AggregateSymbol aggregate = pDest.getAggregate();
		if (!aggregate.isPredefAgg(PredefinedType.PT_G_ILIST) && !aggregate.isPredefAgg(PredefinedType.PT_G_ICOLLECTION) && !aggregate.isPredefAgg(PredefinedType.PT_G_IENUMERABLE) && !aggregate.isPredefAgg(PredefinedType.PT_G_IREADONLYCOLLECTION) && !aggregate.isPredefAgg(PredefinedType.PT_G_IREADONLYLIST))
		{
			return false;
		}
		CType elementType = pSource.GetElementType();
		CType pDest2 = aggregateType.GetTypeArgsAll().Item(0);
		return HasIdentityOrImplicitReferenceConversion(elementType, pDest2);
	}

	public bool HasImplicitReferenceConversion(CType pSource, CType pDest)
	{
		if (pSource.IsRefType() && pDest.isPredefType(PredefinedType.PT_OBJECT))
		{
			return true;
		}
		if (pSource.isClassType() && pDest.isClassType() && IsBaseClass(pSource, pDest))
		{
			return true;
		}
		if (pSource.isClassType() && pDest.isInterfaceType() && HasAnyBaseInterfaceConversion(pSource, pDest))
		{
			return true;
		}
		if (pSource.isInterfaceType() && pDest.isInterfaceType() && HasAnyBaseInterfaceConversion(pSource, pDest))
		{
			return true;
		}
		if (pSource.isInterfaceType() && pDest.isInterfaceType() && pSource != pDest && HasInterfaceConversion(pSource.AsAggregateType(), pDest.AsAggregateType()))
		{
			return true;
		}
		if (pSource.IsArrayType() && pDest.IsArrayType() && HasCovariantArrayConversion(pSource.AsArrayType(), pDest.AsArrayType()))
		{
			return true;
		}
		if (pSource.IsArrayType() && (pDest.isPredefType(PredefinedType.PT_ARRAY) || IsBaseInterface(GetReqPredefType(PredefinedType.PT_ARRAY, fEnsureState: false), pDest)))
		{
			return true;
		}
		if (pSource.IsArrayType() && HasArrayConversionToInterface(pSource.AsArrayType(), pDest))
		{
			return true;
		}
		if (pSource.isDelegateType() && (pDest.isPredefType(PredefinedType.PT_MULTIDEL) || pDest.isPredefType(PredefinedType.PT_DELEGATE) || IsBaseInterface(GetReqPredefType(PredefinedType.PT_MULTIDEL, fEnsureState: false), pDest)))
		{
			return true;
		}
		if (pSource.isDelegateType() && pDest.isDelegateType() && HasDelegateConversion(pSource.AsAggregateType(), pDest.AsAggregateType()))
		{
			return true;
		}
		if (pSource.IsNullType() && pDest.IsRefType())
		{
			return true;
		}
		if (pSource.IsNullType() && pDest.IsNullableType())
		{
			return true;
		}
		if (pSource.IsTypeParameterType() && HasImplicitReferenceTypeParameterConversion(pSource.AsTypeParameterType(), pDest))
		{
			return true;
		}
		return false;
	}

	private bool HasImplicitReferenceTypeParameterConversion(TypeParameterType pSource, CType pDest)
	{
		if (!pSource.IsRefType())
		{
			return false;
		}
		AggregateType effectiveBaseClass = pSource.GetEffectiveBaseClass();
		if (pDest == effectiveBaseClass)
		{
			return true;
		}
		if (IsBaseClass(effectiveBaseClass, pDest))
		{
			return true;
		}
		if (IsBaseInterface(effectiveBaseClass, pDest))
		{
			return true;
		}
		TypeArray interfaceBounds = pSource.GetInterfaceBounds();
		for (int i = 0; i < interfaceBounds.Size; i++)
		{
			if (interfaceBounds.Item(i) == pDest)
			{
				return true;
			}
		}
		if (pDest.IsTypeParameterType() && pSource.DependsOn(pDest.AsTypeParameterType()))
		{
			return true;
		}
		return false;
	}

	private bool HasAnyBaseInterfaceConversion(CType pDerived, CType pBase)
	{
		if (!pBase.isInterfaceType())
		{
			return false;
		}
		if (!pDerived.IsAggregateType())
		{
			return false;
		}
		for (AggregateType aggregateType = pDerived.AsAggregateType(); aggregateType != null; aggregateType = aggregateType.GetBaseClass())
		{
			TypeArray ifacesAll = aggregateType.GetIfacesAll();
			for (int i = 0; i < ifacesAll.size; i++)
			{
				if (HasInterfaceConversion(ifacesAll.Item(i).AsAggregateType(), pBase.AsAggregateType()))
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool HasInterfaceConversion(AggregateType pSource, AggregateType pDest)
	{
		return HasVariantConversion(pSource, pDest);
	}

	private bool HasDelegateConversion(AggregateType pSource, AggregateType pDest)
	{
		return HasVariantConversion(pSource, pDest);
	}

	private bool HasVariantConversion(AggregateType pSource, AggregateType pDest)
	{
		if (pSource == pDest)
		{
			return true;
		}
		AggregateSymbol aggregate = pSource.getAggregate();
		if (aggregate != pDest.getAggregate())
		{
			return false;
		}
		TypeArray typeVarsAll = aggregate.GetTypeVarsAll();
		TypeArray typeArgsAll = pSource.GetTypeArgsAll();
		TypeArray typeArgsAll2 = pDest.GetTypeArgsAll();
		for (int i = 0; i < typeVarsAll.size; i++)
		{
			CType cType = typeArgsAll.Item(i);
			CType cType2 = typeArgsAll2.Item(i);
			if (cType != cType2)
			{
				TypeParameterType typeParameterType = typeVarsAll.Item(i).AsTypeParameterType();
				if (typeParameterType.Invariant)
				{
					return false;
				}
				if (typeParameterType.Covariant && !HasImplicitReferenceConversion(cType, cType2))
				{
					return false;
				}
				if (typeParameterType.Contravariant && !HasImplicitReferenceConversion(cType2, cType))
				{
					return false;
				}
			}
		}
		return true;
	}

	private bool HasImplicitBoxingTypeParameterConversion(TypeParameterType pSource, CType pDest)
	{
		if (pSource.IsRefType())
		{
			return false;
		}
		AggregateType effectiveBaseClass = pSource.GetEffectiveBaseClass();
		if (pDest == effectiveBaseClass)
		{
			return true;
		}
		if (IsBaseClass(effectiveBaseClass, pDest))
		{
			return true;
		}
		if (IsBaseInterface(effectiveBaseClass, pDest))
		{
			return true;
		}
		TypeArray interfaceBounds = pSource.GetInterfaceBounds();
		for (int i = 0; i < interfaceBounds.Size; i++)
		{
			if (interfaceBounds.Item(i) == pDest)
			{
				return true;
			}
		}
		return false;
	}

	private bool HasImplicitTypeParameterBaseConversion(TypeParameterType pSource, CType pDest)
	{
		if (HasImplicitReferenceTypeParameterConversion(pSource, pDest))
		{
			return true;
		}
		if (HasImplicitBoxingTypeParameterConversion(pSource, pDest))
		{
			return true;
		}
		if (pDest.IsTypeParameterType() && pSource.DependsOn(pDest.AsTypeParameterType()))
		{
			return true;
		}
		return false;
	}

	public bool HasImplicitBoxingConversion(CType pSource, CType pDest)
	{
		if (pSource.IsTypeParameterType() && HasImplicitBoxingTypeParameterConversion(pSource.AsTypeParameterType(), pDest))
		{
			return true;
		}
		if (!pSource.IsValType() || !pDest.IsRefType())
		{
			return false;
		}
		if (pSource.IsNullableType())
		{
			return HasImplicitBoxingConversion(pSource.AsNullableType().GetUnderlyingType(), pDest);
		}
		if (IsBaseClass(pSource, pDest))
		{
			return true;
		}
		if (HasAnyBaseInterfaceConversion(pSource, pDest))
		{
			return true;
		}
		return false;
	}

	public bool HasBaseConversion(CType pSource, CType pDest)
	{
		if (pSource.IsAggregateType() && pDest.isPredefType(PredefinedType.PT_OBJECT))
		{
			return true;
		}
		if (HasIdentityOrImplicitReferenceConversion(pSource, pDest))
		{
			return true;
		}
		if (HasImplicitBoxingConversion(pSource, pDest))
		{
			return true;
		}
		if (pSource.IsTypeParameterType() && HasImplicitTypeParameterBaseConversion(pSource.AsTypeParameterType(), pDest))
		{
			return true;
		}
		return false;
	}

	public bool FCanLift()
	{
		return GetOptPredefAgg(PredefinedType.PT_G_OPTIONAL, fEnsureState: false) != null;
	}

	public bool IsBaseAggregate(AggregateSymbol derived, AggregateSymbol @base)
	{
		if (derived == @base)
		{
			return true;
		}
		if (@base.IsInterface())
		{
			while (derived != null)
			{
				for (int i = 0; i < derived.GetIfacesAll().Size; i++)
				{
					AggregateType aggregateType = derived.GetIfacesAll().Item(i).AsAggregateType();
					if (aggregateType.getAggregate() == @base)
					{
						return true;
					}
				}
				derived = derived.GetBaseAgg();
			}
			return false;
		}
		while (derived.GetBaseClass() != null)
		{
			derived = derived.GetBaseClass().getAggregate();
			if (derived == @base)
			{
				return true;
			}
		}
		return false;
	}

	internal void SetSymbolTable(SymbolTable symbolTable)
	{
		RuntimeBinderSymbolTable = symbolTable;
	}
}
