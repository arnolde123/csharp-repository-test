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
        /// <exception cref="System.Exception">Thrown when the database cannot be created or accessed.</exception>
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