using System.ComponentModel;
using System.Reflection;

namespace RagnarokBotWeb.Crosscutting.Extensions
{
    public static class EnumExtensions
    {
        public static string GetDescription(this Enum value)
        {
            FieldInfo field = value.GetType().GetField(value.ToString());

            if (field != null)
            {
                var attribute = field.GetCustomAttribute<DescriptionAttribute>(false);
                if (attribute != null)
                    return attribute.Description;
            }

            // Fallback to enum name if no description is set
            return value.ToString();
        }
    }
}
