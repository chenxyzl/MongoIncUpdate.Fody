using Mono.Cecil;

namespace MongoIncUpdate.Fody;

public class MemberVirtualizer
{
    public void Virtualize(IEnumerable<MethodDefinition> members)
    {
        foreach (var member in members)
        {
            member.IsVirtual = true;
            member.IsNewSlot = true;
        }
    }
}