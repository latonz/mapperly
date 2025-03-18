using System.Runtime.InteropServices;

namespace Riok.Mapperly.Common.Syntax;

[StructLayout(LayoutKind.Auto)]
public record struct SupportedFeatures(bool NameOfParameter, bool NullableAttributes);
