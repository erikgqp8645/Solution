namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal interface ITypeOrNamespace
{
	bool IsType();

	bool IsNamespace();

	AssemblyQualifiedNamespaceSymbol AsNamespace();

	CType AsType();
}
