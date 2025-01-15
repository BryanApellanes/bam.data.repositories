/*
	Copyright © Bryan Apellanes 2015  
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bam.Data;

namespace Bam.Data.Repositories
{
    /// <summary>
    /// An abstract base class defining common
    /// properties for any object that is saved to
    /// a Repository including fields useful
    /// for auditing the modification of persisted
    /// data.
    /// </summary>
    [Serializable]
    public abstract class AuditRepoData: RepoData, IAuditRepoData
    {
        public AuditRepoData() : base()
        {
            Created = DateTime.UtcNow;
        }
        
        public string CreatedBy { get; set; }		
        public string ModifiedBy { get; set; }
        public DateTime? Modified { get; set; }
        public DateTime? Deleted { get; set; }

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
        /// <param name="modifiedBy"></param>
        /// <param name="propertyNames"></param>
        /// <returns></returns>
        public new T EnsureSingle<T>(IRepository repo, string modifiedBy, params string[] propertyNames)  where T: class, new()
        {
            T instance = QueryFirstOrDefault<T>(repo, propertyNames);
            if (instance == null) // wasn't saved/found, should reset Id so the repo will Create
            {
                Id = 0;
                ModifiedBy = modifiedBy;
                Modified = DateTime.UtcNow;
                instance = repo.Save(this as T);
            }
            return instance;
        }
	}
}
