using System.Collections;
using MongoIncUpdate.Base;

namespace Example;

public class PlaceHold: IDiffUpdateable
{
    private BitArray _dirties;
    private Dictionary<int, IPropertyCallAdapter> _idxMapping;
    private Dictionary<string, int> _nameMapping;
 
    BitArray IDiffUpdateable.Dirties 
    {
        get => _dirties;
        set => _dirties = value;
    }  

    Dictionary<int, IPropertyCallAdapter> IDiffUpdateable.IdxMapping
    {
        get => _idxMapping;
        set => _idxMapping = value;
    }

    Dictionary<string, int> IDiffUpdateable.NameMapping
    {
        get => _nameMapping;
        set => _nameMapping = value;
    }
}