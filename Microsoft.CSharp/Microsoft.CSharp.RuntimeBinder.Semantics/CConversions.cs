using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal static class CConversions
{
	public static bool FImpRefConv(SymbolLoader loader, CType typeSrc, CType typeDst)
	{
		if (typeSrc.IsRefType())
		{
			return loader.HasIdentityOrImplicitReferenceConversion(typeSrc, typeDst);
		}
		return false;
	}

	public static bool FExpRefConv(SymbolLoader loader, CType typeSrc, CType typeDst)
	{
		if (typeSrc.IsRefType() && typeDst.IsRefType())
		{
			if (loader.HasIdentityOrImplicitReferenceConversion(typeSrc, typeDst) || loader.HasIdentityOrImplicitReferenceConversion(typeDst, typeSrc))
			{
				return true;
			}
			if (typeSrc.isInterfaceType() && typeDst.IsTypeParameterType())
			{
				return true;
			}
			if (typeSrc.IsTypeParameterType() && typeDst.isInterfaceType())
			{
				return true;
			}
			if (typeSrc.IsAggregateType() && typeDst.IsAggregateType())
			{
				AggregateSymbol aggregate = typeSrc.AsAggregateType().getAggregate();
				AggregateSymbol aggregate2 = typeDst.AsAggregateType().getAggregate();
				if ((aggregate.IsClass() && !aggregate.IsSealed() && aggregate2.IsInterface()) || (aggregate.IsInterface() && aggregate2.IsClass() && !aggregate2.IsSealed()) || (aggregate.IsInterface() && aggregate2.IsInterface()))
				{
					return true;
				}
			}
			if (typeSrc.IsArrayType() && typeDst.IsArrayType())
			{
				if (typeSrc.AsArrayType().rank == typeDst.AsArrayType().rank)
				{
					return FExpRefConv(loader, typeSrc.AsArrayType().GetElementType(), typeDst.AsArrayType().GetElementType());
				}
				return false;
			}
			if (typeSrc.IsArrayType())
			{
				if (typeSrc.AsArrayType().rank != 1 || !typeDst.isInterfaceType() || typeDst.AsAggregateType().GetTypeArgsAll().Size != 1)
				{
					return false;
				}
				AggregateSymbol optPredefAgg = loader.GetOptPredefAgg(PredefinedType.PT_G_ILIST);
				AggregateSymbol optPredefAgg2 = loader.GetOptPredefAgg(PredefinedType.PT_G_IREADONLYLIST);
				if ((optPredefAgg == null || !loader.IsBaseAggregate(optPredefAgg, typeDst.AsAggregateType().getAggregate())) && (optPredefAgg2 == null || !loader.IsBaseAggregate(optPredefAgg2, typeDst.AsAggregateType().getAggregate())))
				{
					return false;
				}
				return FExpRefConv(loader, typeSrc.AsArrayType().GetElementType(), typeDst.AsAggregateType().GetTypeArgsAll().Item(0));
			}
			if (typeDst.IsArrayType() && typeSrc.IsAggregateType())
			{
				if (loader.HasIdentityOrImplicitReferenceConversion(loader.GetReqPredefType(PredefinedType.PT_ARRAY), typeSrc))
				{
					return true;
				}
				ArrayType arrayType = typeDst.AsArrayType();
				AggregateType aggregateType = typeSrc.AsAggregateType();
				if (arrayType.rank != 1 || !typeSrc.isInterfaceType() || aggregateType.GetTypeArgsAll().Size != 1)
				{
					return false;
				}
				AggregateSymbol optPredefAgg3 = loader.GetOptPredefAgg(PredefinedType.PT_G_ILIST);
				AggregateSymbol optPredefAgg4 = loader.GetOptPredefAgg(PredefinedType.PT_G_IREADONLYLIST);
				if ((optPredefAgg3 == null || !loader.IsBaseAggregate(optPredefAgg3, aggregateType.getAggregate())) && (optPredefAgg4 == null || !loader.IsBaseAggregate(optPredefAgg4, aggregateType.getAggregate())))
				{
					return false;
				}
				CType elementType = arrayType.GetElementType();
				CType cType = aggregateType.GetTypeArgsAll().Item(0);
				if (elementType != cType)
				{
					return FExpRefConv(loader, elementType, cType);
				}
				return true;
			}
			if (HasGenericDelegateExplicitReferenceConversion(loader, typeSrc, typeDst))
			{
				return true;
			}
		}
		else
		{
			if (typeSrc.IsRefType())
			{
				return loader.HasIdentityOrImplicitReferenceConversion(typeSrc, typeDst);
			}
			if (typeDst.IsRefType())
			{
				return loader.HasIdentityOrImplicitReferenceConversion(typeDst, typeSrc);
			}
		}
		return false;
	}

	public static bool HasGenericDelegateExplicitReferenceConversion(SymbolLoader loader, CType pSource, CType pTarget)
	{
		if (!pSource.isDelegateType() || !pTarget.isDelegateType() || pSource.getAggregate() != pTarget.getAggregate() || loader.HasIdentityOrImplicitReferenceConversion(pSource, pTarget))
		{
			return false;
		}
		TypeArray typeVarsAll = pSource.getAggregate().GetTypeVarsAll();
		TypeArray typeArgsAll = pSource.AsAggregateType().GetTypeArgsAll();
		TypeArray typeArgsAll2 = pTarget.AsAggregateType().GetTypeArgsAll();
		for (int i = 0; i < typeVarsAll.size; i++)
		{
			CType cType = typeArgsAll.Item(i);
			CType cType2 = typeArgsAll2.Item(i);
			if (cType == cType2 || cType2.IsErrorType() || cType.IsErrorType())
			{
				continue;
			}
			TypeParameterType typeParameterType = typeVarsAll.Item(i).AsTypeParameterType();
			if (typeParameterType.Invariant)
			{
				return false;
			}
			if (typeParameterType.Covariant)
			{
				if (!FExpRefConv(loader, cType, cType2))
				{
					return false;
				}
			}
			else if (typeParameterType.Contravariant && (!cType.IsRefType() || !cType2.IsRefType()))
			{
				return false;
			}
		}
		return true;
	}

	public static bool FIsSameType(CType typeSrc, CType typeDst)
	{
		if (typeSrc == typeDst)
		{
			return !typeSrc.IsNeverSameType();
		}
		return false;
	}

	public static bool FBoxingConv(SymbolLoader loader, CType typeSrc, CType typeDst)
	{
		return loader.HasImplicitBoxingConversion(typeSrc, typeDst);
	}

	public static bool FWrappingConv(CType typeSrc, CType typeDst)
	{
		if (typeDst.IsNullableType())
		{
			return typeSrc == typeDst.AsNullableType().GetUnderlyingType();
		}
		return false;
	}

	public static bool FUnwrappingConv(CType typeSrc, CType typeDst)
	{
		return FWrappingConv(typeDst, typeSrc);
	}
}
