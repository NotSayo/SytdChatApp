namespace Server.Classes;

public class Message
{
    public required string Owner { get; set; }
    public required string Content { get; set; }
    public required DateTime SendDate { get; set; }
}