using Microsoft.Extensions.DependencyInjection;

namespace TcmClinic.Api.UseCases;

public static class UseCasesServiceExtensions
{
    /// <summary>
    /// 註冊 Mediator（handler 於本組件，故 AddMediator 必須在此呼叫，source generator 才能
    /// 於編譯時得知 Scoped 生命週期）。
    /// </summary>
    public static IServiceCollection AddUseCases(this IServiceCollection services)
    {
        services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);
        return services;
    }
}
