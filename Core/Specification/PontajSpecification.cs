using System;
using Core.Entities;

namespace Core.Specification;

public class PontajSpecification : BaseSpecification<Pontaj>
{
    public PontajSpecification(int ziDeLucruId) : base(x => x.ZiDeLucruId == ziDeLucruId)
    {
        AddInclude(x => x.Proiect);
    }
}


