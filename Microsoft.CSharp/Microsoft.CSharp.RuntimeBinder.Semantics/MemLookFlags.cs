namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal enum MemLookFlags : uint
{
	None = 0u,
	Ctor = 2u,
	NewObj = 16u,
	Operator = 8u,
	Indexer = 4u,
	UserCallable = 256u,
	BaseCall = 64u,
	MustBeInvocable = 536870912u,
	TypeVarsAllowed = 1073741824u,
	ExtensionCall = 2147483648u,
	All = 3758096734u
}
