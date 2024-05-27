using Bam.Data.Repositories;

namespace Bam.Data.Repositories
{
    public interface ITemplatedWrapperGenerator : IWrapperGenerator
    {
        ITemplateRenderer<WrapperModel> TemplateRenderer { get; }

        GeneratedAssemblyInfo GenerateAssembly();
        void WriteSource(string writeSourceDir);
    }
}