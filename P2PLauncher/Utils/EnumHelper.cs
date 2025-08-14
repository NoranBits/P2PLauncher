using System;
using System.ComponentModel;
using System.Reflection;

namespace P2PLauncher.Utils
{
    internal static class EnumHelper
    {
        public static string GetDescription(this Enum value)
        {
            ArgumentNullException.ThrowIfNull(value);

            Type type = value.GetType();
            string? name = Enum.GetName(type, value);
            if (name is null)
            {
                return value.ToString();
            }

            FieldInfo? field = type.GetField(name);
            if (field is null)
            {
                return name;
            }

            DescriptionAttribute? attr = field.GetCustomAttribute<DescriptionAttribute>();
            return attr?.Description ?? name;
        }
    }
}
