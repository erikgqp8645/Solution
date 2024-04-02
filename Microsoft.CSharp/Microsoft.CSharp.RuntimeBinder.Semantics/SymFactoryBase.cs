using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class SymFactoryBase
{
	protected SYMTBL m_pSymTable;

	protected Name m_pMissingNameNode;

	protected Name m_pMissingNameSym;

	protected Symbol newBasicSym(SYMKIND kind, Name name, ParentSymbol parent)
	{
		if (name == m_pMissingNameNode)
		{
			name = m_pMissingNameSym;
		}
		Symbol symbol;
		switch (kind)
		{
		case SYMKIND.SK_NamespaceSymbol:
			symbol = new NamespaceSymbol();
			symbol.name = name;
			break;
		case SYMKIND.SK_NamespaceDeclaration:
			symbol = new NamespaceDeclaration();
			symbol.name = name;
			break;
		case SYMKIND.SK_AssemblyQualifiedNamespaceSymbol:
			symbol = new AssemblyQualifiedNamespaceSymbol();
			symbol.name = name;
			break;
		case SYMKIND.SK_AggregateSymbol:
			symbol = new AggregateSymbol();
			symbol.name = name;
			break;
		case SYMKIND.SK_AggregateDeclaration:
			symbol = new AggregateDeclaration();
			symbol.name = name;
			break;
		case SYMKIND.SK_TypeParameterSymbol:
			symbol = new TypeParameterSymbol();
			symbol.name = name;
			break;
		case SYMKIND.SK_FieldSymbol:
			symbol = new FieldSymbol();
			symbol.name = name;
			break;
		case SYMKIND.SK_LocalVariableSymbol:
			symbol = new LocalVariableSymbol();
			symbol.name = name;
			break;
		case SYMKIND.SK_MethodSymbol:
			symbol = new MethodSymbol();
			symbol.name = name;
			break;
		case SYMKIND.SK_PropertySymbol:
			symbol = new PropertySymbol();
			symbol.name = name;
			break;
		case SYMKIND.SK_EventSymbol:
			symbol = new EventSymbol();
			symbol.name = name;
			break;
		case SYMKIND.SK_TransparentIdentifierMemberSymbol:
			symbol = new TransparentIdentifierMemberSymbol();
			symbol.name = name;
			break;
		case SYMKIND.SK_Scope:
			symbol = new Scope();
			symbol.name = name;
			break;
		case SYMKIND.SK_LabelSymbol:
			symbol = new LabelSymbol();
			symbol.name = name;
			break;
		case SYMKIND.SK_GlobalAttributeDeclaration:
			symbol = new GlobalAttributeDeclaration();
			symbol.name = name;
			break;
		case SYMKIND.SK_UnresolvedAggregateSymbol:
			symbol = new UnresolvedAggregateSymbol();
			symbol.name = name;
			break;
		case SYMKIND.SK_InterfaceImplementationMethodSymbol:
			symbol = new InterfaceImplementationMethodSymbol();
			symbol.name = name;
			break;
		case SYMKIND.SK_IndexerSymbol:
			symbol = new IndexerSymbol();
			symbol.name = name;
			break;
		case SYMKIND.SK_ParentSymbol:
			symbol = new ParentSymbol();
			symbol.name = name;
			break;
		case SYMKIND.SK_IteratorFinallyMethodSymbol:
			symbol = new IteratorFinallyMethodSymbol();
			symbol.name = name;
			break;
		default:
			throw Error.InternalCompilerError();
		}
		symbol.setKind(kind);
		if (parent != null)
		{
			parent.AddToChildList(symbol);
			m_pSymTable.InsertChild(parent, symbol);
		}
		return symbol;
	}

	protected SymFactoryBase(SYMTBL symtable, NameManager namemgr)
	{
		m_pSymTable = symtable;
		if (namemgr != null)
		{
			m_pMissingNameNode = namemgr.GetPredefName(PredefinedName.PN_MISSING);
			m_pMissingNameSym = namemgr.GetPredefName(PredefinedName.PN_MISSINGSYM);
		}
	}
}
