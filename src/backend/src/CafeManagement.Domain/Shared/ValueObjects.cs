using MongoDB.Bson.Serialization.Attributes;

namespace CafeManagement.Domain.Shared;

public record Address : IEquatable<Address>
{
    public string Street { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string District { get; init; } = string.Empty;
    public string ZipCode { get; init; } = string.Empty;

    private Address() { }

    public Address(string street, string city, string district, string zipCode)
    {
        Street = street?.Trim() ?? string.Empty;
        City = city?.Trim() ?? string.Empty;
        District = district?.Trim() ?? string.Empty;
        ZipCode = zipCode?.Trim() ?? string.Empty;
    }

    public string FullAddress => $"{Street}, {District}, {City} {ZipCode}".Trim(',', ' ');

    public override string ToString() => FullAddress;
}

public record ContactInfo : IEquatable<ContactInfo>
{
    public string Phone { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;

    private ContactInfo() { }

    public ContactInfo(string phone, string email)
    {
        Phone = phone?.Trim() ?? string.Empty;
        Email = email?.Trim()?.ToLowerInvariant() ?? string.Empty;
    }

    public bool IsValid => !string.IsNullOrWhiteSpace(Phone) || !string.IsNullOrWhiteSpace(Email);

    public override string ToString() => $"{Phone} / {Email}";
}

public record OperatingHours : IEquatable<OperatingHours>
{
    public int DayOfWeek { get; init; } // 0 = Sunday, 6 = Saturday
    public TimeSpan OpenTime { get; init; }
    public TimeSpan CloseTime { get; init; }

    private OperatingHours() { }

    public OperatingHours(int dayOfWeek, TimeSpan openTime, TimeSpan closeTime)
    {
        if (dayOfWeek < 0 || dayOfWeek > 6)
            throw new ArgumentException("DayOfWeek must be between 0 (Sunday) and 6 (Saturday)", nameof(dayOfWeek));
        if (openTime >= closeTime)
            throw new ArgumentException("OpenTime must be before CloseTime", nameof(openTime));

        DayOfWeek = dayOfWeek;
        OpenTime = openTime;
        CloseTime = closeTime;
    }

    public bool IsOpenAt(TimeSpan time) => time >= OpenTime && time <= CloseTime;

    public string DayName => DayOfWeek switch
    {
        0 => "Sunday",
        1 => "Monday",
        2 => "Tuesday",
        3 => "Wednesday",
        4 => "Thursday",
        5 => "Friday",
        6 => "Saturday",
        _ => "Unknown"
    };

    public override string ToString() => $"{DayName}: {OpenTime:hh\\:mm} - {CloseTime:hh\\:mm}";
}