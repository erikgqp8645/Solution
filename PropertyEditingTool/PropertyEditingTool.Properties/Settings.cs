using System.CodeDom.Compiler;
using System.Configuration;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace PropertyEditingTool.Properties;

[CompilerGenerated]
[GeneratedCode("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "15.9.0.0")]
internal sealed class Settings : ApplicationSettingsBase
{
	private static Settings defaultInstance = (Settings)SettingsBase.Synchronized(new Settings());

	public static Settings Default => defaultInstance;

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("0")]
	public int PropRename
	{
		get
		{
			return (int)this["PropRename"];
		}
		set
		{
			this["PropRename"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("0")]
	public int ProcessRange
	{
		get
		{
			return (int)this["ProcessRange"];
		}
		set
		{
			this["ProcessRange"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("图幅代号")]
	public string PropName
	{
		get
		{
			return (string)this["PropName"];
		}
		set
		{
			this["PropName"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("True")]
	public bool IsWriteModel
	{
		get
		{
			return (bool)this["IsWriteModel"];
		}
		set
		{
			this["IsWriteModel"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("False")]
	public bool IsWriteDraw
	{
		get
		{
			return (bool)this["IsWriteDraw"];
		}
		set
		{
			this["IsWriteDraw"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("")]
	public string DrawNumName
	{
		get
		{
			return (string)this["DrawNumName"];
		}
		set
		{
			this["DrawNumName"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("Md-{000}")]
	public string StartNumber
	{
		get
		{
			return (string)this["StartNumber"];
		}
		set
		{
			this["StartNumber"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("True")]
	public bool IsWriteConfig
	{
		get
		{
			return (bool)this["IsWriteConfig"];
		}
		set
		{
			this["IsWriteConfig"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("False")]
	public bool IsWriteCustom
	{
		get
		{
			return (bool)this["IsWriteCustom"];
		}
		set
		{
			this["IsWriteCustom"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("False")]
	public bool IsWriteCorresModel
	{
		get
		{
			return (bool)this["IsWriteCorresModel"];
		}
		set
		{
			this["IsWriteCorresModel"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("False")]
	public bool IsWriteDrawDoc
	{
		get
		{
			return (bool)this["IsWriteDrawDoc"];
		}
		set
		{
			this["IsWriteDrawDoc"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("0")]
	public int PropProcessRange
	{
		get
		{
			return (int)this["PropProcessRange"];
		}
		set
		{
			this["PropProcessRange"] = value;
		}
	}
}
