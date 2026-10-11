namespace CodeHub.Application.Common;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Lightweight CQRS abstractions.
/// Commands mutate state, queries read it; both are dispatched through <see cref="IDispatcher"/>
/// so the API layer never depends on handlers directly.
/// </summary>
public interface ICommand;

public interface ICommand<TResponse>;

public interface IQuery<TResponse>;

public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    Task HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

public interface ICommandHandler<in TCommand, TResponse> where TCommand : ICommand<TResponse>
{
    Task<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

public interface IQueryHandler<in TQuery, TResponse> where TQuery : IQuery<TResponse>
{
    Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}

public interface IDispatcher
{
    Task SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand;

    Task<TResponse> SendAsync<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResponse>;

    Task<TResponse> QueryAsync<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResponse>;
}

public sealed class Dispatcher(IServiceProvider serviceProvider) : IDispatcher
{
    public Task SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand =>
        serviceProvider
            .GetRequiredService<ICommandHandler<TCommand>>()
            .HandleAsync(command, cancellationToken);

    public Task<TResponse> SendAsync<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResponse> =>
        serviceProvider
            .GetRequiredService<ICommandHandler<TCommand, TResponse>>()
            .HandleAsync(command, cancellationToken);

    public Task<TResponse> QueryAsync<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResponse> =>
        serviceProvider
            .GetRequiredService<IQueryHandler<TQuery, TResponse>>()
            .HandleAsync(query, cancellationToken);
}
