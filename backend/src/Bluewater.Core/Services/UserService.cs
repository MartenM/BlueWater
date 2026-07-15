using System.Security.Cryptography;
using Bluewater.Core.Dto;
using Bluewater.Core.Dto.Common;
using Bluewater.Core.Dto.Mail;
using Bluewater.Core.Dto.Users;
using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models;
using Bluewater.Domain.Models.Mail;
using Bluewater.Infra.Context;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bluewater.Core.Services;

public class UserService : IUserService
{
    /// <summary>
    /// Well-known <see cref="MailTemplate.Name"/> looked up for the new-member welcome mail.
    /// Seeded automatically by BluewaterContextSeeder (see RequiredTransactionalMailTemplates);
    /// if it's somehow still missing or has no sender configured, user creation still succeeds
    /// and a warning is logged instead.
    /// </summary>
    public const string WelcomeMailTemplateName = TransactionalMailTemplateNames.WelcomeEmail;

    private readonly BluewaterContext _db;
    private readonly UserManager<BlueUser> _userManager;
    private readonly IValidator<CreateUserRequest> _createValidator;
    private readonly IValidator<UpdateUserRequest> _updateValidator;
    private readonly IMailService _mailService;
    private readonly ILogger<UserService> _logger;

    public UserService(
        BluewaterContext db,
        UserManager<BlueUser> userManager,
        IValidator<CreateUserRequest> createValidator,
        IValidator<UpdateUserRequest> updateValidator,
        IMailService mailService,
        ILogger<UserService> logger)
    {
        _db = db;
        _userManager = userManager;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _mailService = mailService;
        _logger = logger;
    }

    public async Task<PagedResult<UserDto>> ListAsync(int page, int pageSize, string? search)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x =>
                x.Firstname.ToLower().Contains(term) ||
                x.Surname.ToLower().Contains(term) ||
                (x.UserName != null && x.UserName.ToLower().Contains(term)) ||
                (x.Email != null && x.Email.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync();
        var users = await query
            .OrderBy(x => x.Surname)
            .ThenBy(x => x.Firstname)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<UserDto>(users.Select(ToDto).ToList(), page, pageSize, totalCount);
    }

    public async Task<UserDto> GetAsync(Guid id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new BlueNotFoundException($"User '{id}' was not found.");

        return ToDto(user);
    }

    public async Task<CreateUserResponse> CreateAsync(CreateUserRequest request)
    {
        await _createValidator.ValidateAndThrowAsync(request);

        var user = new BlueUser
        {
            UserName = request.UserName,
            Email = request.Email,
            EmailConfirmed = true,
            Firstname = request.Firstname,
            SurnamePrefix = request.SurnamePrefix,
            Surname = request.Surname,
            PhoneNumber = request.PhoneNumber,
            Address = ToAddress(request.Address),
            EmergencyAddress = ToAddress(request.EmergencyAddress),
            EmergencyPhoneNumber = request.EmergencyPhoneNumber,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender
        };

        var password = GeneratePassword();
        ThrowIfFailed(await _userManager.CreateAsync(user, password));

        await SendWelcomeMailAsync(user);

        return new CreateUserResponse(ToDto(user), password);
    }

    private async Task SendWelcomeMailAsync(BlueUser user)
    {
        var template = await _db.MailTemplates
            .FirstOrDefaultAsync(x => x.Name == WelcomeMailTemplateName && x.Kind == MailTemplateKind.Transactional);

        if (template is null || string.IsNullOrWhiteSpace(template.DefaultSenderKey))
        {
            _logger.LogWarning(
                "Welcome mail template '{TemplateName}' (with a configured sender) was not found; skipping welcome mail for user {UserId}.",
                WelcomeMailTemplateName, user.Id);
            return;
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return;
        }

        var request = new SendTransactionalMailRequest(
            TemplateId: template.Id,
            SubjectOverride: null,
            BodyMarkdownOverride: null,
            SenderKey: template.DefaultSenderKey,
            Recipients: [new TransactionalRecipient(user.Email, user.Fullname, new Dictionary<string, string>(), user.Id)],
            Cc: [],
            Bcc: [],
            ReplyToOverride: null,
            AttachmentStoredFileIds: []);

        await _mailService.SendTransactionalAsync(request);
    }

    public async Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request)
    {
        var validationContext = new ValidationContext<UpdateUserRequest>(request);
        validationContext.RootContextData["UserId"] = id;
        var validationResult = await _updateValidator.ValidateAsync(validationContext);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var user = await _userManager.FindByIdAsync(id.ToString())
            ?? throw new BlueNotFoundException($"User '{id}' was not found.");

        if (!string.Equals(user.UserName, request.UserName, StringComparison.Ordinal))
        {
            ThrowIfFailed(await _userManager.SetUserNameAsync(user, request.UserName));
        }

        if (!string.Equals(user.Email, request.Email, StringComparison.Ordinal))
        {
            ThrowIfFailed(await _userManager.SetEmailAsync(user, request.Email));
        }

        user.Firstname = request.Firstname;
        user.SurnamePrefix = request.SurnamePrefix;
        user.Surname = request.Surname;
        user.PhoneNumber = request.PhoneNumber;
        user.Address = ToAddress(request.Address);
        user.EmergencyAddress = ToAddress(request.EmergencyAddress);
        user.EmergencyPhoneNumber = request.EmergencyPhoneNumber;
        user.DateOfBirth = request.DateOfBirth;
        user.Gender = request.Gender;

        ThrowIfFailed(await _userManager.UpdateAsync(user));

        return ToDto(user);
    }
    
    public async Task<ApiStatusResponse> ResetUserPasswordAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString())
                   ?? throw new BlueNotFoundException($"User '{id}' was not found.");

        var template = await _db.MailTemplates.FirstAsync(x => x.Name == TransactionalMailTemplateNames.PasswordReset && x.Kind == MailTemplateKind.Transactional);
        
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        
        var newPassword = GeneratePassword();
        await _userManager.ResetPasswordAsync(user, token, newPassword);
        
        if (string.IsNullOrWhiteSpace(template.DefaultSenderKey))
        {
            _logger.LogError(
                "Welcome mail template '{TemplateName}' (with a configured sender) was not found; skipping welcome mail for user {UserId}.",
                WelcomeMailTemplateName, user.Id);
            
            throw new BlueNotFoundException($"Default address used to send the mail could not be found.");
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            throw new  BlueNotFoundException($"User does not have a valid email address.");
        }
        
        var passwordMail = new SendTransactionalMailRequest(
            TemplateId: template.Id,
            SubjectOverride: null,
            BodyMarkdownOverride: null,
            SenderKey: template.DefaultSenderKey,
            Recipients: [new TransactionalRecipient(user.Email, user.Fullname, new Dictionary<string, string>()
            {
                {"Password", newPassword}
            }, user.Id)],
            Cc: [],
            Bcc: [],
            ReplyToOverride: null,
            AttachmentStoredFileIds: []);
        
        await _mailService.SendTransactionalAsync(passwordMail);
        
        return new ApiStatusResponse()
        {
            Success = true
        };
    }

    public async Task DeleteAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString())
            ?? throw new BlueNotFoundException($"User '{id}' was not found.");

        ThrowIfFailed(await _userManager.DeleteAsync(user));
    }

    private static BlueAddress ToAddress(BlueAddressDto dto) =>
        new() { Address = dto.Address, City = dto.City, Zip = dto.Zip };

    private static void ThrowIfFailed(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new BlueValidationException(string.Join(" ", result.Errors.Select(e => e.Description)));
        }
    }

    /// <summary>
    /// Generates a password meeting ASP.NET Identity's default complexity policy (length,
    /// upper/lower/digit/non-alphanumeric) regardless of environment, since it's handed to the
    /// admin to share out of band rather than chosen by the user.
    /// </summary>
    private static string GeneratePassword()
    {
        const string lower = "abcdefghijkmnpqrstuvwxyz";
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string digits = "23456789";
        const string special = "!@#$%^&*?";
        const string all = lower + upper + digits + special;

        var chars = new char[16];
        chars[0] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
        chars[1] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
        chars[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
        chars[3] = special[RandomNumberGenerator.GetInt32(special.Length)];

        for (var i = 4; i < chars.Length; i++)
        {
            chars[i] = all[RandomNumberGenerator.GetInt32(all.Length)];
        }

        for (var i = chars.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }

    private static UserDto ToDto(BlueUser user) =>
        new(
            user.Id,
            user.UserName!,
            user.Email!,
            user.Firstname,
            user.SurnamePrefix,
            user.Surname,
            user.Fullname,
            user.PhoneNumber,
            new BlueAddressDto(user.Address.Address, user.Address.City, user.Address.Zip),
            new BlueAddressDto(user.EmergencyAddress.Address, user.EmergencyAddress.City, user.EmergencyAddress.Zip),
            user.EmergencyPhoneNumber,
            user.DateOfBirth,
            user.Gender,
            user.ProfilePictureFileId);
}
