using System;
using Core.Entities;

namespace Core.Specification;

public class PontajByUserSpecification : BaseSpecification<Pontaj>
{
    public PontajByUserSpecification(string userId)
        : base(p => p.UserId == userId)
    {
        AddOrderBy(p => p.Data);
    }
}

public class PontajByUserAndPeriodSpecification : BaseSpecification<Pontaj>
{
    public PontajByUserAndPeriodSpecification(string userId, DateTime start, DateTime end)
        : base(p => p.UserId == userId && p.Data >= start && p.Data <= end)
    {
        AddOrderBy(p => p.Data);
    }
}

public class PontajByUserAndDateSpecification : BaseSpecification<Pontaj>
{
    public PontajByUserAndDateSpecification(string userId, DateTime date)
        : base(p => p.UserId == userId && p.Data.Date == date.Date)
    {
        AddOrderBy(p => p.OraStart);
    }
}

public class PontajByUserProjectAndPeriodSpecification : BaseSpecification<Pontaj>
{
    public PontajByUserProjectAndPeriodSpecification(string userId, string project, DateTime start, DateTime end)
        : base(p => p.UserId == userId && p.NumeProiect == project && p.Data >= start && p.Data <= end)
    {
        AddOrderBy(p => p.Data);
    }
}