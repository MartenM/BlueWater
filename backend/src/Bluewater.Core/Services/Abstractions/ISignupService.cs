using Bluewater.Core.Dto.Signup;

namespace Bluewater.Core.Services.Abstractions;

public interface ISignupService
{
    // Member
    Task<List<SignupListItemDto>> ListForMemberAsync();
    Task<SignupDetailDto> GetForMemberAsync(Guid signupId);
    Task<List<SignupListItemDto>> GetMyResponsesAsync();
    Task<SignupResponseDto> SubmitResponseAsync(Guid signupId, SubmitResponseRequest request);
    Task<SignupResponseDto> UpdateMyResponseAsync(Guid signupId, Guid responseId, UpdateResponseRequest request);
    Task DeleteMyResponseAsync(Guid signupId, Guid responseId);

    // Admin
    Task<List<SignupListItemDto>> AdminListAsync();
    Task<SignupAdminDetailDto> AdminGetAsync(Guid signupId);
    Task<SignupAdminDetailDto> AdminCreateAsync(UpsertSignupRequest request);
    Task<SignupAdminDetailDto> AdminUpdateAsync(Guid id, UpsertSignupRequest request);
    Task AdminDeleteAsync(Guid id);
    Task<SignupInputFieldDto> AdminAddFieldAsync(Guid signupId, UpsertSignupInputFieldRequest request);
    Task<SignupInputFieldDto> AdminUpdateFieldAsync(Guid signupId, Guid fieldId, UpsertSignupInputFieldRequest request);
    Task AdminDeleteFieldAsync(Guid signupId, Guid fieldId);
    Task AdminReorderFieldsAsync(Guid signupId, ReorderFieldsRequest request);
    Task<SignupResponseDto> AdminSetReservationAsync(Guid signupId, Guid responseId, SetReservationRequest request);
    Task AdminDeleteResponseAsync(Guid signupId, Guid responseId);
    Task<byte[]> AdminExportCsvAsync(Guid signupId);
}
