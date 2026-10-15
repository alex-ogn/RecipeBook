using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace RecipeBook.Models
{
    public class UserFollower
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string FollowerId { get; set; } = string.Empty;

        [ForeignKey("FollowerId")]
        public ApplicationUser Follower { get; set; } = null!;

        [Required]
        public string FollowedId { get; set; } = string.Empty;

        [ForeignKey("FollowedId")]
        public ApplicationUser Followed { get; set; } = null!;

        public DateTime FollowedOn { get; set; } = DateTime.UtcNow;
    }

}
