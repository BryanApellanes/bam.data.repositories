using Bam.Data;
using Bam.Net.Data.Schema;

namespace Bam.Net.Data.Repositories
{
    public interface ISchemaRepositoryGeneratorSettings
    {
        IDaoRepoGenerationConfig DaoRepoGenerationConfig { get; set; }
        IDaoCodeWriter DaoCodeWriter { get; set; }
        IDaoTargetStreamResolver DaoTargetStreamResolver { get; set; }
        IWrapperGenerator WrapperGenerator { get; set; }
    }
}