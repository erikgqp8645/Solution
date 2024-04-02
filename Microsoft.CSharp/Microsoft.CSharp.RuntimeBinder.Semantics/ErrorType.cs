using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class ErrorType : CType
{
	public Name nameText;

	public TypeArray typeArgs;

	private CType m_pParentType;

	private AssemblyQualifiedNamespaceSymbol m_pParentNS;

	public bool HasParent()
	{
		if (m_pParentType == null)
		{
			return m_pParentNS != null;
		}
		return true;
	}

	public bool HasTypeParent()
	{
		return m_pParentType != null;
	}

	public CType GetTypeParent()
	{
		return m_pParentType;
	}

	public void SetTypeParent(CType pType)
	{
		m_pParentType = pType;
	}

	public bool HasNSParent()
	{
		return m_pParentNS != null;
	}

	public AssemblyQualifiedNamespaceSymbol GetNSParent()
	{
		return m_pParentNS;
	}

	public void SetNSParent(AssemblyQualifiedNamespaceSymbol pNS)
	{
		m_pParentNS = pNS;
	}
}
