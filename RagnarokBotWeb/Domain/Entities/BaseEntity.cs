using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RagnarokBotWeb.Domain.Entities
{
    public abstract class BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public DateTime CreateDate { get; set; }

        protected BaseEntity()
        {
            CreateDate = DateTime.Now;
        }

        public bool IsTransitory()
        {
            return Id == 0;
        }
    }
}
