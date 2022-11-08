using Mono.Cecil;
using Mono.Cecil.Cil;

namespace MongoIncUpdate.Fody;

public partial class ModuleWeaver
{
    public void InjectOverrideProperty(TypeDefinition typ, string propName, bool isStatic = false)
    {
        //找到基类Prop
        var baseProp = _typeSelector.SelectPropFromType(MongoIncUpdateInterface, $"{propName}");
        //创建field//字段
        var fieldDef = new FieldDefinition($"_{propName.FirstCharToLowerCase()}",
            isStatic ? FieldAttributes.Static | FieldAttributes.Private : FieldAttributes.Private, //
            baseProp.PropertyType); 
        typ.Fields.Add(fieldDef);

        //插入getter
        var baseGetPropMethodDef = _typeSelector.SelectMethodFromType(MongoIncUpdateInterface, $"get_{propName}");
        var getPropMethodDef = new MethodDefinition(
            $"{baseGetPropMethodDef.DeclaringType.FullName}.{baseGetPropMethodDef.Name}",
            MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.NewSlot |
            MethodAttributes.SpecialName | MethodAttributes.Virtual | MethodAttributes.Final,
            baseGetPropMethodDef.ReturnType);
        getPropMethodDef.Overrides.Add(baseGetPropMethodDef);
        // getPropMethodDef.CustomAttributes.Add(new CustomAttribute(CompilerGeneratedAttributeTypeReference));
        var getterBodyIl = isStatic
            ? new[]
            {
                Instruction.Create(OpCodes.Ldsfld, fieldDef),
                Instruction.Create(OpCodes.Ret)
            }
            : new[]
            {
                Instruction.Create(OpCodes.Ldarg_0),
                Instruction.Create(OpCodes.Ldfld, fieldDef),
                Instruction.Create(OpCodes.Ret)
            };
        getPropMethodDef.Body.Instructions.Append(getterBodyIl);
        typ.Methods.Add(getPropMethodDef);

        //插入setter
        var baseSetPropMethodDef = _typeSelector.SelectMethodFromType(MongoIncUpdateInterface, $"set_{propName}");
        var setPropMethodDef = new MethodDefinition(
            $"{baseSetPropMethodDef.DeclaringType.FullName}.{baseSetPropMethodDef.Name}",
            MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.NewSlot |
            MethodAttributes.SpecialName | MethodAttributes.Virtual | MethodAttributes.Final,
            baseSetPropMethodDef.ReturnType);
        setPropMethodDef.Overrides.Add(baseSetPropMethodDef);
        foreach (var parameterDefinition in baseSetPropMethodDef.Parameters)
            setPropMethodDef.Parameters.Add(parameterDefinition);

        // setPropMethodDef.CustomAttributes.Add(new CustomAttribute(CompilerGeneratedAttributeTypeReference));
        var setterBodyIl = isStatic
            ? new[]
            {
                Instruction.Create(OpCodes.Ldarg_1),
                Instruction.Create(OpCodes.Stsfld, fieldDef),
                Instruction.Create(OpCodes.Ret)
            }
            : new[]
            {
                Instruction.Create(OpCodes.Ldarg_0),
                Instruction.Create(OpCodes.Ldarg_1),
                Instruction.Create(OpCodes.Stfld, fieldDef),
                Instruction.Create(OpCodes.Ret)
            };
        setPropMethodDef.Body.Instructions.Append(setterBodyIl);
        typ.Methods.Add(setPropMethodDef);

        // _typeSelector.SelectMethodFromType(typ, "get_Prop");

        //插入重写
        //Prop重载
        var prop = new PropertyDefinition($"{MongoIncUpdateInterface}.{baseProp.Name}",
            baseProp.Attributes,
            baseProp.PropertyType);
        prop.GetMethod = BuildGetMethodDefinitionFromBase(
            $"{MongoIncUpdateInterface}.{baseProp.GetMethod.Name}",
            typ,
            baseProp.GetMethod);
        prop.GetMethod.Body = new MethodBody(prop.GetMethod);
        prop.GetMethod.Body.GetILProcessor().Append(Instruction.Create(OpCodes.Ldarg_0));
        prop.GetMethod.Body.GetILProcessor().Append(Instruction.Create(OpCodes.Ldfld, fieldDef));
        prop.GetMethod.Body.GetILProcessor().Append(Instruction.Create(OpCodes.Ret));

        prop.SetMethod = BuildGetMethodDefinitionFromBase(
            $"{MongoIncUpdateInterface}.{baseProp.SetMethod.Name}",
            typ,
            baseProp.SetMethod);
        prop.SetMethod.Body = new MethodBody(prop.SetMethod);
        prop.SetMethod.Body.GetILProcessor().Append(Instruction.Create(OpCodes.Ldarg_0));
        prop.SetMethod.Body.GetILProcessor().Append(Instruction.Create(OpCodes.Ldarg_1));
        prop.SetMethod.Body.GetILProcessor().Append(Instruction.Create(OpCodes.Stfld, fieldDef));
        prop.SetMethod.Body.GetILProcessor().Append(Instruction.Create(OpCodes.Ret));
        typ.Properties.Add(prop);
    }
}