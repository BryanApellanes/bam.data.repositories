using Bam.Data.Repositories;

namespace Bam.Net.Data.Repositories
{
    public interface ITemplatedWrapperGenerator : IWrapperGenerator
    {
        ITemplateRenderer<WrapperModel> TemplateRenderer { get; }

        GeneratedAssemblyInfo GenerateAssembly();
        void WriteSource(string writeSourceDir);
    }
}