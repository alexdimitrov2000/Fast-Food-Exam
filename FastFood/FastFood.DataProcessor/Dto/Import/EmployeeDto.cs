using System.ComponentModel.DataAnnotations;

namespace FastFood.DataProcessor.Dto.Import
{
    public class EmployeeDto
    {
        [StringLength(30, MinimumLength = 3)]
        public string Name { get; set; }

        [Range(15, 80)]
        public int Age { get; set; }

        [StringLength(30, MinimumLength = 3)]
        public string Position { get; set; }
    }
}
