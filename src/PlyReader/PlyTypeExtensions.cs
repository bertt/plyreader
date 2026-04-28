using System;

namespace PlyReader
{
    internal static class PlyTypeExtensions
    {
        internal static PlyPropertyType ParsePropertyType(string token)
        {
            switch (token.ToLowerInvariant())
            {
                case "char":    case "int8":    return PlyPropertyType.Char;
                case "uchar":   case "uint8":   return PlyPropertyType.UChar;
                case "short":   case "int16":   return PlyPropertyType.Short;
                case "ushort":  case "uint16":  return PlyPropertyType.UShort;
                case "int":     case "int32":   return PlyPropertyType.Int;
                case "uint":    case "uint32":  return PlyPropertyType.UInt;
                case "float":   case "float32": return PlyPropertyType.Float;
                case "double":  case "float64": return PlyPropertyType.Double;
                default:
                    throw new FormatException($"Unknown PLY property type: '{token}'");
            }
        }

        internal static string ToTypeString(this PlyPropertyType type)
        {
            switch (type)
            {
                case PlyPropertyType.Char:   return "char";
                case PlyPropertyType.UChar:  return "uchar";
                case PlyPropertyType.Short:  return "short";
                case PlyPropertyType.UShort: return "ushort";
                case PlyPropertyType.Int:    return "int";
                case PlyPropertyType.UInt:   return "uint";
                case PlyPropertyType.Float:  return "float";
                case PlyPropertyType.Double: return "double";
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        internal static int SizeOf(this PlyPropertyType type)
        {
            switch (type)
            {
                case PlyPropertyType.Char:   return 1;
                case PlyPropertyType.UChar:  return 1;
                case PlyPropertyType.Short:  return 2;
                case PlyPropertyType.UShort: return 2;
                case PlyPropertyType.Int:    return 4;
                case PlyPropertyType.UInt:   return 4;
                case PlyPropertyType.Float:  return 4;
                case PlyPropertyType.Double: return 8;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}
