using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Domain.Constants
{
    public static class Permissions
    {
        public const string ViewProduct = "product.view";
        public const string ViewByIdProduct = "product.viewbyid";
        public const string CreateProduct = "product.create";
        public const string UpdateProduct = "product.update";
        public const string DeleteProduct = "product.delete";

        public const string ViewCategory = "category.view";
        public const string ViewByIdCategory = "category.viewbyid";
        public const string CreateCategory = "category.create";
        public const string UpdateCategory = "category.update";
        public const string DeleteCategory = "categories.delete";

        public const string ViewUser = "user.view";
        public const string ViewByIdUser = "user.viewbyid";
        public const string UpdateUser = "user.update";
        public const string DeleteUser = "user.delete";

        public const string ViewRole = "role.view";
        public const string ViewByIdRole = "role.viewbyid";
        public const string CreateRole = "role.create";
        public const string UpdateRole = "role.update";
        public const string DeleteRole = "role.delete";


    }
}
