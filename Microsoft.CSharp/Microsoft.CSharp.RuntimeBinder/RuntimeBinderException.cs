using System;
using System.Runtime.Serialization;

namespace Microsoft.CSharp.RuntimeBinder;

[Serializable]
[__DynamicallyInvokable]
public class RuntimeBinderException : Exception
{
	[__DynamicallyInvokable]
	public RuntimeBinderException()
	{
	}

	[__DynamicallyInvokable]
	public RuntimeBinderException(string message)
		: base(message)
	{
	}

	[__DynamicallyInvokable]
	public RuntimeBinderException(string message, Exception innerException)
		: base(message, innerException)
	{
	}

	protected RuntimeBinderException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
