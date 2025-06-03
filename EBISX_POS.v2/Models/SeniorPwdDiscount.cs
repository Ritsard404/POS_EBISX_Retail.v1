using System.ComponentModel.DataAnnotations;

namespace EBISX_POS.Models
{
    public class SeniorPwdDiscount
    {
        public bool IsSenior { get; set; }
        public bool IsPwd { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "OSCA/PWD ID number is required")]
        [StringLength(20, MinimumLength = 5, ErrorMessage = "ID number must be between 5 and 20 characters")]
        public string IdNumber { get; set; } = string.Empty;

        public void Validate()
        {
            if (!IsSenior && !IsPwd)
                throw new ValidationException("Either Senior or PWD must be selected");
            
            if (IsSenior && IsPwd)
                throw new ValidationException("Cannot select both Senior and PWD");
        }
    }
} 