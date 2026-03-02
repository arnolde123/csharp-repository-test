using Microsoft.EntityFrameworkCore;

namespace RepositoryPattern
{
    /// <summary>
    /// A factory to create an instance of the <see cref="MyContext"/>. 
    /// </summary>
    public static class MyContextFactory
    {
        /// <summary>
        /// Creates a new instance of <see cref="MyContext"/> with the specified connection string.
        /// </summary>
        /// <param name="connectionString">The connection string to be used for the database connection.</param>
        /// <returns>A new instance of <see cref="MyContext"/>.</returns>
        /// <remarks>
        /// This method ensures that the database and schema are created if they do not already exist.
        /// </remarks>
        public static MyContext Create(string connectionString)
        {
            var optionsBuilder = new DbContextOptionsBuilder<MyContext>();
            optionsBuilder.UseSqlServer(connectionString);

            // Ensure that the SQLite database and schema is created!
            var context = new MyContext(optionsBuilder.Options);
            context.Database.EnsureCreated();

            return context;
        }
    }
}