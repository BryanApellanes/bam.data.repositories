namespace Bam.Data.Repositories
{
    public interface ITemplatedWrapperGenerator : IWrapperGenerator
    {
        ITemplateRenderer<WrapperModel> TemplateRenderer { get; }

        new GeneratedAssemblyInfo GenerateAssembly();
        new void WriteSource(string writeSourceDir);
    }
}