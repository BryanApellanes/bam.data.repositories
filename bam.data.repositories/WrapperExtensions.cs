namespace Bam.Data.Repositories
{
    public static class WrapperExtensions
    {
        public static T Wrap<T>(this object instance, DaoRepository repo)
        {
            return (T)repo.Wrap(typeof(T), instance);
        }
    }
}
