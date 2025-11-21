using System.Globalization;
using System.Windows.Controls;
using System.Windows.Input;

namespace S7Simulator.Wpf.Controls
{
    public class NumericTextBox : TextBox
    {
        public NumericTextBox()
        {
            PreviewTextInput += OnPreviewTextInput;
        }

        private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            string text = textBox.Text.Remove(textBox.SelectionStart, textBox.SelectionLength).Insert(textBox.SelectionStart, e.Text);
            
            if (string.IsNullOrEmpty(text) || text == "-")
            {
                e.Handled = false;
                return;
            }

            bool isValid = float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
            e.Handled = !isValid;
        }
    }
}
