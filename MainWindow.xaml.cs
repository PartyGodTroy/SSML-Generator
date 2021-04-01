using System;
using System.Linq;
using System.Windows;
using MaterialDesignExtensions.Controls;
using System.Speech.Synthesis;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using System.IO;
using System.Diagnostics;
using System.Windows.Navigation;

/// <summary>
/// Generates SSML files from given Text
/// </summary>
namespace SpeechGenerator
{
   
    /// <summary>
    /// App Window
    /// </summary>
    public partial class MainWindow : MaterialWindow , IDisposable
    {

        /// <summary>
        /// Matches english sentence terminators "." "!" "?"
        /// </summary>
        readonly Regex SentenceMatcher = new Regex(@"(\S.+?[.!?])(?=\s+|$)");
     
        /// <summary>
        /// Placeholder output
        /// </summary>
        readonly string OutPlaceHolder = "<?xml version=\"1.0\"><speak>...Output SSML</speak>";

        /// <summary>
        /// Speech Synthesizer
        /// </summary>
        SpeechSynthesizer Synth;


        /// <summary>
        /// Voices installed on the users machine
        /// </summary>
        ReadOnlyCollection<InstalledVoice> Voices;

        public MainWindow()
        {
            InitializeComponent();

            SSMLOutput.Text = OutPlaceHolder;

            InitSynthesizer();
        }


        /// <summary>
        /// Populates VoiceComboBox with Installed Voices from the SpeechSynthesizer and AudioFormatsComboBox, with voices available 
        /// AudioFormatsComboBox with formats related to each voice
        /// </summary>
        private void InitSynthesizer()
        {
            Synth = new SpeechSynthesizer();
            // Initialize a new instance of the SpeechSynthesizer.  
            Voices = Synth.GetInstalledVoices();
                
            if(Voices is null || Voices.Count == 0)
            {
                Console.WriteLine("No Voices Available");
                return; // Exit 
            }

            /// update ui
            VoiceComboBox.ItemsSource = Voices;
            VoiceComboBox.SelectedIndex = 0;
            VoiceComboBox.DisplayMemberPath = "VoiceInfo.Name";
        }

        /// <summary>
        /// Synthesizes a voice from text
        /// </summary>
        /// <param name="speakMode"></param>
        private void Synthesize()
        {
            Synth.SelectVoice(Voices[VoiceComboBox.SelectedIndex].VoiceInfo.Name);

            string speechText = Util.StringFromRichTextBox(SpeechInputBox);

            string ssml = this.TextToSSML(speechText);

            SSMLOutput.Text = ssml;

            try
            {
                Synth.SpeakAsync(speechText);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        
        /// <summary>
        /// Converts text data to SSML.
        /// </summary>
        /// <param name="textInput"></param>
        /// <returns></returns>
        string TextToSSML(string textInput)
        {
            var xmlOutput = new XElement("speak");

            xmlOutput.SetAttributeValue("version", "1.0");

            // Adds "xml:lang" to doc
            xmlOutput.SetAttributeValue(XNamespace.Xml + "lang", "en-US");

            // paragraphs are seperated a linebreak \n or \r\n
            var paragraphs = textInput.Split(Environment.NewLine.ToCharArray());

            foreach (var p in paragraphs)
            {
                if (p.Length == 0) continue ;

                var paragraphElement = new XElement("p");

                var sentences = SentenceMatcher.Matches(p);

                foreach (var s in sentences)
                {
                    var sentenceText = s.ToString();
                    var punctuation = sentenceText.Last();
                    var pitch = "";
                    switch(punctuation)
                    {
                        case '.':
                            pitch = "medium";
                            break;
                        case '!':
                            pitch = "high";
                            break;
                        case '?':
                            pitch = "x-high";
                            break;
                        default:
                            pitch = "medium";
                            break;
                    }

                    paragraphElement.Add
                    (
                        new XElement("s",
                            new XElement("prosody"
                            , new XText(sentenceText)
                            , new XAttribute("pitch", pitch)
                            )
                        )
                    );
                }
                xmlOutput.Add(new XElement("break"));
                xmlOutput.Add(paragraphElement);
            }
            return "<?xml version=\"1.0\" encoding=\"UTF - 8\"?>" + xmlOutput.ToString();
        }
       

        /// <summary>
        /// Called when the user clicks the run button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void RunVoice(object sender, RoutedEventArgs e)
        {
            Synthesize();
        }

        /// <summary>
        /// Clears the input box when called 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ClearInput(object sender, RoutedEventArgs e)
        {
            SpeechInputBox.Document.Blocks.Clear();
        }

    

        /// <summary>
        /// Copies the SSML output to the clipboard
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void CopyOutputToClipboard(object sender, RoutedEventArgs e)
        {
            SSMLOutput.SelectAll();
            Clipboard.SetText(SSMLOutput.Text);
        }
        

        /// <summary>
        /// Disposes of the Synthesizer
        /// </summary>
        void IDisposable.Dispose()
        {
            Synth.Dispose();
        }
        
        /// <summary>
        /// Called when the user clicks to download the generated SSML txt
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DownloadSSML(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "speech"; // Default file name
            dlg.DefaultExt = ".xml"; // Default file extension
            dlg.Filter = "XML, Speech Text (.xml)|*.ssml"; // Filter files by extension

            Nullable<bool> result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                // Save document
              File.WriteAllText(dlg.FileName, Environment.NewLine + SSMLOutput.Text);
            }
        }

        /// <summary>
        /// Called when my shameless plug is clicked 😂
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            // for .NET Core you need to add UseShellExecute = true
            // see https://docs.microsoft.com/dotnet/api/system.diagnostics.processstartinfo.useshellexecute#property-value
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
