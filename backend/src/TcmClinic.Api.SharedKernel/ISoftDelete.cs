namespace TcmClinic.Api.SharedKernel;

/// <summary>軟刪除標記（Principle XI）。</summary>
public interface ISoftDelete
{
    bool Removed { get; set; }
}
