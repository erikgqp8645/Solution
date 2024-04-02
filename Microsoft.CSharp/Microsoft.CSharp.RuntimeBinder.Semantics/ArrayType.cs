namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class ArrayType : CType
{
	public int rank;

	private CType m_pElementType;

	public CType GetElementType()
	{
		return m_pElementType;
	}

	public void SetElementType(CType pType)
	{
		m_pElementType = pType;
	}

	public CType GetBaseElementType()
	{
		CType elementType = GetElementType();
		while (elementType.IsArrayType())
		{
			elementType = elementType.AsArrayType().GetElementType();
		}
		return elementType;
	}
}
