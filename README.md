# bertt.plyreader

A .NET Standard 2.0 library for reading PLY (Polygon File Format / Stanford Triangle Format) files.

[![NuGet](https://img.shields.io/nuget/v/bertt.plyreader)](https://www.nuget.org/packages/bertt.plyreader)
[![Build](https://github.com/bertt/plyreader/actions/workflows/build.yml/badge.svg)](https://github.com/bertt/plyreader/actions/workflows/build.yml)

## Features

- Parses the full PLY header: format, version, comments, obj_info, elements and properties
- Supports **ASCII**, **binary little-endian** and **binary big-endian** formats
- Supports all standard scalar property types: `char`, `uchar`, `short`, `ushort`, `int`, `uint`, `float`, `double` (and their `int8`/`uint8`/… aliases)
- Supports **list properties** (e.g. `property list uchar int vertex_indices`)
- High-performance fast-path for fixed-size binary elements (reads entire element in one block)
- Targets **.NET Standard 2.0** — compatible with .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5/6/7/8/9/10

## Installation

```
dotnet add package bertt.plyreader
```

## Quick start

```csharp
using PlyReader;

// Read from file path
PlyFile file = PlyReader.Read("model.ply");

// Or from a stream
using var stream = File.OpenRead("model.ply");
PlyFile file = PlyReader.Read(stream);
```

## API

### `PlyReader.Read(string filePath)` / `PlyReader.Read(Stream stream)`

Returns a `PlyFile` containing the parsed header and all element data.

### `PlyFile`

| Member | Type | Description |
|--------|------|-------------|
| `Header` | `PlyHeader` | Parsed header metadata |
| `Elements` | `IReadOnlyList<PlyElementData>` | Parsed data for each element |
| `GetElement(string name)` | `PlyElementData?` | Find element data by name, or `null` |

### `PlyHeader`

| Member | Type | Description |
|--------|------|-------------|
| `Format` | `PlyFormat` | `Ascii`, `BinaryLittleEndian`, or `BinaryBigEndian` |
| `Version` | `string` | Format version (typically `"1.0"`) |
| `Comments` | `IReadOnlyList<string>` | Lines from `comment` entries |
| `ObjInfo` | `IReadOnlyList<string>` | Lines from `obj_info` entries |
| `Elements` | `IReadOnlyList<PlyElement>` | Element descriptors |

### `PlyElement`

| Member | Type | Description |
|--------|------|-------------|
| `Name` | `string` | Element name (e.g. `"vertex"`, `"face"`) |
| `Count` | `long` | Number of instances |
| `Properties` | `IReadOnlyList<PlyProperty>` | Property declarations |

### `PlyProperty`

| Member | Type | Description |
|--------|------|-------------|
| `Name` | `string` | Property name |
| `Type` | `PlyPropertyType` | Value type |
| `IsList` | `bool` | `true` for list properties |
| `CountType` | `PlyPropertyType?` | Count type for list properties |

### `PlyElementData`

| Member | Description |
|--------|-------------|
| `Element` | The element descriptor |
| `Rows` | `IReadOnlyList<object[]>` — one array per instance, values match property order |
| `GetValue(int rowIndex, string propertyName)` | Convenience accessor by name |

Row values are boxed primitives matching their property type:
`sbyte`, `byte`, `short`, `ushort`, `int`, `uint`, `float`, `double`.
List properties are stored as `object[]`.

## Examples

### Read vertex positions

```csharp
PlyFile file = PlyReader.Read("pointcloud.ply");
PlyElementData? vertices = file.GetElement("vertex");
if (vertices != null)
{
    for (int i = 0; i < vertices.Rows.Count; i++)
    {
        float x = (float)vertices.GetValue(i, "x");
        float y = (float)vertices.GetValue(i, "y");
        float z = (float)vertices.GetValue(i, "z");
        Console.WriteLine($"{x}, {y}, {z}");
    }
}
```

### Read face indices

```csharp
PlyElementData? faces = file.GetElement("face");
if (faces != null)
{
    foreach (var row in faces.Rows)
    {
        var indices = (object[])row[0]; // first property: vertex_indices list
        Console.WriteLine(string.Join(", ", indices));
    }
}
```

### Inspect header

```csharp
Console.WriteLine($"Format: {file.Header.Format}");
Console.WriteLine($"Version: {file.Header.Version}");
foreach (var element in file.Header.Elements)
{
    Console.WriteLine($"element {element.Name} {element.Count}");
    foreach (var prop in element.Properties)
        Console.WriteLine($"  {prop}");
}
```

## Supported PLY property types

| PLY token | Alias | .NET type |
|-----------|-------|-----------|
| `char` | `int8` | `sbyte` |
| `uchar` | `uint8` | `byte` |
| `short` | `int16` | `short` |
| `ushort` | `uint16` | `ushort` |
| `int` | `int32` | `int` |
| `uint` | `uint32` | `uint` |
| `float` | `float32` | `float` |
| `double` | `float64` | `double` |

## Building from source

```
git clone https://github.com/bertt/plyreader
cd plyreader
dotnet build
dotnet test
```

## Publishing a NuGet package

```
dotnet pack src/PlyReader/PlyReader.csproj -c Release -o nupkg
dotnet nuget push nupkg/bertt.plyreader.1.0.0.nupkg --api-key <key> --source https://api.nuget.org/v3/index.json
```

## License

MIT
