using System.Collections.Generic;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class TypeTable
{
	private Dictionary<KeyPair<AggregateSymbol, Name>, AggregateType> m_pAggregateTable;

	private Dictionary<KeyPair<CType, Name>, ErrorType> m_pErrorWithTypeParentTable;

	private Dictionary<KeyPair<AssemblyQualifiedNamespaceSymbol, Name>, ErrorType> m_pErrorWithNamespaceParentTable;

	private Dictionary<KeyPair<CType, Name>, ArrayType> m_pArrayTable;

	private Dictionary<KeyPair<CType, Name>, ParameterModifierType> m_pParameterModifierTable;

	private Dictionary<CType, PointerType> m_pPointerTable;

	private Dictionary<CType, NullableType> m_pNullableTable;

	private Dictionary<TypeParameterSymbol, TypeParameterType> m_pTypeParameterTable;

	public TypeTable()
	{
		m_pAggregateTable = new Dictionary<KeyPair<AggregateSymbol, Name>, AggregateType>();
		m_pErrorWithNamespaceParentTable = new Dictionary<KeyPair<AssemblyQualifiedNamespaceSymbol, Name>, ErrorType>();
		m_pErrorWithTypeParentTable = new Dictionary<KeyPair<CType, Name>, ErrorType>();
		m_pArrayTable = new Dictionary<KeyPair<CType, Name>, ArrayType>();
		m_pParameterModifierTable = new Dictionary<KeyPair<CType, Name>, ParameterModifierType>();
		m_pPointerTable = new Dictionary<CType, PointerType>();
		m_pNullableTable = new Dictionary<CType, NullableType>();
		m_pTypeParameterTable = new Dictionary<TypeParameterSymbol, TypeParameterType>();
	}

	public AggregateType LookupAggregate(Name pName, AggregateSymbol pAggregate)
	{
		KeyPair<AggregateSymbol, Name> key = new KeyPair<AggregateSymbol, Name>(pAggregate, pName);
		if (m_pAggregateTable.TryGetValue(key, out var value))
		{
			return value;
		}
		return null;
	}

	public void InsertAggregate(Name pName, AggregateSymbol pAggregateSymbol, AggregateType pAggregate)
	{
		m_pAggregateTable.Add(new KeyPair<AggregateSymbol, Name>(pAggregateSymbol, pName), pAggregate);
	}

	public ErrorType LookupError(Name pName, CType pParentType)
	{
		KeyPair<CType, Name> key = new KeyPair<CType, Name>(pParentType, pName);
		if (m_pErrorWithTypeParentTable.TryGetValue(key, out var value))
		{
			return value;
		}
		return null;
	}

	public ErrorType LookupError(Name pName, AssemblyQualifiedNamespaceSymbol pParentNS)
	{
		KeyPair<AssemblyQualifiedNamespaceSymbol, Name> key = new KeyPair<AssemblyQualifiedNamespaceSymbol, Name>(pParentNS, pName);
		if (m_pErrorWithNamespaceParentTable.TryGetValue(key, out var value))
		{
			return value;
		}
		return null;
	}

	public void InsertError(Name pName, CType pParentType, ErrorType pError)
	{
		m_pErrorWithTypeParentTable.Add(new KeyPair<CType, Name>(pParentType, pName), pError);
	}

	public void InsertError(Name pName, AssemblyQualifiedNamespaceSymbol pParentNS, ErrorType pError)
	{
		m_pErrorWithNamespaceParentTable.Add(new KeyPair<AssemblyQualifiedNamespaceSymbol, Name>(pParentNS, pName), pError);
	}

	public ArrayType LookupArray(Name pName, CType pElementType)
	{
		KeyPair<CType, Name> key = new KeyPair<CType, Name>(pElementType, pName);
		if (m_pArrayTable.TryGetValue(key, out var value))
		{
			return value;
		}
		return null;
	}

	public void InsertArray(Name pName, CType pElementType, ArrayType pArray)
	{
		m_pArrayTable.Add(new KeyPair<CType, Name>(pElementType, pName), pArray);
	}

	public ParameterModifierType LookupParameterModifier(Name pName, CType pElementType)
	{
		KeyPair<CType, Name> key = new KeyPair<CType, Name>(pElementType, pName);
		if (m_pParameterModifierTable.TryGetValue(key, out var value))
		{
			return value;
		}
		return null;
	}

	public void InsertParameterModifier(Name pName, CType pElementType, ParameterModifierType pParameterModifier)
	{
		m_pParameterModifierTable.Add(new KeyPair<CType, Name>(pElementType, pName), pParameterModifier);
	}

	public PointerType LookupPointer(CType pElementType)
	{
		if (m_pPointerTable.TryGetValue(pElementType, out var value))
		{
			return value;
		}
		return null;
	}

	public void InsertPointer(CType pElementType, PointerType pPointer)
	{
		m_pPointerTable.Add(pElementType, pPointer);
	}

	public NullableType LookupNullable(CType pUnderlyingType)
	{
		if (m_pNullableTable.TryGetValue(pUnderlyingType, out var value))
		{
			return value;
		}
		return null;
	}

	public void InsertNullable(CType pUnderlyingType, NullableType pNullable)
	{
		m_pNullableTable.Add(pUnderlyingType, pNullable);
	}

	public TypeParameterType LookupTypeParameter(TypeParameterSymbol pTypeParameterSymbol)
	{
		if (m_pTypeParameterTable.TryGetValue(pTypeParameterSymbol, out var value))
		{
			return value;
		}
		return null;
	}

	public void InsertTypeParameter(TypeParameterSymbol pTypeParameterSymbol, TypeParameterType pTypeParameter)
	{
		m_pTypeParameterTable.Add(pTypeParameterSymbol, pTypeParameter);
	}
}
