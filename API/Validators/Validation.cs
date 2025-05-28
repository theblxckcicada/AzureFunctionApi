using System.ComponentModel.DataAnnotations;

namespace EasySMSV2.Shared.Azure.Models
{
    [Obsolete("Use EntityValidator instead")]
    public static class ModelValidation
    {
        public static bool Validate<T>(T obj, out ICollection<ValidationResult> results)
        {
            results = [];

            return Validator.TryValidateObject(obj, new ValidationContext(obj), results, true);
        }

 
        public static bool ValidateEntity<T>(T obj, out string validationMessage)
        {
            validationMessage = string.Empty;
            var isValid = true;

            try
            {
                if (!Validate(obj, out var validationResult))
                {
                    isValid = false;
                    validationMessage = string.Join(
                        "\n",
                        validationMessage,
                        string.Join("\n", validationResult.Select(o => o.ErrorMessage))
                    );
                }
                return isValid;
            }
            catch (Exception ex)
            {
                isValid = false;
                validationMessage = ex.Message;
                return isValid;
            }
        }

        public static bool ValidateEntities<T>(List<T> objects, out string validationMessage)
        {
            validationMessage = string.Empty;
            var isValid = true;

            try
            {
                foreach (var obj in objects)
                {
                    if (!Validate(obj, out var validationResult))
                    {
                        isValid = false;
                        validationMessage = string.Join(
                            "\n",
                            validationMessage,
                            string.Join("\n", validationResult.Select(o => o.ErrorMessage))
                        );
                    }
                }

                return isValid;
            }
            catch (Exception ex)
            {
                isValid = false;
                validationMessage = ex.Message;
                return isValid;
            }
        }
    }
}
