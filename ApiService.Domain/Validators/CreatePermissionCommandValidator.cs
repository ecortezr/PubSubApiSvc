using ApiService.Domain.UseCases.Permissions.Commands;
using FluentValidation;

namespace ApiService.Domain.Validators;

public class CreatePermissionCommandValidator : AbstractValidator<CreatePermissionCommand>
{
	public CreatePermissionCommandValidator()
	{
        RuleFor(permission => permission.Name)
            .NotEmpty()
            .WithMessage("Invalid Name");
    }
}

