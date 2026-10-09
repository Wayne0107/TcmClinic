using Mediator;
using Microsoft.Extensions.DependencyInjection;
using TcmClinic.Api.SharedKernel;

namespace TcmClinic.Api.Web.Configurations;

public static class MediatorConfigs
{
    /// <summary>註冊 Mediator pipeline 行為（AddMediator 本身在 UseCases.AddUseCases 呼叫）。</summary>
    public static IServiceCollection AddMediatorBehaviors(this IServiceCollection services)
    {
        // 跨切面 pipeline：記錄（Principle III）。
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        return services;
    }
}
