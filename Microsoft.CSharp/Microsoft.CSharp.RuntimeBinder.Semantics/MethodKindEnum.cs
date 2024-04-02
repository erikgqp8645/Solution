namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal enum MethodKindEnum
{
	None,
	Constructor,
	Destructor,
	PropAccessor,
	EventAccessor,
	ExplicitConv,
	ImplicitConv,
	Anonymous,
	Invoke,
	BeginInvoke,
	EndInvoke,
	AnonymousTypeToString,
	AnonymousTypeEquals,
	AnonymousTypeGetHashCode,
	IteratorDispose,
	IteratorReset,
	IteratorGetEnumerator,
	IteratorGetEnumeratorDelegating,
	IteratorMoveNext,
	Latent,
	Actual,
	IteratorFinally
}
