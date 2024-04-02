namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal enum ConvKind
{
	Identity = 1,
	Implicit,
	Explicit,
	Unknown,
	None
}
