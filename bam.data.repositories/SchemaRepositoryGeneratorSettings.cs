using Bam.Data;
using Bam.Data.Repositories;
using Bam.Net.Data.Schema;

namespace Bam.Net.Data.Repositories
{
    public class SchemaRepositoryGeneratorSettings : ISchemaRepositoryGeneratorSettings
    {
        public SchemaRepositoryGeneratorSettings(IDaoCodeWriter daoCodeWriter, IDaoTargetStreamResolver daoTargetStreamResolver, IWrapperGenerator wrapperGenerator)
        {
            DaoCodeWriter = daoCodeWriter;
            DaoTargetStreamResolver = daoTargetStreamResolver;
            WrapperGenerator = wrapperGenerator;
        }

        public IDaoCodeWriter DaoCodeWriter { get; set; }
        public IDaoTargetStreamResolver DaoTargetStreamResolver { get; set; }
        public IWrapperGenerator WrapperGenerator { get; set; }
        public IDaoRepoGenerationConfig DaoRepoGenerationConfig { get; set; }

    }
}
