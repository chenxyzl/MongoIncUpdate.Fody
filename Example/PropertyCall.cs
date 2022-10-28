using System.Reflection;

namespace Example;

public interface IPropertyCallAdapter
{
    object InvokeGet(object @this);

    string PropName();
    //add void InvokeSet(TThis @this, object value) if necessary
}

public class PropertyCallAdapter<TThis, TResult> : IPropertyCallAdapter where TThis : class
{
    private readonly Func<TThis, TResult> _getterInvocation;
    private readonly string _propName;

    public PropertyCallAdapter(Func<TThis, TResult> getterInvocation, string propName)
    {
        _getterInvocation = getterInvocation;
        _propName = propName;
    }

    public object InvokeGet(object @this)
    {
        return _getterInvocation.Invoke(@this as TThis ?? throw new InvalidOperationException()) ??
               throw new InvalidOperationException();
    }

    public string PropName()
    {
        return _propName;
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
        return Activator.CreateInstance(concreteAdapterType, getterInvocation, property.Name) as IPropertyCallAdapter ??
               throw new InvalidOperationException();
        // return getterInvocation;
    }
}