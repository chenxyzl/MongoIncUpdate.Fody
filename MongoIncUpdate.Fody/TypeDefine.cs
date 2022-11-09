using System.Runtime.CompilerServices;
using Mono.Cecil;

namespace MongoIncUpdate.Fody;

public partial class ModuleWeaver
{
    public TypeReference MongoIncUpdateInterface;

    public void FindCoreReferences()
    {
        //Mongo
        MongoIncUpdateInterface = _typeSelector.SelectMongoIncUpdateInterface(ModuleDefinition);
    }
}