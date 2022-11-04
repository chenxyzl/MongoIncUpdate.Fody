using System.Runtime.CompilerServices;
using Mono.Cecil;


namespace MongoIncUpdate.Fody;

public partial class ModuleWeaver
{
    private TypeReference _mongoIncUpdateInterface;

    public void FindCoreReferences()
    {

        // var v  = _typeSelector.TestSelect(ModuleDefinition)??throw new ArgumentNullException($"\"Program.PlaceHold\" must not null");
        // _mongoIncUpdateInterface = v.Interfaces[0].InterfaceType ?? throw new ArgumentNullException($"\"Program.PlaceHold interface mongoIncUpdateInterface \" must not null");
        
        var mongoIncUpdate = ModuleDefinition.FindAssembly("MongoIncUpdate.Base") ??
                             throw new ArgumentNullException($"\"MongoIncUpdate.Base\" must not null");
        
        var mongoIncUpdateInterface =
            ModuleDefinition.FindType("MongoIncUpdate.Base", "IDiffUpdateable", mongoIncUpdate) ??
            throw new ArgumentNullException($"\"MongoIncUpdate.Base.IDiffUpdateable\" must not null");
        
        _mongoIncUpdateInterface = ModuleDefinition.ImportReference(mongoIncUpdateInterface.Resolve());
    }
}