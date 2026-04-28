using System;
using System.IO;
using System.Text;

namespace PlyReader.Tests
{
    [TestFixture]
    public class AsciiTests
    {
        private const string SimpleAsciiPly = @"ply
format ascii 1.0
comment A simple test PLY
element vertex 3
property float x
property float y
property float z
element face 1
property list uchar int vertex_indices
end_header
0.0 0.0 0.0
1.0 0.0 0.0
0.0 1.0 0.0
3 0 1 2
";

        private static PlyFile ReadFromString(string content)
        {
            var bytes = Encoding.ASCII.GetBytes(content);
            using var stream = new MemoryStream(bytes);
            return PlyReader.Read(stream);
        }

        [Test]
        public void Read_AsciiFormat_ParsesFormat()
        {
            var file = ReadFromString(SimpleAsciiPly);
            Assert.That(file.Header.Format, Is.EqualTo(PlyFormat.Ascii));
            Assert.That(file.Header.Version, Is.EqualTo("1.0"));
        }

        [Test]
        public void Read_AsciiFormat_ParsesComment()
        {
            var file = ReadFromString(SimpleAsciiPly);
            Assert.That(file.Header.Comments, Has.Count.EqualTo(1));
            Assert.That(file.Header.Comments[0], Is.EqualTo("A simple test PLY"));
        }

        [Test]
        public void Read_AsciiFormat_ParsesElementNames()
        {
            var file = ReadFromString(SimpleAsciiPly);
            Assert.That(file.Header.Elements, Has.Count.EqualTo(2));
            Assert.That(file.Header.Elements[0].Name, Is.EqualTo("vertex"));
            Assert.That(file.Header.Elements[1].Name, Is.EqualTo("face"));
        }

        [Test]
        public void Read_AsciiFormat_ParsesVertexCount()
        {
            var file = ReadFromString(SimpleAsciiPly);
            var vertices = file.GetElement("vertex")!;
            Assert.That(vertices.Element.Count, Is.EqualTo(3));
            Assert.That(vertices.Rows, Has.Count.EqualTo(3));
        }

        [Test]
        public void Read_AsciiFormat_ParsesVertexCoordinates()
        {
            var file = ReadFromString(SimpleAsciiPly);
            var vertices = file.GetElement("vertex")!;

            Assert.That((float)vertices.GetValue(0, "x"), Is.EqualTo(0.0f));
            Assert.That((float)vertices.GetValue(0, "y"), Is.EqualTo(0.0f));
            Assert.That((float)vertices.GetValue(0, "z"), Is.EqualTo(0.0f));

            Assert.That((float)vertices.GetValue(1, "x"), Is.EqualTo(1.0f));
            Assert.That((float)vertices.GetValue(1, "y"), Is.EqualTo(0.0f));
            Assert.That((float)vertices.GetValue(1, "z"), Is.EqualTo(0.0f));

            Assert.That((float)vertices.GetValue(2, "x"), Is.EqualTo(0.0f));
            Assert.That((float)vertices.GetValue(2, "y"), Is.EqualTo(1.0f));
            Assert.That((float)vertices.GetValue(2, "z"), Is.EqualTo(0.0f));
        }

        [Test]
        public void Read_AsciiFormat_ParsesListProperty()
        {
            var file = ReadFromString(SimpleAsciiPly);
            var faces = file.GetElement("face")!;
            Assert.That(faces.Element.Count, Is.EqualTo(1));
            var indices = (object[])faces.GetValue(0, "vertex_indices");
            Assert.That(indices, Has.Length.EqualTo(3));
            Assert.That((int)indices[0], Is.EqualTo(0));
            Assert.That((int)indices[1], Is.EqualTo(1));
            Assert.That((int)indices[2], Is.EqualTo(2));
        }

        [Test]
        public void Read_AsciiFormat_PropertyTypes_AreCorrect()
        {
            var file = ReadFromString(SimpleAsciiPly);
            var vertElem = file.Header.Elements[0];
            Assert.That(vertElem.Properties[0].Type, Is.EqualTo(PlyPropertyType.Float));
            Assert.That(vertElem.Properties[0].Name, Is.EqualTo("x"));
        }

        [Test]
        public void Read_ListProperty_IsList()
        {
            var file = ReadFromString(SimpleAsciiPly);
            var faceProp = file.Header.Elements[1].Properties[0];
            Assert.That(faceProp.IsList, Is.True);
            Assert.That(faceProp.CountType, Is.EqualTo(PlyPropertyType.UChar));
            Assert.That(faceProp.Type, Is.EqualTo(PlyPropertyType.Int));
        }

        [Test]
        public void Read_NoComments_EmptyList()
        {
            var ply = @"ply
format ascii 1.0
element vertex 1
property float x
end_header
1.5
";
            var file = ReadFromString(ply);
            Assert.That(file.Header.Comments, Is.Empty);
            Assert.That(file.Header.ObjInfo, Is.Empty);
        }

        [Test]
        public void Read_ObjInfo_IsParsed()
        {
            var ply = @"ply
format ascii 1.0
obj_info test info line
element vertex 1
property float x
end_header
0.0
";
            var file = ReadFromString(ply);
            Assert.That(file.Header.ObjInfo, Has.Count.EqualTo(1));
            Assert.That(file.Header.ObjInfo[0], Is.EqualTo("test info line"));
        }

        [Test]
        public void Read_AllScalarTypes_Parsed()
        {
            var ply = @"ply
format ascii 1.0
element data 1
property char a
property uchar b
property short c
property ushort d
property int e
property uint f
property float g
property double h
end_header
-1 255 -32768 65535 -2147483648 4294967295 3.14 2.718281828
";
            var file = ReadFromString(ply);
            var row = file.GetElement("data")!;
            Assert.That((sbyte)row.GetValue(0, "a"), Is.EqualTo((sbyte)(-1)));
            Assert.That((byte)row.GetValue(0, "b"),  Is.EqualTo((byte)255));
            Assert.That((short)row.GetValue(0, "c"), Is.EqualTo((short)(-32768)));
            Assert.That((ushort)row.GetValue(0, "d"), Is.EqualTo((ushort)65535));
            Assert.That((int)row.GetValue(0, "e"),   Is.EqualTo(int.MinValue));
            Assert.That((uint)row.GetValue(0, "f"),  Is.EqualTo(uint.MaxValue));
            Assert.That((float)row.GetValue(0, "g"), Is.EqualTo(3.14f).Within(1e-5f));
            Assert.That((double)row.GetValue(0, "h"), Is.EqualTo(2.718281828).Within(1e-8));
        }

        [Test]
        public void GetElement_UnknownName_ReturnsNull()
        {
            var file = ReadFromString(SimpleAsciiPly);
            Assert.That(file.GetElement("nonexistent"), Is.Null);
        }

        [Test]
        public void GetValue_UnknownProperty_Throws()
        {
            var file = ReadFromString(SimpleAsciiPly);
            var vertices = file.GetElement("vertex")!;
            Assert.Throws<ArgumentException>(() => vertices.GetValue(0, "nonexistent"));
        }
    }
}