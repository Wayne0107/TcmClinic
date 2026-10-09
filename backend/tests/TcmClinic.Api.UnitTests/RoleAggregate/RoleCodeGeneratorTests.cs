using TcmClinic.Api.UseCases.Roles;

namespace TcmClinic.Api.UnitTests.RoleAggregate;

/// <summary>角色代碼自動產生規則（SPEC-012，FR-007）。</summary>
public class RoleCodeGeneratorTests
{
    [Fact(DisplayName = "無現存代碼時產生 R001（012）")]
    public void Empty_generates_R001()
    {
        Assert.Equal("R001", RoleCodeGenerator.Next([]));
    }

    // @SPEC-012 @AC-18
    [Fact(DisplayName = "取現存 R### 最大序號 +1（012）")]
    public void Takes_max_plus_one()
    {
        Assert.Equal("R003", RoleCodeGenerator.Next(["R001", "R002"]));
        Assert.Equal("R011", RoleCodeGenerator.Next(["R001", "R010", "R007"]));
    }

    [Fact(DisplayName = "非 R 數字代碼（如 BH／SM／SUPERADMIN）不影響序號（012）")]
    public void Ignores_non_r_numeric_codes()
    {
        Assert.Equal("R001", RoleCodeGenerator.Next(["CLERK", "SYSADMIN", "SUPERADMIN", "R"]));
        Assert.Equal("R006", RoleCodeGenerator.Next(["CLERK", "R005", "SYSADMIN"]));
    }

    [Fact(DisplayName = "序號零填補為 3 碼，超過 999 自然進位（012）")]
    public void Pads_to_three_digits()
    {
        Assert.Equal("R009", RoleCodeGenerator.Next(["R008"]));
        Assert.Equal("R1000", RoleCodeGenerator.Next(["R999"]));
    }
}
