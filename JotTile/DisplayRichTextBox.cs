using System.Windows.Forms;

namespace JotTile
{
    internal sealed class DisplayRichTextBox : RichTextBox
    {
        internal DisplayRichTextBox()
        {
            SetStyle(ControlStyles.Selectable, false);
            TabStop = false;
        }
    }
}
