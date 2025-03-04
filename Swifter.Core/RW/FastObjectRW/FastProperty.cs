﻿
using Swifter.Tools;

using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Swifter.RW
{
    partial class StaticFastObjectRW<T>
    {
        internal sealed class FastProperty : BaseField
        {
            public PropertyInfo Property => (PropertyInfo)Member;

            readonly FieldInfo AutoFieldInfo;

            public FastProperty(PropertyInfo property, RWFieldAttribute attribute)
                : base(property, attribute)
            {
                if (TypeHelper.IsAutoProperty(property, out var fieldInfo) && fieldInfo != null)
                {
                    AutoFieldInfo = fieldInfo;
                }
            }

            public override string Name
            {
                get
                {
                    if (Attribute != null && Attribute.Name != null)
                    {
                        return Attribute.Name;
                    }

                    return Property.Name;
                }
            }

            public override bool CanRead
            {
                get
                {
                    // 没有读取方式。
                    if (GetMethod is null && AutoFieldInfo is null)
                    {
                        return false;
                    }

                    // 特性指定。
                    if (Attribute != null)
                    {
                        return (Attribute.Access & RWFieldAccess.ReadOnly) != 0;
                    }

                    // ref struct
                    if (BeforeType.IsByRefLike())
                    {
                        return false;
                    }

                    // 公开的 Get 方法。
                    if (GetMethod?.IsPublic == true)
                    {
                        return true;
                    }

                    // 自动属性 Set 方法可访问。
                    if ((Options & FastObjectRWOptions.AutoPropertyDirectRW) != 0)
                    {
                        return SetMethod.IsPublic == true;
                    }

                    return false;
                }
            }

            public override bool CanWrite
            {
                get
                {
                    // ByRef 属性判断 Get 方法是否可读。
                    if (IsByRef)
                    {
                        return CanRead;
                    }

                    // 没有设置方式。
                    if (SetMethod is null && AutoFieldInfo is null)
                    {
                        return false;
                    }

                    // 特性指定。
                    if (Attribute != null)
                    {
                        return (Attribute.Access & RWFieldAccess.WriteOnly) != 0;
                    }

                    // ref struct
                    if (BeforeType.IsByRefLike())
                    {
                        return false;
                    }

                    // 公开的 Set 方法。
                    if (SetMethod?.IsPublic == true)
                    {
                        return true;
                    }

                    // 自动属性 Get 方法可访问。
                    if ((Options & FastObjectRWOptions.AutoPropertyDirectRW) != 0)
                    {
                        return GetMethod?.IsPublic == true;
                    }

                    return false;
                }
            }
            
            public MethodInfo GetMethod => Property.GetGetMethod(true);

            public MethodInfo SetMethod => Property.GetSetMethod(true);

            public override bool IsPublicGet => true;

            public override bool IsPublicSet => true;

            public override Type BeforeType => IsByRef ? Property.PropertyType.GetElementType() : Property.PropertyType.IsPointer ? typeof(IntPtr) : Property.PropertyType;

            public override Type AfterType => ReadValueMethod?.ReturnType ?? BeforeType;

            public override bool IsStatic => (GetMethod != null && GetMethod.IsStatic) || (SetMethod != null && SetMethod.IsStatic);

            public override bool SkipDefaultValue => (Attribute?.SkipDefaultValue ?? RWBoolean.None) != RWBoolean.None ? (Attribute.SkipDefaultValue == RWBoolean.Yes) : (Options & FastObjectRWOptions.SkipDefaultValue) != 0;

            public override bool CannotGetException => (Attribute?.CannotGetException ?? RWBoolean.None) != RWBoolean.None ? (Attribute.CannotGetException == RWBoolean.Yes) : (Options & FastObjectRWOptions.CannotGetException) != 0;

            public override bool CannotSetException => (Attribute?.CannotSetException ?? RWBoolean.None) != RWBoolean.None ? (Attribute.CannotSetException == RWBoolean.Yes) : (Options & FastObjectRWOptions.CannotSetException) != 0;


            public bool IsByRef => Property.PropertyType.IsByRef;

            public override void GetValueAfter(ILGenerator ilGen)
            {
                if (IsByRef)
                {
                    ilGen.Call(GetMethod);

                    ilGen.LoadValue(BeforeType);
                }
                else
                {
                    if (GetMethod is null)
                    {
                        if (AutoFieldInfo.IsExternalVisible())
                        {
                            ilGen.LoadField(AutoFieldInfo);
                        }
                        else
                        {
                            ilGen.UnsafeLoadField(AutoFieldInfo);
                        }
                    }
                    else if (GetMethod?.IsExternalVisible() == true || DynamicAssembly.CanAccessNonPublicMembers || IsVisibleTo)
                    {
                        ilGen.Call(GetMethod);
                    }
                    else
                    {

//#if DEBUG
//                        Console.WriteLine($"{nameof(FastProperty)} : \"{typeof(T)}.{GetMethod}\" Use UnsafeCall");
//#endif

                        ilGen.UnsafeCall(GetMethod);
                    }
                }
            }

            public override void SetValueAfter(ILGenerator ilGen)
            {
                if (IsByRef)
                {
                    ilGen.StoreValue(BeforeType);
                }
                else
                {
                    if (SetMethod is null)
                    {
                        if (AutoFieldInfo.IsExternalVisible())
                        {
                            ilGen.StoreField(AutoFieldInfo);
                        }
                        else
                        {
                            ilGen.UnsafeStoreField(AutoFieldInfo);
                        }
                    }
                    else if (SetMethod.IsExternalVisible() || DynamicAssembly.CanAccessNonPublicMembers || IsVisibleTo)
                    {
                        ilGen.Call(SetMethod);
                    }
                    else
                    {

#if DEBUG
                        Console.WriteLine($"{nameof(FastProperty)} : \"{typeof(T)}.{SetMethod}\" Use UnsafeCall");
#endif

                        ilGen.UnsafeCall(SetMethod);
                    }
                }
            }

            public override void GetValueBefore(ILGenerator ilGen)
            {
                if (!IsStatic)
                {
                    LoadContent(ilGen);
                }
            }

            public override void SetValueBefore(ILGenerator ilGen)
            {
                if (!IsStatic)
                {
                    LoadContent(ilGen);
                }

                if (IsByRef)
                {
                    ilGen.Call(GetMethod);
                }
            }

            public override void ReadValueBefore(ILGenerator ilGen)
            {
                if (ReadValueMethod != null)
                {
                    if (ReadValueMethod.IsStatic)
                    {
                        return;
                    }

                    var index = Array.IndexOf(Fields, this);

                    ilGen.LoadConstant(index);

                    ilGen.Call(GetValueInterfaceInstanceMethod);
                }
            }

            public override void ReadValueAfter(ILGenerator ilGen)
            {
                if (ReadValueMethod != null)
                {
                    ilGen.Call(ReadValueMethod);

                    if (BeforeType != AfterType)
                    {
                        if (AfterType.IsValueType)
                        {
                            ilGen.Box(AfterType);
                        }

                        ilGen.CastClass( BeforeType);

                        if (BeforeType.IsValueType)
                        {
                            ilGen.UnboxAny(BeforeType);
                        }
                    }

                    return;
                }

                var methodName = GetReadValueMethodName(BeforeType);

                if (methodName != null)
                {
                    var method = typeof(IValueReader).GetMethod(methodName);

                    if (methodName == nameof(IValueReader.ReadNullable) && method.IsGenericMethodDefinition)
                    {
                        method = method.MakeGenericMethod(Nullable.GetUnderlyingType(BeforeType));
                    }

                    ilGen.Call(method);

                    return;
                }

                var valueInterfaceType = typeof(ValueInterface<>).MakeGenericType(BeforeType);

                var valueInterfaceReadValueMethod = valueInterfaceType.GetMethod(nameof(ValueInterface<object>.ReadValue), StaticDeclaredOnly);

                ilGen.Call(valueInterfaceReadValueMethod);
            }

            public override void WriteValueBefore(ILGenerator ilGen)
            {
                if (WriteValueMethod != null)
                {
                    if (ReadValueMethod.IsStatic)
                    {
                        return;
                    }

                    var index = Array.IndexOf(Fields, this);

                    ilGen.LoadConstant(index);

                    ilGen.Call(GetValueInterfaceInstanceMethod);

                    return;
                }
            }

            public override void WriteValueAfter(ILGenerator ilGen)
            {
                if (WriteValueMethod != null)
                {
                    if (BeforeType != AfterType)
                    {
                        if (BeforeType.IsValueType)
                        {
                            ilGen.Box(BeforeType);
                        }

                        ilGen.CastClass(AfterType);

                        if (AfterType.IsValueType)
                        {
                            ilGen.UnboxAny(AfterType);
                        }
                    }

                    ilGen.Call(WriteValueMethod);

                    return;
                }

                var methodName = GetWriteValueMethodName(BeforeType);

                if (methodName != null)
                {
                    ilGen.Call(typeof(IValueWriter).GetMethod(methodName));

                    return;
                }

                var valueInterfaceType = typeof(ValueInterface<>).MakeGenericType(BeforeType);

                var valueInterfaceWriteValueMethod = valueInterfaceType.GetMethod(nameof(ValueInterface<object>.WriteValue), StaticDeclaredOnly);

                ilGen.Call(valueInterfaceWriteValueMethod);
            }
        }
    }
}