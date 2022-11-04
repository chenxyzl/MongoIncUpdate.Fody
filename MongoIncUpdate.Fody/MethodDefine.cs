using Mono.Cecil;

namespace MongoIncUpdate.Fody;

public partial class ModuleWeaver
{
    private MethodDefinition BuildGetMethodDefinitionFromBase(string fullName, TypeDefinition targetType,
        MethodDefinition baseMethod)
    {
        var outMethod = new MethodDefinition(fullName,
            baseMethod.Attributes, baseMethod.ReturnType)
        {
            DeclaringType = targetType,
            SemanticsAttributes = baseMethod.SemanticsAttributes,
            HasThis = baseMethod.IsHideBySig,
            MetadataToken = baseMethod.MetadataToken,
            MethodReturnType = baseMethod.MethodReturnType,
            ImplAttributes = baseMethod.ImplAttributes,
            Body = baseMethod.Body,
            PInvokeInfo = baseMethod.PInvokeInfo
            // Overrides = baseMethod.Overrides,
            // Parameters = baseMethod.Parameters,
            // SecurityDeclarations =  baseMethod.SecurityDeclarations
        };
        outMethod.Attributes &= ~MethodAttributes.Family;
        outMethod.Attributes &= ~MethodAttributes.Abstract;
        outMethod.Attributes |= MethodAttributes.Private;
        outMethod.Attributes |= MethodAttributes.Final;
        outMethod.Body = baseMethod.Body;
        foreach (var baseMethodOverride in baseMethod.Overrides) outMethod.Overrides.Add(baseMethodOverride);
        foreach (var baseMethodParameter in baseMethod.Parameters) outMethod.Parameters.Add(baseMethodParameter);
        foreach (var baseMethodSecurityDeclaration in baseMethod.SecurityDeclarations)
            outMethod.SecurityDeclarations.Add(baseMethodSecurityDeclaration);
        return outMethod;
    }
}