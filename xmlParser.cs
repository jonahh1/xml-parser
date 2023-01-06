using System.Text.RegularExpressions;

struct xmlElement
{
  public string name;
  public string tagStringData;
  public List<xmlElement> children;
  public List<xmlTag> tags;
  public xmlElement(string name, string stringTags, List<xmlTag> tags)
  {
    children = new List<xmlElement>();
    this.name = name;
    this.tags = tags;
    tagStringData = stringTags;
  }
  public xmlElement()
  {
    children = new List<xmlElement>();
    name = "";
    tags = new List<xmlTag>();
    tagStringData = "";
  }
}

struct xmlTag
{
  public string name;
  public string value;
  public xmlTag(string name, string value)
  {
    this.name = name;
    this.value = value;
  }
}
class XmlParser
{
  public static xmlElement ParseRootXML(string input)
  {
    string xml = Regex.Replace(input, "\t|\r|\n", "");

    xml = Regex.Replace(xml, @"<(\w+)\s*([^>]*)\/>", m => XmlParser.ExpandSelfClosingTag(m.Value));
    
    return XmlParser.ParseXML(xml);
  }
  public static xmlElement ParseXML(string input)
  {
    xmlElement root = new xmlElement();
    bool inTagOpening = false;
    string tagOpening = "";
    bool withinElementPrev = false;
    bool withinElement = false;
    string data = "";

    for (int i = 0; i < input.Length; i++)
    {
      char prevChar = input[Math.Max(0, i - 1)];
      char currChar = input[i];
      char nextChar = input[Math.Min(i + 1, input.Length - 1)];

      if (withinElement)
      {
        bool addToData = true;
        if (currChar == '<' && nextChar == '/')
        {
          string tag = "";

          for (int n = i; n < input.Length; n++)
          {
            if (input[n] == '>') break;
            tag += input[n];
          }
          if (tag == "</" + tagOpening.Split(" ", 2)[0])
          {
            addToData = false;
            withinElement = false;
          }
        }
        if (addToData) data += input[i];
      }
      if (withinElementPrev && !withinElement)
      {
        string[] xmlElementsCode = ExtractTags(data);
        root.tagStringData = data;

        foreach (var element in xmlElementsCode)
        {
          root.children.Add(ParseXML(element));
        }
      }
      if ((currChar == '>' || (currChar == '/' && nextChar == '>')) && inTagOpening)
      {
        if (currChar == '>') withinElement = true;

        string[] attributes = attributes = splitViaSpaces(tagOpening);
        root.name += attributes[0];
        root.tags = getTagsFromStringArray(attributes).ToList();
        inTagOpening = false;
      }
      if (inTagOpening)
      {
        tagOpening += currChar;
      }
      if (currChar == '<' && nextChar != '/' && !withinElement)
      {
        tagOpening = "";
        inTagOpening = true;
      }
      withinElementPrev = withinElement;
    }
    return root;
  }

  public static xmlTag[] getTagsFromStringArray(string[] input)
  {
    xmlTag[] tags = new xmlTag[input.Skip(1).Count()];
    for (int t = 0; t < tags.Length; t++)
    {
      string[] nameAndValue = input.Skip(1).ToArray()[t].Split("=");
      tags[t] = new xmlTag(nameAndValue[0], nameAndValue[1].Substring(1, nameAndValue[1].Length - 2));
    }
    return tags;
  }

  public static string[] splitViaSpaces(string input)
  {
    List<string> output = new List<string>();
    bool withinQuotes = false;
    string currentSnippet = "";
    for (int i = 0; i < input.Length; i++)
    {
      if (input[i] == '"') withinQuotes = !withinQuotes;
      currentSnippet += input[i];
      if ((input[i] == ' ' || i == input.Length - 1) && !withinQuotes)
      {
        if (currentSnippet.EndsWith(' ')) currentSnippet = currentSnippet.Substring(0, currentSnippet.Length - 1);
        output.Add(currentSnippet);
        currentSnippet = "";
      }
    }
    return output.ToArray();
  }

  public static string ExpandSelfClosingTag(string tag)
  {
    // Use a regular expression to capture the tag name and attributes
    Match match = Regex.Match(tag, @"<(\w+)\s*([^>]*)\/>");
    if (match.Success)
    {
      // Extract the tag name and attributes
      string tagName = match.Groups[1].Value;
      string attributes = match.Groups[2].Value;

      // Return the expanded tag
      return $"<{tagName} {attributes}></{tagName}>";
    }
    else return tag; // Return the original tag if it is not a self-closing tag
  }
  public static string[] ExtractTags(string input)
  {
    // turn <b><c/></b><d>hello</d> into {"<b><c/></b>", "<d>hello</d>"}
    List<string> elements = new List<string>();

    bool withinElement = false;
    string currentElementName = "";
    string currentElement = "";
    for (int i = 0; i < input.Length; i++)
    {
      char prevChar = input[Math.Max(0, i - 1)];
      char currChar = input[i];
      char nextChar = input[Math.Min(i + 1, input.Length - 1)];

      if (currChar == '<' && !withinElement)
      {
        currentElementName = "";
        for (int n = i + 1; n < input.Length; n++)
        {
          if (input[n] == '>' || input[n] == ' ') break;
          currentElementName += input[n];
        }
        currentElement = "";
        withinElement = true;
      }
      if (withinElement) currentElement += currChar;
      if (currChar == '<' && nextChar == '/')
      {
        string name = "";
        for (int n = i + 2; n < input.Length; n++)
        {
          if (input[n] == '>' || input[n] == ' ') break;
          name += input[n];
        }

        if (name == currentElementName)
        {
          elements.Add(currentElement + '/' + name + '>');
          currentElement = "";
          withinElement = false;
        }
      }
    }
    return elements.ToArray();
  }
}