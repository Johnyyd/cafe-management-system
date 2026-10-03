using MongoDB.Bson.Serialization.Attributes;

namespace CafeManagement.Domain.Menu;

public record Availability
{
    public TimeSpan? StartTime { get; init; }
    public TimeSpan? EndTime { get; init; }
    public IReadOnlyList<int> DaysOfWeek { get; init; } = Array.Empty<int>(); // 0 = Sunday, 6 = Saturday

    private Availability() { }

    public Availability(TimeSpan? startTime, TimeSpan? endTime, IEnumerable<int> daysOfWeek)
    {
        StartTime = startTime;
        EndTime = endTime;
        DaysOfWeek = (daysOfWeek ?? Enumerable.Empty<int>()).Distinct().OrderBy(d => d).ToList().AsReadOnly();

        // Validation could be done via factory or constructor
        if (StartTime.HasValue && EndTime.HasValue && StartTime.Value >= EndTime.Value)
            throw new ArgumentException("StartTime must be before EndTime");
        if (DaysOfWeek.Any(d => d < 0 || d > 6))
            throw new ArgumentException("DaysOfWeek must be between 0 (Sunday) and 6 (Saturday)");
    }

    public static Availability AllDay => new(null, null, Enumerable.Range(0, 7));

    public static Availability Create(TimeSpan startTime, TimeSpan endTime, params int[] daysOfWeek) =>
        new(startTime, endTime, daysOfWeek);

    public bool IsAvailableAt(DateTime dateTime)
    {
        var dayOfWeek = (int)dateTime.DayOfWeek;
        var timeOfDay = dateTime.TimeOfDay;

        if (!DaysOfWeek.Contains(dayOfWeek))
            return false;

        if (StartTime.HasValue && timeOfDay < StartTime.Value)
            return false;

        if (EndTime.HasValue && timeOfDay > EndTime.Value)
            return false;

        return true;
    }

    public override string ToString()
    {
        if (!StartTime.HasValue && !EndTime.HasValue && DaysOfWeek.Count == 7)
            return "All day, every day";

        var days = string.Join(", ", DaysOfWeek.Select(d => ((DayOfWeek)d).ToString()));
        var times = StartTime.HasValue && EndTime.HasValue
            ? $"{StartTime.Value:hh\\:mm} - {EndTime.Value:hh\\:mm}"
            : StartTime.HasValue
                ? $"from {StartTime.Value:hh\\:mm}"
                : EndTime.HasValue
                    ? $"until {EndTime.Value:hh\\:mm}"
                    : "any time";

        return $"{times} on {days}";
    }
}