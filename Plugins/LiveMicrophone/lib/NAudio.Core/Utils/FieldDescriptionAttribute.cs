using System;

namespace NAudio.Utils
{
    /// <summary>
    /// Allows us to add descriptions to interop members
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class FieldDescriptionAttribute : Attribute
    {
        /// <summary>
        /// The description
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Field description
        /// </summary>
        public FieldDescriptionAttribute(string description)
        {
            Description = description;
        }

        /// <summary>
        /// Returns the description of the object as a string.
        /// </summary>
        /// <returns>The description of the object as a string.</returns>
        public override string ToString()
        {
            return Description;
        }
    }
}
