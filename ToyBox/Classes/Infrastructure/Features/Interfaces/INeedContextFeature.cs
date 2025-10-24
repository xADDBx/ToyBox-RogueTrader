namespace ToyBox;
public interface INeedContextFeature { }
public interface INeedContextFeature<T> : INeedContextFeature {
    bool GetContext(out T? context);
}
public interface INeedContextFeature<TIn, TOut> : INeedContextFeature {
    bool GetContext(TIn? data, out TOut? context);
}
