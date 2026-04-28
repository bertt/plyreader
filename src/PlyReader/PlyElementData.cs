using System.Collections.Generic;

namespace PlyReader
{
    /// <summary>
    /// Holds the parsed data rows for one PLY element.
    /// Each row is an array of boxed values whose types match the element's property declarations.
    /// For scalar properties the value is a boxed primitive (float, double, int, etc.).
    /// For list properties the value is an <see cref="object[]"/> containing the list elements.
    /// </summary>
    public class PlyElementData
    {
        /// <summary>The element descriptor (name, count, properties).</summary>
        public PlyElement Element { get; }

        /// <summary>
        /// The parsed rows. Outer index is row (0..Element.Count-1),
        /// inner index matches <see cref="PlyElement.Properties"/>.
        /// </summary>
        public IReadOnlyList<object[]> Rows { get; }

        public PlyElementData(PlyElement element, IReadOnlyList<object[]> rows)
        {
            Element = element;
            Rows = rows;
        }

        /// <summary>Returns the value of a named property for a given row index.</summary>
        public object GetValue(int rowIndex, string propertyName)
        {
            var props = Element.Properties;
            for (int i = 0; i < props.Count; i++)
            {
                if (props[i].Name == propertyName)
                    return Rows[rowIndex][i];
            }
            throw new System.ArgumentException($"Property '{propertyName}' not found in element '{Element.Name}'.");
        }
    }
}
