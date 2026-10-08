using System;

namespace PlumbingSolution.LoginLicense.Models.User
{
    public class ProductCategoryData
    {
        public Guid Id { get; set; }
        public string ProductCode { get; set; } = "";
        public string Name { get; set; } = "";
        public string Slug { get; set; } = "";
        public string Description { get; set; } = "";
        public string Thumbnail { get; set; } = "";
    }
}