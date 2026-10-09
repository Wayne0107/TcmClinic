using Ardalis.Specification;

namespace TcmClinic.Api.SharedKernel;

/// <summary>寫入用 Repository（含追蹤）。</summary>
public interface IRepository<T> : IRepositoryBase<T> where T : class, IAggregateRoot;

/// <summary>唯讀查詢用 Repository。</summary>
public interface IReadRepository<T> : IReadRepositoryBase<T> where T : class, IAggregateRoot;
