using System;
using System.ComponentModel;

namespace Microsoft.CSharp.RuntimeBinder;

[Flags]
[EditorBrowsable(EditorBrowsableState.Never)]
[__DynamicallyInvokable]
public enum CSharpBinderFlags
{
	[__DynamicallyInvokable]
	None = 0,
	[__DynamicallyInvokable]
	CheckedContext = 1,
	[__DynamicallyInvokable]
	InvokeSimpleName = 2,
	[__DynamicallyInvokable]
	InvokeSpecialName = 4,
	[__DynamicallyInvokable]
	BinaryOperationLogical = 8,
	[__DynamicallyInvokable]
	ConvertExplicit = 0x10,
	[__DynamicallyInvokable]
	ConvertArrayIndex = 0x20,
	[__DynamicallyInvokable]
	ResultIndexed = 0x40,
	[__DynamicallyInvokable]
	ValueFromCompoundAssignment = 0x80,
	[__DynamicallyInvokable]
	ResultDiscarded = 0x100
}
