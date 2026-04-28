using System.Collections.Generic;

namespace PlyReader
{
    /// <summary>Parsed PLY file header.</summary>
    public class PlyHeader
    {
        /// <summary>Format of the file (ASCII, binary little-endian, binary big-endian).</summary>
        public PlyFormat Format { get; }

        /// <summary>Format version string (typically "1.0").</summary>
        public string Version { get; }

        /// <summary>Comment lines from the header.</summary>
        public IReadOnlyList<string> Comments { get; }

        /// <summary>obj_info lines from the header.</summary>
        public IReadOnlyList<string> ObjInfo { get; }

        /// <summary>Ordered list of element descriptors.</summary>
        public IReadOnlyList<PlyElement> Elements { get; }

        public PlyHeader(
            PlyFormat format,
            string version,
            IReadOnlyList<string> comments,
            IReadOnlyList<string> objInfo,
            IReadOnlyList<PlyElement> elements)
        {
            Format = format;
            Version = version;
            Comments = comments;
            ObjInfo = objInfo;
            Elements = elements;
        }
    }
}
