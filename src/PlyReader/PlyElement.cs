using System.Collections.Generic;

namespace PlyReader
{
    /// <summary>Represents one element block declared in a PLY header (e.g. "element vertex 200000").</summary>
    public class PlyElement
    {
        /// <summary>Element name (e.g. "vertex", "face").</summary>
        public string Name { get; }

        /// <summary>Number of instances of this element in the file.</summary>
        public long Count { get; }

        /// <summary>Ordered list of properties belonging to this element.</summary>
        public IReadOnlyList<PlyProperty> Properties { get; }

        public PlyElement(string name, long count, IReadOnlyList<PlyProperty> properties)
        {
            Name = name;
            Count = count;
            Properties = properties;
        }
    }
}
