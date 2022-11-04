using System.Runtime.CompilerServices;
using Mono.Cecil;

namespace MongoIncUpdate.Fody;

public partial class ModuleWeaver
{
    public TypeReference BitArrayTypeDefReference;
    public MethodReference CompilerGeneratedAttributeTypeReference;

    public TypeReference MongoIncUpdateInterface;

    public void FindCoreReferences()
    {
        //BitArray
        var bitArrayTypeDefinition = FindTypeDefinition("System.Collections.BitArray");
        BitArrayTypeDefReference = ModuleDefinition.ImportReference(bitArrayTypeDefinition);

        CompilerGeneratedAttributeTypeReference = ModuleDefinition.Import(
            typeof(CompilerGeneratedAttribute).GetConstructor(Type.EmptyTypes));

        //Mongo
        MongoIncUpdateInterface = _typeSelector.SelectMongoIncUpdateInterface(ModuleDefinition);
    }
}