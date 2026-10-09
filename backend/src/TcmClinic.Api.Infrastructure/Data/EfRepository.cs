using Ardalis.Specification.EntityFrameworkCore;
using TcmClinic.Api.SharedKernel;

namespace TcmClinic.Api.Infrastructure.Data;

/// <summary>
/// 以 Ardalis.Specification 評估的通用 Repository 實作。
/// </summary>
public class EfRepository<T>(AppDbContext dbContext)
    : RepositoryBase<T>(dbContext), IReadRepository<T>, IRepository<T>
    where T : class, IAggregateRoot;
