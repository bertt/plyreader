using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PlyReader
{
    /// <summary>
    /// Reads PLY (Polygon File Format / Stanford Triangle Format) files.
    /// Supports ASCII, binary little-endian and binary big-endian formats.
    /// </summary>
    public class PlyReader
    {
        /// <summary>Reads a PLY file from the given file path.</summary>
        public static PlyFile Read(string filePath)
        {
            using (var stream = File.OpenRead(filePath))
                return Read(stream);
        }

        /// <summary>Reads a PLY file from the given stream.</summary>
        public static PlyFile Read(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            // We need to read the header as text, then switch to binary for binary formats.
            // We read byte-by-byte to locate the end_header marker precisely.
            var headerBytes = ReadHeaderBytes(stream);
            var header = ParseHeader(headerBytes);
            var elements = ReadElementData(stream, header);
            return new PlyFile(header, elements);
        }

        // ─── Header reading ──────────────────────────────────────────────────────

        private static byte[] ReadHeaderBytes(Stream stream)
        {
            // Read until we find "end_header\n" or "end_header\r\n"
            var buffer = new List<byte>(4096);
            const string endMarker = "end_header";
            int b;
            while ((b = stream.ReadByte()) != -1)
            {
                buffer.Add((byte)b);
                // Check if the buffer ends with "end_header\n"
                if (buffer.Count >= endMarker.Length + 1)
                {
                    int tail = buffer.Count - 1;
                    if (buffer[tail] == '\n')
                    {
                        int start = tail - endMarker.Length;
                        if (start >= 0)
                        {
                            bool match = true;
                            for (int i = 0; i < endMarker.Length; i++)
                            {
                                if (buffer[start + i] != (byte)endMarker[i])
                                {
                                    match = false;
                                    break;
                                }
                            }
                            if (match)
                                return buffer.ToArray();
                        }
                    }
                }
            }
            throw new FormatException("PLY header 'end_header' not found.");
        }

        private static PlyHeader ParseHeader(byte[] headerBytes)
        {
            var text = Encoding.ASCII.GetString(headerBytes);
            var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length == 0 || lines[0].Trim() != "ply")
                throw new FormatException("Not a valid PLY file: missing 'ply' magic.");

            PlyFormat format = PlyFormat.Ascii;
            string version = "1.0";
            var comments = new List<string>();
            var objInfo = new List<string>();
            var elements = new List<PlyElement>();

            PlyElement? currentElement = null;
            List<PlyProperty>? currentProps = null;
            long currentCount = 0;
            string currentName = string.Empty;

            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line == "end_header")
                {
                    FlushElement(ref currentElement, ref currentName, ref currentCount, ref currentProps, elements);
                    break;
                }

                if (line.StartsWith("format "))
                {
                    var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 3)
                        throw new FormatException($"Malformed format line: '{line}'");
                    format = ParseFormat(parts[1]);
                    version = parts[2];
                }
                else if (line.StartsWith("comment "))
                {
                    comments.Add(line.Substring("comment ".Length));
                }
                else if (line.StartsWith("obj_info "))
                {
                    objInfo.Add(line.Substring("obj_info ".Length));
                }
                else if (line.StartsWith("element "))
                {
                    FlushElement(ref currentElement, ref currentName, ref currentCount, ref currentProps, elements);
                    var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 3)
                        throw new FormatException($"Malformed element line: '{line}'");
                    currentName = parts[1];
                    currentCount = long.Parse(parts[2]);
                    currentProps = new List<PlyProperty>();
                    currentElement = null; // will be built in FlushElement
                }
                else if (line.StartsWith("property "))
                {
                    if (currentProps == null)
                        throw new FormatException("'property' declared before any 'element'.");
                    currentProps.Add(ParseProperty(line));
                }
            }

            return new PlyHeader(format, version, comments, objInfo, elements);
        }

        private static void FlushElement(
            ref PlyElement? current,
            ref string name,
            ref long count,
            ref List<PlyProperty>? props,
            List<PlyElement> elements)
        {
            if (props != null)
            {
                elements.Add(new PlyElement(name, count, props));
                props = null;
                name = string.Empty;
                count = 0;
            }
            current = null;
        }

        private static PlyFormat ParseFormat(string token)
        {
            switch (token)
            {
                case "ascii":                return PlyFormat.Ascii;
                case "binary_little_endian": return PlyFormat.BinaryLittleEndian;
                case "binary_big_endian":    return PlyFormat.BinaryBigEndian;
                default:
                    throw new FormatException($"Unknown PLY format: '{token}'");
            }
        }

        private static PlyProperty ParseProperty(string line)
        {
            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            // "property <type> <name>"  or  "property list <countType> <elementType> <name>"
            if (parts.Length < 3)
                throw new FormatException($"Malformed property line: '{line}'");

            if (parts[1] == "list")
            {
                if (parts.Length < 5)
                    throw new FormatException($"Malformed list property line: '{line}'");
                var countType = PlyTypeExtensions.ParsePropertyType(parts[2]);
                var elemType  = PlyTypeExtensions.ParsePropertyType(parts[3]);
                return new PlyProperty(parts[4], countType, elemType);
            }
            else
            {
                var type = PlyTypeExtensions.ParsePropertyType(parts[1]);
                return new PlyProperty(parts[2], type);
            }
        }

        // ─── Data reading ────────────────────────────────────────────────────────

        private static List<PlyElementData> ReadElementData(Stream stream, PlyHeader header)
        {
            var result = new List<PlyElementData>(header.Elements.Count);

            if (header.Format == PlyFormat.Ascii)
            {
                var reader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false, bufferSize: 65536, leaveOpen: true);
                foreach (var element in header.Elements)
                    result.Add(ReadAsciiElement(reader, element));
            }
            else
            {
                bool littleEndian = header.Format == PlyFormat.BinaryLittleEndian;
                foreach (var element in header.Elements)
                    result.Add(ReadBinaryElement(stream, element, littleEndian));
            }

            return result;
        }

        // ── ASCII ──

        private static PlyElementData ReadAsciiElement(StreamReader reader, PlyElement element)
        {
            var rows = new List<object[]>((int)Math.Min(element.Count, int.MaxValue));
            for (long i = 0; i < element.Count; i++)
            {
                var line = reader.ReadLine();
                if (line == null)
                    throw new FormatException($"Unexpected end of file while reading element '{element.Name}' at row {i}.");
                var tokens = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                var row = ParseAsciiRow(tokens, element);
                rows.Add(row);
            }
            return new PlyElementData(element, rows);
        }

        private static object[] ParseAsciiRow(string[] tokens, PlyElement element)
        {
            var row = new object[element.Properties.Count];
            int ti = 0;
            for (int pi = 0; pi < element.Properties.Count; pi++)
            {
                var prop = element.Properties[pi];
                if (prop.IsList)
                {
                    int count = Convert.ToInt32(ParseAsciiScalar(tokens[ti++], prop.CountType!.Value));
                    var list = new object[count];
                    for (int k = 0; k < count; k++)
                        list[k] = ParseAsciiScalar(tokens[ti++], prop.Type);
                    row[pi] = list;
                }
                else
                {
                    row[pi] = ParseAsciiScalar(tokens[ti++], prop.Type);
                }
            }
            return row;
        }

        private static object ParseAsciiScalar(string token, PlyPropertyType type)
        {
            switch (type)
            {
                case PlyPropertyType.Char:   return sbyte.Parse(token);
                case PlyPropertyType.UChar:  return byte.Parse(token);
                case PlyPropertyType.Short:  return short.Parse(token);
                case PlyPropertyType.UShort: return ushort.Parse(token);
                case PlyPropertyType.Int:    return int.Parse(token);
                case PlyPropertyType.UInt:   return uint.Parse(token);
                case PlyPropertyType.Float:  return float.Parse(token, System.Globalization.CultureInfo.InvariantCulture);
                case PlyPropertyType.Double: return double.Parse(token, System.Globalization.CultureInfo.InvariantCulture);
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        // ── Binary ──

        private static PlyElementData ReadBinaryElement(Stream stream, PlyElement element, bool littleEndian)
        {
            // Compute fixed row byte size (or -1 if variable due to list properties)
            int fixedRowSize = ComputeFixedRowSize(element);

            var rows = new List<object[]>((int)Math.Min(element.Count, int.MaxValue));

            if (fixedRowSize >= 0)
            {
                // Fast path: read a large block and parse in memory
                rows = ReadBinaryFixedElement(stream, element, littleEndian, fixedRowSize);
            }
            else
            {
                // Slow path: row-by-row for elements with list properties
                for (long i = 0; i < element.Count; i++)
                    rows.Add(ReadBinaryRow(stream, element, littleEndian));
            }

            return new PlyElementData(element, rows);
        }

        private static int ComputeFixedRowSize(PlyElement element)
        {
            int size = 0;
            foreach (var prop in element.Properties)
            {
                if (prop.IsList) return -1;
                size += prop.Type.SizeOf();
            }
            return size;
        }

        private static List<object[]> ReadBinaryFixedElement(Stream stream, PlyElement element, bool littleEndian, int rowSize)
        {
            long count = element.Count;
            var rows = new List<object[]>((int)Math.Min(count, int.MaxValue));

            // Read all bytes at once for performance
            long totalBytes = count * rowSize;
            byte[] buffer = new byte[totalBytes];
            int read = 0;
            while (read < buffer.Length)
            {
                int n = stream.Read(buffer, read, buffer.Length - read);
                if (n == 0)
                    throw new FormatException($"Unexpected end of stream reading element '{element.Name}'.");
                read += n;
            }

            int offset = 0;
            for (long i = 0; i < count; i++)
            {
                var row = new object[element.Properties.Count];
                for (int pi = 0; pi < element.Properties.Count; pi++)
                {
                    var prop = element.Properties[pi];
                    row[pi] = ReadScalarFromBuffer(buffer, offset, prop.Type, littleEndian);
                    offset += prop.Type.SizeOf();
                }
                rows.Add(row);
            }
            return rows;
        }

        private static object[] ReadBinaryRow(Stream stream, PlyElement element, bool littleEndian)
        {
            var row = new object[element.Properties.Count];
            for (int pi = 0; pi < element.Properties.Count; pi++)
            {
                var prop = element.Properties[pi];
                if (prop.IsList)
                {
                    var countVal = ReadScalarFromStream(stream, prop.CountType!.Value, littleEndian);
                    int count = Convert.ToInt32(countVal);
                    var list = new object[count];
                    for (int k = 0; k < count; k++)
                        list[k] = ReadScalarFromStream(stream, prop.Type, littleEndian);
                    row[pi] = list;
                }
                else
                {
                    row[pi] = ReadScalarFromStream(stream, prop.Type, littleEndian);
                }
            }
            return row;
        }

        private static object ReadScalarFromStream(Stream stream, PlyPropertyType type, bool littleEndian)
        {
            int size = type.SizeOf();
            var buf = new byte[size];
            int read = 0;
            while (read < size)
            {
                int n = stream.Read(buf, read, size - read);
                if (n == 0)
                    throw new FormatException("Unexpected end of stream.");
                read += n;
            }
            return ReadScalarFromBuffer(buf, 0, type, littleEndian);
        }

        private static object ReadScalarFromBuffer(byte[] buf, int offset, PlyPropertyType type, bool littleEndian)
        {
            switch (type)
            {
                case PlyPropertyType.Char:
                    return (sbyte)buf[offset];
                case PlyPropertyType.UChar:
                    return buf[offset];
                case PlyPropertyType.Short:
                {
                    short v = BitConverter.ToInt16(buf, offset);
                    return NeedsSwap(littleEndian) ? SwapBytes(v) : v;
                }
                case PlyPropertyType.UShort:
                {
                    ushort v = BitConverter.ToUInt16(buf, offset);
                    return NeedsSwap(littleEndian) ? SwapBytes(v) : v;
                }
                case PlyPropertyType.Int:
                {
                    int v = BitConverter.ToInt32(buf, offset);
                    return NeedsSwap(littleEndian) ? SwapBytes(v) : v;
                }
                case PlyPropertyType.UInt:
                {
                    uint v = BitConverter.ToUInt32(buf, offset);
                    return NeedsSwap(littleEndian) ? SwapBytes(v) : v;
                }
                case PlyPropertyType.Float:
                {
                    float v = BitConverter.ToSingle(buf, offset);
                    return NeedsSwap(littleEndian) ? SwapBytes(v) : v;
                }
                case PlyPropertyType.Double:
                {
                    double v = BitConverter.ToDouble(buf, offset);
                    return NeedsSwap(littleEndian) ? SwapBytes(v) : v;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        private static bool NeedsSwap(bool fileLittleEndian) =>
            fileLittleEndian != BitConverter.IsLittleEndian;

        private static short  SwapBytes(short v)  => (short)((v >> 8) | (v << 8));
        private static ushort SwapBytes(ushort v) => (ushort)((v >> 8) | (v << 8));
        private static int    SwapBytes(int v)    => (int)SwapBytes((uint)v);
        private static uint   SwapBytes(uint v)   => (v >> 24) | ((v >> 8) & 0x0000FF00u) | ((v << 8) & 0x00FF0000u) | (v << 24);
        private static long   SwapBytes(long v)   => (long)SwapBytes((ulong)v);
        private static ulong  SwapBytes(ulong v)  => (v >> 56) | ((v >> 40) & 0x000000000000FF00ul)
                                                    | ((v >> 24) & 0x0000000000FF0000ul) | ((v >> 8) & 0x00000000FF000000ul)
                                                    | ((v << 8) & 0x000000FF00000000ul) | ((v << 24) & 0x0000FF0000000000ul)
                                                    | ((v << 40) & 0x00FF000000000000ul) | (v << 56);
        private static float  SwapBytes(float v)
        {
            var bytes = BitConverter.GetBytes(v);
            Array.Reverse(bytes);
            return BitConverter.ToSingle(bytes, 0);
        }
        private static double SwapBytes(double v)
        {
            var bytes = BitConverter.GetBytes(v);
            Array.Reverse(bytes);
            return BitConverter.ToDouble(bytes, 0);
        }
    }
}
