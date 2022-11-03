using System.Runtime.CompilerServices;
using Mono.Cecil;


namespace MongoIncUpdate.Fody;

public partial class ModuleWeaver
{
    private TypeReference _mongoIncUpdateInterface;

    public void FindCoreReferences()
    {
        //BitArray
        var bitArrayTypeDefinition = FindTypeDefinition("System.Collections.BitArray");
        ModuleDefinition.Assembly.MainModule.ImportReference(bitArrayTypeDefinition.Resolve());

        var mongoIncUpdate = ModuleDefinition.FindAssembly("MongoIncUpdate.Base") ??
                             throw new ArgumentNullException($"\"MongoIncUpdate.Base\" must not null");
        _mongoIncUpdateInterface =
            ModuleDefinition.FindType("MongoIncUpdate.Base", "IDiffUpdateable", mongoIncUpdate) ??
            throw new ArgumentNullException($"\"MongoIncUpdate.Base.IDiffUpdateable\" must not null");

        //
        // // mongoIncUpdate.
        // MongoIncUpdateInterface = ModuleDefinition.FindType("MongoIncUpdate.Base", "IDiffUpdateable", mongoIncUpdate);
        // MongoIncUpdateInterface.Resolve();
    }
}