using VerusLLC.Application.Common.Correlation;

namespace VerusLLC.Api.Middleware;

internal sealed class HttpCorrelationIdAccessor(IHttpContextAccessor httpContextAccessor)
    : ICorrelationIdAccessor
{
    public string CorrelationId =>
        httpContextAccessor.HttpContext?.GetCorrelationId() ?? ICorrelationIdAccessor.None;
}
