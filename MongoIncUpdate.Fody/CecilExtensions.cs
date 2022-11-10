using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace MongoIncUpdate.Fody;

public static class CecilExtensions
{
    public static AssemblyNameReference? FindAssembly(this ModuleDefinition module, string name)
    {
        return module.AssemblyReferences.Where(x => x.Name == name).MaxBy(x => x.Version);
    }

    public static TypeReference FindType(this ModuleDefinition currentModule, string @namespace, string typeName,
        IMetadataScope? scope = null, params string[] typeParameters)
    {
        var result = new TypeReference(@namespace, typeName, currentModule, scope);
        foreach (var typeParameter in typeParameters)
        {
            result.GenericParameters.Add(new GenericParameter(typeParameter, result));
        }

        return currentModule.ImportReference(result);
    }
    
    public static TypeReference GetGeneric(this TypeReference reference)
    {
        if (!reference.HasGenericParameters) return reference;

        var genericType = new GenericInstanceType(reference);
        foreach (var parameter in reference.GenericParameters) genericType.GenericArguments.Add(parameter);

        return genericType;
    }

    public static FieldReference GetGeneric(this FieldDefinition definition)
    {
        if (!definition.DeclaringType.HasGenericParameters) return definition;

        var declaringType = new GenericInstanceType(definition.DeclaringType);
        foreach (var parameter in definition.DeclaringType.GenericParameters)
            declaringType.GenericArguments.Add(parameter);

        return new FieldReference(definition.Name, definition.FieldType, declaringType);
    }

    public static MethodReference GetGeneric(this MethodReference reference)
    {
        if (!reference.DeclaringType.HasGenericParameters) return reference;

        var declaringType = new GenericInstanceType(reference.DeclaringType);
        foreach (var parameter in reference.DeclaringType.GenericParameters)
            declaringType.GenericArguments.Add(parameter);

        var methodReference = new MethodReference(reference.Name, reference.MethodReturnType.ReturnType, declaringType);
        foreach (var parameterDefinition in reference.Parameters) methodReference.Parameters.Add(parameterDefinition);

        methodReference.HasThis = reference.HasThis;
        return methodReference;
    }

    public static MethodReference MakeGeneric(this MethodReference self, params TypeReference[] arguments)
    {
        var reference = new MethodReference(self.Name, self.ReturnType)
        {
            HasThis = self.HasThis,
            ExplicitThis = self.ExplicitThis,
            DeclaringType = self.DeclaringType.MakeGenericInstanceType(arguments),
            CallingConvention = self.CallingConvention
        };

        foreach (var parameter in self.Parameters)
            reference.Parameters.Add(new ParameterDefinition(parameter.ParameterType));

        foreach (var genericParameter in self.GenericParameters)
            reference.GenericParameters.Add(new GenericParameter(genericParameter.Name, reference));

        return reference;
    }

    
}