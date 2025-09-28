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
            var name = Enum.GetName(type, value);
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
            // Return the attribute description if present; fall back to the enum member name.
            return attr?.Description ?? name;
        }
    }
}
