using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities;

public class Fika
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public int Calories { get; set; }
}
