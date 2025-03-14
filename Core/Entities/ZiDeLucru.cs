using System;

namespace Core.Entities;

public class ZiDeLucru : BaseEntity
{
    public DateTime Data { get; set; }
    public ICollection<Pontaj> Pontaje { get; set; } = [];
}
