using FluentResults;

namespace CafeManagement.Domain.Common;

public static class ResultExtensions
{
    public static Result<T> ToResult<T>(this T value) => Result.Ok(value);

    public static Result ToResult(this Exception exception) => Result.Fail(exception.Message);

    public static Result<T> ToResult<T>(this Exception exception) => Result.Fail<T>(exception.Message);
}

public static class DomainErrors
{
    public static class General
    {
        public static Error NotFound(string entityName, object id) =>
            new Error($"{entityName} with id '{id}' was not found")
                .WithMetadata("Code", $"{entityName}.NotFound");

        public static Error AlreadyExists(string entityName, string field, object value) =>
            new Error($"{entityName} with {field} '{value}' already exists")
                .WithMetadata("Code", $"{entityName}.AlreadyExists");

        public static Error InvalidOperation(string message) =>
            new Error(message)
                .WithMetadata("Code", "General.InvalidOperation");

        public static Error Unauthorized(string message = "Unauthorized") =>
            new Error(message)
                .WithMetadata("Code", "General.Unauthorized");

        public static Error Forbidden(string message = "Forbidden") =>
            new Error(message)
                .WithMetadata("Code", "General.Forbidden");
    }

    public static class Validation
    {
        public static Error Required(string fieldName) =>
            new Error($"Field '{fieldName}' is required")
                .WithMetadata("Code", "Validation.Required");

        public static Error InvalidFormat(string fieldName, string expectedFormat) =>
            new Error($"Field '{fieldName}' has invalid format. Expected: {expectedFormat}")
                .WithMetadata("Code", "Validation.InvalidFormat");

        public static Error OutOfRange(string fieldName, object min, object max) =>
            new Error($"Field '{fieldName}' must be between {min} and {max}")
                .WithMetadata("Code", "Validation.OutOfRange");

        public static Error InvalidEnumValue(string fieldName, string validValues) =>
            new Error($"Field '{fieldName}' has invalid value. Valid values: {validValues}")
                .WithMetadata("Code", "Validation.InvalidEnum");
    }

    public static class Business
    {
        public static Error InvalidOperation(string message) =>
            new Error(message)
                .WithMetadata("Code", "Business.InvalidOperation");

        public static Error InsufficientStock(string itemName, decimal available, decimal requested) =>
            new Error($"Insufficient stock for '{itemName}'. Available: {available}, Requested: {requested}")
                .WithMetadata("Code", "Business.InsufficientStock");

        public static Error InvalidStatusTransition(string entityName, string fromStatus, string toStatus) =>
            new Error($"Cannot transition {entityName} from '{fromStatus}' to '{toStatus}'")
                .WithMetadata("Code", "Business.InvalidStatusTransition");

        public static Error PaymentRequired(string message = "Payment is required before completing the order") =>
            new Error(message)
                .WithMetadata("Code", "Business.PaymentRequired");

        public static Error OrderCannotBeModified(string reason) =>
            new Error(reason)
                .WithMetadata("Code", "Business.OrderCannotBeModified");

        public static Error ShopNotActive(string shopName) =>
            new Error($"Shop '{shopName}' is not active")
                .WithMetadata("Code", "Business.ShopNotActive");

        public static Error StaffNotActive(string staffName) =>
            new Error($"Staff '{staffName}' is not active")
                .WithMetadata("Code", "Business.StaffNotActive");
    }
}