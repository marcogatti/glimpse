using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace glimpse.ViewModels
{
    public class UserViewModel
    {
        [Required]
        [DataType(DataType.EmailAddress)]
        [Display(Name = "Email")]
        public String Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public String Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool rememberMe { get; set; }


        public UserViewModel() { }

        public UserViewModel(String email, String password)
        {
            this.Email = email;
            this.Password = password;
        }

    }
}