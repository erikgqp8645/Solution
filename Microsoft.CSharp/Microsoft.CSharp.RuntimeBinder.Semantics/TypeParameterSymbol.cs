namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class TypeParameterSymbol : Symbol
{
	private bool m_bIsMethodTypeParameter;

	private bool m_bHasRefBound;

	private bool m_bHasValBound;

	private SpecCons m_constraints;

	private TypeParameterType m_pTypeParameterType;

	private int m_nIndexInOwnParameters;

	private int m_nIndexInTotalParameters;

	private TypeArray m_pBounds;

	private TypeArray m_pInterfaceBounds;

	private AggregateType m_pEffectiveBaseClass;

	private CType m_pDeducedBaseClass;

	public bool Covariant;

	public bool Contravariant;

	public bool Invariant
	{
		get
		{
			if (!Covariant)
			{
				return !Contravariant;
			}
			return false;
		}
	}

	public void SetTypeParameterType(TypeParameterType pType)
	{
		m_pTypeParameterType = pType;
	}

	public TypeParameterType GetTypeParameterType()
	{
		return m_pTypeParameterType;
	}

	public bool IsMethodTypeParameter()
	{
		return m_bIsMethodTypeParameter;
	}

	public void SetIsMethodTypeParameter(bool b)
	{
		m_bIsMethodTypeParameter = b;
	}

	public int GetIndexInOwnParameters()
	{
		return m_nIndexInOwnParameters;
	}

	public void SetIndexInOwnParameters(int index)
	{
		m_nIndexInOwnParameters = index;
	}

	public int GetIndexInTotalParameters()
	{
		return m_nIndexInTotalParameters;
	}

	public void SetIndexInTotalParameters(int index)
	{
		m_nIndexInTotalParameters = index;
	}

	public TypeArray GetInterfaceBounds()
	{
		return m_pInterfaceBounds;
	}

	public void SetBounds(TypeArray pBounds)
	{
		m_pBounds = pBounds;
		m_pInterfaceBounds = null;
		m_pEffectiveBaseClass = null;
		m_pDeducedBaseClass = null;
		m_bHasRefBound = false;
		m_bHasValBound = false;
	}

	public TypeArray GetBounds()
	{
		return m_pBounds;
	}

	public void SetConstraints(SpecCons constraints)
	{
		m_constraints = constraints;
	}

	public AggregateType GetEffectiveBaseClass()
	{
		return m_pEffectiveBaseClass;
	}

	public bool IsValueType()
	{
		if ((m_constraints & SpecCons.Val) <= SpecCons.None)
		{
			return m_bHasValBound;
		}
		return true;
	}

	public bool IsReferenceType()
	{
		if ((m_constraints & SpecCons.Ref) <= SpecCons.None)
		{
			return m_bHasRefBound;
		}
		return true;
	}

	public bool IsNonNullableValueType()
	{
		if ((m_constraints & SpecCons.Val) <= SpecCons.None)
		{
			if (m_bHasValBound)
			{
				return !m_pDeducedBaseClass.IsNullableType();
			}
			return false;
		}
		return true;
	}

	public bool HasNewConstraint()
	{
		return (m_constraints & SpecCons.New) > SpecCons.None;
	}

	public bool HasRefConstraint()
	{
		return (m_constraints & SpecCons.Ref) > SpecCons.None;
	}

	public bool HasValConstraint()
	{
		return (m_constraints & SpecCons.Val) > SpecCons.None;
	}
}
