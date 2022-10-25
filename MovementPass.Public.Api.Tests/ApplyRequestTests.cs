namespace MovementPass.Public.Api.Tests;

using System.ComponentModel.DataAnnotations;
using System.Linq;

using Xunit;

using Features.Apply;
using Infrastructure;

public class ApplyRequestTests
{
    [Fact]
    public void Validate_returns_error_if_date_time_is_in_past()
    {
        var input = new ApplyRequest
        {
            DateTime = Clock.Now().AddSeconds(-1)
        };

        var result = input.Validate(new ValidationContext(input));

        Assert.NotEmpty(result);
    }

    [Fact]
    public void Validate_returns_error_if_date_time_is_greater_than_tomorrow()
    {
        var input = new ApplyRequest
        {
            DateTime = Clock.Now().AddDays(1).AddSeconds(1)
        };

        var result = input.Validate(new ValidationContext(input));

        Assert.NotEmpty(result);
    }

    [Fact]
    public void Validate_returns_error_if_vehicle_is_included_but_vehicle_no_is_missing()
    {
        var input = new ApplyRequest
        {
            DateTime = Clock.Now().AddHours(1),
            IncludeVehicle = true,
            SelfDriven = true
        };

        var result = input.Validate(new ValidationContext(input));

        Assert.NotEmpty(result);
    }

    [Fact]
    public void Validate_does_not_return_any_error_if_vehicle_is_not_included()
    {
        var input = new ApplyRequest
        {
            DateTime = Clock.Now().AddHours(2)
        };

        var result = input.Validate(new ValidationContext(input));

        Assert.Empty(result);
    }

    [Fact]
    public void Validate_does_not_return_any_error_if_vehicle_is_included_and_vehicle_no_is_provided()
    {
        var input = new ApplyRequest
        {
            DateTime = Clock.Now().AddDays(1),
            IncludeVehicle = true,
            VehicleNo = "Dhaka Metro x-xx-xxxx",
            SelfDriven = true
        };

        var result = input.Validate(new ValidationContext(input));

        Assert.Empty(result);
    }

    [Fact]
    public void Validate_returns_errors_if_self_driven_is_false_but_driver_name_and_driver_license_no_are_not_provided()
    {
        var input = new ApplyRequest
        {
            DateTime = Clock.Now().AddHours(6),
            IncludeVehicle = true,
            VehicleNo = "Dhaka Metro x-xx-xxxx",
            SelfDriven = false
        };

        var result = input.Validate(new ValidationContext(input));

        Assert.Equal(2, result.Count());
    }
}
