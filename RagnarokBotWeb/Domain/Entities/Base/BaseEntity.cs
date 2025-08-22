using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RagnarokBotWeb.Domain.Entities.Base
{
    public abstract class BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public DateTime CreateDate { get; set; }
        public DateTime? UpdateDate { get; set; }


        protected BaseEntity()
        {
            CreateDate = DateTime.UtcNow;
        }

        public bool IsTransitory()
        {
            return Id == 0;
        }
    }
}
