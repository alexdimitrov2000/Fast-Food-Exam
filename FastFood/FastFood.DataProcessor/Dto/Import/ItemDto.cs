using System.ComponentModel.DataAnnotations;

namespace FastFood.DataProcessor.Dto.Import
{
    public class ItemDto
    {
        [StringLength(30, MinimumLength = 3)]
        public string Name { get; set; }

        [Range(0.01, (double)decimal.MaxValue)]
        public decimal Price { get; set; }

        [StringLength(30, MinimumLength = 3)]
        public string Category { get; set; }
    }
}
