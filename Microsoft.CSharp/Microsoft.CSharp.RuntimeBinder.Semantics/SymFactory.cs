using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class SymFactory : SymFactoryBase
{
	public SymFactory(SYMTBL symtable, NameManager namemgr)
		: base(symtable, namemgr)
	{
	}

	public NamespaceSymbol CreateNamespace(Name name, NamespaceSymbol parent)
	{
		NamespaceSymbol namespaceSymbol = newBasicSym(SYMKIND.SK_NamespaceSymbol, name, parent).AsNamespaceSymbol();
		namespaceSymbol.SetAccess(ACCESS.ACC_PUBLIC);
		return namespaceSymbol;
	}

	public AssemblyQualifiedNamespaceSymbol CreateNamespaceAid(Name name, ParentSymbol parent, KAID assemblyID)
	{
		return newBasicSym(SYMKIND.SK_AssemblyQualifiedNamespaceSymbol, name, parent).AsAssemblyQualifiedNamespaceSymbol();
	}

	public AggregateSymbol CreateAggregate(Name name, NamespaceOrAggregateSymbol parent, InputFile infile, TypeManager typeManager)
	{
		if (name == null || parent == null || infile == null || typeManager == null)
		{
			throw Error.InternalCompilerError();
		}
		AggregateSymbol aggregateSymbol = null;
		if (infile.GetAssemblyID() == KAID.kaidUnresolved)
		{
			aggregateSymbol = CreateUnresolvedAggregate(name, parent, typeManager);
		}
		else
		{
			aggregateSymbol = newBasicSym(SYMKIND.SK_AggregateSymbol, name, parent).AsAggregateSymbol();
			aggregateSymbol.name = name;
			aggregateSymbol.SetTypeManager(typeManager);
			aggregateSymbol.SetSealed(@sealed: false);
			aggregateSymbol.SetAccess(ACCESS.ACC_UNKNOWN);
			aggregateSymbol.initBogus();
			aggregateSymbol.SetIfaces(null);
			aggregateSymbol.SetIfacesAll(null);
			aggregateSymbol.SetTypeVars(null);
		}
		aggregateSymbol.InitFromInfile(infile);
		return aggregateSymbol;
	}

	public AggregateDeclaration CreateAggregateDecl(AggregateSymbol agg, Declaration declOuter)
	{
		AggregateDeclaration aggregateDeclaration = newBasicSym(SYMKIND.SK_AggregateDeclaration, agg.name, null).AsAggregateDeclaration();
		declOuter?.AddToChildList(aggregateDeclaration);
		agg.AddDecl(aggregateDeclaration);
		return aggregateDeclaration;
	}

	public AggregateSymbol CreateUnresolvedAggregate(Name name, ParentSymbol parent, TypeManager typeManager)
	{
		Symbol symbol = newBasicSym(SYMKIND.SK_UnresolvedAggregateSymbol, name, parent);
		AggregateSymbol aggregateSymbol = null;
		symbol.setKind(SYMKIND.SK_AggregateSymbol);
		aggregateSymbol = symbol.AsAggregateSymbol();
		aggregateSymbol.SetTypeManager(typeManager);
		return aggregateSymbol;
	}

	public FieldSymbol CreateMemberVar(Name name, ParentSymbol parent, AggregateDeclaration declaration, int iIteratorLocal)
	{
		FieldSymbol fieldSymbol = newBasicSym(SYMKIND.SK_FieldSymbol, name, parent).AsFieldSymbol();
		fieldSymbol.declaration = declaration;
		return fieldSymbol;
	}

	public LocalVariableSymbol CreateLocalVar(Name name, ParentSymbol parent, CType type)
	{
		LocalVariableSymbol localVariableSymbol = newBasicSym(SYMKIND.SK_LocalVariableSymbol, name, parent).AsLocalVariableSymbol();
		localVariableSymbol.SetType(type);
		localVariableSymbol.SetAccess(ACCESS.ACC_UNKNOWN);
		localVariableSymbol.wrap = null;
		return localVariableSymbol;
	}

	public MethodSymbol CreateMethod(Name name, ParentSymbol parent, AggregateDeclaration declaration)
	{
		MethodSymbol methodSymbol = newBasicSym(SYMKIND.SK_MethodSymbol, name, parent).AsMethodSymbol();
		methodSymbol.declaration = declaration;
		return methodSymbol;
	}

	public PropertySymbol CreateProperty(Name name, ParentSymbol parent, AggregateDeclaration declaration)
	{
		PropertySymbol propertySymbol = newBasicSym(SYMKIND.SK_PropertySymbol, name, parent).AsPropertySymbol();
		propertySymbol.declaration = declaration;
		return propertySymbol;
	}

	public EventSymbol CreateEvent(Name name, ParentSymbol parent, AggregateDeclaration declaration)
	{
		EventSymbol eventSymbol = newBasicSym(SYMKIND.SK_EventSymbol, name, parent).AsEventSymbol();
		eventSymbol.declaration = declaration;
		return eventSymbol;
	}

	public TypeParameterSymbol CreateMethodTypeParameter(Name pName, MethodSymbol pParent, int index, int indexTotal)
	{
		TypeParameterSymbol typeParameterSymbol = newBasicSym(SYMKIND.SK_TypeParameterSymbol, pName, pParent).AsTypeParameterSymbol();
		typeParameterSymbol.SetIndexInOwnParameters(index);
		typeParameterSymbol.SetIndexInTotalParameters(indexTotal);
		typeParameterSymbol.SetIsMethodTypeParameter(b: true);
		typeParameterSymbol.SetAccess(ACCESS.ACC_PRIVATE);
		return typeParameterSymbol;
	}

	public TypeParameterSymbol CreateClassTypeParameter(Name pName, AggregateSymbol pParent, int index, int indexTotal)
	{
		TypeParameterSymbol typeParameterSymbol = newBasicSym(SYMKIND.SK_TypeParameterSymbol, pName, pParent).AsTypeParameterSymbol();
		typeParameterSymbol.SetIndexInOwnParameters(index);
		typeParameterSymbol.SetIndexInTotalParameters(indexTotal);
		typeParameterSymbol.SetIsMethodTypeParameter(b: false);
		typeParameterSymbol.SetAccess(ACCESS.ACC_PRIVATE);
		return typeParameterSymbol;
	}
}
