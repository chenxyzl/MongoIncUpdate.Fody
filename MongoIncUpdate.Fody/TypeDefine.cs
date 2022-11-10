using System.Runtime.CompilerServices;
using Mono.Cecil;

namespace MongoIncUpdate.Fody;

public partial class ModuleWeaver
{
    private TypeReference _mongoIncUpdateInterface;

    private void FindCoreReferences()
    {
        //Mongo
        _mongoIncUpdateInterface = _typeSelector.SelectMongoIncUpdateInterface(ModuleDefinition);
    }
}