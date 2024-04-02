using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class MiscSymFactory : SymFactoryBase
{
	public MiscSymFactory(SYMTBL symtable)
		: base(symtable, null)
	{
	}

	public InputFile CreateMDInfile(Name name, mdToken idLocalAssembly)
	{
		InputFile inputFile = new InputFile();
		inputFile.isSource = false;
		return inputFile;
	}

	public Scope CreateScope(Scope parent)
	{
		Scope scope = newBasicSym(SYMKIND.SK_Scope, null, parent).AsScope();
		if (parent != null)
		{
			scope.nestingOrder = parent.nestingOrder + 1;
		}
		return scope;
	}

	public IndexerSymbol CreateIndexer(Name name, ParentSymbol parent, Name realName, AggregateDeclaration declaration)
	{
		IndexerSymbol indexerSymbol = (IndexerSymbol)newBasicSym(SYMKIND.SK_IndexerSymbol, name, parent);
		indexerSymbol.setKind(SYMKIND.SK_PropertySymbol);
		indexerSymbol.isOperator = true;
		indexerSymbol.declaration = declaration;
		return indexerSymbol;
	}
}
