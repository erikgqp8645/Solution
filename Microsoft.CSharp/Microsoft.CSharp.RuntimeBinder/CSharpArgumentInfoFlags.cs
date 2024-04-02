using System;
using System.ComponentModel;

namespace Microsoft.CSharp.RuntimeBinder;

[Flags]
[EditorBrowsable(EditorBrowsableState.Never)]
[__DynamicallyInvokable]
public enum CSharpArgumentInfoFlags
{
	[__DynamicallyInvokable]
	None = 0,
	[__DynamicallyInvokable]
	UseCompileTimeType = 1,
	[__DynamicallyInvokable]
	Constant = 2,
	[__DynamicallyInvokable]
	NamedArgument = 4,
	[__DynamicallyInvokable]
	IsRef = 8,
	[__DynamicallyInvokable]
	IsOut = 0x10,
	[__DynamicallyInvokable]
	IsStaticType = 0x20
}
