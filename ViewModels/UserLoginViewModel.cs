using System.ComponentModel.DataAnnotations;
using Orchard.ContentManagement;
using Orchard.Users.Models;

namespace EXPEDIT.Flow.ViewModels
{
    public class UserLoginViewModel
    {
        [Required]
        public string UserName { get; set; }

        [Required, DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [Required, DataType(DataType.Password)]
        [StringLength(50, MinimumLength = 7)]
        public string Password { get; set; }

        public bool RememberMe { get; set; }

    }
}