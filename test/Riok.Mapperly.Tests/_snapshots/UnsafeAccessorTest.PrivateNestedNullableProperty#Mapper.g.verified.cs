﻿//HintName: Mapper.g.cs
// <auto-generated />
#nullable enable
public partial class Mapper
{
    [global::System.CodeDom.Compiler.GeneratedCode("Riok.Mapperly", "0.0.1.0")]
    partial global::B Map(global::A source)
    {
        var target = new global::B();
        if (source.GetNested()?.GetValue() != null)
        {
            target.SetValue(source.GetNested().GetValue().Value);
        }
        return target;
    }
}
    
[global::System.CodeDom.Compiler.GeneratedCode("Riok.Mapperly", "0.0.1.0")]
static file class AAccessor
{
    [global::System.CodeDom.Compiler.GeneratedCode("Riok.Mapperly", "0.0.1.0")]
    [global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Method, Name = "get_nested")]
    public static extern global::C? GetNested(this global::A source);
}
    
[global::System.CodeDom.Compiler.GeneratedCode("Riok.Mapperly", "0.0.1.0")]
static file class CAccessor
{
    [global::System.CodeDom.Compiler.GeneratedCode("Riok.Mapperly", "0.0.1.0")]
    [global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Method, Name = "get_value")]
    public static extern int? GetValue(this global::C source);
}
    
[global::System.CodeDom.Compiler.GeneratedCode("Riok.Mapperly", "0.0.1.0")]
static file class BAccessor
{
    [global::System.CodeDom.Compiler.GeneratedCode("Riok.Mapperly", "0.0.1.0")]
    [global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Method, Name = "set_value")]
    public static extern void SetValue(this global::B target, int value);
}