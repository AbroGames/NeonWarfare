namespace NeonWarfare.Scripts.Utils.InstanceRouting;

public interface IInstanceResolver
{
    object ResolveInstance(object key);
}

public interface IInstanceResolver<TInstance> : IInstanceResolver where TInstance : class;

public delegate object InstanceResolverDelegate(object key);

// эта штука нужна, чтобы делать резолверы через лямбды
public class DelegateInstanceResolver : IInstanceResolver
{
    private readonly InstanceResolverDelegate _resolver;

    public DelegateInstanceResolver(InstanceResolverDelegate resolver)
    {
        _resolver = resolver;
    }

    public object ResolveInstance(object key)
    {
        return _resolver(key);
    }
}
