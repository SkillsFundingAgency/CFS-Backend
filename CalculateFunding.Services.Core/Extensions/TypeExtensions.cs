using System;
using System.Text;

namespace CalculateFunding.Services.Core.Extensions
{
    public static class TypeExtensions
    {
        /// <summary>
        /// Gets a friendly type name, whether the type is a generic or not
        /// </summary>
        /// <param name="type">The type to get the name for</param>
        /// <returns>The friendly name of the type</returns>
        public static string GetFriendlyName(this Type type)
        {
            StringBuilder friendlyName = new StringBuilder();
            string typeName = type.Name;

            if (type.IsGenericType)
            {
                int backtickPosition = typeName.IndexOf('`');
                if (backtickPosition > 0)
                {
                    friendlyName.Append(typeName.Remove(backtickPosition));
                }

                friendlyName.Append("<");
                Type[] typeParameters = type.GetGenericArguments();
                for (int i = 0; i < typeParameters.Length; ++i)
                {
                    string typeParamName = GetFriendlyName(typeParameters[i]);
                    friendlyName.Append(i == 0 ? typeParamName : ", " + typeParamName);
                }
                friendlyName.Append(">");
            }
            else
            {
                friendlyName.Append(type.Name);
            }

            return friendlyName.ToString();
        }
    }
}
