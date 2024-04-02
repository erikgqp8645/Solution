namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class EXPRCONSTANT : EXPR
{
	public EXPR OptionalConstructorCall;

	private CONSTVAL val;

	public bool IsZero => Val.IsZero(type.constValKind());

	public CONSTVAL Val
	{
		get
		{
			return val;
		}
		set
		{
			val = value;
		}
	}

	public long I64Value
	{
		get
		{
			switch (type.fundType())
			{
			case FUNDTYPE.FT_I8:
			case FUNDTYPE.FT_U8:
				return val.longVal;
			case FUNDTYPE.FT_U4:
				return val.uiVal;
			case FUNDTYPE.FT_I1:
			case FUNDTYPE.FT_I2:
			case FUNDTYPE.FT_I4:
			case FUNDTYPE.FT_U1:
			case FUNDTYPE.FT_U2:
				return val.iVal;
			default:
				return 0L;
			}
		}
	}

	public EXPR GetOptionalConstructorCall()
	{
		return OptionalConstructorCall;
	}

	public void SetOptionalConstructorCall(EXPR value)
	{
		OptionalConstructorCall = value;
	}

	public bool isZero()
	{
		return IsZero;
	}

	public CONSTVAL getVal()
	{
		return Val;
	}

	public void setVal(CONSTVAL newValue)
	{
		Val = newValue;
	}

	public ulong getU64Value()
	{
		return val.ulongVal;
	}

	public long getI64Value()
	{
		return I64Value;
	}
}
