﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MangaBook.WebApp.Areas.Admin.ViewModel
{
    public class EditRoleViewModel
    {
        public EditRoleViewModel()
        {
            Users = new List<string>();
        }

        public Guid Id { get; set; }

        [Required(ErrorMessage = "Role Name is required")]
        public string RoleName { get; set; }
        public string RoleDescription { get; set; }

        public List<string> Users { get; set; }
    }
}
