using System;
using Core.Entities;

namespace Core.Specification;

public class ProiectSpecification : BaseSpecification<Proiect>
{
    public ProiectSpecification(string userId) : base(x => x.UserId == userId)
    {
        
    }

    public ProiectSpecification(string userId, int id): base(x => x.Id == id && x.UserId == userId)
    {
        
    }
}
