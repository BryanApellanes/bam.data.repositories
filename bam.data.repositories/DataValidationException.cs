namespace Bam.Data.Repositories;

public class DataValidationException : Exception
{
    public DataValidationException(string message) : base(message) 
    { }
}