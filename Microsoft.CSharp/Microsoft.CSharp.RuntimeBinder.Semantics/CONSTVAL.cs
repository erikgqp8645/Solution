using System;
using System.Globalization;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal sealed class CONSTVAL
{
	private object value;

	public object objectVal
	{
		get
		{
			return value;
		}
		set
		{
			this.value = value;
		}
	}

	public bool boolVal
	{
		get
		{
			return SpecialUnbox<bool>(value);
		}
		set
		{
			this.value = SpecialBox(value);
		}
	}

	public sbyte sbyteVal => SpecialUnbox<sbyte>(value);

	public byte byteVal => SpecialUnbox<byte>(value);

	public short shortVal => SpecialUnbox<short>(value);

	public ushort ushortVal => SpecialUnbox<ushort>(value);

	public int iVal
	{
		get
		{
			return SpecialUnbox<int>(value);
		}
		set
		{
			this.value = SpecialBox(value);
		}
	}

	public uint uiVal
	{
		get
		{
			return SpecialUnbox<uint>(value);
		}
		set
		{
			this.value = SpecialBox(value);
		}
	}

	public long longVal
	{
		get
		{
			return SpecialUnbox<long>(value);
		}
		set
		{
			this.value = SpecialBox(value);
		}
	}

	public ulong ulongVal
	{
		get
		{
			return SpecialUnbox<ulong>(value);
		}
		set
		{
			this.value = SpecialBox(value);
		}
	}

	public float floatVal
	{
		get
		{
			return SpecialUnbox<float>(value);
		}
		set
		{
			this.value = SpecialBox(value);
		}
	}

	public double doubleVal
	{
		get
		{
			return SpecialUnbox<double>(value);
		}
		set
		{
			this.value = SpecialBox(value);
		}
	}

	public decimal decVal
	{
		get
		{
			return SpecialUnbox<decimal>(value);
		}
		set
		{
			this.value = SpecialBox(value);
		}
	}

	public char cVal => SpecialUnbox<char>(value);

	public string strVal
	{
		get
		{
			return SpecialUnbox<string>(value);
		}
		set
		{
			this.value = SpecialBox(value);
		}
	}

	internal CONSTVAL()
		: this(null)
	{
	}

	internal CONSTVAL(object value)
	{
		this.value = value;
	}

	public bool IsNullRef()
	{
		return value == null;
	}

	public bool IsZero(ConstValKind kind)
	{
		return kind switch
		{
			ConstValKind.Decimal => decVal == 0m, 
			ConstValKind.String => false, 
			_ => IsDefault(value), 
		};
	}

	private T SpecialUnbox<T>(object o)
	{
		if (IsDefault(o))
		{
			return default(T);
		}
		return (T)Convert.ChangeType(o, typeof(T), CultureInfo.InvariantCulture);
	}

	private object SpecialBox<T>(T x)
	{
		return x;
	}

	private bool IsDefault(object o)
	{
		if (o == null)
		{
			return true;
		}
		return Type.GetTypeCode(o.GetType()) switch
		{
			TypeCode.Boolean => false.Equals(o), 
			TypeCode.SByte => ((sbyte)0).Equals(o), 
			TypeCode.Byte => ((byte)0).Equals(o), 
			TypeCode.Int16 => ((short)0).Equals(o), 
			TypeCode.UInt16 => ((ushort)0).Equals(o), 
			TypeCode.Int32 => 0.Equals(o), 
			TypeCode.UInt32 => 0u.Equals(o), 
			TypeCode.Int64 => 0L.Equals(o), 
			TypeCode.UInt64 => 0uL.Equals(o), 
			TypeCode.Single => 0f.Equals(o), 
			TypeCode.Double => 0.0.Equals(o), 
			TypeCode.Decimal => 0m.Equals(o), 
			TypeCode.Char => '\0'.Equals(o), 
			_ => false, 
		};
	}
}
