using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Vexe.Runtime.Extensions
{
    public delegate void MemberSetter<TTarget, TValue>(ref TTarget target, TValue value);
    public delegate TReturn MemberGetter<TTarget, TReturn>(TTarget target);
    public delegate TReturn MethodCaller<TTarget, TReturn>(TTarget target, object[] args);
    public delegate T CtorInvoker<T>(object[] parameters);

    /// <summary>
    /// A dynamic reflection extensions library that emits IL to set/get fields/properties, call methods and invoke constructors
    /// Once the delegate is created, it can be stored and reused resulting in much faster access times than using regular reflection
    /// The results are cached. Once a delegate is generated, any subsequent call to generate the same delegate on the same field/property/method will return the previously generated delegate
    /// Note: Since this generates IL, it won't work on AOT platforms such as iOS an Android. But is useful and works very well in editor codes and standalone targets
    /// </summary>
    public static class FastReflection
    {
        static ILEmitter emit = new ILEmitter();
        static Dictionary<int, Delegate> cache = new Dictionary<int, Delegate>();

        const string kCtorInvokerName = "CI<>";
        const string kMethodCallerName = "MC<>";
        const string kFieldSetterName = "FS<>";
        const string kFieldGetterName = "FG<>";
        const string kPropertySetterName = "PS<>";
        const string kPropertyGetterName = "PG<>";

        /// <summary>
        /// Generates or gets a strongly-typed open-instance delegate to the specified type constructor that takes the specified type params
        /// </summary>
        public static CtorInvoker<T> DelegateForCtor<T>(this Type type, params Type[] paramTypes)
        {
            int key = kCtorInvokerName.GetHashCode() ^ type.GetHashCode();
            for (int i = 0; i < paramTypes.Length; i++)
                key ^= paramTypes[i].GetHashCode();

            Delegate result;
            if (cache.TryGetValue(key, out result))
                return (CtorInvoker<T>)result;

            var dynMethod = new DynamicMethod(kCtorInvokerName,
                typeof(T), new Type[] { typeof(object[]) });

            emit.il = dynMethod.GetILGenerator();
            GenCtor<T>(type, paramTypes);

            result = dynMethod.CreateDelegate(typeof(CtorInvoker<T>));
            cache[key] = result;
            return (CtorInvoker<T>)result;
        }

        /// <summary>
        /// Generates or gets a weakly-typed open-instance delegate to the specified type constructor that takes the specified type params
        /// </summary>
        public static CtorInvoker<object> DelegateForCtor(this Type type, params Type[] ctorParamTypes)
        {
            return DelegateForCtor<object>(type, ctorParamTypes);
        }

        /// <summary>
        /// Generates or gets a strongly-typed open-instance delegate to get the value of the specified property from a given target
        /// </summary>
        public static MemberGetter<TTarget, TReturn> DelegateForGet<TTarget, TReturn>(this PropertyInfo property)
        {
            if (!property.CanRead)
                throw new InvalidOperationException("Property is not readable " + property.Name);

            int key = GetKey<TTarget, TReturn>(property, kPropertyGetterName);
            Delegate result;
            if (cache.TryGetValue(key, out result))
                return (MemberGetter<TTarget, TReturn>)result;

            return GenDelegateForMember<MemberGetter<TTarget, TReturn>, PropertyInfo>(
                property, key, kPropertyGetterName, GenPropertyGetter<TTarget>,
                typeof(TReturn), typeof(TTarget));
        }

        /// <summary>
        /// Generates or gets a weakly-typed open-instance delegate to get the value of the specified property from a given target
        /// </summary>
        public static MemberGetter<object, object> DelegateForGet(this PropertyInfo property)
        {
            return DelegateForGet<object, object>(property);
        }

        /// <summary>
        /// Generates or gets a strongly-typed open-instance delegate to set the value of the specified property on a given target
        /// </summary>
        public static MemberSetter<TTarget, TValue> DelegateForSet<TTarget, TValue>(this PropertyInfo property)
        {
            if (!property.CanWrite)
                throw new InvalidOperationException("Property is not writable " + property.Name);

            int key = GetKey<TTarget, TValue>(property, kPropertySetterName);
            Delegate result;
            if (cache.TryGetValue(key, out result))
                return (MemberSetter<TTarget, TValue>)result;

            return GenDelegateForMember<MemberSetter<TTarget, TValue>, PropertyInfo>(
                property, key, kPropertySetterName, GenPropertySetter<TTarget>,
                typeof(void), typeof(TTarget).MakeByRefType(), typeof(TValue));
        }

        /// <summary>
        /// Generates or gets a weakly-typed open-instance delegate to set the value of the specified property on a given target
        /// </summary>
        public static MemberSetter<object, object> DelegateForSet(this PropertyInfo property)
        {
            return DelegateForSet<object, object>(property);
        }

        /// <summary>
        /// Generates an open-instance delegate to get the value of the property from a given target
        /// </summary>
        public static MemberGetter<TTarget, TReturn> DelegateForGet<TTarget, TReturn>(this FieldInfo field)
        {
            int key = GetKey<TTarget, TReturn>(field, kFieldGetterName);
            Delegate result;
            if (cache.TryGetValue(key, out result))
                return (MemberGetter<TTarget, TReturn>)result;

            return GenDelegateForMember<MemberGetter<TTarget, TReturn>, FieldInfo>(
                field, key, kFieldGetterName, GenFieldGetter<TTarget>,
                typeof(TReturn), typeof(TTarget));
        }

        /// <summary>
        /// Generates a weakly-typed open-instance delegate to set the value of the field in a given target
        /// </summary>
        public static MemberGetter<object, object> DelegateForGet(this FieldInfo field)
        {
            return DelegateForGet<object, object>(field);
        }

        /// <summary>
        /// Generates a strongly-typed open-instance delegate to set the value of the field in a given target
        /// </summary>
        public static MemberSetter<TTarget, TValue> DelegateForSet<TTarget, TValue>(this FieldInfo field)
        {
            int key = GetKey<TTarget, TValue>(field, kFieldSetterName);
            Delegate result;
            if (cache.TryGetValue(key, out result))
                return (MemberSetter<TTarget, TValue>)result;

            return GenDelegateForMember<MemberSetter<TTarget, TValue>, FieldInfo>(
                field, key, kFieldSetterName, GenFieldSetter<TTarget>,
                typeof(void), typeof(TTarget).MakeByRefType(), typeof(TValue));
        }

        /// <summary>
        /// Generates a weakly-typed open-instance delegate to set the value of the field in a given target
        /// </summary>
        public static MemberSetter<object, object> DelegateForSet(this FieldInfo field)
        {
            return DelegateForSet<object, object>(field);
        }

        /// <summary>
        /// Generates a strongly-typed open-instance delegate to invoke the specified method
        /// </summary>
        public static MethodCaller<TTarget, TReturn> DelegateForCall<TTarget, TReturn>(this MethodInfo method)
        {
            int key = GetKey<TTarget, TReturn>(method, kMethodCallerName);
            Delegate result;
            if (cache.TryGetValue(key, out result))
                return (MethodCaller<TTarget, TReturn>)result;

            return GenDelegateForMember<MethodCaller<TTarget, TReturn>, MethodInfo>(
                method, key, kMethodCallerName, GenMethodInvocation<TTarget>,
                typeof(TReturn), typeof(TTarget), typeof(object[]));
        }

        /// <summary>
        /// Generates a weakly-typed open-instance delegate to invoke the specified method
        /// </summary>
        public static MethodCaller<object, object> DelegateForCall(this MethodInfo method)
        {
            return DelegateForCall<object, object>(method);
        }

        /// <summary>
        /// Executes the delegate on the specified target and arguments but only if it's not null
        /// </summary>
        public static void SafeInvoke<TTarget, TValue>(this MethodCaller<TTarget, TValue> caller, TTarget target, params object[] args)
        {
            if (caller != null)
                caller(target, args);
        }

        /// <summary>
        /// Executes the delegate on the specified target and value but only if it's not null
        /// </summary>
        public static void SafeInvoke<TTarget, TValue>(this MemberSetter<TTarget, TValue> setter, ref TTarget target, TValue value)
        {
            if (setter != null)
                setter(ref target, value);
        }

        /// <summary>
        /// Executes the delegate on the specified target only if it's not null, returns default(TReturn) otherwise
        /// </summary>
        public static TReturn SafeInvoke<TTarget, TReturn>(this MemberGetter<TTarget, TReturn> getter, TTarget target)
        {
            if (getter != null)
                return getter(target);
            return default(TReturn);
        }

        /// <summary>
        /// Generates a assembly called 'name' that's useful for debugging purposes and inspecting the resulting C# code in ILSpy
        /// If 'field' is not null, it generates a setter and getter for that field
        /// If 'property' is not null, it generates a setter and getter for that property
        /// If 'method' is not null, it generates a call for that method
        /// if 'targetType' and 'ctorParamTypes' are not null, it generates a constructor for the target type that takes the specified arguments
        /// </summary>
        public static void GenDebugAssembly(string name, FieldInfo field, PropertyInfo property, MethodInfo method, Type targetType, Type[] ctorParamTypes)
        {
            GenDebugAssembly<object>(name, field, property, method, targetType, ctorParamTypes);
        }

        /// <summary>
        /// Generates a assembly called 'name' that's useful for debugging purposes and inspecting the resulting C# code in ILSpy
        /// If 'field' is not null, it generates a setter and getter for that field
        /// If 'property' is not null, it generates a setter and getter for that property
        /// If 'method' is not null, it generates a call for that method
        /// if 'targetType' and 'ctorParamTypes' are not null, it generates a constructor for the target type that takes the specified arguments
        /// </summary>
        public static void GenDebugAssembly<TTarget>(string name, FieldInfo field, PropertyInfo property, MethodInfo method, Type targetType, Type[] ctorParamTypes)
        {
            var asmName = new AssemblyName("Asm");
            var asmBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.RunAndSave);
            var modBuilder = asmBuilder.DefineDynamicModule("Mod", name);
            var typeBuilder = modBuilder.DefineType("Test", TypeAttributes.Public);

            var weakTyping = typeof(TTarget) == typeof(object);

            Func<string, Type, Type[], ILGenerator> buildMethod = (methodName, returnType, parameterTypes) =>
            {
                var methodBuilder = typeBuilder.DefineMethod(methodName,
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                    CallingConventions.Standard,
                    returnType, parameterTypes);
                return methodBuilder.GetILGenerator();
            };

            if (field != null)
            {
                var fieldType = weakTyping ? typeof(object) : field.FieldType;
                emit.il = buildMethod("FieldSetter", typeof(void), new Type[] { typeof(TTarget).MakeByRefType(), fieldType });
                GenFieldSetter<TTarget>(field);
                emit.il = buildMethod("FieldGetter", fieldType, new Type[] { typeof(TTarget) });
                GenFieldGetter<TTarget>(field);
            }

            if (property != null)
            {
                var propType = weakTyping ? typeof(object) : property.PropertyType;
                emit.il = buildMethod("PropertySetter", typeof(void), new Type[] { typeof(TTarget).MakeByRefType(), propType });
                GenPropertySetter<TTarget>(property);
                emit.il = buildMethod("PropertyGetter", propType, new Type[] { typeof(TTarget) });
                GenPropertyGetter<TTarget>(property);
            }

            if (method != null)
            {
                var returnType = (weakTyping || method.ReturnType == typeof(void)) ? typeof(object) : method.ReturnType;
                emit.il = buildMethod("MethodCaller", returnType, new Type[] { typeof(TTarget), typeof(object[]) });
                GenMethodInvocation<TTarget>(method);
            }

            if (targetType != null)
            {
                emit.il = buildMethod("Ctor", typeof(TTarget), new Type[] { typeof(object[]) });
                GenCtor<TTarget>(targetType, ctorParamTypes);
            }

            typeBuilder.CreateType();
            asmBuilder.Save(name);
        }

        static int GetKey<T, R>(MemberInfo member, string dynMethodName)
        {
            return member.GetHashCode() ^ dynMethodName.GetHashCode() ^ typeof(T).GetHashCode() ^ typeof(R).GetHashCode();
        }

        static TDelegate GenDelegateForMember<TDelegate, TMember>(TMember member, int key, string dynMethodName,
            Action<TMember> generator, Type returnType, params Type[] paramTypes)
            where TMember : MemberInfo
            where TDelegate : class
        {
            var dynMethod = new DynamicMethod(dynMethodName, returnType, paramTypes, true);

            emit.il = dynMethod.GetILGenerator();
            generator(member);

            var result = dynMethod.CreateDelegate(typeof(TDelegate));
            cache[key] = result;
            return (TDelegate)(object)result;
        }

        static void GenCtor<T>(Type type, Type[] paramTypes)
        {
            // arg0: object[] arguments
            // goal: return new T(arguments)
            Type targetType = typeof(T) == typeof(object) ? type : typeof(T);

            if (targetType.IsValueType && paramTypes.Length == 0)
            {
                var tmp = emit.declocal(targetType);
                emit.ldloca(tmp)
                    .initobj(targetType)
                    .ldloc(0);
            }
            else
            {
                var ctor = targetType.GetConstructor(paramTypes);
                if (ctor == null)
                    throw new Exception("Generating constructor for type: " + targetType +
                        (paramTypes.Length == 0 ?  "No empty constructor found!" :
                        "No constructor found that matches the following parameter types: " +
                        string.Join(",", paramTypes.Select(x => x.Name).ToArray())));

                // push parameters in order to then call ctor
                for (int i = 0, imax = paramTypes.Length; i < imax; i++)
                {
                    emit.ldarg0()					// push args array
                        .ldc_i4(i)					// push index
                        .ldelem_ref()				// push array[index]
                        .unbox_any(paramTypes[i]);	// cast
                }

                emit.newobj(ctor);
            }

            if (typeof(T) == typeof(object) && targetType.IsValueType)
                emit.box(targetType);

            emit.ret();
        }

        static void GenMethodInvocation<TTarget>(MethodInfo method)
        {
            var weaklyTyped = typeof(TTarget) == typeof(object);

            // push target if not static (instance-method. in that case first arg is always 'this')
            if (!method.IsStatic)
            {
                var targetType = weaklyTyped ? method.DeclaringType : typeof(TTarget);
                emit.declocal(targetType);
                emit.ldarg0();
                if (weaklyTyped)
                    emit.unbox_any(targetType);
                emit.stloc0()
                    .ifclass_ldloc_else_ldloca(0, targetType);
            }

            // push arguments in order to call method
            var prams = method.GetParameters();
            for (int i = 0, imax = prams.Length; i < imax; i++)
            {
                emit.ldarg1()		// push array
                    .ldc_i4(i)		// push index
                    .ldelem_ref();	// pop array, index and push array[index]

                var param = prams[i];
                var dataType = param.ParameterType;

                if (dataType.IsByRef)
                    dataType = dataType.GetElementType();

                var tmp = emit.declocal(dataType);
                emit.unbox_any(dataType)
                    .stloc(tmp)
                    .ifbyref_ldloca_else_ldloc(tmp, param.ParameterType);
            }

            // perform the correct call (pushes the result)
            emit.callorvirt(method);

            // if method wasn't static that means we declared a temp local to load the target
            // that means our local variables index for the arguments start from 1
            int localVarStart = method.IsStatic ? 0 : 1;
            for (int i = 0; i < prams.Length; i++)
            {
                var paramType = prams[i].ParameterType;
                if (paramType.IsByRef)
                {
                    var byRefType = paramType.GetElementType();
                    emit.ldarg1()
                        .ldc_i4(i)
                        .ldloc(i + localVarStart);
                    if (byRefType.IsValueType)
                        emit.box(byRefType);
                    emit.stelem_ref();
                }
            }

            if (method.ReturnType == typeof(void))
                emit.ldnull();
            else if (weaklyTyped)
                emit.ifvaluetype_box(method.ReturnType);

            emit.ret();
        }

        static void GenFieldGetter<TTarget>(FieldInfo field)
        {
            GenMemberGetter<TTarget>(field, field.FieldType, field.IsStatic,
                (e, f) => e.lodfld((FieldInfo)f)
            );
        }

        static void GenPropertyGetter<TTarget>(PropertyInfo property)
        {
            GenMemberGetter<TTarget>(property, property.PropertyType,
                property.GetGetMethod(true).IsStatic,
                (e, p) => e.callorvirt(((PropertyInfo)p).GetGetMethod(true))
            );
        }

        static void GenMemberGetter<TTarget>(MemberInfo member, Type memberType, bool isStatic, Action<ILEmitter, MemberInfo> get)
        {
            if (typeof(TTarget) == typeof(object)) // weakly-typed?
            {
                // if we're static immediately load member and return value
                // otherwise load and cast target, get the member value and box it if neccessary:
                // return ((DeclaringType)target).member;
                if (!isStatic)
                    emit.ldarg0()
                        .unboxorcast(member.DeclaringType);
                emit.perform(get, member)
                    .ifvaluetype_box(memberType);
            }
            else // we're strongly-typed, don't need any casting or boxing
            {
                // if we're static return member value immediately
                // otherwise load target and get member value immeidately
                // return target.member;
                if (!isStatic)
                    emit.ifclass_ldarg_else_ldarga(0, typeof(TTarget));
                emit.perform(get, member);
            }

            emit.ret();
        }

        static void GenFieldSetter<TTarget>(FieldInfo field)
        {
            GenMemberSetter<TTarget>(field, field.FieldType, field.IsStatic,
                (e, f) => e.setfld((FieldInfo)f)
            );
        }

        static void GenPropertySetter<TTarget>(PropertyInfo property)
        {
            GenMemberSetter<TTarget>(property, property.PropertyType,
                property.GetSetMethod(true).IsStatic, (e, p) =>
                e.callorvirt(((PropertyInfo)p).GetSetMethod(true))
            );
        }

        static void GenMemberSetter<TTarget>(MemberInfo member, Type memberType, bool isStatic, Action<ILEmitter, MemberInfo> set)
        {
            var targetType = typeof(TTarget);
            var stronglyTyped = targetType != typeof(object);

            // if we're static set member immediately
            if (isStatic)
            {
                emit.ldarg1();
                if (!stronglyTyped)
                    emit.unbox_any(memberType);
                emit.perform(set, member)
                    .ret();
                return;
            }

            if (stronglyTyped)
            {
                // push target and value argument, set member immediately
                // target.member = value;
                emit.ldarg0()
                    .ifclass_ldind_ref(targetType)
                    .ldarg1()
                    .perform(set, member)
                    .ret();
                return;
            }

            // we're weakly-typed
            targetType = member.DeclaringType;
            if (!targetType.IsValueType) // are we a reference-type?
            {
                // load and cast target, load and cast value and set
                // ((TargetType)target).member = (MemberType)value;
                emit.ldarg0()
                    .ldind_ref()
                    .cast(targetType)
                    .ldarg1()
                    .unbox_any(memberType)
                    .perform(set, member)
                    .ret();
                return;
            }

            // we're a value-type
            // handle boxing/unboxing for the user so he doesn't have to do it himself
            // here's what we're basically generating (remember, we're weakly typed, so
            // the target argument is of type object here):
            // TargetType tmp = (TargetType)target; // unbox
            // tmp.member = (MemberField)value;		// set member value
            // target = tmp;						// box back

            emit.declocal(targetType);
            emit.ldarg0()
                .ldind_ref()
                .unbox_any(targetType)
                .stloc0()
                .ldloca(0)
                .ldarg1()
                .unbox_any(memberType)
                .perform(set, member)
                .ldarg0()
                .ldloc0()
                .box(targetType)
                .stind_ref()
                .ret();
        }

        private class ILEmitter
        {
            public ILGenerator il;

            public ILEmitter ret()                                 { il.Emit(OpCodes.Ret); return this; }
            public ILEmitter cast(Type type)                       { il.Emit(OpCodes.Castclass, type); return this; }
            public ILEmitter box(Type type)                        { il.Emit(OpCodes.Box, type); return this; }
            public ILEmitter unbox_any(Type type)                  { il.Emit(OpCodes.Unbox_Any, type); return this; }
            public ILEmitter unbox(Type type)                      { il.Emit(OpCodes.Unbox, type); return this; }
            public ILEmitter call(MethodInfo method)               { il.Emit(OpCodes.Call, method); return this; }
            public ILEmitter callvirt(MethodInfo method)           { il.Emit(OpCodes.Callvirt, method); return this; }
            public ILEmitter ldnull()                              { il.Emit(OpCodes.Ldnull); return this; }
            public ILEmitter bne_un(Label target)                  { il.Emit(OpCodes.Bne_Un, target); return this; }
            public ILEmitter beq(Label target)                     { il.Emit(OpCodes.Beq, target); return this; }
            public ILEmitter ldc_i4_0()                            { il.Emit(OpCodes.Ldc_I4_0); return this; }
            public ILEmitter ldc_i4_1()                            { il.Emit(OpCodes.Ldc_I4_1); return this; }
            public ILEmitter ldc_i4(int c)                         { il.Emit(OpCodes.Ldc_I4, c); return this; }
            public ILEmitter ldarg0()                              { il.Emit(OpCodes.Ldarg_0); return this; }
            public ILEmitter ldarg1()                              { il.Emit(OpCodes.Ldarg_1); return this; }
            public ILEmitter ldarg2()                              { il.Emit(OpCodes.Ldarg_2); return this; }
            public ILEmitter ldarga(int idx)                       { il.Emit(OpCodes.Ldarga, idx); return this; }
            public ILEmitter ldarga_s(int idx)                     { il.Emit(OpCodes.Ldarga_S, idx); return this; }
            public ILEmitter ldarg(int idx)                        { il.Emit(OpCodes.Ldarg, idx); return this; }
            public ILEmitter ldarg_s(int idx)                      { il.Emit(OpCodes.Ldarg_S, idx); return this; }
            public ILEmitter ifclass_ldind_ref(Type type)		   { if (!type.IsValueType) il.Emit(OpCodes.Ldind_Ref); return this; }
            public ILEmitter ldloc0()                              { il.Emit(OpCodes.Ldloc_0); return this; }
            public ILEmitter ldloc1()                              { il.Emit(OpCodes.Ldloc_1); return this; }
            public ILEmitter ldloc2()                              { il.Emit(OpCodes.Ldloc_2); return this; }
            public ILEmitter ldloca_s(int idx)                     { il.Emit(OpCodes.Ldloca_S, idx); return this; }
            public ILEmitter ldloca_s(LocalBuilder local)          { il.Emit(OpCodes.Ldloca_S, local); return this; }
            public ILEmitter ldloc_s(int idx)                      { il.Emit(OpCodes.Ldloc_S, idx); return this; }
            public ILEmitter ldloc_s(LocalBuilder local)           { il.Emit(OpCodes.Ldloc_S, local); return this; }
            public ILEmitter ldloca(int idx)                       { il.Emit(OpCodes.Ldloca, idx); return this; }
            public ILEmitter ldloca(LocalBuilder local)            { il.Emit(OpCodes.Ldloca, local); return this; }
            public ILEmitter ldloc(int idx)                        { il.Emit(OpCodes.Ldloc, idx); return this; }
            public ILEmitter ldloc(LocalBuilder local)             { il.Emit(OpCodes.Ldloc, local); return this; }
            public ILEmitter initobj(Type type)                    { il.Emit(OpCodes.Initobj, type); return this; }
            public ILEmitter newobj(ConstructorInfo ctor)          { il.Emit(OpCodes.Newobj, ctor); return this; }
            public ILEmitter Throw()                               { il.Emit(OpCodes.Throw); return this; }
            public ILEmitter throw_new(Type type)                  { var exp = type.GetConstructor(Type.EmptyTypes); newobj(exp).Throw(); return this; }
            public ILEmitter stelem_ref()                          { il.Emit(OpCodes.Stelem_Ref); return this; }
            public ILEmitter ldelem_ref()                          { il.Emit(OpCodes.Ldelem_Ref); return this; }
            public ILEmitter ldlen()                               { il.Emit(OpCodes.Ldlen); return this; }
            public ILEmitter stloc(int idx)                        { il.Emit(OpCodes.Stloc, idx); return this; }
            public ILEmitter stloc_s(int idx)                      { il.Emit(OpCodes.Stloc_S, idx); return this; }
            public ILEmitter stloc(LocalBuilder local)             { il.Emit(OpCodes.Stloc, local); return this; }
            public ILEmitter stloc_s(LocalBuilder local)           { il.Emit(OpCodes.Stloc_S, local); return this; }
            public ILEmitter stloc0()                              { il.Emit(OpCodes.Stloc_0); return this; }
            public ILEmitter stloc1()                              { il.Emit(OpCodes.Stloc_1); return this; }
            public ILEmitter mark(Label label)                     { il.MarkLabel(label); return this; }
            public ILEmitter ldfld(FieldInfo field)                { il.Emit(OpCodes.Ldfld, field); return this; }
            public ILEmitter ldsfld(FieldInfo field)               { il.Emit(OpCodes.Ldsfld, field); return this; }
            public ILEmitter lodfld(FieldInfo field)               { if (field.IsStatic) ldsfld(field); else ldfld(field); return this; }
            public ILEmitter ifvaluetype_box(Type type)            { if (type.IsValueType) il.Emit(OpCodes.Box, type); return this; }
            public ILEmitter stfld(FieldInfo field)                { il.Emit(OpCodes.Stfld, field); return this; }
            public ILEmitter stsfld(FieldInfo field)               { il.Emit(OpCodes.Stsfld, field); return this; }
            public ILEmitter setfld(FieldInfo field)               { if (field.IsStatic) stsfld(field); else stfld(field); return this; }
            public ILEmitter unboxorcast(Type type)                { if (type.IsValueType) unbox(type); else cast(type); return this; }
            public ILEmitter callorvirt(MethodInfo method)         { if (method.IsVirtual) il.Emit(OpCodes.Callvirt, method); else il.Emit(OpCodes.Call, method); return this; }
            public ILEmitter stind_ref()                           { il.Emit(OpCodes.Stind_Ref); return this; }
            public ILEmitter ldind_ref()                           { il.Emit(OpCodes.Ldind_Ref); return this; }
            public LocalBuilder declocal(Type type)                { return il.DeclareLocal(type); }
            public Label deflabel()                                { return il.DefineLabel(); }
            public ILEmitter ifclass_ldarg_else_ldarga(int idx, Type type) { if (type.IsValueType) emit.ldarga(idx); else emit.ldarg(idx); return this; }
            public ILEmitter ifclass_ldloc_else_ldloca(int idx, Type type) { if (type.IsValueType) emit.ldloca(idx); else emit.ldloc(idx); return this; }
            public ILEmitter perform(Action<ILEmitter, MemberInfo> action, MemberInfo member) { action(this, member); return this; }
            public ILEmitter ifbyref_ldloca_else_ldloc(LocalBuilder local, Type type) { if (type.IsByRef) ldloca(local); else ldloc(local); return this; }
        }
    }
}