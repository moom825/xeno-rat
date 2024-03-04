using System;
using System.Reflection;

namespace NAudio.Utils
{
    /// <summary>
    /// Helper to get descriptions
    /// </summary>
    public static class FieldDescriptionHelper
    {

        /// <summary>
        /// Describes the specified type and GUID by retrieving the description from the FieldDescriptionAttribute associated with the matching static public field.
        /// </summary>
        /// <param name="t">The type to be described.</param>
        /// <param name="guid">The GUID used to identify the field.</param>
        /// <returns>The description associated with the matching field, or the name of the field if no description is found, or the string representation of the GUID if no matching field is found.</returns>
        /// <remarks>
        /// This method searches for a static public field within the specified type that matches the provided GUID. If a matching field is found, it retrieves the description from the associated FieldDescriptionAttribute, if present, and returns it. If no description is found, it returns the name of the field. If no matching field is found, it returns the string representation of the provided GUID.
        /// </remarks>
        public static string Describe(Type t, Guid guid)
        {
            // when we go to .NET 3.5, use LINQ for this
            foreach (var f in t
                .GetFields(BindingFlags.Static | BindingFlags.Public))
            {
                if (f.IsPublic && f.IsStatic && f.FieldType == typeof (Guid) && (Guid) f.GetValue(null) == guid)
                {
                    foreach (var a in f.GetCustomAttributes(false))
                    {
                        var d = a as FieldDescriptionAttribute;
                        if (d != null)
                        {
                            return d.Description;
                        }
                    }
                    // no attribute, return the name
                    return f.Name;
                }
            }
            return guid.ToString();
        }
    }
}
