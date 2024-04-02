namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal static class ExpressionKindExtensions
{
	public static bool isRelational(this ExpressionKind kind)
	{
		if (ExpressionKind.EK_EQ <= kind)
		{
			return kind <= ExpressionKind.EK_GE;
		}
		return false;
	}

	public static bool isUnaryOperator(this ExpressionKind kind)
	{
		switch (kind)
		{
		case ExpressionKind.EK_TRUE:
		case ExpressionKind.EK_FALSE:
		case ExpressionKind.EK_INC:
		case ExpressionKind.EK_DEC:
		case ExpressionKind.EK_LOGNOT:
		case ExpressionKind.EK_NEG:
		case ExpressionKind.EK_UPLUS:
		case ExpressionKind.EK_BITNOT:
		case ExpressionKind.EK_ADDR:
		case ExpressionKind.EK_DECIMALNEG:
		case ExpressionKind.EK_DECIMALINC:
		case ExpressionKind.EK_DECIMALDEC:
			return true;
		default:
			return false;
		}
	}
}
