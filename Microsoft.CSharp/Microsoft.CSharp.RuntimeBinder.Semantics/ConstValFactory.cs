namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal sealed class ConstValFactory
{
	public CONSTVAL Copy(ConstValKind kind, CONSTVAL value)
	{
		return new CONSTVAL(value.objectVal);
	}

	public static CONSTVAL GetDefaultValue(ConstValKind kind)
	{
		CONSTVAL cONSTVAL = new CONSTVAL();
		switch (kind)
		{
		case ConstValKind.Int:
			cONSTVAL.iVal = 0;
			break;
		case ConstValKind.Double:
			cONSTVAL.doubleVal = 0.0;
			break;
		case ConstValKind.Long:
			cONSTVAL.longVal = 0L;
			break;
		case ConstValKind.Decimal:
			cONSTVAL.decVal = 0m;
			break;
		case ConstValKind.Float:
			cONSTVAL.floatVal = 0f;
			break;
		case ConstValKind.Boolean:
			cONSTVAL.boolVal = false;
			break;
		}
		return cONSTVAL;
	}

	public static CONSTVAL GetNullRef()
	{
		return new CONSTVAL();
	}

	public static CONSTVAL GetBool(bool value)
	{
		CONSTVAL cONSTVAL = new CONSTVAL();
		cONSTVAL.boolVal = value;
		return cONSTVAL;
	}

	public static CONSTVAL GetInt(int value)
	{
		CONSTVAL cONSTVAL = new CONSTVAL();
		cONSTVAL.iVal = value;
		return cONSTVAL;
	}

	public static CONSTVAL GetUInt(uint value)
	{
		CONSTVAL cONSTVAL = new CONSTVAL();
		cONSTVAL.uiVal = value;
		return cONSTVAL;
	}

	public CONSTVAL Create(decimal value)
	{
		CONSTVAL cONSTVAL = new CONSTVAL();
		cONSTVAL.decVal = value;
		return cONSTVAL;
	}

	public CONSTVAL Create(string value)
	{
		CONSTVAL cONSTVAL = new CONSTVAL();
		cONSTVAL.strVal = value;
		return cONSTVAL;
	}

	public CONSTVAL Create(float value)
	{
		CONSTVAL cONSTVAL = new CONSTVAL();
		cONSTVAL.floatVal = value;
		return cONSTVAL;
	}

	public CONSTVAL Create(double value)
	{
		CONSTVAL cONSTVAL = new CONSTVAL();
		cONSTVAL.doubleVal = value;
		return cONSTVAL;
	}

	public CONSTVAL Create(long value)
	{
		CONSTVAL cONSTVAL = new CONSTVAL();
		cONSTVAL.longVal = value;
		return cONSTVAL;
	}

	public CONSTVAL Create(ulong value)
	{
		CONSTVAL cONSTVAL = new CONSTVAL();
		cONSTVAL.ulongVal = value;
		return cONSTVAL;
	}

	internal CONSTVAL Create(bool value)
	{
		CONSTVAL cONSTVAL = new CONSTVAL();
		cONSTVAL.boolVal = value;
		return cONSTVAL;
	}

	internal CONSTVAL Create(object p)
	{
		CONSTVAL cONSTVAL = new CONSTVAL();
		cONSTVAL.objectVal = p;
		return cONSTVAL;
	}
}
