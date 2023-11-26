using Bam.Net.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bam.Data.Repositories
{
    public interface IDaoRepositoryDatabaseSource
    {
        IDatabase Database { get; set; }
    }
}
