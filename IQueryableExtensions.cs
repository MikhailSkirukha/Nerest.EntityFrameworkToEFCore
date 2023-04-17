using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace Nerest.EntityFrameworkToEFCore
{
    public static class IQueryableExtensions
    {
        /// <summary>
        /// The method translates a list of EF-like "include" chains, such as "parent => parent.Children.Select(c => c.Children)",
        /// to Include(parent => parent.Children).ThenInclude(child => child.Children)
        /// and applies to IQuearyable<TEntity>
        /// </summary>
        /// <param name="includes">The list of EF-like "include" chains to be translated to EF Core "includes" and "thenIncludes"</param>
        public static IQueryable<TEntity> ApplyEntityFrameworkIncludes<TEntity>(this IQueryable<TEntity> query, params Expression<Func<TEntity, object>>[] includes)
            where TEntity : class
        {
            IIncludableQueryable<TEntity, object> includesChainQuery = null;

            foreach (var include in includes)
            {
                if (include.Body is MethodCallExpression)
                {
                    var includeParts = SplitExpression(include);
                    includesChainQuery = GetIncludesChainQuery(query, includesChainQuery, includeParts);

                    continue;
                }

                includesChainQuery = GetIncludeQuery(query, includesChainQuery, include);
            }

            var resultQuery = includesChainQuery != null
                ? includesChainQuery.AsQueryable()
                : query;

            return resultQuery;
        }


        private static IReadOnlyCollection<dynamic> SplitExpression(LambdaExpression expression)
        {
            var expressions = new List<dynamic>();

            var expressionBody = (MethodCallExpression)expression.Body;
            foreach (var argument in expressionBody.Arguments)
            {
                if (argument.NodeType == ExpressionType.MemberAccess)
                {
                    var parameter = expression.Parameters.First();
                    var lambdaExpression = Expression.Lambda(argument, parameter);
                    expressions.Add(lambdaExpression);

                    continue;
                }

                if (argument.NodeType == ExpressionType.Lambda)
                {
                    var lambdaExpression = (LambdaExpression)argument;
                    if (lambdaExpression.Body is MemberExpression)
                    {
                        expressions.Add(lambdaExpression);

                        return expressions;
                    }

                    var expressionParts = SplitExpression(lambdaExpression);
                    expressions.AddRange(expressionParts);
                }
            }

            return expressions;
        }

        private static IIncludableQueryable<TEntity, object> GetIncludesChainQuery<TEntity>(
            IQueryable<TEntity> query,
            IIncludableQueryable<TEntity, object> includesChainQuery,
            IReadOnlyCollection<dynamic> expressions) where TEntity : class
        {
            if (expressions.Count == 0)
            {
                return includesChainQuery;
            }

            var firstExpression = expressions.First();
            includesChainQuery = GetIncludeQuery<TEntity>(query, includesChainQuery, firstExpression);
            if (expressions.Count == 1)
            {
                return includesChainQuery;
            }

            foreach(var expression in expressions.Skip(1))
            {
                includesChainQuery = GetThenIncludeQuery<TEntity>(includesChainQuery, expression);
            }

            return includesChainQuery;
        }

        private static IIncludableQueryable<TEntity, object> GetIncludeQuery<TEntity>(IQueryable<TEntity> query, dynamic current, dynamic expression)
            where TEntity : class
        {
            if (current == null)
            {
                current = query;
            }

            return EntityFrameworkQueryableExtensions.Include(current, expression);
        }

        private static IIncludableQueryable<TEntity, object> GetThenIncludeQuery<TEntity>(dynamic current, dynamic expression)
            where TEntity : class
        {
            return EntityFrameworkQueryableExtensions.ThenInclude(current, expression);
        }
    }
}