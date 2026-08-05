using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NiiePay.Entities
{
    [Table("LaiSuatKyHan")]
    public class LaiSuatKyHan
    {
        [Key]
        public int TermMonths { get; set; }
        public decimal InterestRate { get; set; }
    }
}