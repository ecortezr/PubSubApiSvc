using ApiService.Domain.UseCases.Permissions.Commands;
using FluentValidation;

namespace ApiService.Domain.Validators;

public class UpdateBodyPermissionCommandValidator : AbstractValidator<UpdateBodyPermissionCommand>
{
	public UpdateBodyPermissionCommandValidator()
	{
        RuleFor(permission => permission.Name)
            .NotEmpty()
            .WithMessage("Invalid Name");
    }
}

