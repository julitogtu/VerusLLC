namespace VerusLLC.Application.Common.Correlation;

public interface ICorrelationIdAccessor
{
    const string None = "none";

    string CorrelationId { get; }
}
