using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace PurchaseService.Api.Mediator;

public interface IRequest<out TResponse>
{
}

public interface ICommand<out TResponse> : IRequest<TResponse>
{
}

public interface IQuery<out TResponse> : IRequest<TResponse>
{
}

public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}

public interface IMediator
{
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}

public sealed class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var requestType = request.GetType();
        var invokeMethod = typeof(Mediator)
            .GetMethod(nameof(Invoke), BindingFlags.Instance | BindingFlags.NonPublic)!
            .MakeGenericMethod(requestType, typeof(TResponse));

        return (Task<TResponse>)invokeMethod.Invoke(this, new object[] { request, cancellationToken })!;
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Delegates manage lifetime.")]
    private Task<TResponse> Invoke<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        var handler = _serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
        RequestHandlerDelegate<TRequest, TResponse> pipeline = handler.Handle;

        foreach (var behavior in _serviceProvider.GetServices<IPipelineBehavior<TRequest, TResponse>>().Reverse())
        {
            var next = pipeline;
            pipeline = (req, ct) => behavior.Handle(req, ct, next);
        }

        return pipeline(request, cancellationToken);
    }
}
