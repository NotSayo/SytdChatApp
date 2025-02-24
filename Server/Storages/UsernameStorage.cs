namespace Server.Storages;

public static class UsernameStorage
{
    public static Dictionary<string, string> Storage { get; set; } = new();
}