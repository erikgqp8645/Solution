using System.Collections.Generic;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class AggregateType : CType
{
	private TypeArray m_pTypeArgsThis;

	private TypeArray m_pTypeArgsAll;

	private AggregateSymbol m_pOwningAggregate;

	private AggregateType baseType;

	private TypeArray ifacesAll;

	private TypeArray winrtifacesAll;

	public bool fConstraintsChecked;

	public bool fConstraintError;

	public bool fAllHidden;

	public bool fDiffHidden;

	public AggregateType outerType;

	public void SetOwningAggregate(AggregateSymbol agg)
	{
		m_pOwningAggregate = agg;
	}

	public AggregateSymbol GetOwningAggregate()
	{
		return m_pOwningAggregate;
	}

	public AggregateType GetBaseClass()
	{
		if (baseType == null)
		{
			baseType = getAggregate().GetTypeManager().SubstType(getAggregate().GetBaseClass(), GetTypeArgsAll()) as AggregateType;
		}
		return baseType;
	}

	public void SetTypeArgsThis(TypeArray pTypeArgsThis)
	{
		TypeArray typeArgsAll = ((outerType == null) ? BSYMMGR.EmptyTypeArray() : outerType.GetTypeArgsAll());
		m_pTypeArgsThis = pTypeArgsThis;
		SetTypeArgsAll(typeArgsAll);
	}

	public void SetTypeArgsAll(TypeArray outerTypeArgs)
	{
		TypeArray pTypeArray = outerTypeArgs;
		TypeManager typeManager = getAggregate().GetTypeManager();
		if (m_pTypeArgsThis.Size > 0 && AreAllTypeArgumentsUnitTypes(m_pTypeArgsThis) && outerTypeArgs.Size > 0 && !AreAllTypeArgumentsUnitTypes(outerTypeArgs))
		{
			pTypeArray = typeManager.CreateArrayOfUnitTypes(outerTypeArgs.Size);
		}
		m_pTypeArgsAll = typeManager.ConcatenateTypeArrays(pTypeArray, m_pTypeArgsThis);
	}

	public bool AreAllTypeArgumentsUnitTypes(TypeArray typeArray)
	{
		if (typeArray.Size == 0)
		{
			return true;
		}
		for (int i = 0; i < typeArray.size; i++)
		{
			if (!typeArray.Item(i).IsOpenTypePlaceholderType())
			{
				return false;
			}
		}
		return true;
	}

	public TypeArray GetTypeArgsThis()
	{
		return m_pTypeArgsThis;
	}

	public TypeArray GetTypeArgsAll()
	{
		return m_pTypeArgsAll;
	}

	public TypeArray GetIfacesAll()
	{
		if (ifacesAll == null)
		{
			ifacesAll = getAggregate().GetTypeManager().SubstTypeArray(getAggregate().GetIfacesAll(), GetTypeArgsAll());
		}
		return ifacesAll;
	}

	public TypeArray GetWinRTCollectionIfacesAll(SymbolLoader pSymbolLoader)
	{
		if (winrtifacesAll == null)
		{
			TypeArray typeArray = GetIfacesAll();
			List<CType> list = new List<CType>();
			for (int i = 0; i < typeArray.size; i++)
			{
				AggregateType aggregateType = typeArray.Item(i).AsAggregateType();
				if (aggregateType.IsCollectionType())
				{
					list.Add(aggregateType);
				}
			}
			winrtifacesAll = pSymbolLoader.getBSymmgr().AllocParams(list.Count, list.ToArray());
		}
		return winrtifacesAll;
	}

	public TypeArray GetDelegateParameters(SymbolLoader pSymbolLoader)
	{
		MethodSymbol methodSymbol = pSymbolLoader.LookupInvokeMeth(getAggregate());
		if (methodSymbol == null || !methodSymbol.isInvoke())
		{
			return null;
		}
		return getAggregate().GetTypeManager().SubstTypeArray(methodSymbol.Params, this);
	}

	public CType GetDelegateReturnType(SymbolLoader pSymbolLoader)
	{
		MethodSymbol methodSymbol = pSymbolLoader.LookupInvokeMeth(getAggregate());
		if (methodSymbol == null || !methodSymbol.isInvoke())
		{
			return null;
		}
		return getAggregate().GetTypeManager().SubstType(methodSymbol.RetType, this);
	}
}
