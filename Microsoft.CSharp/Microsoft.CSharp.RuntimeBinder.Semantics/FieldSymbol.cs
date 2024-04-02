using System.Reflection;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class FieldSymbol : VariableSymbol
{
	public new bool isStatic;

	public bool isReadOnly;

	public bool isEvent;

	public bool isAssigned;

	public FieldInfo AssociatedFieldInfo;

	public AggregateDeclaration declaration;

	public void SetType(CType pType)
	{
		type = pType;
	}

	public new CType GetType()
	{
		return type;
	}

	public AggregateSymbol getClass()
	{
		return parent.AsAggregateSymbol();
	}

	public AggregateDeclaration containingDeclaration()
	{
		return declaration;
	}

	public EventSymbol getEvent(SymbolLoader symbolLoader)
	{
		return symbolLoader.LookupAggMember(name, getClass(), symbmask_t.MASK_EventSymbol).AsEventSymbol();
	}
}
