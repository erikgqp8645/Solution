using System.Collections.Generic;
using System.IO;

namespace PropertyEditingTool.Models;

public class SwFile
{
	public string Name => Path.GetFileName(FullName);
	public string NameWithoutExtension => Path.GetFileNameWithoutExtension(FullName);

	public string FileType
	{
		get
		{
			return Path.GetExtension(FullName).ToUpper() switch
			{
				".SLDPRT" => "零件", 
				".SLDASM" => "装配", 
				".SLDDRW" => "图纸", 
				".PRTDOT" => "模板", 
				".DRWDOT" => "模板", 
				".ASMDOT" => "模板", 
				".SLDLFP" => "库特征", 
				_ => "NULL", 
			};
		}
		set
		{
		}
	}

	public int FileType_Int => Path.GetExtension(FullName).ToUpper() switch
	{
		".SLDPRT" => 1, 
		".SLDASM" => 2, 
		".SLDDRW" => 3, 
		".PRTDOT" => 1, 
		".ASMDOT" => 2, 
		".DRWDOT" => 3, 
		".SLDLFP" => 1, 
		_ => 0, 
	};

	public string FullName { get; set; }

	public List<SwProperty> listSwProperty { get; set; }

	public string EditResult { get; set; } = "未修改";


	public string FileTypeEx { get; set; }

	public string DrwNumName { get; set; }

	public string DrwNumValue { get; set; }

	public List<SwAddProperty> listSwAddProperty { get; set; }

	public SwFile(string fullName)
	{
		FullName = fullName;
		listSwProperty = new List<SwProperty>(); 
	}
}
