using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using AssemblyToProcess;
using Fody;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace MongoIncUpdate.Fody;

public partial class ModuleWeaver : BaseModuleWeaver
{
    private readonly CallMapper _callMapper;

    private readonly MemberSelector _memberSelector;
    private readonly MemberVirtualizer _memberVirtualizer;
    private readonly TypeSelector _typeSelector;

    public ModuleWeaver()
        : this(new TypeSelector(), new MemberSelector(), new MemberVirtualizer(), new CallMapper())
    {
        // Debugger.Launch();
    }

    public ModuleWeaver(
        TypeSelector typeSelector,
        MemberSelector memberSelector,
        MemberVirtualizer memberVirtualizer,
        CallMapper callMapper)
    {
        _typeSelector = typeSelector;
        _memberSelector = memberSelector;
        _memberVirtualizer = memberVirtualizer;
        _callMapper = callMapper;
    }

    public override void Execute()
    {
        //查找类型引用
        FindCoreReferences();

        //获取带有MongoIncUpdateAttribute属性的类(也支持继承至某类,后续补充实现)
        var selectedTypes = _typeSelector.Select(ModuleDefinition);

        // _memberVirtualizer.Virtualize(selectedMembers);
        // _callMapper.MapCallsToVirtual(selectedMembers, ModuleDefinition);

        //init方法
        var initMethodDef = _typeSelector.SelectMethodFromType(MongoIncUpdateInterface, "Init");

        //interface的属性
        var baseDirtiesProp = _typeSelector.SelectPropFromType(MongoIncUpdateInterface, "Dirties");
        // var baseDirtiesMethod = _typeSelector.SelectMethodFromType(MongoIncUpdateInterface, "Dirties");
        //字段
        var dirtiesFieldDef = new FieldDefinition("_dirties", FieldAttributes.Private, BitArrayTypeDefReference);

        //get方法
        var baseGetDirtiesMethodDef = _typeSelector.SelectMethodFromType(MongoIncUpdateInterface, "get_Dirties");
        var getDirtiesMethodDef = new MethodDefinition(
            $"{baseGetDirtiesMethodDef.DeclaringType.FullName}.{baseGetDirtiesMethodDef.Name}",
            MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.NewSlot |
            MethodAttributes.SpecialName | MethodAttributes.Virtual | MethodAttributes.Final,
            baseGetDirtiesMethodDef.ReturnType);
        getDirtiesMethodDef.Overrides.Add(baseGetDirtiesMethodDef);
        // getDirtiesMethodDef.CustomAttributes.Add(new CustomAttribute(CompilerGeneratedAttributeTypeReference));
        getDirtiesMethodDef.Body.Instructions.Append(
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Ldfld, dirtiesFieldDef),
            Instruction.Create(OpCodes.Ret)
        );
        //set方法
        var baseSetDirtiesMethodDef = _typeSelector.SelectMethodFromType(MongoIncUpdateInterface, "set_Dirties");
        var setDirtiesMethodDef = new MethodDefinition(
            $"{baseSetDirtiesMethodDef.DeclaringType.FullName}.{baseSetDirtiesMethodDef.Name}",
            MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.NewSlot |
            MethodAttributes.SpecialName | MethodAttributes.Virtual | MethodAttributes.Final,
            baseSetDirtiesMethodDef.ReturnType);
        setDirtiesMethodDef.Overrides.Add(baseSetDirtiesMethodDef);
        foreach (var parameterDefinition in baseSetDirtiesMethodDef.Parameters)
        {
            setDirtiesMethodDef.Parameters.Add(parameterDefinition);
        }


        // setDirtiesMethodDef.CustomAttributes.Add(new CustomAttribute(CompilerGeneratedAttributeTypeReference));
        setDirtiesMethodDef.Body.Instructions.Append(
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Ldarg_1),
            Instruction.Create(OpCodes.Stfld, dirtiesFieldDef),
            Instruction.Create(OpCodes.Ret)
        );

        foreach (var typ in selectedTypes)
        {
            //继承这个接口
            typ.Interfaces.Add(new InterfaceImplementation(MongoIncUpdateInterface));

            //创建field
            typ.Fields.Add(dirtiesFieldDef);

            //插入init函数调用
            var ctors = typ.Methods.Where(m => m.IsConstructor).ToList();
            foreach (var ctor in ctors)
            {
                var last = ctor.Body.Instructions.Count - 1; //插入到ret之前的位置
                ctor.Body.Instructions.Insert(last,
                    Instruction.Create(OpCodes.Ldarg_0),
                    Instruction.Create(OpCodes.Callvirt, initMethodDef),
                    Instruction.Create(OpCodes.Nop));
            }


            //插入getter
            typ.Methods.Add(getDirtiesMethodDef);

            //插入setter
            typ.Methods.Add(setDirtiesMethodDef);

            // _typeSelector.SelectMethodFromType(typ, "get_Dirties");

            //插入重写
            //Dirties重载
            var Dirties =
                new PropertyDefinition($"{MongoIncUpdateInterface}.{baseDirtiesProp.Name}", baseDirtiesProp.Attributes,
                    BitArrayTypeDefReference);
            Dirties.GetMethod = BuildGetMethodDefinitionFromBase(
                $"{MongoIncUpdateInterface}.{baseDirtiesProp.GetMethod.Name}",
                typ,
                baseDirtiesProp.GetMethod);
            Dirties.GetMethod.Body = new MethodBody(Dirties.GetMethod);
            Dirties.GetMethod.Body.GetILProcessor().Append(Instruction.Create(OpCodes.Ldarg_0));
            Dirties.GetMethod.Body.GetILProcessor().Append(Instruction.Create(OpCodes.Ldfld, dirtiesFieldDef));
            Dirties.GetMethod.Body.GetILProcessor().Append(Instruction.Create(OpCodes.Ret));

            Dirties.SetMethod = BuildGetMethodDefinitionFromBase(
                $"{MongoIncUpdateInterface}.{baseDirtiesProp.SetMethod.Name}",
                typ,
                baseDirtiesProp.SetMethod);
            Dirties.SetMethod.Body = new MethodBody(Dirties.SetMethod);
            Dirties.SetMethod.Body.GetILProcessor().Append(Instruction.Create(OpCodes.Ldarg_0));
            Dirties.SetMethod.Body.GetILProcessor().Append(Instruction.Create(OpCodes.Ldarg_1));
            Dirties.SetMethod.Body.GetILProcessor().Append(Instruction.Create(OpCodes.Stfld, dirtiesFieldDef));
            Dirties.SetMethod.Body.GetILProcessor().Append(Instruction.Create(OpCodes.Ret));
            typ.Properties.Add(Dirties);
            
            
            //注入属性变化监听
            InjectPropSetterPropChangeNotify(typ);
        }
        
        //获取所有符合条件的类
        //先处理继承 1继承,2属性实现
        //处理属性 1.获取需要监听setter的属性,2.调用继承的方法Dirties[idx]=true;
    }

    public override IEnumerable<string> GetAssembliesForScanning()
    {
        yield return "mscorlib";
        yield return "System";
    }
}