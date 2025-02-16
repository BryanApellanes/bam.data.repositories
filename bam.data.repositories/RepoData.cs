using Bam.Logging;
using Bam.Services.DataReplication;

namespace Bam.Data.Repositories
{
    /// <summary>
    /// An abstract base class defining common
    /// properties for any object that is stored in a Repository.
    /// </summary>
    [Serializable]
	public abstract class RepoData : IRepoData
	{
        /// <summary>
        /// Gets or sets the identifier for the current instance.  This should be
        /// considered a "local" id, meaning it identifies the instance
        /// from the current repository of the current process.  This value
        /// may be different for the same instance in a different process or repository.
        /// For universal identity use Uuid + Cuid.
        /// </summary>
        [Key]
		public ulong Id { get; set; }
        
        private DateTime? _created;
        /// <summary>
        /// The time that the Created property
        /// was first referenced prior to persisting
        /// the object instance
        /// </summary>
        public DateTime? Created
        {
            get
            {
                if (_created == null)
                {
                    _created = DateTime.UtcNow;
                }
                return _created;
            }
            set => _created = value;
        }
        
        string _uuid = Guid.NewGuid().ToString();
        /// <summary>
        /// The universally unique identifier.  While this value should be
        /// universally unique, a very small possibility exists of collisions
        /// when generating Uuids concurrently across multiple threads and/or
        /// processes.  To confidently identify a unique data instance use a
        /// combination of Uuid and Cuid.  See Cuid.
        /// </summary>
        [CompositeKey]
        public string Uuid
        {
            get => _uuid;
            set => _uuid = value;
        }
        
        string _cuid = Bam.Cuid.Generate();
        /// <summary>
        /// The collision resistant unique identifier.
        /// </summary>
        [CompositeKey]
        public string Cuid
        {
            get => _cuid;
            set => _cuid = value;
        }

        /// <summary>
        /// Does a query for an instance of the specified
        /// generic type T having the specified properties whose values
        /// match those of the current instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="repo"></param>
        /// <param name="propertyNames"></param>
        /// <returns></returns>
        public virtual T? QueryFirstOrDefault<T>(IRepository repo, params string[] propertyNames) where T : class, new()
        {
            ValidatePropertyNamesOrDie(propertyNames);
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            propertyNames.Each(new { Parameters = parameters, Instance = this }, (ctx, pn) =>
            {
                ctx.Parameters.Add(pn, ReflectionExtensions.Property(ctx.Instance, pn));
            });
            T? instance = repo.Query<T>(parameters).FirstOrDefault();
            return instance;
        }

        public override bool Equals(object? obj)
        {
            if (obj is RepoData o)
            {
                return o.Uuid.Equals(Uuid) && o.Cuid.Equals(Cuid);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Uuid.GetHashCode() + Cuid.GetHashCode();
        }

        public override string ToString()
        {
            return this.ToJson(true);
        }

        public virtual IRepoData? Save(IRepository repo)
        {
            return (RepoData)repo.Save((object)this)!;
        }

        public bool GetIsPersisted()
        {
            return IsPersisted;
        }

        public bool GetIsPersisted(out IRepository repo)
        {
            repo = Repository;
            return IsPersisted;
        }
        
        /// <summary>
        /// Ensure the current RepoData instance has been 
        /// persisted to the specified repo
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="repo"></param>
        /// <returns></returns>
        public T? EnsurePersisted<T>(IRepository repo) where T: class, new()
        {
            T? instance = repo.Retrieve<T>(Cuid) ?? repo.Save(this as T);

            return instance;
        }
        
        /// <summary>
        /// Ensures that an instance of the current RepoData
        /// has been saved to the specified repo where the 
        /// specified properties equal the values of those
        /// properties on this instance.  Will cause the 
        /// Id of this instance to be reset if a representative
        /// value is not found in the repo
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="repo"></param>
        /// <param name="propertyNames"></param>
        /// <returns></returns>
        public T? EnsureSingle<T>(IRepository repo, params string[] propertyNames) where T: class, new()
        {
            T? instance = QueryFirstOrDefault<T>(repo, propertyNames);
            if (instance == null) // wasn't saved/found, should reset Id so the repo will Create
            {
                Id = 0;
                instance = repo.Save(this as T);
            }
            return instance;
        }
        
        protected void ValidatePropertyNamesOrDie(params string[] propertyNames)
        {
            propertyNames.Each(new { Instance = this }, (ctx, pn) =>
            {
                Args.ThrowIf(!ReflectionExtensions.HasProperty(ctx.Instance, pn), "Specified property ({0}) was not found on instance of type ({1})", pn, ctx.Instance.GetType().Name);
            });
        }
        
        protected internal bool IsPersisted { get; set; }
        protected internal IRepository? Repository { get; set; } // gets set by Repository.Save

        public static object GetInstanceId(object instance, UniversalIdentifiers universalIdentifier = UniversalIdentifiers.Cuid)
        {
            Args.ThrowIfNull(instance);
            if (!(instance is RepoData repoData))
            {
                Log.Warn("Getting instance id but specified object instance is not of type {0}: {1}", nameof(RepoData),
                    instance.ToString()!);
            }

            switch (universalIdentifier)
            {
                case UniversalIdentifiers.Uuid:
                    if (instance.HasProperty("Uuid"))
                    {
                        return instance.Property("Uuid");
                    }
                    break;
                case UniversalIdentifiers.Cuid:
                    if (instance.HasProperty("Cuid"))
                    {
                        return instance.Property("Cuid");
                    }
                    break;
                case UniversalIdentifiers.CKey:
                    if (instance.HasProperty("CompositeKeyId"))
                    {
                        if (!(instance is CompositeKeyAuditRepoData))
                        {
                            Log.Warn("Getting CompositeKeyId as instance id but specified object instance is not of type {0}: {1}", nameof(CompositeKeyAuditRepoData), instance.ToString());
                        }
                        return instance.Property("CompositeKey");
                    }else if (instance.HasProperty("Key"))
                    {
                        if (!(instance is KeyedAuditRepoData))
                        {
                            Log.Warn("Getting Key property as instance id but specified object instance is not of type {0}: {1}", nameof(KeyedAuditRepoData), instance.ToString());
                        }

                        return instance.Property("Key");
                    }
                    break;
            }
            
            return null;
        }
    }
}
