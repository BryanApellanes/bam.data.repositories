namespace Bam.Data.Repositories
{
    public interface ISchemaRepositoryGeneratorSettingsProvider
    {
        ISchemaRepositoryGeneratorSettings GetSettings(IDaoRepoGenerationConfig daoRepoGenerationConfig);
    }
}
