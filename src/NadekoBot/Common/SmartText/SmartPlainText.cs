namespace NadekoBot
{
    public sealed class SmartPlainText : SmartText
    {
        public string Text { get; set; }

        public SmartPlainText(string text)
        {
            Text = text;
        }

        public static implicit operator SmartPlainText(string input)
            => new SmartPlainText(input);

        public static implicit operator string(SmartPlainText input)
            => input.Text;

        public override string ToString()
        {
            return Text;
        }
    }
}