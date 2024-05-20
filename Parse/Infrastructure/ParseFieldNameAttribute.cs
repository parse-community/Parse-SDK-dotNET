using System;

namespace Parse
{
    /// <summary>
    /// Specifies a field name for a property on a ParseObject subclass.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class ParseFieldNameAttribute : Attribute
    {
        /// <summary>
        /// Constructs a new ParseFieldName attribute.
        /// </summary>
        /// <param name="fieldName">The name of the field on the ParseObject that the
        /// property represents.</param>
        public ParseFieldNameAttribute(string fieldName) => FieldName = fieldName;

        /// <summary>
        /// Gets the name of the field represented by this property.
        /// </summary>
        public string FieldName { get; private set; }
    }
}
