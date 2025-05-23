using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace RecipeBook.Models
{
    public class UserFollower
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string FollowerId { get; set; }

        [ForeignKey("FollowerId")]
        public ApplicationUser Follower { get; set; }

        [Required]
        public string FollowedId { get; set; }

        [ForeignKey("FollowedId")]
        public ApplicationUser Followed { get; set; }

        public DateTime FollowedOn { get; set; } = DateTime.UtcNow;
    }

}
