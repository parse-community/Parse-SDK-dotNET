using System;

namespace Parse
{
    /// <summary>
    /// Defines the class name for a subclass of ParseObject.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class ParseClassNameAttribute : Attribute
    {
        /// <summary>
        /// Constructs a new ParseClassName attribute.
        /// </summary>
        /// <param name="className">The class name to associate with the ParseObject subclass.</param>
        public ParseClassNameAttribute(string className) => ClassName = className;

        /// <summary>
        /// Gets the class name to associate with the ParseObject subclass.
        /// </summary>
        public string ClassName { get; private set; }
    }
}
