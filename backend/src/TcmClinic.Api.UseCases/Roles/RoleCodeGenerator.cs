using System.Globalization;

namespace TcmClinic.Api.UseCases.Roles;

/// <summary>
/// 角色代碼產生器（012，FR-007）：前端新增表單不提供代碼時，後端自動產生。
/// 規則：取現存「R + 數字」代碼之最大序號 +1，格式 R###（3 碼零填補；序號 &gt;999 時自然進位）。
/// </summary>
public static class RoleCodeGenerator
{
    /// <summary>依現存代碼集合產生下一個不重複的 R### 代碼。</summary>
    public static string Next(IEnumerable<string> existingCodes)
    {
        var maxNumber = 0;
        foreach (var value in existingCodes)
        {
            if (!string.IsNullOrEmpty(value) &&
                value.Length > 1 &&
                (value[0] == 'R' || value[0] == 'r') &&
                int.TryParse(value.AsSpan(1), NumberStyles.None, CultureInfo.InvariantCulture, out var n) &&
                n > maxNumber)
            {
                maxNumber = n;
            }
        }

        return $"R{(maxNumber + 1).ToString("000", CultureInfo.InvariantCulture)}";
    }
}
