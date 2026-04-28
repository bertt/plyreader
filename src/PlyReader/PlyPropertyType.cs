namespace PlyReader
{
    public enum PlyPropertyType
    {
        Char,
        UChar,
        Short,
        UShort,
        Int,
        UInt,
        Float,
        Double,

        // Aliases used in some files
        Int8   = Char,
        UInt8  = UChar,
        Int16  = Short,
        UInt16 = UShort,
        Int32  = Int,
        UInt32 = UInt,
        Float32 = Float,
        Float64 = Double
    }
}
