using Microsoft.EntityFrameworkCore;
using RepositoryPattern.models;

namespace RepositoryPattern
{
    /// <summary>
    /// Represents the database context for the application, inheriting from <see cref="DbContext"/>.
    /// </summary>
    public class MyContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MyContext"/> class.
        /// </summary>
        /// <param name="options">The options to be used by the <see cref="DbContext"/>.</param>
        public MyContext(DbContextOptions<MyContext> options) : base(options) {}

        /// <summary>
        /// Gets or sets the <see cref="DbSet{Treatment}"/> for treatments in the database.
        /// </summary>
        public virtual DbSet<Treatment> Treatments { get; set; }
    }
}