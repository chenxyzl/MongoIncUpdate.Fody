using Mono.Cecil;
using Mono.Cecil.Cil;

namespace MongoIncUpdate.Fody;

public partial class ModuleWeaver
{
    private void InjectPropSetterPropChangeNotify(TypeDefinition type)
    {
        var propChangeMethod = _typeSelector.SelectMethodFromType(ModuleDefinition, _mongoIncUpdateInterface, "PropChange");
        foreach (var prop in type.Properties)
        {
            var setter = prop.SetMethod;
            if (setter != null && setter.IsPublic && setter.HasBody)
            {
                var last = setter.Body.Instructions.Count - 1; //插入到ret之前的位置
                setter.Body.Instructions.Insert(last,
                    Instruction.Create(OpCodes.Ldarg_0),
                    Instruction.Create(OpCodes.Ldstr, prop.Name),
                    Instruction.Create(OpCodes.Call, propChangeMethod),
                    Instruction.Create(OpCodes.Nop));
            }
        }
    }
}