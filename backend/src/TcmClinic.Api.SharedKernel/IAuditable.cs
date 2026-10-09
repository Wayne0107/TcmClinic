namespace TcmClinic.Api.SharedKernel;

/// <summary>稽核欄位（Principle XI）；由 SaveChangesInterceptor 自動填入。</summary>
public interface IAuditable
{
    DateTime CreateDate { get; set; }
    string CreateUser { get; set; }
    DateTime ModifyDate { get; set; }
    string ModifyUser { get; set; }
}
