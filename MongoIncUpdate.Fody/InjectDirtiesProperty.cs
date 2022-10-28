using Mono.Cecil;
using Mono.Cecil.Cil;

namespace MongoIncUpdate.Fody;

public partial class ModuleWeaver
{
    public void InjectDirtiesProperty(TypeDefinition typ)
    {
        var baseDirtiesProp = _typeSelector.SelectPropFromType(MongoIncUpdateInterface, "Dirties");

        //创建field//字段
        var dirtiesFieldDef = new FieldDefinition("_dirties", FieldAttributes.Private, baseDirtiesProp.PropertyType);
        typ.Fields.Add(dirtiesFieldDef);

        //插入getter
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
        typ.Methods.Add(getDirtiesMethodDef);

        //插入setter
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
        typ.Methods.Add(setDirtiesMethodDef);

        // _typeSelector.SelectMethodFromType(typ, "get_Dirties");

        //插入重写
        //Dirties重载
        var Dirties = new PropertyDefinition($"{MongoIncUpdateInterface}.{baseDirtiesProp.Name}",
            baseDirtiesProp.Attributes,
            baseDirtiesProp.PropertyType);
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
    }
}