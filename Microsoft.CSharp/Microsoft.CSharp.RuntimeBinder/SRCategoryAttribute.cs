using System;
using System.ComponentModel;

namespace Microsoft.CSharp.RuntimeBinder;

[AttributeUsage(AttributeTargets.All)]
internal sealed class SRCategoryAttribute : CategoryAttribute
{
	public SRCategoryAttribute(string category)
		: base(category)
	{
	}

	protected override string GetLocalizedString(string value)
	{
		return SR.GetString(value);
	}
}
