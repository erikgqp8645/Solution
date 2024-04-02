using System;
using System.Dynamic;

namespace Microsoft.CSharp.RuntimeBinder;

internal sealed class CSharpConvertBinder : ConvertBinder
{
	private CSharpConversionKind m_conversionKind;

	private bool m_isChecked;

	private Type m_callingContext;

	private RuntimeBinder m_binder;

	internal CSharpConversionKind ConversionKind => m_conversionKind;

	internal bool IsChecked => m_isChecked;

	internal Type CallingContext => m_callingContext;

	public CSharpConvertBinder(Type type, CSharpConversionKind conversionKind, bool isChecked, Type callingContext)
		: base(type, conversionKind == CSharpConversionKind.ExplicitConversion)
	{
		m_conversionKind = conversionKind;
		m_isChecked = isChecked;
		m_callingContext = callingContext;
		m_binder = RuntimeBinder.GetInstance();
	}

	public override DynamicMetaObject FallbackConvert(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
	{
		if (!BinderHelper.IsWindowsRuntimeObject(target) && ComBinder.TryConvert(this, target, out var result))
		{
			return result;
		}
		return BinderHelper.Bind(this, m_binder, new DynamicMetaObject[1] { target }, null, errorSuggestion);
	}
}
