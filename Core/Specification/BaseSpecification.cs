using System;
using System.Linq.Expressions;
using Core.Interfaces;

namespace Core.Specification;

public class BaseSpecification<T>(Expression<Func<T, bool>>? criteria) : ISpecification<T>
{
    public BaseSpecification() : this(null)
    {
    }

    public Expression<Func<T, bool>>? Criteria => criteria;

    public List<Expression<Func<T, object>>> Includes { get; private set; } = [];

    protected void AddInclude(Expression<Func<T, object>> includeExpression)
    {
        Includes.Add(includeExpression);
    }
}
