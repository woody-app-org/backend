namespace Woody.Application.Validation;

public static class InputValidationLimits
{
    public const int UsernameMaxLength = 30;
    public const int DisplayNameMaxLength = 80;
    public const int EmailMaxLength = 254;
    public const int PasswordMaxLength = 256;
    public const int CpfMaxLength = 32;
    public const int VerificationCodeMaxLength = 16;
    public const int RefreshTokenMaxLength = 512;

    public const int ProfileBioMaxLength = 500;
    public const int ProfilePronounsMaxLength = 40;
    public const int ProfileLocationMaxLength = 80;
    public const int ProfileProfessionMaxLength = 60;
    public const int ProfileInterestsMaxCount = 12;
    public const int ProfileInterestLabelMaxLength = 40;

    public const int PostTitleMaxLength = 160;
    public const int PostContentMaxLength = 20_000;
    public const int PostTagsMaxCount = 5;
    public const int TagMaxLength = 40;

    public const int CommentContentMaxLength = 5_000;

    public const int CommentGifTitleMaxLength = 200;
    public const int CommentGifExternalIdMaxLength = 128;
    public const int CommentGifProviderMaxLength = 40;

    public const int CommunityNameMinLength = 2;
    public const int CommunityNameMaxLength = 80;
    public const int CommunityDescriptionMinLength = 10;
    public const int CommunityDescriptionMaxLength = 2_000;
    public const int CommunityRulesMaxLength = 4_000;
    public const int CommunityTagsMaxCount = 12;
    public const int CommunityTagMaxLength = 40;

    public const int ReportReasonCodeMaxLength = 80;
    public const int ReportDetailsMaxLength = 2_000;

    public const int PlanCodeMaxLength = 80;
    public const int MembershipRoleMaxLength = 20;
    public const int MembershipStatusMaxLength = 20;

    public const int JoinRequestRejectionReasonMaxLength = 500;
}
