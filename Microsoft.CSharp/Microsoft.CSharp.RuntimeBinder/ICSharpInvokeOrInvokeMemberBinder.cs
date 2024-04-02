using System;
using System.Collections.Generic;

namespace Microsoft.CSharp.RuntimeBinder;

internal interface ICSharpInvokeOrInvokeMemberBinder
{
	bool StaticCall { get; }

	bool ResultDiscarded { get; }

	Type CallingContext { get; }

	CSharpCallFlags Flags { get; }

	string Name { get; }

	IList<Type> TypeArguments { get; }

	IList<CSharpArgumentInfo> ArgumentInfo { get; }
}
