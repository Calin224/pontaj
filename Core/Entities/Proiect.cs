using System;
using System.Text.Json.Serialization;

namespace Core.Entities;

public class Proiect : BaseEntity
{
    public string DenumireaActivitatii { get; set; } = "";
    public string DescriereaActivitatii { get; set; } = "";
    public string PozitiaInProiect { get; set; } = "";
    public string CategoriaExpertului { get; set; } = "";
    public string DenumireBeneficiar { get; set; } = "";
    public string? TitluProiect { get; set; }
    public string? CodProiect { get; set; }

    public string UserId { get; set; } = "";
    [JsonIgnore]
    public AppUser User { get; set; }

    [JsonIgnore]
    public ICollection<Pontaj> Pontaje { get; set; } = [];
}
