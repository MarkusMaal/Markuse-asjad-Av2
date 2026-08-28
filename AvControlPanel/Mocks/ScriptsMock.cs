using System.Xml.Linq;
using AvControlPanel.Models.Menu;

namespace AvControlPanel.Mocks;

public class ScriptsMock
{
    public static ScriptMenu ScriptMenuObject => new(new XDocument(new XDeclaration("1.0", "UTF-8", null),
        new XElement("MasTUI",
            new XElement("MenuItem", new XElement("Title", "Näidis 1"),
                new XElement("Tooltip", "Esimene näidis UI disaineri jaoks"),
                new XElement("Script", new XAttribute("Wait", "False"))),
            new XElement("MenuItem", new XElement("Title", "Näidis 2"),
                new XElement("Tooltip", "Teine näidis UI disaineri jaoks"),
                new XElement("Script", new XAttribute("Wait", "False"))),
            new XElement("MenuItem", new XElement("Title", "Näidis 3"),
                new XElement("Tooltip", "Kolmas näidis UI disaineri jaoks"),
                new XElement("Script", new XAttribute("Wait", "False"))))));
    
    private const string DefaultText = "Siin kuvatakse teave, kui liigutate kursori teatud nupu peale.";
    
    public string TipText { get; set; } = DefaultText;
}