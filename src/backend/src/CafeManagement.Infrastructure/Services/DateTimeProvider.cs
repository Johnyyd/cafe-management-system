using CafeManagement.Application.Common.Interfaces;

namespace CafeManagement.Infrastructure.Services;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}