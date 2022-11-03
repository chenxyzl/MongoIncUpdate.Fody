using Mono.Cecil;
using Mono.Cecil.Cil;

namespace MongoIncUpdate.Fody;

public class CallMapper
{
    public void MapCallsToVirtual(IEnumerable<MethodDefinition> members, ModuleDefinition moduleDefinition)
    {
        foreach (var typeDefinition in moduleDefinition.GetTypes())
        {
            if (typeDefinition.IsAbstract || typeDefinition.IsEnum) continue;

            foreach (var methodDefinition in typeDefinition.Methods)
                if (methodDefinition.HasBody)
                    ReplaceCallsTo(methodDefinition, members);
        }
    }

    private static void ReplaceCallsTo(MethodDefinition methodDefinition, IEnumerable<MethodDefinition> members)
    {
        foreach (var instruction in methodDefinition.Body.Instructions)
        {
            if (instruction.OpCode != OpCodes.Call) continue;

            if (members.Any(member => member == instruction.Operand)) instruction.OpCode = OpCodes.Callvirt;
        }
    }
}