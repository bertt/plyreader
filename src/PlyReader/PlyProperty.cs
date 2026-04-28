namespace PlyReader
{
    /// <summary>Represents a single property declaration in a PLY element.</summary>
    public class PlyProperty
    {
        /// <summary>Property name.</summary>
        public string Name { get; }

        /// <summary>True when this is a list property (e.g. vertex_indices).</summary>
        public bool IsList { get; }

        /// <summary>Type of the scalar value, or the element type for list properties.</summary>
        public PlyPropertyType Type { get; }

        /// <summary>Count type for list properties (e.g. uchar before int in "property list uchar int …").</summary>
        public PlyPropertyType? CountType { get; }

        public PlyProperty(string name, PlyPropertyType type)
        {
            Name = name;
            Type = type;
            IsList = false;
        }

        public PlyProperty(string name, PlyPropertyType countType, PlyPropertyType elementType)
        {
            Name = name;
            CountType = countType;
            Type = elementType;
            IsList = true;
        }

        public override string ToString() =>
            IsList
                ? $"property list {CountType!.Value.ToTypeString()} {Type.ToTypeString()} {Name}"
                : $"property {Type.ToTypeString()} {Name}";
    }
}
