using System.IO;
using System.Text;
using System.Windows;

namespace DropInpector
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void AddEntry(DragEventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Allowed Effects:");
            sb.AppendLine(e.AllowedEffects.ToString());
            sb.Append("Effects:");
            sb.AppendLine(e.Effects.ToString());
            sb.Append("Key States:");
            sb.AppendLine(e.KeyStates.ToString());

            sb.AppendLine("Formats:");
            Span<byte> buffer = stackalloc byte[256];

            foreach (string format in e.Data.GetFormats(false))
            {
                sb.AppendLine($"  {format}");
                try
                {
                    object? data = e.Data.GetData(format);
                    if (data != null)
                    {
                        sb.AppendLine($"    Type: {data.GetType().FullName}");
                        switch (data)
                        {
                            case MemoryStream ms when format.StartsWith("text/", StringComparison.OrdinalIgnoreCase):
                                using (StreamReader reader = new StreamReader(ms, leaveOpen: true))
                                {
                                    sb.AppendLine($"    Value: {reader.ReadToEnd()}");
                                    ms.Position = 0;
                                }

                                break;
                            case MemoryStream ms:
                                int read = ms.Read(buffer);
                                AppendFormat(sb, buffer[..read]);
                                break;
                            case string stringValue:
                                sb.AppendLine($"    Value: {stringValue}");
                                break;
                            case string[] stringArray:
                                foreach (string s in stringArray)
                                {
                                    sb.AppendLine($"    Value: {s}");
                                }

                                break;
                            default:
                                sb.AppendLine($"    Value: {data}");
                                break;
                        }
                    }
                }
                catch (Exception exception)
                {
                    sb.AppendLine(exception.Message);
                }
            }

            string preview = sb.ToString();

            if (PreviewCurrent.Text == preview)
            {
            }

            PreviewCurrent.Text = preview;
            History.Items.Insert(0, preview);
        }

        private void AppendFormat(StringBuilder sb, Span<byte> span)
        {
            int maxAddress = (int)(Math.Log(span.Length) / Math.Log(16));
            // Write the hex output with address and preview
            Span<char> line = stackalloc char[maxAddress + 3 + (16 * 4)];
            line.Fill(' ');
            Span<char> addressPart = line[..maxAddress];
            Span<char> hexPart = line[(maxAddress + 1)..(maxAddress + 1 + (16 * 3))];
            Span<char> previewPart = line[(maxAddress + 1 + (16 * 3) + 1)..];

            line[maxAddress] = ' ';
            int offset = 0;
            while (!span.IsEmpty)
            {
                offset.TryFormat(addressPart, out int writtenLength, "x");
                addressPart[..writtenLength].CopyTo(addressPart[^writtenLength..]);
                addressPart[..^writtenLength].Fill(' ');
                int lineLength = Math.Min(16, span.Length);
                for (int i = 0; i < lineLength; i++)
                {
                    span[i].TryFormat(hexPart.Slice(i * 3, 2), out _, "X2");
                    previewPart[i] = span[i] >= 32 && span[i] <= 126 ? (char)span[i] : '.';
                }

                if (lineLength < 16)
                {
                    hexPart[(lineLength * 3)..].Fill(' ');
                    previewPart[lineLength..].Fill(' ');
                }

                sb.AppendLine(line.ToString());
                span = span[lineLength..];
                offset++;
            }
        }

        private void UIElement_OnDragEnter(object sender, DragEventArgs e)
        {
            AddEntry(e);
        }

        private void UIElement_OnDragLeave(object sender, DragEventArgs e)
        {
            PreviewCurrent.Text = string.Empty;
        }

        private void UIElement_OnDragOver(object sender, DragEventArgs e)
        {
            AddEntry(e);
        }

        private void UIElement_OnDrop(object sender, DragEventArgs e)
        {
            AddEntry(e);
            PreviewCurrent.Text = string.Empty;
        }
    }
}
