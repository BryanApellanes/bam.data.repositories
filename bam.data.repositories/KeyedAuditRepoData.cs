namespace Bam.Data.Repositories
{
    /// <summary>
    /// Extend this class to define a type that uses multiple properties to determine
    /// persistence instance uniqueness.  Adorn key properties with the CompositeKey
    /// attribute.  Adds the Key property to CompositeKeyAuditRepoData.
    /// </summary>
    [Serializable]
    public abstract class KeyedAuditRepoData : CompositeKeyAuditRepoData, IKeyedAuditRepoData
    {
        public static implicit operator ulong(KeyedAuditRepoData repoData)
        {
            return repoData.Key;
        }
        
        ulong _key = Convert.ToUInt64(0);
        public new ulong Key
        {
            get
            {
                if (_key == 0)
                {
                    _key = GetULongKeyHash();
                }
                return _key;
            }
            set => _key = value;
        }

        readonly object _saveLock = new object();
        
        /// <summary>
        /// Save the current instance to the specified repository, first checking if an instance with the same key is already saved.
        /// If an instance already exists it is updated with the state of this instance.
        /// </summary>
        /// <param name="repository"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T? SaveByKey<T>(IRepository repository) where T : class, new()
        {
            T? result = null;
            lock (_saveLock)
            {
                T? existing = LoadExisting<T>(repository);
                result = existing != null ? repository.Save<T>((T)existing.CopyProperties(this)) : repository.Save<T>(this as T);
            }
            return result;
        }

        /// <summary>
        /// Query for the KeyedAuditRepoData with the key matching the current instance then load it by its Uuid if it is found.
        /// </summary>
        /// <param name="repository"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T? LoadByKey<T>(IRepository repository) where T : class, new()
        {
            return LoadExisting<T>(repository);
        }
        
        private T? LoadExisting<T>(IRepository repository) where T : class, new()
        {
            T? existing = repository.LoadByKey<T>(this);
            if (existing is IHasUuid hasUuid)
            {
                this.Uuid = hasUuid.Uuid;
            }
            if (existing is IHasCuid hasCuid)
            {
                this.Cuid = hasCuid.Cuid;
            }

            return existing;
        }
    }
}
