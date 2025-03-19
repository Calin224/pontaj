using Core.Entities;

namespace Core.Specification;

public class PontajByMonthSpecification : BaseSpecification<Pontaj>
{
    public PontajByMonthSpecification(string userId, DateTime startDate, DateTime endDate) : base(p => p.UserId == userId && p.ZiDeLucru.Data >= startDate && p.ZiDeLucru.Data <= endDate)
    {
        AddInclude(p => p.Proiect);
        AddInclude(z => z.ZiDeLucru);
    }

    public PontajByMonthSpecification(string userId, int year, int month) : base(p => p.UserId == userId && p.ZiDeLucru.Data.Year == year && p.ZiDeLucru.Data.Month == month)
    {
        AddInclude(p => p.Proiect);
        AddInclude(z => z.ZiDeLucru);
    }

    public PontajByMonthSpecification(string userId, DateTime startTime, DateTime endTime, int projectId) : base(p => p.UserId == userId
                                                                                                                && p.ZiDeLucru.Data >= startTime 
                                                                                                                && p.ZiDeLucru.Data <= endTime 
                                                                                                                && (projectId == null || p.Proiect.Id == projectId))
    {
        AddInclude(z => z.ZiDeLucru);
        AddInclude(p => p.Proiect);
    }
}