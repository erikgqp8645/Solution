using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;

namespace Microsoft.CSharp.RuntimeBinder;

internal sealed class GetMemberValueBinder : GetMemberBinder
{
	public GetMemberValueBinder(string name, bool ignoreCase)
		: base(name, ignoreCase)
	{
	}

	public override DynamicMetaObject FallbackGetMember(DynamicMetaObject self, DynamicMetaObject onBindingError)
	{
		if (onBindingError == null)
		{
			List<DynamicMetaObject> contributingObjects = new List<DynamicMetaObject> { self };
			return new DynamicMetaObject(Expression.Throw(Expression.Constant(new DynamicBindingFailedException(), typeof(Exception)), typeof(object)), BindingRestrictions.Combine(contributingObjects));
		}
		return onBindingError;
	}
}
