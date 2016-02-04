using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;

namespace System.Reflection {
  public class TypeInfo : Type {
    private static ConditionalWeakTable<Type, TypeInfo> typeInfoMap = new ConditionalWeakTable<Type,TypeInfo>();

    internal static TypeInfo FromType(Type type) {
      return typeInfoMap.GetValue(type, _ => {
        return new TypeInfo(type);
      });
    }

    private TypeInfo(Type underlyingType) {
      this.underlyingType = underlyingType;
    }

    private Type underlyingType;

    #region Missing Methods

    public T GetCustomAttribute<T>(bool inherit) where T : Attribute {
      return (T)underlyingType.GetCustomAttributes(typeof(T), inherit).FirstOrDefault();
    }

    public T GetCustomAttribute<T>() where T : Attribute {
      return GetCustomAttribute<T>(true);
    }

    public IEnumerable<Type> ImplementedInterfaces {
      get {
        return underlyingType.GetInterfaces();
      }
    }

    #endregion

    #region Inherited from System.Type

    public override Assembly Assembly {
      get { return underlyingType.Assembly; }
    }

    public override string AssemblyQualifiedName {
      get { return underlyingType.AssemblyQualifiedName; }
    }

    public override Type BaseType {
      get { return underlyingType.BaseType; }
    }

    public override string FullName {
      get { return underlyingType.FullName; }
    }

    public override Guid GUID {
      get { return underlyingType.GUID; }
    }

    protected override TypeAttributes GetAttributeFlagsImpl() {
      return underlyingType.Attributes;
    }

    protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers) {
      return underlyingType.GetConstructor(bindingAttr, binder, callConvention, types, modifiers);
    }

    public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr) {
      return underlyingType.GetConstructors(bindingAttr);
    }

    public override Type GetElementType() {
      return underlyingType.GetElementType();
    }

    public override EventInfo GetEvent(string name, BindingFlags bindingAttr) {
      return underlyingType.GetEvent(name, bindingAttr);
    }

    public override EventInfo[] GetEvents(BindingFlags bindingAttr) {
      return underlyingType.GetEvents(bindingAttr);
    }

    public override FieldInfo GetField(string name, BindingFlags bindingAttr) {
      return underlyingType.GetField(name, bindingAttr);
    }

    public override FieldInfo[] GetFields(BindingFlags bindingAttr) {
      return underlyingType.GetFields(bindingAttr);
    }

    public override Type GetInterface(string name, bool ignoreCase) {
      return underlyingType.GetInterface(name, ignoreCase);
    }

    public override Type[] GetInterfaces() {
      return underlyingType.GetInterfaces();
    }

    public override MemberInfo[] GetMembers(BindingFlags bindingAttr) {
      return underlyingType.GetMembers(bindingAttr);
   }

    protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers) {
      return underlyingType.GetMethod(name, bindingAttr, binder, callConvention, types, modifiers);
   }

    public override MethodInfo[] GetMethods(BindingFlags bindingAttr) {
      return underlyingType.GetMethods(bindingAttr);
    }

    public override Type GetNestedType(string name, BindingFlags bindingAttr) {
      return underlyingType.GetNestedType(name, bindingAttr);
    }

    public override Type[] GetNestedTypes(BindingFlags bindingAttr) {
      return underlyingType.GetNestedTypes(bindingAttr);
    }

    public override PropertyInfo[] GetProperties(BindingFlags bindingAttr) {
      return underlyingType.GetProperties(bindingAttr);
    }

    protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers) {
      return underlyingType.GetProperty(name, bindingAttr, binder, returnType, types, modifiers);
    }

    protected override bool HasElementTypeImpl() {
      return underlyingType.HasElementType;
    }

    public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, Globalization.CultureInfo culture, string[] namedParameters) {
      return underlyingType.InvokeMember(name, invokeAttr, binder, target, args, modifiers, culture, namedParameters);
   }

    protected override bool IsArrayImpl() {
      return underlyingType.IsArray;
   }

    protected override bool IsByRefImpl() {
      return underlyingType.IsByRef;
   }

    protected override bool IsCOMObjectImpl() {
      return underlyingType.IsCOMObject;
    }

    protected override bool IsPointerImpl() {
      return underlyingType.IsPointer;
    }

    protected override bool IsPrimitiveImpl() {
      return underlyingType.IsPrimitive;
    }

    public override Module Module {
      get { return underlyingType.Module; }
    }

    public override string Namespace {
      get { return underlyingType.Namespace; }
    }

    public override Type UnderlyingSystemType {
      get { return underlyingType.UnderlyingSystemType; }
    }

    public override object[] GetCustomAttributes(Type attributeType, bool inherit) {
      return underlyingType.GetCustomAttributes(attributeType, inherit);
    }

    public override object[] GetCustomAttributes(bool inherit) {
      return underlyingType.GetCustomAttributes(inherit);
    }

    public override bool IsDefined(Type attributeType, bool inherit) {
      return underlyingType.IsDefined(attributeType, inherit);
    }

    public override string Name {
      get { return underlyingType.Name;  }
    }

    #endregion
  }
}