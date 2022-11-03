using System.Reflection;

namespace MongoIncUpdate.Base;

public interface IPropertyCallAdapter
{
    object? InvokeGet(object @this);

    string PropName();

    bool IsDirectType();
    //add void InvokeSet(TThis @this, object value) if necessary
}

public class PropertyCallAdapter<TThis, TResult> : IPropertyCallAdapter where TThis : class
{
    private readonly Func<TThis, TResult> _getterInvocation;
    private readonly bool _isDirectType;
    private readonly string _propName;

    public PropertyCallAdapter(Func<TThis, TResult> getterInvocation, string propName, bool isDirectType)
    {
        _getterInvocation = getterInvocation;
        _propName = propName;
        _isDirectType = isDirectType;
    }

    public object? InvokeGet(object @this)
    {
        var target = @this as TThis ?? throw new InvalidOperationException();
        return _getterInvocation.Invoke(target);
    }

    public string PropName()
    {
        return _propName;
    }

    public bool IsDirectType()
    {
        return _isDirectType;
    }
}

public static class PropertyCallAdapterProvider
{
    public static IPropertyCallAdapter GetInstance<TThis>(this TThis self, PropertyInfo property) where TThis : notnull
    // public static Delegate GetInstance<TThis>(this TThis self, string forPropertyName)
    {
        // var property = typeof(TThis).GetProperty(
        //     forPropertyName,
        //     BindingFlags.Instance | BindingFlags.NonPublic);

        MethodInfo? getMethod;
        Delegate? getterInvocation;
        if ((getMethod = property.GetGetMethod(true)) != null)
        {
            // property.GetMethod.MakeGenericMethod(property.GetMethod.GetGenericArguments());
            var openGetterType = typeof(Func<,>);
            var concreteGetterType = openGetterType
                .MakeGenericType(self.GetType(), property.PropertyType);

            getterInvocation =
                Delegate.CreateDelegate(concreteGetterType, null, getMethod);
        }
        else
        {
            throw new Exception("getterInvocation must not null, create get delegate err");
        }

        var openAdapterType = typeof(PropertyCallAdapter<,>);
        var concreteAdapterType = openAdapterType.MakeGenericType(self.GetType(), property.PropertyType);

        //注意这里string也当作值类型来处理
        return Activator.CreateInstance(concreteAdapterType, getterInvocation, property.Name,
                       property.PropertyType.IsValueType || property.PropertyType.FullName == "System.String") as
                   IPropertyCallAdapter ??
               throw new InvalidOperationException();
        // return getterInvocation;
    }
}