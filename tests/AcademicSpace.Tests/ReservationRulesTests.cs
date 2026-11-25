using AcademicSpace.Api.Modules.Reservations.Services;
using Xunit;

namespace AcademicSpace.Tests;

public class ReservationRulesTests
{
    [Fact]
    public void DuracionDeTresHoras_NoExcedeElMaximo()
    {
        Assert.False(ReservationRules.ExceedsMaxDuration(
            new TimeOnly(8, 0, 0), new TimeOnly(11, 0, 0)));
    }

    [Fact]
    public void DuracionDeExactamenteCuatroHoras_NoExcedeElMaximo()
    {
        Assert.False(ReservationRules.ExceedsMaxDuration(
            new TimeOnly(8, 0, 0), new TimeOnly(12, 0, 0)));
    }

    [Fact]
    public void DuracionDeCuatroHorasYUnSegundo_ExcedeElMaximo()
    {
        Assert.True(ReservationRules.ExceedsMaxDuration(
            new TimeOnly(8, 0, 0), new TimeOnly(12, 0, 1)));
    }

    [Fact]
    public void DuracionDeSeisHoras_ExcedeElMaximo()
    {
        Assert.True(ReservationRules.ExceedsMaxDuration(
            new TimeOnly(8, 0, 0), new TimeOnly(14, 0, 0)));
    }
}
