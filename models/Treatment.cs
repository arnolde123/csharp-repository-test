using System.ComponentModel.DataAnnotations.Schema;

namespace RepositoryPattern.models
{
    /// <summary>
    /// Represents a treatment entity in the repository.
    /// </summary>
    [Table("treatment")]
    public class Treatment
    {
        /// <summary>
        /// Gets or sets the unique identifier for the treatment.
        /// </summary>
        [Column("treatmentid")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the description of the treatment.
        /// </summary>
        [Column("treatmenttext")]
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the price of the treatment.
        /// </summary>
        [Column("price")]
        public int Price { get; set; }
    }
}