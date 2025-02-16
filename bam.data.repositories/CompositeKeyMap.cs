namespace Bam.Data.Repositories
{
    /// <summary>
    /// Maps the composite key of an instance to its Cuid.
    /// </summary>
    [Serializable]
    public class CompositeKeyMap: RepoData
    {
        public CompositeKeyMap() { }
        public CompositeKeyMap(IHasKeyHash instance)
        {
            Instance = instance;
            ULongCompositeKey = instance.GetULongKeyHash();
            ReferencedCuid = instance.Property<string>("Cuid", true);
        }
        protected IHasKeyHash Instance { get; set; }
        public ulong ULongCompositeKey { get; set; }
        public string ReferencedCuid { get; set; }

        /// <summary>
        /// The AssemblyQualifiedName of the type
        /// </summary>
        public string TypeName { get; set; }

        public Type GetReferencedType()
        {
            return Type.GetType(TypeName);
        }
    }
}
