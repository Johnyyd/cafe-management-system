using CafeManagement.Domain.Shared;

namespace CafeManagement.Domain.Orders;

public record CustomerInfo : IEquatable<CustomerInfo>
{
    public string Name { get; init; } = string.Empty;
    public string Contact { get; init; } = string.Empty; // Phone or email
    public CustomerType Type { get; init; }

    private CustomerInfo() { }

    public CustomerInfo(string name, string contact, CustomerType type)
    {
        Name = name?.Trim() ?? string.Empty;
        Contact = contact?.Trim() ?? string.Empty;
        Type = type;
    }

    public bool IsValid => !string.IsNullOrWhiteSpace(Name) && (!string.IsNullOrWhiteSpace(Contact));
    public bool IsAnonymous => string.IsNullOrWhiteSpace(Name) || Name.Equals("Anonymous", StringComparison.OrdinalIgnoreCase);

    public override string ToString() => $"{Name} ({Type}) - {Contact}";
}

public enum CustomerType
{
    DineIn = 1,
    Takeaway = 2,
    Online = 3
}

public static class CustomerTypeExtensions
{
    public static string GetDisplayName(this CustomerType type) => type switch
    {
        CustomerType.DineIn => "Dine-in",
        CustomerType.Takeaway => "Takeaway",
        CustomerType.Online => "Online",
        _ => "Unknown"
    };
}