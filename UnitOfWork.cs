using RepositoryPattern.repositories;

namespace RepositoryPattern
{
    /// <summary>
    /// Represents a unit of work for managing database operations.
    /// </summary>
    class UnitOfWork : IDisposable
    {
        private readonly MyContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitOfWork"/> class.
        /// </summary>
        /// <param name="context">The database context to be used by the unit of work.</param>
        public UnitOfWork(MyContext context)
        {
            this._context = context;
            Persons = new TreatmentRepository(context);
        }

        /// <summary>
        /// Gets the repository for managing treatment entities.
        /// </summary>
        public ITreatmentRepository Persons { get; private set; }

        /// <summary>
        /// Saves all changes made in this unit of work to the database.
        /// </summary>
        /// <returns>The number of state entries written to the database.</returns>
        public int Complete()
        {
            return _context.SaveChanges();
        }

        /// <summary>
        /// Releases the resources used by the <see cref="UnitOfWork"/> class.
        /// </summary>
        public void Dispose()
        {
            _context.Dispose();
        }
    }
}