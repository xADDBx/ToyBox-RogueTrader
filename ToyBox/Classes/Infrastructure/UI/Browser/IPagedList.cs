namespace ToyBox.Infrastructure;

public interface IPagedList {
    void UpdatePages();
    void SetCacheInvalid();
}
