using System;
using System.Linq;

namespace PropertyEditingTool.Models;

[Serializable]
public class SwProperty
{
	public string Old_Name { get; set; } // 旧名称

	public string Old_Value { get; set; } // 原值

	public string New_Name { get; set; } // 新名称

	public object New_Value { get; set; } // 新值

	public int Type
	{
		get
		{
			if (New_Value != null)
			{
				Type type = New_Value.GetType();
				if (type == typeof(string))
				{
					return 30;
				}
				if (type == typeof(float))
				{
					return 5;
				}
				if (type == typeof(int))
				{
					return 3;
				}
				if (type == typeof(DateTime))
				{
					return 64;
				}
				if (type == typeof(bool))
				{
					return 11;
				}
				return 30;
			}
			return 30;
		}
		set
		{
		}
	}

	public string EditType
	{
		get
		{
			if (Old_Name != null && Old_Value == null && New_Name == null && New_Value != null)
			{
				return "原名 → 新值";
			}
			if (Old_Name == null && Old_Value != null && New_Name == null && New_Value != null)
			{
				if (Old_Value[0] == '{' && Old_Value.Last() == '}')
				{
					return "关键字替换";
				}
				return "原值 → 新值";
			}
			if (Old_Name != null && Old_Value != null && New_Name == null && New_Value != null)
			{
				if (Old_Value[0] == '{' && Old_Value.Last() == '}')
				{
					return "关键字替换2";
				}
				return "（原名、原值） → 新值";
			}
			if (Old_Name != null && Old_Value == null && New_Name != null && New_Value == null)
			{
				return "修改属性名";
			}
			if (Old_Name == null && New_Name != null && New_Value != null)
			{
				return "添加属性";
			}
			if(Old_Name == null && New_Name !=null && New_Value == null)
			{
                return "拷贝（新名 → 旧值）";
            }
			return null;
		}
	}

	public string ConfigName { get; set; }

	public string ErrorInfo { get; set; }

	public bool Checked { get; set; }

	public string Name { get; set; }

	public string FullName { get; set; }

	public int FileType_Int { get; set; }
}
