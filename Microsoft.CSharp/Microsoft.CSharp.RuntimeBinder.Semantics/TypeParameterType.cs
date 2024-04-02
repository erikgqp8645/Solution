namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class TypeParameterType : CType
{
	private TypeParameterSymbol m_pTypeParameterSymbol;

	public bool Covariant => m_pTypeParameterSymbol.Covariant;

	public bool Invariant => m_pTypeParameterSymbol.Invariant;

	public bool Contravariant => m_pTypeParameterSymbol.Contravariant;

	public TypeParameterSymbol GetTypeParameterSymbol()
	{
		return m_pTypeParameterSymbol;
	}

	public void SetTypeParameterSymbol(TypeParameterSymbol pTypePArameterSymbol)
	{
		m_pTypeParameterSymbol = pTypePArameterSymbol;
	}

	public ParentSymbol GetOwningSymbol()
	{
		return m_pTypeParameterSymbol.parent;
	}

	public bool DependsOn(TypeParameterType pType)
	{
		TypeArray bounds = GetBounds();
		for (int i = 0; i < bounds.size; i++)
		{
			CType cType = bounds.Item(i);
			if (cType == pType)
			{
				return true;
			}
			if (cType.IsTypeParameterType() && cType.AsTypeParameterType().DependsOn(pType))
			{
				return true;
			}
		}
		return false;
	}

	public bool IsValueType()
	{
		return m_pTypeParameterSymbol.IsValueType();
	}

	public bool IsReferenceType()
	{
		return m_pTypeParameterSymbol.IsReferenceType();
	}

	public bool IsNonNullableValueType()
	{
		return m_pTypeParameterSymbol.IsNonNullableValueType();
	}

	public bool HasNewConstraint()
	{
		return m_pTypeParameterSymbol.HasNewConstraint();
	}

	public bool HasRefConstraint()
	{
		return m_pTypeParameterSymbol.HasRefConstraint();
	}

	public bool HasValConstraint()
	{
		return m_pTypeParameterSymbol.HasValConstraint();
	}

	public bool IsMethodTypeParameter()
	{
		return m_pTypeParameterSymbol.IsMethodTypeParameter();
	}

	public int GetIndexInOwnParameters()
	{
		return m_pTypeParameterSymbol.GetIndexInOwnParameters();
	}

	public int GetIndexInTotalParameters()
	{
		return m_pTypeParameterSymbol.GetIndexInTotalParameters();
	}

	public TypeArray GetBounds()
	{
		return m_pTypeParameterSymbol.GetBounds();
	}

	public TypeArray GetInterfaceBounds()
	{
		return m_pTypeParameterSymbol.GetInterfaceBounds();
	}

	public AggregateType GetEffectiveBaseClass()
	{
		return m_pTypeParameterSymbol.GetEffectiveBaseClass();
	}
}
