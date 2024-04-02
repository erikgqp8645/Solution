namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class ParameterModifierType : CType
{
	public bool isOut;

	private CType m_pParameterType;

	public CType GetParameterType()
	{
		return m_pParameterType;
	}

	public void SetParameterType(CType pType)
	{
		m_pParameterType = pType;
	}
}
