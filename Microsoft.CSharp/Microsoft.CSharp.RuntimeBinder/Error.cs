using System;

namespace Microsoft.CSharp.RuntimeBinder;

internal static class Error
{
	internal static Exception InternalCompilerError()
	{
		return new RuntimeBinderInternalCompilerException(Strings.InternalCompilerError);
	}

	internal static Exception BindRequireArguments()
	{
		return new ArgumentException(Strings.BindRequireArguments);
	}

	internal static Exception BindCallFailedOverloadResolution()
	{
		return new RuntimeBinderException(Strings.BindCallFailedOverloadResolution);
	}

	internal static Exception BindBinaryOperatorRequireTwoArguments()
	{
		return new ArgumentException(Strings.BindBinaryOperatorRequireTwoArguments);
	}

	internal static Exception BindUnaryOperatorRequireOneArgument()
	{
		return new ArgumentException(Strings.BindUnaryOperatorRequireOneArgument);
	}

	internal static Exception BindPropertyFailedMethodGroup(object p0)
	{
		return new RuntimeBinderException(Strings.BindPropertyFailedMethodGroup(p0));
	}

	internal static Exception BindPropertyFailedEvent(object p0)
	{
		return new RuntimeBinderException(Strings.BindPropertyFailedEvent(p0));
	}

	internal static Exception BindInvokeFailedNonDelegate()
	{
		return new RuntimeBinderException(Strings.BindInvokeFailedNonDelegate);
	}

	internal static Exception BindImplicitConversionRequireOneArgument()
	{
		return new ArgumentException(Strings.BindImplicitConversionRequireOneArgument);
	}

	internal static Exception BindExplicitConversionRequireOneArgument()
	{
		return new ArgumentException(Strings.BindExplicitConversionRequireOneArgument);
	}

	internal static Exception BindBinaryAssignmentRequireTwoArguments()
	{
		return new ArgumentException(Strings.BindBinaryAssignmentRequireTwoArguments);
	}

	internal static Exception BindBinaryAssignmentFailedNullReference()
	{
		return new RuntimeBinderException(Strings.BindBinaryAssignmentFailedNullReference);
	}

	internal static Exception NullReferenceOnMemberException()
	{
		return new RuntimeBinderException(Strings.NullReferenceOnMemberException);
	}

	internal static Exception BindCallToConditionalMethod(object p0)
	{
		return new RuntimeBinderException(Strings.BindCallToConditionalMethod(p0));
	}

	internal static Exception BindToVoidMethodButExpectResult()
	{
		return new RuntimeBinderException(Strings.BindToVoidMethodButExpectResult);
	}

	internal static Exception ArgumentNull(string paramName)
	{
		return new ArgumentNullException(paramName);
	}

	internal static Exception ArgumentOutOfRange(string paramName)
	{
		return new ArgumentOutOfRangeException(paramName);
	}

	internal static Exception NotImplemented()
	{
		return new NotImplementedException();
	}

	internal static Exception NotSupported()
	{
		return new NotSupportedException();
	}
}
