using System.Collections.Generic;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal static class SymbolExtensions
{
	public static IEnumerable<Symbol> Children(this ParentSymbol symbol)
	{
		if (symbol != null)
		{
			for (Symbol current = symbol.firstChild; current != null; current = current.nextChild)
			{
				yield return current;
			}
		}
	}

	internal static MethodSymbol AsFMETHSYM(this Symbol symbol)
	{
		return symbol as MethodSymbol;
	}

	internal static NamespaceOrAggregateSymbol AsNamespaceOrAggregateSymbol(this Symbol symbol)
	{
		return symbol as NamespaceOrAggregateSymbol;
	}

	internal static NamespaceSymbol AsNamespaceSymbol(this Symbol symbol)
	{
		return symbol as NamespaceSymbol;
	}

	internal static AssemblyQualifiedNamespaceSymbol AsAssemblyQualifiedNamespaceSymbol(this Symbol symbol)
	{
		return symbol as AssemblyQualifiedNamespaceSymbol;
	}

	internal static NamespaceDeclaration AsNamespaceDeclaration(this Symbol symbol)
	{
		return symbol as NamespaceDeclaration;
	}

	internal static AggregateSymbol AsAggregateSymbol(this Symbol symbol)
	{
		return symbol as AggregateSymbol;
	}

	internal static AggregateDeclaration AsAggregateDeclaration(this Symbol symbol)
	{
		return symbol as AggregateDeclaration;
	}

	internal static FieldSymbol AsFieldSymbol(this Symbol symbol)
	{
		return symbol as FieldSymbol;
	}

	internal static LocalVariableSymbol AsLocalVariableSymbol(this Symbol symbol)
	{
		return symbol as LocalVariableSymbol;
	}

	internal static MethodSymbol AsMethodSymbol(this Symbol symbol)
	{
		return symbol as MethodSymbol;
	}

	internal static PropertySymbol AsPropertySymbol(this Symbol symbol)
	{
		return symbol as PropertySymbol;
	}

	internal static MethodOrPropertySymbol AsMethodOrPropertySymbol(this Symbol symbol)
	{
		return symbol as MethodOrPropertySymbol;
	}

	internal static Scope AsScope(this Symbol symbol)
	{
		return symbol as Scope;
	}

	internal static TypeParameterSymbol AsTypeParameterSymbol(this Symbol symbol)
	{
		return symbol as TypeParameterSymbol;
	}

	internal static EventSymbol AsEventSymbol(this Symbol symbol)
	{
		return symbol as EventSymbol;
	}
}
