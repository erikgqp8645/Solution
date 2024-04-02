namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class PointerType : CType
{
	private CType m_pReferentType;

	public CType GetReferentType()
	{
		return m_pReferentType;
	}

	public void SetReferentType(CType pType)
	{
		m_pReferentType = pType;
	}
}
