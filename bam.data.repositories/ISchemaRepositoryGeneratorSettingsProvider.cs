using Bam.Net.Application;
using Bam.Net.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bam.data.repositories
{
    public interface ISchemaRepositoryGeneratorSettingsProvider
    {
        SchemaRepositoryGeneratorSettings GetSettings(DaoRepoGenerationConfig daoRepoGenerationConfig);
    }
}
