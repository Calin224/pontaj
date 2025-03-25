using System;

namespace Core.Entities;

public class Pontaj : BaseEntity
{
        public string UserId { get; set; }
        public DateTime Data { get; set; }
        public TimeSpan OraStart { get; set; }
        public TimeSpan OraFinal { get; set; }
        public string NumeProiect { get; set; }
        public bool NormaBaza { get; set; }
}
