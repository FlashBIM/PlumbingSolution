using PlumbingSolution.LoginLicense.Enums;
using System;

namespace PlumbingSolution.LoginLicense.Models.User
{
    public class ProductData
    {
        public Guid Id { get; set; }
        public ProductCategoryData Category { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public string Description { get; set; }
        public string Thumbnail { get; set; }
        public ProductType Type { get; set; }
    }

    public enum ProductMode
    {
        Unknown,
        Package1,
        Package2
    }
}