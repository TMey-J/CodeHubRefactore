namespace CodeHub.Infrastructure.Repositories;

using CodeHub.Application.Abstractions;
using CodeHub.Domain.Entities;
using CodeHub.Infrastructure.Persistence;

public sealed class AiRequestRepository(CodeHubDbContext writeContext) : IAiRequestRepository
{
    public async Task AddAsync(int? userId, string service, CancellationToken cancellationToken = default)
    {
        writeContext.AiRequests.Add(AiRequest.Create(userId, service));
        await writeContext.SaveChangesAsync(cancellationToken);
    }
}
