using System.Globalization;
using System.Resources;
using System.Threading;

namespace Microsoft.CSharp.RuntimeBinder;

internal sealed class SR
{
	internal const string InternalCompilerError = "InternalCompilerError";

	internal const string BindRequireArguments = "BindRequireArguments";

	internal const string BindCallFailedOverloadResolution = "BindCallFailedOverloadResolution";

	internal const string BindBinaryOperatorRequireTwoArguments = "BindBinaryOperatorRequireTwoArguments";

	internal const string BindUnaryOperatorRequireOneArgument = "BindUnaryOperatorRequireOneArgument";

	internal const string BindPropertyFailedMethodGroup = "BindPropertyFailedMethodGroup";

	internal const string BindPropertyFailedEvent = "BindPropertyFailedEvent";

	internal const string BindInvokeFailedNonDelegate = "BindInvokeFailedNonDelegate";

	internal const string BindImplicitConversionRequireOneArgument = "BindImplicitConversionRequireOneArgument";

	internal const string BindExplicitConversionRequireOneArgument = "BindExplicitConversionRequireOneArgument";

	internal const string BindBinaryAssignmentRequireTwoArguments = "BindBinaryAssignmentRequireTwoArguments";

	internal const string BindBinaryAssignmentFailedNullReference = "BindBinaryAssignmentFailedNullReference";

	internal const string NullReferenceOnMemberException = "NullReferenceOnMemberException";

	internal const string BindCallToConditionalMethod = "BindCallToConditionalMethod";

	internal const string BindToVoidMethodButExpectResult = "BindToVoidMethodButExpectResult";

	internal const string EmptyDynamicView = "EmptyDynamicView";

	internal const string GetValueonWriteOnlyProperty = "GetValueonWriteOnlyProperty";

	private static SR loader;

	private ResourceManager resources;

	private static CultureInfo Culture => null;

	public static ResourceManager Resources => GetLoader().resources;

	internal SR()
	{
		resources = new ResourceManager("Microsoft.CSharp.RuntimeBinder", GetType().Assembly);
	}

	private static SR GetLoader()
	{
		if (loader == null)
		{
			SR value = new SR();
			Interlocked.CompareExchange(ref loader, value, null);
		}
		return loader;
	}

	public static string GetString(string name, params object[] args)
	{
		SR sR = GetLoader();
		if (sR == null)
		{
			return null;
		}
		string @string = sR.resources.GetString(name, Culture);
		if (args != null && args.Length != 0)
		{
			for (int i = 0; i < args.Length; i++)
			{
				if (args[i] is string { Length: >1024 } text)
				{
					args[i] = text.Substring(0, 1021) + "...";
				}
			}
			return string.Format(CultureInfo.CurrentCulture, @string, args);
		}
		return @string;
	}

	public static string GetString(string name)
	{
		return GetLoader()?.resources.GetString(name, Culture);
	}

	public static string GetString(string name, out bool usedFallback)
	{
		usedFallback = false;
		return GetString(name);
	}

	public static object GetObject(string name)
	{
		return GetLoader()?.resources.GetObject(name, Culture);
	}
}
