namespace MongoIncUpdate.Base;

[AttributeUsage(AttributeTargets.Interface)]
public class MongoIncUpdateInterfaceAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Class)]
public class MongoIncUpdateAttribute : Attribute
{
}