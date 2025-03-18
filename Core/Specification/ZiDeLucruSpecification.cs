using Core.Entities;

namespace Core.Specification;

public class ZiDeLucruSpecification : BaseSpecification<ZiDeLucru>
{
    public ZiDeLucruSpecification(string userId) : base(x => x.UserId == userId)
    {
        
    }

    public ZiDeLucruSpecification(string userId, int year, int month) : base(x => x.UserId == userId && x.Data.Year == year && x.Data.Month == month)
    {
        AddInclude(x => x.Pontaje);
    }
}