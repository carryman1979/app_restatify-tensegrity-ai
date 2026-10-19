namespace Restatify.Tensegrity.App.Core;

/// <summary>
/// Eine Chat-Nachricht im Verlauf (NORA = Assistent, IsUser = Nutzer).
/// Klasse statt Record: der WinUI-XAML-Typinfo-Generator kann init-only-Properties
/// nicht zuweisen.
/// </summary>
public sealed class ChatMessage
{
    public ChatMessage(string text, bool isUser)
    {
        Text = text;
        IsUser = isUser;
    }

    public string Text { get; set; }

    public bool IsUser { get; set; }

    public static ChatMessage User(string text) => new(text, true);

    public static ChatMessage Assistant(string text) => new(text, false);
}
