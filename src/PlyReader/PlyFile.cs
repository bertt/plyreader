using System.Collections.Generic;

namespace PlyReader
{
    /// <summary>Represents a fully parsed PLY file: header metadata and element data.</summary>
    public class PlyFile
    {
        /// <summary>Parsed PLY header.</summary>
        public PlyHeader Header { get; }

        /// <summary>Parsed data for each element declared in the header.</summary>
        public IReadOnlyList<PlyElementData> Elements { get; }

        public PlyFile(PlyHeader header, IReadOnlyList<PlyElementData> elements)
        {
            Header = header;
            Elements = elements;
        }

        /// <summary>Returns the data for the element with the given name, or null if not found.</summary>
        public PlyElementData? GetElement(string name)
        {
            foreach (var e in Elements)
            {
                if (e.Element.Name == name)
                    return e;
            }
            return null;
        }
    }
}
