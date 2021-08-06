namespace MovementPass.Public.Api.Features.Register
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    using MediatR;

    using Infrastructure;

    public class RegisterRequest : IValidatableObject, IRequest<JwtResult>
    {
        [Required, MaxLength(64)]
        public string Name { get; set; }

        [Required, RegularExpression("^01[3-9]\\d{8}$")]
        public string MobilePhone { get; set; }

        [Required, Range(1001, 1075)]
        public int District { get; set; }

        [Required, Range(10001, 10626)]
        public int Thana { get; set; }

        [Required, DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Required, RegularExpression("^F|M|O$")]
        public string Gender { get; set; }

        [Required, RegularExpression("^NID|DL|PP|BR|EID|SID$")]
        public string IdType { get; set; }

        [Required, MaxLength(64)]
        public string IdNumber { get; set; }

        [Required, DataType(DataType.Url)]
        public string Photo { get; set; }

        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            var diff = Clock.Now().ToUniversalTime() -
                       this.DateOfBirth.ToUniversalTime();
            var years = (new DateTime(1, 1, 1) + diff).Year - 1;

            if (years < 18)
            {
                yield return new ValidationResult(
                    "Age must be 18 or over!",
                    new[] {nameof(this.DateOfBirth)});
            }
        }
    }
}