using System;
using System.Linq;
using System.Reflection;

namespace CalculateFunding.Models
{
	public static class EnumHelper
	{
		public static T GetAttributeOfType<T>(this Enum enumVal) where T : System.Attribute
		{
			Type type = enumVal.GetType();
			MemberInfo[] memInfo = type.GetMember(enumVal.ToString());
			if (memInfo == null || !memInfo.Any())
			{
				return null;
			}
			object[] attributes = memInfo[0].GetCustomAttributes(typeof(T), false);
			return (attributes?.Length > 0) ? (T)attributes[0] : null;
		}
	}
}
