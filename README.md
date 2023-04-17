# Nerest.EntityFrameworkToEFCore
This tool will be helpful in order to create generic repositories accepting list of "include" 
expressions with nested "selects". By default this approach work only for Entity Framework 
but not for EF Core which requires "includes" and "thenIncludes". The tool translates the "EF-like" syntax to the EF Core in runtime 
and allows to make your BLL code abstract from the EF Core. Especially helpful for those who migrate their projects from EF to EF Core.

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
