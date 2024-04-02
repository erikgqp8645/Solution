using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Microsoft.CSharp.RuntimeBinder;

[Serializable]
[EditorBrowsable(EditorBrowsableState.Never)]
internal sealed class DynamicBindingFailedException : Exception
{
	public DynamicBindingFailedException()
	{
	}

	private DynamicBindingFailedException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
