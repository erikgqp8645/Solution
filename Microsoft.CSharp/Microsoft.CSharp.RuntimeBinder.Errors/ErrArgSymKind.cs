using Microsoft.CSharp.RuntimeBinder.Semantics;

namespace Microsoft.CSharp.RuntimeBinder.Errors;

internal sealed class ErrArgSymKind : ErrArgRef
{
	public ErrArgSymKind(Symbol sym)
	{
		eak = ErrArgKind.SymKind;
		eaf = ErrArgFlags.None;
		sk = sym.getKind();
		if (sk == SYMKIND.SK_AssemblyQualifiedNamespaceSymbol)
		{
			if (!string.IsNullOrEmpty(sym.AsAssemblyQualifiedNamespaceSymbol().GetNS().name.Text))
			{
				sk = SYMKIND.SK_NamespaceSymbol;
			}
			else
			{
				sk = SYMKIND.SK_ExternalAliasDefinitionSymbol;
			}
		}
	}
}
