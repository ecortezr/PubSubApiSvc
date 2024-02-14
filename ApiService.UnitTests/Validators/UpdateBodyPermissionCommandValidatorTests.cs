using ApiService.Domain.UseCases.Permissions.Commands;
using ApiService.Domain.Validators;
using FluentValidation.TestHelper;

namespace ApiService.UnitTests.Validators;

public class UpdateBodyPermissionCommandValidatorTests
{
    private readonly UpdateBodyPermissionCommandValidator _validator = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GivenAnInvalidNameValue_ShouldHaveValidationError(string name)
    {
        var permission = new UpdateBodyPermissionCommand { Name = name };
        var result = _validator.TestValidate(permission);

        result.ShouldHaveValidationErrorFor(model => model.Name);
    }
}
