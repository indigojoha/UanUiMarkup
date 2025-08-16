namespace UanUiMarkup;

public class UiParseException : Exception
{
    public int Position { get; }
    public UiParseException(string message, int pos) : base($"{message} (at {pos})")
    {
        Position = pos;
    }
}
