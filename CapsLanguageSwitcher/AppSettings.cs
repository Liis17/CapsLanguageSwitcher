namespace CapsLanguageSwitcher
{
    public enum SwitchMethod
    {
        PostMessage,
        SendInput
    }

    public class AppSettings
    {
        public SwitchMethod SwitchMethod { get; set; } = SwitchMethod.PostMessage;
        public string Language { get; set; } = "ru";
    }
}
