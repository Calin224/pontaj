using System;
using System.Text.Json.Serialization;

namespace Core.Entities;

public class ZiDeLucru : BaseEntity
{
    public DateTime Data { get; set; }
    public string UserId { get; set; }
    [JsonIgnore]
    public AppUser User { get; set; } = null!;

    [JsonIgnore]
    public ICollection<Pontaj> Pontaje { get; set; } = [];
}
