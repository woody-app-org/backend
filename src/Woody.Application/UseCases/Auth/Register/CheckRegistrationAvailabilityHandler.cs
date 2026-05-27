using Woody.Application.DTOs;
using Woody.Application.Interfaces;
using Woody.Application.Validation;

namespace Woody.Application.UseCases.Auth.Register;

public class CheckRegistrationAvailabilityHandler
{
    public const string UsernameTakenMessage = "Este nome de usuário já está em uso.";
    public const string EmailTakenMessage = "Este e-mail já está cadastrado.";
    public const string CpfTakenMessage = "Este CPF já está cadastrado.";

    private readonly IUserRepository _users;

    public CheckRegistrationAvailabilityHandler(IUserRepository users)
    {
        _users = users;
    }

    public async Task<CheckRegistrationAvailabilityResponseDTO> HandleAsync(
        CheckRegistrationAvailabilityRequestDTO request,
        CancellationToken cancellationToken = default)
    {
        var response = new CheckRegistrationAvailabilityResponseDTO();

        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            if (!UsernameInputValidator.TryValidate(request.Username, out var username, out var error))
            {
                response.Username = new FieldAvailabilityDTO { Available = false, Message = error };
            }
            else
            {
                var taken = await _users.ExistsUsernameAsync(username);
                response.Username = taken
                    ? new FieldAvailabilityDTO { Available = false, Message = UsernameTakenMessage }
                    : new FieldAvailabilityDTO { Available = true };
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            if (!InputValidator.TryNormalizeRequiredText(
                    request.Email,
                    "E-mail",
                    InputValidationLimits.EmailMaxLength,
                    out var email,
                    out var error))
            {
                response.Email = new FieldAvailabilityDTO { Available = false, Message = error };
            }
            else
            {
                email = email.ToLowerInvariant();
                var taken = await _users.ExistsEmailAsync(email);
                response.Email = taken
                    ? new FieldAvailabilityDTO { Available = false, Message = EmailTakenMessage }
                    : new FieldAvailabilityDTO { Available = true };
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Cpf))
        {
            var digits = CpfInputNormalizer.NormalizeDigits(request.Cpf);
            if (digits.Length != 11 || !CpfInputNormalizer.IsValid(digits))
            {
                response.Cpf = new FieldAvailabilityDTO { Available = false, Message = "CPF inválido." };
            }
            else
            {
                var taken = await _users.ExistsCpfAsync(digits, cancellationToken);
                response.Cpf = taken
                    ? new FieldAvailabilityDTO { Available = false, Message = CpfTakenMessage }
                    : new FieldAvailabilityDTO { Available = true };
            }
        }

        return response;
    }
}
