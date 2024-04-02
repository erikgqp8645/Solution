namespace PropertyEditingTool.Models;

public class SwAddProperty
{
	public string PropertyName { get; set; }

	public string Rule { get; set; }

	public string AddResult { get; set; }

	public SwAddProperty(string propertyName, string rule)
	{
		PropertyName = propertyName;
		Rule = rule;
	}

	public SwAddProperty()
	{
	}
}
