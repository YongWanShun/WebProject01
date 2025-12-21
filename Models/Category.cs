using System;
using System.Collections.Generic;

namespace WebProject.Models;

public partial class Category
{
    public int CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
}
