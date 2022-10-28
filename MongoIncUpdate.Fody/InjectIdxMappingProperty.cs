using Mono.Cecil;
using Mono.Cecil.Cil;

namespace MongoIncUpdate.Fody;

public partial class ModuleWeaver
{
    public void InjectIdxMappingProperty(TypeDefinition typ)
    {
        var baseIdxMappingProp = _typeSelector.SelectPropFromType(MongoIncUpdateInterface, "IdxMapping");
        //创建field//字段
        var idxMappingFieldDef = new FieldDefinition("_idxMapping", FieldAttributes.Private, baseIdxMappingProp.PropertyType);
        typ.Fields.Add(idxMappingFieldDef);

        //插入getter
        var baseGetIdxMappingMethodDef = _typeSelector.SelectMethodFromType(MongoIncUpdateInterface, "get_IdxMapping");
        var getIdxMappingMethodDef = new MethodDefinition(
            $"{baseGetIdxMappingMethodDef.DeclaringType.FullName}.{baseGetIdxMappingMethodDef.Name}",
            MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.NewSlot |
            MethodAttributes.SpecialName | MethodAttributes.Virtual | MethodAttributes.Final,
            baseGetIdxMappingMethodDef.ReturnType);
        getIdxMappingMethodDef.Overrides.Add(baseGetIdxMappingMethodDef);
        // getIdxMappingMethodDef.CustomAttributes.Add(new CustomAttribute(CompilerGeneratedAttributeTypeReference));
        getIdxMappingMethodDef.Body.Instructions.Append(
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Ldfld, idxMappingFieldDef),
            Instruction.Create(OpCodes.Ret)
        );
        typ.Methods.Add(getIdxMappingMethodDef);

        //插入setter
        var baseSetIdxMappingMethodDef = _typeSelector.SelectMethodFromType(MongoIncUpdateInterface, "set_IdxMapping");
        var setIdxMappingMethodDef = new MethodDefinition(
            $"{baseSetIdxMappingMethodDef.DeclaringType.FullName}.{baseSetIdxMappingMethodDef.Name}",
            MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.NewSlot |
            MethodAttributes.SpecialName | MethodAttributes.Virtual | MethodAttributes.Final,
            baseSetIdxMappingMethodDef.ReturnType);
        setIdxMappingMethodDef.Overrides.Add(baseSetIdxMappingMethodDef);
        foreach (var parameterDefinition in baseSetIdxMappingMethodDef.Parameters)
        {
            setIdxMappingMethodDef.Parameters.Add(parameterDefinition);
        }

        // setIdxMappingMethodDef.CustomAttributes.Add(new CustomAttribute(CompilerGeneratedAttributeTypeReference));
        setIdxMappingMethodDef.Body.Instructions.Append(
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Ldarg_1),
            Instruction.Create(OpCodes.Stfld, idxMappingFieldDef),
            Instruction.Create(OpCodes.Ret)
        );
        typ.Methods.Add(setIdxMappingMethodDef);

        // _typeSelector.SelectMethodFromType(typ, "get_IdxMapping");

        //插入重写
        //IdxMapping重载
        var IdxMapping = new PropertyDefinition($"{MongoIncUpdateInterface}.{baseIdxMappingProp.Name}",
            baseIdxMappingProp.Attributes,
            baseIdxMappingProp.PropertyType);
        IdxMapping.GetMethod = BuildGetMethodDefinitionFromBase(
            $"{MongoIncUpdateInterface}.{baseIdxMappingProp.GetMethod.Name}",
            typ,
            baseIdxMappingProp.GetMethod);
        IdxMapping.GetMethod.Body = new MethodBody(IdxMapping.GetMethod);
        IdxMapping.GetMethod.Body.GetILProcessor().Append(Instruction.Create(OpCodes.Ldarg_0));
        IdxMapping.GetMethod.Body.GetILProcessor().Append(Instruction.Create(OpCodes.Ldfld, idxMappingFieldDef));
        IdxMapping.GetMethod.Body.GetILProcessor().Append(Instruction.Create(OpCodes.Ret));

        IdxMapping.SetMethod = BuildGetMethodDefinitionFromBase(
            $"{MongoIncUpdateInterface}.{baseIdxMappingProp.SetMethod.Name}",
            typ,
            baseIdxMappingProp.SetMethod);
        IdxMapping.SetMethod.Body = new MethodBody(IdxMapping.SetMethod);
        IdxMapping.SetMethod.Body.GetILProcessor().Append(Instruction.Create(OpCodes.Ldarg_0));
        IdxMapping.SetMethod.Body.GetILProcessor().Append(Instruction.Create(OpCodes.Ldarg_1));
        IdxMapping.SetMethod.Body.GetILProcessor().Append(Instruction.Create(OpCodes.Stfld, idxMappingFieldDef));
        IdxMapping.SetMethod.Body.GetILProcessor().Append(Instruction.Create(OpCodes.Ret));
        typ.Properties.Add(IdxMapping);
    }
}