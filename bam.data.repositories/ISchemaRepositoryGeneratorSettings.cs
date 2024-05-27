using Bam.Data;
using Bam.Data.Schema;

namespace Bam.Data.Repositories
{
    public interface ISchemaRepositoryGeneratorSettings
    {
        IDaoRepoGenerationConfig DaoRepoGenerationConfig { get; set; }
        IDaoCodeWriter DaoCodeWriter { get; set; }
        IDaoTargetStreamResolver DaoTargetStreamResolver { get; set; }
        IWrapperGenerator WrapperGenerator { get; set; }
    }
}