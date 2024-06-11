# Nerest.EntityFrameworkToEFCore
This tool will be helpful for creating generic repositories that accept a list of "include" expressions with nested "selects".
By default, this approach works only for Entity Framework, but not for EF Core, which requires "includes" and "thenIncludes".
The tool translates the "EF-like" syntax to EF Core at runtime, allowing you to make your BLL code abstract from EF Core.
It's especially helpful for those who are migrating their projects from EF to EF Core.

## Example
Let's assume you have a UnitOfWork which creates GenericRepository:
```cs
public class GenericRepository: IGenericRepository<TEntity>
{
  public GenericRepository(DbContext context)
  {
    _dbSet = context.Set<TEntity>();
  }
  
  
  public async Task<IReadOnlyCollection<TEntity>> GetAllAsync(params Expression<Func<TEntity, object>>[] includes)
  {   
    return await _dbSet
      .ApplyEntityFrameworkIncludes(includes) //this extension method applies includes in EF Core specific manner
      .ToListAsync();
  }
}
```
and then in BLL:
```cs
var repository = _unitOfWork.GetRepository<User>();
var users = await repository.GetAllAsync(u => u.Friends,
  u => u.Role.Permissions,
  u => u.Posts.Select(p => p.Comments.Select(c => c.Likes))
);
```
In runtime it will be translated into:
```cs
_dbSet.Include(u => u.Friends)
  .Include(u => u.Role.Permissions)
  .Include(u => Posts).ThenInclude(p => p.Comments).ThenInclude(c => c.Likes);
```
