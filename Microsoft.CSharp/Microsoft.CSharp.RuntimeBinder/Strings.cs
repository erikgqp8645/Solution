namespace Microsoft.CSharp.RuntimeBinder;

internal static class Strings
{
	internal static string InternalCompilerError => SR.GetString("InternalCompilerError");

	internal static string BindRequireArguments => SR.GetString("BindRequireArguments");

	internal static string BindCallFailedOverloadResolution => SR.GetString("BindCallFailedOverloadResolution");

	internal static string BindBinaryOperatorRequireTwoArguments => SR.GetString("BindBinaryOperatorRequireTwoArguments");

	internal static string BindUnaryOperatorRequireOneArgument => SR.GetString("BindUnaryOperatorRequireOneArgument");

	internal static string BindInvokeFailedNonDelegate => SR.GetString("BindInvokeFailedNonDelegate");

	internal static string BindImplicitConversionRequireOneArgument => SR.GetString("BindImplicitConversionRequireOneArgument");

	internal static string BindExplicitConversionRequireOneArgument => SR.GetString("BindExplicitConversionRequireOneArgument");

	internal static string BindBinaryAssignmentRequireTwoArguments => SR.GetString("BindBinaryAssignmentRequireTwoArguments");

	internal static string BindBinaryAssignmentFailedNullReference => SR.GetString("BindBinaryAssignmentFailedNullReference");

	internal static string NullReferenceOnMemberException => SR.GetString("NullReferenceOnMemberException");

	internal static string BindToVoidMethodButExpectResult => SR.GetString("BindToVoidMethodButExpectResult");

	internal static string EmptyDynamicView => SR.GetString("EmptyDynamicView");

	internal static string GetValueonWriteOnlyProperty => SR.GetString("GetValueonWriteOnlyProperty");

	internal static string BindPropertyFailedMethodGroup(object p0)
	{
		return SR.GetString("BindPropertyFailedMethodGroup", p0);
	}

	internal static string BindPropertyFailedEvent(object p0)
	{
		return SR.GetString("BindPropertyFailedEvent", p0);
	}

	internal static string BindCallToConditionalMethod(object p0)
	{
		return SR.GetString("BindCallToConditionalMethod", p0);
	}
}
