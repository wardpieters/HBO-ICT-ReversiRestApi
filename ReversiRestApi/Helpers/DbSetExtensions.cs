using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace ReversiRestApi.Helpers
{
    public static class DbSetExtensions
    {
        public static void RemoveWhere<TEntity>(this DbSet<TEntity> entities, Expression<Func<TEntity, bool>> predicate) where TEntity : class
        {
            var records = entities
                .Where(predicate)
                .ToList();
            if (records.Count > 0)
                entities.RemoveRange(records);
        }
    }
}