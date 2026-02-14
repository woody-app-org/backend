namespace Woody.Application.DTOs.User
{
    public class UserPostResponseDTO
    {
        public int Id { get; set; }
        public string Username { get; set; } = null!;
        public string ProfilePic { get; set; } = null!;
    }
}