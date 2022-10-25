namespace MovementPass.Public.Api.Tests;

using System.ComponentModel.DataAnnotations;

using Xunit;

using Features.Register;
using Infrastructure;

public class RegisterRequestTests
{
    [Fact]
    public void Validate_returns_error_if_age_is_less_than_18()
    {
        var input = new RegisterRequest
        {
            DateOfBirth = Clock.Now().AddYears(-18).AddDays(2)
        };

        var result = input.Validate(new ValidationContext(input));

        Assert.NotEmpty(result);
    }

    [Fact]
    public void Validate_does_not_return_any_error_if_age_is_18()
    {
        var input = new RegisterRequest
        {
            DateOfBirth = Clock.Now().AddYears(-18)
        };

        var result = input.Validate(new ValidationContext(input));

        Assert.Empty(result);
    }

    [Fact]
    public void Validate_does_not_return_any_error_if_age_is_more_than_18()
    {
        var input = new RegisterRequest
        {
            DateOfBirth = Clock.Now().AddYears(-19)
        };

        var result = input.Validate(new ValidationContext(input));

        Assert.Empty(result);
    }
}
