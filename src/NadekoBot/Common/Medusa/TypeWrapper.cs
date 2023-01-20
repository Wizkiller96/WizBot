using System.Globalization;
using System.Reflection;

namespace Nadeko.Medusa;

public sealed class TypeWrapper : Type
{
    private readonly int _hashCode;

    public TypeWrapper(Type t)
    {
        _hashCode = t.GetHashCode();
        Namespace = t.Namespace;
        Name = t.Name;
        FullName = t.FullName;
        IsGenericType = t.IsGenericType;
        IsConstructedGenericType = t.IsConstructedGenericType;
    }

    public override bool IsGenericType { get; }
    public override bool IsConstructedGenericType { get; }

    public override object[] GetCustomAttributes(bool inherit)
        => throw new NotImplementedException();

    public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        => throw new NotImplementedException();

    public override bool IsDefined(Type attributeType, bool inherit)
        => throw new NotImplementedException();

    public override Module Module { get; }
    public override string? Namespace { get; }
    public override string Name { get; }

    protected override TypeAttributes GetAttributeFlagsImpl()
        => throw new NotImplementedException();

    protected override ConstructorInfo? GetConstructorImpl(
        BindingFlags bindingAttr,
        Binder? binder,
        CallingConventions callConvention,
        Type[] types,
        ParameterModifier[]? modifiers)
        => throw new NotImplementedException();

    public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
        => throw new NotImplementedException();

    public override Type? GetElementType()
        => throw new NotImplementedException();

    public override EventInfo? GetEvent(string name, BindingFlags bindingAttr)
        => throw new NotImplementedException();

    public override EventInfo[] GetEvents(BindingFlags bindingAttr)
        => throw new NotImplementedException();

    public override FieldInfo? GetField(string name, BindingFlags bindingAttr)
        => throw new NotImplementedException();

    public override FieldInfo[] GetFields(BindingFlags bindingAttr)
        => throw new NotImplementedException();

    public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        => throw new NotImplementedException();

    protected override MethodInfo? GetMethodImpl(
        string name,
        BindingFlags bindingAttr,
        Binder? binder,
        CallingConventions callConvention,
        Type[]? types,
        ParameterModifier[]? modifiers)
        => throw new NotImplementedException();

    public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
        => throw new NotImplementedException();

    public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
        => throw new NotImplementedException();

    public override object? InvokeMember(
        string name,
        BindingFlags invokeAttr,
        Binder? binder,
        object? target,
        object?[]? args,
        ParameterModifier[]? modifiers,
        CultureInfo? culture,
        string[]? namedParameters)
        => throw new NotImplementedException();

    public override Type UnderlyingSystemType { get; }

    protected override bool IsArrayImpl()
        => throw new NotImplementedException();

    protected override bool IsByRefImpl()
        => throw new NotImplementedException();

    protected override bool IsCOMObjectImpl()
        => throw new NotImplementedException();

    protected override bool IsPointerImpl()
        => throw new NotImplementedException();

    protected override bool IsPrimitiveImpl()
        => throw new NotImplementedException();

    public override Assembly Assembly { get; }
    public override string? AssemblyQualifiedName { get; }
    public override Type? BaseType { get; }
    public override string? FullName { get; }
    public override Guid GUID { get; }

    protected override PropertyInfo? GetPropertyImpl(
        string name,
        BindingFlags bindingAttr,
        Binder? binder,
        Type? returnType,
        Type[]? types,
        ParameterModifier[]? modifiers)
        => throw new NotImplementedException();

    protected override bool HasElementTypeImpl()
        => throw new NotImplementedException();

    public override Type? GetNestedType(string name, BindingFlags bindingAttr)
        => throw new NotImplementedException();

    public override Type[] GetNestedTypes(BindingFlags bindingAttr)
        => throw new NotImplementedException();

    public override int GetHashCode()
        => _hashCode;

    public override Type? GetInterface(string name, bool ignoreCase)
        => throw new NotImplementedException();

    public override Type[] GetInterfaces()
        => throw new NotImplementedException();
}