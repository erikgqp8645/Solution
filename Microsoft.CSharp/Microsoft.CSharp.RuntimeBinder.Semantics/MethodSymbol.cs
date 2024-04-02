using System.Reflection;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class MethodSymbol : MethodOrPropertySymbol
{
	private MethodKindEnum methKind;

	private bool inferenceMustFail;

	private bool checkedInfMustFail;

	private MethodSymbol m_convNext;

	private PropertySymbol m_prop;

	private EventSymbol m_evt;

	public bool isExtension;

	public bool isExternal;

	public bool isVirtual;

	public bool isAbstract;

	public bool isVarargs;

	public MemberInfo AssociatedMemberInfo;

	public TypeArray typeVars;

	public bool InferenceMustFail()
	{
		if (checkedInfMustFail)
		{
			return inferenceMustFail;
		}
		checkedInfMustFail = true;
		for (int i = 0; i < typeVars.Size; i++)
		{
			TypeParameterType typeFind = typeVars.ItemAsTypeParameterType(i);
			int num = 0;
			while (true)
			{
				if (num >= base.Params.Size)
				{
					inferenceMustFail = true;
					return true;
				}
				if (TypeManager.TypeContainsType(base.Params.Item(num), typeFind))
				{
					break;
				}
				num++;
			}
		}
		return false;
	}

	public bool IsExtension()
	{
		return isExtension;
	}

	public MethodKindEnum MethKind()
	{
		return methKind;
	}

	public bool IsConstructor()
	{
		return methKind == MethodKindEnum.Constructor;
	}

	public bool IsNullableConstructor()
	{
		if (getClass().isPredefAgg(PredefinedType.PT_G_OPTIONAL) && base.Params.Size == 1 && base.Params.Item(0).IsGenericParameter)
		{
			return IsConstructor();
		}
		return false;
	}

	public bool IsDestructor()
	{
		return methKind == MethodKindEnum.Destructor;
	}

	public bool isPropertyAccessor()
	{
		return methKind == MethodKindEnum.PropAccessor;
	}

	public bool isEventAccessor()
	{
		return methKind == MethodKindEnum.EventAccessor;
	}

	public bool isExplicit()
	{
		return methKind == MethodKindEnum.ExplicitConv;
	}

	public bool isImplicit()
	{
		return methKind == MethodKindEnum.ImplicitConv;
	}

	public bool isInvoke()
	{
		return methKind == MethodKindEnum.Invoke;
	}

	public void SetMethKind(MethodKindEnum mk)
	{
		methKind = mk;
	}

	public MethodSymbol ConvNext()
	{
		return m_convNext;
	}

	public void SetConvNext(MethodSymbol conv)
	{
		m_convNext = conv;
	}

	public PropertySymbol getProperty()
	{
		return m_prop;
	}

	public void SetProperty(PropertySymbol prop)
	{
		m_prop = prop;
	}

	public EventSymbol getEvent()
	{
		return m_evt;
	}

	public void SetEvent(EventSymbol evt)
	{
		m_evt = evt;
	}

	public bool isConversionOperator()
	{
		if (!isExplicit())
		{
			return isImplicit();
		}
		return true;
	}

	public new bool isUserCallable()
	{
		if (!isOperator)
		{
			return !isAnyAccessor();
		}
		return false;
	}

	public bool isAnyAccessor()
	{
		if (!isPropertyAccessor())
		{
			return isEventAccessor();
		}
		return true;
	}

	public bool isSetAccessor()
	{
		if (!isPropertyAccessor())
		{
			return false;
		}
		PropertySymbol property = getProperty();
		if (property == null)
		{
			return false;
		}
		return this == property.methSet;
	}
}
