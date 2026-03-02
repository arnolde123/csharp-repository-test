using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Linq.Expressions;

namespace RepositoryPattern.repositories
{
    /// <summary>
    /// Represents a generic repository for accessing and managing entities of type <typeparamref name="TModel"/>.
    /// </summary>
    /// <typeparam name="TModel">The type of the entity.</typeparam>
    public class Repository<TModel> : IRepository<TModel> where TModel : class
    {
        // It is protected, because other child classes have access.
        /// <summary>
        /// Gets the database context associated with the repository.
        /// </summary>
        protected readonly DbContext Context;

        /// <summary>
        /// Initializes a new instance of the <see cref="Repository{TModel}"/> class.
        /// </summary>
        /// <param name="context">The database context to be used by the repository.</param>
        public Repository(DbContext context)
        {
            Context = context;
        }

        /// <summary>
        /// Retrieves an entity by its identifier.
        /// </summary>
        /// <param name="id">The identifier of the entity to retrieve.</param>
        /// <returns>The entity with the specified identifier, or null if not found.</returns>
        public TModel Get(int id)
        {
            return Context.Set<TModel>().Find(id);
        }

        /// <summary>
        /// Retrieves all entities from the repository.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{TModel}"/> containing all entities.</returns>
        public IEnumerable<TModel> GetAll()
        {
            return Context.Set<TModel>().ToList();
        }

        /// <summary>
        /// Finds entities that match the specified predicate.
        /// </summary>
        /// <param name="predicate">The expression used to filter entities.</param>
        /// <returns>An <see cref="IEnumerable{TModel}"/> containing the matching entities.</returns>
        public IEnumerable<TModel> Find(Expression<Func<TModel, bool>> predicate)
        {
            return Context.Set<TModel>().Where(predicate);
        }

        /// <summary>
        /// Adds a new entity to the repository.
        /// </summary>
        /// <param name="model">The entity to add.</param>
        public void Add(TModel model)
        {
            Context.Set<TModel>().Add(model);
        }

        /// <summary>
        /// Adds a range of entities to the repository.
        /// </summary>
        /// <param name="models">The entities to add.</param>
        public void AddRange(IEnumerable<TModel> models)
        {
            Context.Set<TModel>().AddRange(models);
        }

        /// <summary>
        /// Removes an entity from the repository.
        /// </summary>
        /// <param name="model">The entity to remove.</param>
        public void Remove(TModel model)
        {
            Context.Set<TModel>().Remove(model);
        }

        /// <summary>
        /// Removes a range of entities from the repository.
        /// </summary>
        /// <param name="models">The entities to remove.</param>
        public void RemoveRange(IEnumerable<TModel> models)
        {
            Context.Set<TModel>().RemoveRange(models);
        }
    }
}