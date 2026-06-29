namespace Dashboard.Domain.ValueObjects;

public record DateRangeFilter(DateTime FromDate, DateTime ToDate)
{
    public static DateRangeFilter Create(DateTime fromDate, DateTime toDate)
    {   
        return new DateRangeFilter(fromDate, toDate);
    }
}