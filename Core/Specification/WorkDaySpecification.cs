using System;
using Core.Entities;

namespace Core.Specification;

public class WorkDaySpecification : BaseSpecification<ZiDeLucru>
{
    public WorkDaySpecification(string userId, DateTime date) : base(x => x.UserId == userId && x.Data.Date == date.Date)
    {
        
    }
}
