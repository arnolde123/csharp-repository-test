using System;
using System.IO;
using System.Data.SqlClient;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using RepositoryPattern.repositories;
using RepositoryPattern.models;

namespace RepositoryPattern
{
    /// <summary>
    /// The main class for the Repository Pattern application.
    /// </summary>
    class Program
    {
        /// <summary>
        /// The entry point of the application.
        /// </summary>
        /// <param name="args">An array of command-line arguments.</param>
        static void Main(string[] args)
        {
            // Test the pattern
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("global.json");
            string conString = builder.Build().GetConnectionString("DefaultConnection");

            MyContext myContext = MyContextFactory.Create(conString);
            TreatmentRepository tr = new TreatmentRepository(myContext);
            foreach (Treatment item in tr.GetAll())
            {
                Console.WriteLine(item.Text);
            }
            Console.WriteLine(conString);
        }
    }
}