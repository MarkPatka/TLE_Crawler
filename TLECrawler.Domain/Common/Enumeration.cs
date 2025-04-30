using System.Reflection;

namespace TLECrawler.Domain.Common;

public abstract class Enumeration : 
    IComparable, 
    IEquatable<Enumeration>
{
    public int ID { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; } = null;

    protected Enumeration(int id, string name, string? description = null) =>
        (ID, Name, Description) = (id, name, description);

    public static IEnumerable<T> GetAll<T>() where T : Enumeration =>
        typeof(T).GetFields(
            BindingFlags.Public | 
            BindingFlags.Static | 
            BindingFlags.DeclaredOnly)
        .Select(f => f.GetValue(null))
        .Cast<T>();    

    public static T GetFromId<T>(int id)
        where T : Enumeration
    {
        return Parse<T, int>(id, i => i.ID == id);
    }

    private static T Parse<T, K>(K value, Func<T, bool> predicate)
        where T : Enumeration
    {
        return GetAll<T>().FirstOrDefault(predicate)
            ?? throw new ApplicationException($"{value} is not valid item for the type: {typeof(T)}");
    }
    
    public override string ToString() => Name;

    public int CompareTo(object? other)
    {
        if (other == null) return 1;
        return ID.CompareTo(((Enumeration)other).ID);
    }
    public override bool Equals(object? obj)
    {
        if (obj is not Enumeration otherValue || obj is null)
            return false;

        var typeMatch = GetType().Equals(obj.GetType());
        var valueMath = ID.Equals(otherValue.ID);

        return typeMatch && valueMath;
    }
    public bool Equals(Enumeration? other)
    {
        if (other is null) return false;

        var typeMatch = GetType().Equals(other.GetType());
        var valueMath = ID.Equals(other.ID);

        return typeMatch && valueMath;
    }
    public override int GetHashCode() => ID.GetHashCode();
}
