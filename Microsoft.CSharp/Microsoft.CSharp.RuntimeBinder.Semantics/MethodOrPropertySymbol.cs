using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class MethodOrPropertySymbol : ParentSymbol
{
	public uint modOptCount;

	public new bool isStatic;

	public bool isOverride;

	public bool useMethInstead;

	public bool isOperator;

	public bool isParamArray;

	public bool isHideByName;

	private bool[] optionalParameterIndex;

	private bool[] defaultParameterIndex;

	private CONSTVAL[] defaultParameters;

	private CType[] defaultParameterConstValTypes;

	private bool[] dispatchConstantParameterIndex;

	private bool[] unknownConstantParameterIndex;

	private bool[] marshalAsIndex;

	private UnmanagedType[] marshalAsBuffer;

	public SymWithType swtSlot;

	public ErrorType errExpImpl;

	public CType RetType;

	private TypeArray _Params;

	public AggregateDeclaration declaration;

	public int MetadataToken;

	public List<Name> ParameterNames { get; private set; }

	public TypeArray Params
	{
		get
		{
			return _Params;
		}
		set
		{
			_Params = value;
			optionalParameterIndex = new bool[_Params.size];
			defaultParameterIndex = new bool[_Params.size];
			defaultParameters = new CONSTVAL[_Params.size];
			defaultParameterConstValTypes = new CType[_Params.size];
			dispatchConstantParameterIndex = new bool[_Params.size];
			unknownConstantParameterIndex = new bool[_Params.size];
			marshalAsIndex = new bool[_Params.size];
			marshalAsBuffer = new UnmanagedType[_Params.size];
		}
	}

	public MethodOrPropertySymbol()
	{
		ParameterNames = new List<Name>();
	}

	public bool IsParameterOptional(int index)
	{
		if (optionalParameterIndex == null)
		{
			return false;
		}
		return optionalParameterIndex[index];
	}

	public void SetOptionalParameter(int index)
	{
		optionalParameterIndex[index] = true;
	}

	public bool HasOptionalParameters()
	{
		if (optionalParameterIndex == null)
		{
			return false;
		}
		bool[] array = optionalParameterIndex;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i])
			{
				return true;
			}
		}
		return false;
	}

	public bool HasDefaultParameterValue(int index)
	{
		return defaultParameterIndex[index];
	}

	public void SetDefaultParameterValue(int index, CType type, CONSTVAL cv)
	{
		ConstValFactory constValFactory = new ConstValFactory();
		defaultParameterIndex[index] = true;
		defaultParameters[index] = constValFactory.Copy(type.constValKind(), cv);
		defaultParameterConstValTypes[index] = type;
	}

	public CONSTVAL GetDefaultParameterValue(int index)
	{
		return defaultParameters[index];
	}

	public CType GetDefaultParameterValueConstValType(int index)
	{
		return defaultParameterConstValTypes[index];
	}

	public bool IsMarshalAsParameter(int index)
	{
		return marshalAsIndex[index];
	}

	public void SetMarshalAsParameter(int index, UnmanagedType umt)
	{
		marshalAsIndex[index] = true;
		marshalAsBuffer[index] = umt;
	}

	public UnmanagedType GetMarshalAsParameterValue(int index)
	{
		return marshalAsBuffer[index];
	}

	public bool MarshalAsObject(int index)
	{
		UnmanagedType unmanagedType = (UnmanagedType)0;
		if (IsMarshalAsParameter(index))
		{
			unmanagedType = GetMarshalAsParameterValue(index);
		}
		if (unmanagedType != UnmanagedType.Interface && unmanagedType != UnmanagedType.IUnknown)
		{
			return unmanagedType == UnmanagedType.IDispatch;
		}
		return true;
	}

	public bool IsDispatchConstantParameter(int index)
	{
		return dispatchConstantParameterIndex[index];
	}

	public void SetDispatchConstantParameter(int index)
	{
		dispatchConstantParameterIndex[index] = true;
	}

	public bool IsUnknownConstantParameter(int index)
	{
		return unknownConstantParameterIndex[index];
	}

	public void SetUnknownConstantParameter(int index)
	{
		unknownConstantParameterIndex[index] = true;
	}

	public AggregateSymbol getClass()
	{
		return parent.AsAggregateSymbol();
	}

	public bool IsExpImpl()
	{
		return name == null;
	}

	public AggregateDeclaration containingDeclaration()
	{
		return declaration;
	}
}
