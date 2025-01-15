namespace Bam.Data.Repositories;

public class DataValidationResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public Exception? Exception { get; set; }
}