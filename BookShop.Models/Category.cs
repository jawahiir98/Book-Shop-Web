using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BookShop.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [Length(1, 255)]
        public string Name { get; set; }
        [Required]
        [Range(1, 1000)]
        [DisplayName("Display order")]
        public int DisplayOrder { get; set; }
    }
}
