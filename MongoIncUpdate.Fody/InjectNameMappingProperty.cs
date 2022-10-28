using Mono.Cecil;
using Mono.Cecil.Cil;

namespace MongoIncUpdate.Fody;

public partial class ModuleWeaver
{
    public void InjectNameMappingProperty(TypeDefinition typ)
    {
        var baseNameMappingProp = _typeSelector.SelectPropFromType(MongoIncUpdateInterface, "NameMapping");
        //创建field//字段
        var nameMappingFieldDef = new FieldDefinition("_nameMapping", FieldAttributes.Private, baseNameMappingProp.PropertyType);
        typ.Fields.Add(nameMappingFieldDef);

        //插入getter
        var baseGetNameMappingMethodDef =
            _typeSelector.SelectMethodFromType(MongoIncUpdateInterface, "get_NameMapping");
        var getNameMappingMethodDef = new MethodDefinition(
            $"{baseGetNameMappingMethodDef.DeclaringType.FullName}.{baseGetNameMappingMethodDef.Name}",
            MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.NewSlot |
            MethodAttributes.SpecialName | MethodAttributes.Virtual | MethodAttributes.Final,
            baseGetNameMappingMethodDef.ReturnType);
        getNameMappingMethodDef.Overrides.Add(baseGetNameMappingMethodDef);
        // getNameMappingMethodDef.CustomAttributes.Add(new CustomAttribute(CompilerGeneratedAttributeTypeReference));
        getNameMappingMethodDef.Body.Instructions.Append(
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Ldfld, nameMappingFieldDef),
            Instruction.Create(OpCodes.Ret)
        );
        typ.Methods.Add(getNameMappingMethodDef);

        //插入setter
        var baseSetNameMappingMethodDef =
            _typeSelector.SelectMethodFromType(MongoIncUpdateInterface, "set_NameMapping");
        var setNameMappingMethodDef = new MethodDefinition(
            $"{baseSetNameMappingMethodDef.DeclaringType.FullName}.{baseSetNameMappingMethodDef.Name}",
            MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.NewSlot |
            MethodAttributes.SpecialName | MethodAttributes.Virtual | MethodAttributes.Final,
            baseSetNameMappingMethodDef.ReturnType);
        setNameMappingMethodDef.Overrides.Add(baseSetNameMappingMethodDef);
        foreach (var parameterDefinition in baseSetNameMappingMethodDef.Parameters)
        {
            setNameMappingMethodDef.Parameters.Add(parameterDefinition);
        }

        // setNameMappingMethodDef.CustomAttributes.Add(new CustomAttribute(CompilerGeneratedAttributeTypeReference));
        setNameMappingMethodDef.Body.Instructions.Append(
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Ldarg_1),
            Instruction.Create(OpCodes.Stfld, nameMappingFieldDef),
            Instruction.Create(OpCodes.Ret)
        );
        typ.Methods.Add(setNameMappingMethodDef);

        // _typeSelector.SelectMethodFromType(typ, "get_NameMapping");

        //插入重写
        //NameMapping重载
        var NameMapping = new PropertyDefinition($"{MongoIncUpdateInterface}.{baseNameMappingProp.Name}",
            baseNameMappingProp.Attributes,
            baseNameMappingProp.PropertyType);
        NameMapping.GetMethod = BuildGetMethodDefinitionFromBase(
            $"{MongoIncUpdateInterface}.{baseNameMappingProp.GetMethod.Name}",
            typ,
            baseNameMappingProp.GetMethod);
        NameMapping.GetMethod.Body = new MethodBody(NameMapping.GetMethod);
        NameMapping.GetMethod.Body.GetILProcessor().Append(Instruction.Create(OpCodes.Ldarg_0));
        NameMapping.GetMethod.Body.GetILProcessor().Append(Instruction.Create(OpCodes.Ldfld, nameMappingFieldDef));
        NameMapping.GetMethod.Body.GetILProcessor().Append(Instruction.Create(OpCodes.Ret));

        NameMapping.SetMethod = BuildGetMethodDefinitionFromBase(
            $"{MongoIncUpdateInterface}.{baseNameMappingProp.SetMethod.Name}",
            typ,
            baseNameMappingProp.SetMethod);
        NameMapping.SetMethod.Body = new MethodBody(NameMapping.SetMethod);
        NameMapping.SetMethod.Body.GetILProcessor().Append(Instruction.Create(OpCodes.Ldarg_0));
        NameMapping.SetMethod.Body.GetILProcessor().Append(Instruction.Create(OpCodes.Ldarg_1));
        NameMapping.SetMethod.Body.GetILProcessor().Append(Instruction.Create(OpCodes.Stfld, nameMappingFieldDef));
        NameMapping.SetMethod.Body.GetILProcessor().Append(Instruction.Create(OpCodes.Ret));
        typ.Properties.Add(NameMapping);
    }
}