using Magic.Contracts.Automation;

namespace Magic.Application.Automation.Services;

public interface IAiAssistantProvider
{
    Task<AiAssistantResponseDto> GenerateInsightAsync(
        Guid companyId,
        Guid? branchId,
        string prompt,
        string groundedContextJson,
        CancellationToken cancellationToken = default);
}

