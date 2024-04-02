using System;
using System.Runtime.Serialization;

namespace Microsoft.CSharp.RuntimeBinder;

[Serializable]
[__DynamicallyInvokable]
public class RuntimeBinderInternalCompilerException : Exception
{
	[__DynamicallyInvokable]
	public RuntimeBinderInternalCompilerException()
	{
	}

	[__DynamicallyInvokable]
	public RuntimeBinderInternalCompilerException(string message)
		: base(message)
	{
	}

	[__DynamicallyInvokable]
	public RuntimeBinderInternalCompilerException(string message, Exception innerException)
		: base(message, innerException)
	{
	}

	protected RuntimeBinderInternalCompilerException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
