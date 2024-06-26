using System;
using System.ComponentModel;

namespace Microsoft.CSharp.RuntimeBinder;

[AttributeUsage(AttributeTargets.All)]
internal sealed class SRDescriptionAttribute : DescriptionAttribute
{
	private bool replaced;

	public override string Description
	{
		get
		{
			if (!replaced)
			{
				replaced = true;
				base.DescriptionValue = SR.GetString(base.Description);
			}
			return base.Description;
		}
	}

	public SRDescriptionAttribute(string description)
		: base(description)
	{
	}
}
