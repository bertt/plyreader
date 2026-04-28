using System;
using System.IO;
using System.Text;

namespace PlyReader.Tests
{
    /// <summary>
    /// Tests for binary little-endian and big-endian PLY reading using in-memory streams.
    /// </summary>
    [TestFixture]
    public class BinaryTests
    {
        /// <summary>
        /// Builds a binary_little_endian PLY with 2 vertices (x, y, z as float)
        /// and 1 face (vertex_indices as list uchar int).
        /// </summary>
        private static Stream BuildBinaryLittleEndianPly()
        {
            const string header =
                "ply\n" +
                "format binary_little_endian 1.0\n" +
                "comment binary test\n" +
                "element vertex 2\n" +
                "property float x\n" +
                "property float y\n" +
                "property float z\n" +
                "element face 1\n" +
                "property list uchar int vertex_indices\n" +
                "end_header\n";

            var ms = new MemoryStream();

            // Header
            ms.Write(Encoding.ASCII.GetBytes(header), 0, Encoding.ASCII.GetByteCount(header));

            // Vertex 0: (1.0, 2.0, 3.0)
            ms.Write(BitConverter.GetBytes(1.0f), 0, 4);
            ms.Write(BitConverter.GetBytes(2.0f), 0, 4);
            ms.Write(BitConverter.GetBytes(3.0f), 0, 4);

            // Vertex 1: (4.0, 5.0, 6.0)
            ms.Write(BitConverter.GetBytes(4.0f), 0, 4);
            ms.Write(BitConverter.GetBytes(5.0f), 0, 4);
            ms.Write(BitConverter.GetBytes(6.0f), 0, 4);

            // Face 0: list [0, 1] (count=2 as uchar, then two ints)
            ms.WriteByte(2); // count
            ms.Write(BitConverter.GetBytes(0), 0, 4);
            ms.Write(BitConverter.GetBytes(1), 0, 4);

            ms.Position = 0;
            return ms;
        }

        /// <summary>
        /// Builds a binary_big_endian PLY with 1 vertex (x, y as double).
        /// </summary>
        private static Stream BuildBinaryBigEndianPly()
        {
            const string header =
                "ply\n" +
                "format binary_big_endian 1.0\n" +
                "element vertex 1\n" +
                "property double x\n" +
                "property double y\n" +
                "end_header\n";

            var ms = new MemoryStream();
            ms.Write(Encoding.ASCII.GetBytes(header), 0, Encoding.ASCII.GetByteCount(header));

            // Write 1.5 as big-endian double
            var xBytes = BitConverter.GetBytes(1.5);
            var yBytes = BitConverter.GetBytes(2.5);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(xBytes);
                Array.Reverse(yBytes);
            }
            ms.Write(xBytes, 0, 8);
            ms.Write(yBytes, 0, 8);

            ms.Position = 0;
            return ms;
        }

        [Test]
        public void Read_BinaryLE_Format_IsCorrect()
        {
            using var stream = BuildBinaryLittleEndianPly();
            var file = PlyReader.Read(stream);
            Assert.That(file.Header.Format, Is.EqualTo(PlyFormat.BinaryLittleEndian));
            Assert.That(file.Header.Version, Is.EqualTo("1.0"));
        }

        [Test]
        public void Read_BinaryLE_Comment_IsParsed()
        {
            using var stream = BuildBinaryLittleEndianPly();
            var file = PlyReader.Read(stream);
            Assert.That(file.Header.Comments, Has.Count.EqualTo(1));
            Assert.That(file.Header.Comments[0], Is.EqualTo("binary test"));
        }

        [Test]
        public void Read_BinaryLE_ElementNames_AreCorrect()
        {
            using var stream = BuildBinaryLittleEndianPly();
            var file = PlyReader.Read(stream);
            Assert.That(file.Header.Elements, Has.Count.EqualTo(2));
            Assert.That(file.Header.Elements[0].Name, Is.EqualTo("vertex"));
            Assert.That(file.Header.Elements[1].Name, Is.EqualTo("face"));
        }

        [Test]
        public void Read_BinaryLE_VertexCount_IsCorrect()
        {
            using var stream = BuildBinaryLittleEndianPly();
            var file = PlyReader.Read(stream);
            var vertices = file.GetElement("vertex")!;
            Assert.That(vertices.Element.Count, Is.EqualTo(2));
            Assert.That(vertices.Rows, Has.Count.EqualTo(2));
        }

        [Test]
        public void Read_BinaryLE_VertexValues_AreCorrect()
        {
            using var stream = BuildBinaryLittleEndianPly();
            var file = PlyReader.Read(stream);
            var vertices = file.GetElement("vertex")!;

            Assert.That((float)vertices.GetValue(0, "x"), Is.EqualTo(1.0f));
            Assert.That((float)vertices.GetValue(0, "y"), Is.EqualTo(2.0f));
            Assert.That((float)vertices.GetValue(0, "z"), Is.EqualTo(3.0f));

            Assert.That((float)vertices.GetValue(1, "x"), Is.EqualTo(4.0f));
            Assert.That((float)vertices.GetValue(1, "y"), Is.EqualTo(5.0f));
            Assert.That((float)vertices.GetValue(1, "z"), Is.EqualTo(6.0f));
        }

        [Test]
        public void Read_BinaryLE_ListProperty_IsCorrect()
        {
            using var stream = BuildBinaryLittleEndianPly();
            var file = PlyReader.Read(stream);
            var faces = file.GetElement("face")!;
            Assert.That(faces.Element.Count, Is.EqualTo(1));
            var indices = (object[])faces.GetValue(0, "vertex_indices");
            Assert.That(indices, Has.Length.EqualTo(2));
            Assert.That((int)indices[0], Is.EqualTo(0));
            Assert.That((int)indices[1], Is.EqualTo(1));
        }

        [Test]
        public void Read_BinaryBE_Format_IsCorrect()
        {
            using var stream = BuildBinaryBigEndianPly();
            var file = PlyReader.Read(stream);
            Assert.That(file.Header.Format, Is.EqualTo(PlyFormat.BinaryBigEndian));
        }

        [Test]
        public void Read_BinaryBE_DoubleValues_AreCorrect()
        {
            using var stream = BuildBinaryBigEndianPly();
            var file = PlyReader.Read(stream);
            var vertices = file.GetElement("vertex")!;
            Assert.That((double)vertices.GetValue(0, "x"), Is.EqualTo(1.5).Within(1e-10));
            Assert.That((double)vertices.GetValue(0, "y"), Is.EqualTo(2.5).Within(1e-10));
        }

        [Test]
        public void Read_BinaryLE_AllIntegerTypes_AreCorrect()
        {
            const string header =
                "ply\n" +
                "format binary_little_endian 1.0\n" +
                "element data 1\n" +
                "property char a\n" +
                "property uchar b\n" +
                "property short c\n" +
                "property ushort d\n" +
                "property int e\n" +
                "property uint f\n" +
                "end_header\n";

            var ms = new MemoryStream();
            ms.Write(Encoding.ASCII.GetBytes(header), 0, Encoding.ASCII.GetByteCount(header));
            ms.WriteByte(unchecked((byte)(sbyte)-10));       // char  -10
            ms.WriteByte(200);                               // uchar  200
            ms.Write(BitConverter.GetBytes((short)-1000), 0, 2);  // short -1000
            ms.Write(BitConverter.GetBytes((ushort)60000), 0, 2); // ushort 60000
            ms.Write(BitConverter.GetBytes(-123456), 0, 4);       // int
            ms.Write(BitConverter.GetBytes(4000000000u), 0, 4);   // uint
            ms.Position = 0;

            var file = PlyReader.Read(ms);
            var row = file.GetElement("data")!;
            Assert.That((sbyte)row.GetValue(0, "a"),  Is.EqualTo((sbyte)(-10)));
            Assert.That((byte)row.GetValue(0, "b"),   Is.EqualTo((byte)200));
            Assert.That((short)row.GetValue(0, "c"),  Is.EqualTo((short)(-1000)));
            Assert.That((ushort)row.GetValue(0, "d"), Is.EqualTo((ushort)60000));
            Assert.That((int)row.GetValue(0, "e"),    Is.EqualTo(-123456));
            Assert.That((uint)row.GetValue(0, "f"),   Is.EqualTo(4000000000u));
        }
    }
}