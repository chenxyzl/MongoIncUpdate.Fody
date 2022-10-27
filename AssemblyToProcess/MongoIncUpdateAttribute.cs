using System;

namespace AssemblyToProcess;

[AttributeUsage(AttributeTargets.Interface)]
public class MongoIncUpdateInterfaceAttribute : Attribute
{
    public MongoIncUpdateInterfaceAttribute()
    {
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class MongoIncUpdateAttribute : Attribute
{
    public MongoIncUpdateAttribute()
    {
    }
}