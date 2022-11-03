using Mono.Cecil;

namespace MongoIncUpdate.Fody;

public class MemberSelector
{
    public IEnumerable<MethodDefinition> Select(TypeDefinition type)
    {
        var membersToProcess = new List<MethodDefinition>();
        foreach (var member in type.Methods)
            if (member.IsPublic
                && !member.IsStatic
                && !member.IsConstructor
                && !member.IsVirtual)
                membersToProcess.Add(member);

        return membersToProcess;
    }
}