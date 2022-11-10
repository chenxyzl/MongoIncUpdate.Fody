using Fody;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace MongoIncUpdate.Fody;

public class TypeSelector
{
    public void CheckTypeLegal(IEnumerable<TypeDefinition> types)
    {
        foreach (var typ in types)
        {
            // if (!IsContainer(typ))
            //     throw new WeavingException($"{typ.Name} member must only property. [means:method only getter/setter");
            if (HasPublicFiled(typ))
            {
                var v = typ.Fields.Where(v => v.IsPublic).Select(v => v.Name).ToList();
                throw new WeavingException(
                    $"{typ.Name} member must only property. [means: no public filed:{v[0]}]");
            }

            if (!typ.IsClass)
            {
                throw new WeavingException($"{typ.Name} type must: class");
            }
            if (!typ.IsPublic)
            {
                throw new WeavingException($"{typ.Name} type must: public");
            }
            if (!typ.IsSealed)
            {
                throw new WeavingException($"{typ.Name} type must: sealed");
            }
        }
    }

    public IEnumerable<TypeDefinition> Select(ModuleDefinition moduleDefinition)
    {
        var typesToProcess = new List<TypeDefinition>();
        foreach (var type in moduleDefinition.GetTypes())
            if (HasMongoIncUpdateAttribute(type))
                typesToProcess.Add(type);

        return typesToProcess;
    }

    public TypeReference SelectMongoIncUpdateInterface(ModuleDefinition moduleDefinition)
    {
        var ns = "MongoIncUpdate.Base";
        var idu = "IDiffUpdateable";
        var mongoIncUpdate = moduleDefinition.FindAssembly(ns) ??
                             throw new WeavingException($"\"{ns}\" must import in {moduleDefinition.Name}");

        var mongoIncUpdateInterface =
            moduleDefinition.FindType(ns, idu, mongoIncUpdate) ??
            throw new WeavingException($"\"{ns}.{idu}\" must not null");

        var v = moduleDefinition.ImportReference(mongoIncUpdateInterface);
        if (v == null) throw new WeavingException($"\"{ns}.{idu}\" import err");
        return v;
    }

    public MethodReference SelectMethodFromType(ModuleDefinition moduleDefinition, TypeReference typeReference,
        string methodName)
    {
        var methodsToProcess = new List<MethodDefinition>();
        foreach (var type in typeReference.Resolve().GetMethods())
            if (type.Name == methodName)
                methodsToProcess.Add(type);
        if (methodsToProcess.Count < 1)
            throw new WeavingException($"SelectMethodFromType ${typeReference.Name}:{methodName} must exist");

        if (methodsToProcess.Count > 1)
            throw new WeavingException(
                $"SelectMethodFromType ${typeReference.Name}:{methodName} must only one; now:{methodsToProcess.Count}");

        return moduleDefinition.ImportReference(methodsToProcess[0]);
    }

    public PropertyDefinition SelectPropFromType(TypeReference typeReference, string propName)
    {
        var propToProcess = new List<PropertyDefinition>();
        foreach (var type in typeReference.Resolve().Properties)
            if (type.Name == propName)
                propToProcess.Add(type);
        if (propToProcess.Count < 1)
            throw new WeavingException($"SelectMethodFromType ${typeReference.Name}:{propName} must exist");

        if (propToProcess.Count > 1)
            throw new WeavingException(
                $"SelectMethodFromType ${typeReference.Name}:{propName} must only one; now:{propToProcess.Count}");

        return propToProcess[0];
    }

    private static bool HasMongoIncUpdateAttribute(TypeDefinition type)
    {
        return type.CustomAttributes.Any(_ => _.AttributeType.Name == "MongoIncUpdateAttribute");
    }

    public static bool IsContainer(TypeDefinition type)
    {
        return type.Methods.All(_ => _.IsGetter || _.IsSetter || _.IsConstructor);
    }

    public static bool HasPublicFiled(TypeDefinition type)
    {
        return type.Fields.Any(t => t.IsPublic);
    }
}