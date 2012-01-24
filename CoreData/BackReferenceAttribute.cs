using System;

namespace CoreData
{
    /// <summary>
    /// Indicates that the property is a backwards reference to the parent object that owns it.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class BackReferenceAttribute : Attribute
    {
    }
}
