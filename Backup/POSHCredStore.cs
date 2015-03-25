// Decompiled with JetBrains decompiler
// Type: VMware.Security.CredentialStore.CredentialStore
// Assembly: VMware.Security.CredentialStore, Version=5.8.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B602E82B-CA33-42BA-BBE5-BCAA5F312917
// Assembly location: C:\Program Files (x86)\VMware\Infrastructure\vSphere PowerCLI\VMware.Security.CredentialStore.dll

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Schema;

namespace POSHCredStore
{
  internal class CredentialStore : ICredentialStore
  {
    private bool _isUsernameCaseSensitive = true;
    private static readonly string s_DefaultCredentialFilePath = "%APPDATA%\\credstore\\credentials.xml";
    private const int s_AquireLockSleepIntervalMilliseconds = 500;
    private const int s_AquireLockTimeoutSeconds = 20;
    private const string s_CredentialElementXPath = "/Credentials/passwordEntry";
    private const string s_CredentialEntryElementName = "passwordEntry";
    private const string s_CredentialsElementXPath = "/Credentials";
    private const string s_HostElementName = "host";
    private const string s_PasswordElementName = "password";
    private const string s_UsernameElementName = "username";
    private const string s_VersionElementName = "version";
    private const string s_VersionElementXPath = "/Credentials/version";
    private const string s_CredentialsElementName = "Credentials";
    private const bool DEFAULT_USERNAME_CASE_SENSITIVITY = true;
    private const string s_LatestDocumentVersionSupported = "2.0";
    private static readonly string s_CredentialFilePath;
    private readonly string _credentialFilePath;
    private bool _objectAlreadyDisposed;

    static CredentialStore()
    {
      string str = ConfigurationManager.AppSettings["DefaultCredentialStoreFilePath"];
      if (!string.IsNullOrEmpty(str))
        CredentialStore.s_DefaultCredentialFilePath = str;
      CredentialStore.s_CredentialFilePath = Environment.ExpandEnvironmentVariables(CredentialStore.s_DefaultCredentialFilePath);
    }

    public CredentialStore()
      : this(new FileInfo(CredentialStore.s_CredentialFilePath), true)
    {
    }

    public CredentialStore(bool isUsernameCaseSensitive)
      : this(new FileInfo(CredentialStore.s_CredentialFilePath), isUsernameCaseSensitive)
    {
    }

    public CredentialStore(FileInfo file)
      : this(file, true)
    {
    }

    public CredentialStore(FileInfo file, bool isUsernameCaseSensitive)
    {
      if (file == null)
        file = new FileInfo(CredentialStore.s_CredentialFilePath);
      this._credentialFilePath = file.FullName;
      this._isUsernameCaseSensitive = isUsernameCaseSensitive;
      if (file.Directory.Exists)
        return;
      string directoryName = Path.GetDirectoryName(Environment.ExpandEnvironmentVariables(CredentialStore.s_DefaultCredentialFilePath));
      if (!file.DirectoryName.Equals(directoryName, StringComparison.OrdinalIgnoreCase))
        throw new DirectoryNotFoundException(file.DirectoryName);
      file.Directory.Create();
    }

    ~CredentialStore()
    {
      this.Dispose(false);
    }

    public bool AddPassword(string host, string username, char[] password)
    {
      if (this._objectAlreadyDisposed)
        throw new ObjectDisposedException("CredentialStore");
      if (string.IsNullOrEmpty(host))
        throw new ArgumentException("Host name cannot be empty.", "host");
      if (string.IsNullOrEmpty(username))
        throw new ArgumentException("User name cannot be empty.", "username");
      if (password == null)
        password = new char[0];
      FileStream fileStream = (FileStream) null;
      bool flag;
      try
      {
        if (!File.Exists(this._credentialFilePath))
        {
          using (File.Create(this._credentialFilePath, 8192, FileOptions.RandomAccess, CredentialStore.GetSecuritySettings()))
            ;
          fileStream = this.OpenFile(FileShare.None);
          CredentialStore.InitializeCredentialsDocument((Stream) fileStream);
        }
        else
          fileStream = this.OpenFile(FileShare.None);
        XmlDocument credentialsXmlDocument = this.LoadCredentialsDocument((Stream) fileStream);
        XmlNode xmlNode = CredentialStore.GetCredentialNode(credentialsXmlDocument, host, username, this._isUsernameCaseSensitive);
        flag = xmlNode == null;
        if (xmlNode == null)
          xmlNode = (XmlNode) credentialsXmlDocument.CreateElement("passwordEntry");
        else
          xmlNode.RemoveAll();
        CredentialStore.FillCredentialNode(xmlNode, host, username, password);
        Array.Clear((Array) password, 0, password.Length);
        credentialsXmlDocument.SelectSingleNode("/Credentials").AppendChild(xmlNode);
        CredentialStore.SaveCredentialsDocument(credentialsXmlDocument, (Stream) fileStream);
      }
      finally
      {
        if (fileStream != null)
          fileStream.Dispose();
      }
      return flag;
    }

    public bool RemovePassword(string host, string username)
    {
      if (this._objectAlreadyDisposed)
        throw new ObjectDisposedException("CredentialStore");
      bool flag = false;
      if (File.Exists(this._credentialFilePath))
      {
        FileStream fileStream = (FileStream) null;
        try
        {
          fileStream = this.OpenFile(FileShare.None);
          XmlDocument credentialsXmlDocument = this.LoadCredentialsDocument((Stream) fileStream);
          XmlNode credentialNode = CredentialStore.GetCredentialNode(credentialsXmlDocument, host, username, this._isUsernameCaseSensitive);
          flag = credentialNode != null;
          if (credentialNode != null)
          {
            credentialsXmlDocument.SelectSingleNode("/Credentials").RemoveChild(credentialNode);
            CredentialStore.SaveCredentialsDocument(credentialsXmlDocument, (Stream) fileStream);
          }
        }
        finally
        {
          if (fileStream != null)
            fileStream.Dispose();
        }
      }
      return flag;
    }

    public void ClearPasswords()
    {
      if (this._objectAlreadyDisposed)
        throw new ObjectDisposedException("CredentialStore");
      if (!File.Exists(this._credentialFilePath))
        return;
      FileStream fileStream = (FileStream) null;
      try
      {
        fileStream = this.OpenFile(FileShare.None);
        XmlDocument credentialsXmlDocument = this.LoadCredentialsDocument((Stream) fileStream);
        XmlNode xmlNode = credentialsXmlDocument.SelectSingleNode("/Credentials");
        foreach (XmlNode oldChild in credentialsXmlDocument.SelectNodes("/Credentials/passwordEntry"))
          xmlNode.RemoveChild(oldChild);
        CredentialStore.SaveCredentialsDocument(credentialsXmlDocument, (Stream) fileStream);
      }
      finally
      {
        if (fileStream != null)
          fileStream.Dispose();
      }
    }

    public IEnumerable<string> GetHosts()
    {
      if (this._objectAlreadyDisposed)
        throw new ObjectDisposedException("CredentialStore");
      Dictionary<string, string> dictionary = new Dictionary<string, string>();
      if (File.Exists(this._credentialFilePath))
      {
        FileStream fileStream = (FileStream) null;
        try
        {
          fileStream = this.OpenFile(FileShare.Read);
          foreach (XmlNode node in this.LoadCredentialsDocument((Stream) fileStream).SelectNodes("/Credentials/passwordEntry"))
          {
            if (CredentialStore.IsValidPasswordEntryNode(node))
            {
              string innerText = node["host"].InnerText;
              string key = innerText.ToLower();
              if (!dictionary.ContainsKey(key))
                dictionary[key] = innerText;
            }
          }
        }
        finally
        {
          if (fileStream != null)
            fileStream.Dispose();
        }
      }
      return (IEnumerable<string>) dictionary.Values;
    }

    public IEnumerable<string> GetUsernames(string host)
    {
      if (this._objectAlreadyDisposed)
        throw new ObjectDisposedException("CredentialStore");
      List<string> list = new List<string>();
      if (File.Exists(this._credentialFilePath))
      {
        FileStream fileStream = (FileStream) null;
        try
        {
          fileStream = this.OpenFile(FileShare.Read);
          foreach (XmlNode node in this.LoadCredentialsDocument((Stream) fileStream).SelectNodes("/Credentials/passwordEntry"))
          {
            if (CredentialStore.IsValidPasswordEntryNode(node) && node["host"].InnerText.Equals(host, StringComparison.OrdinalIgnoreCase))
              list.Add(node["username"].InnerText);
          }
        }
        finally
        {
          if (fileStream != null)
            fileStream.Dispose();
        }
      }
      return (IEnumerable<string>) list;
    }

    public char[] GetPassword(string host, string username)
    {
      if (this._objectAlreadyDisposed)
        throw new ObjectDisposedException("CredentialStore");
      char[] chArray = (char[]) null;
      if (File.Exists(this._credentialFilePath))
      {
        FileStream fileStream = (FileStream) null;
        try
        {
          fileStream = this.OpenFile(FileShare.Read);
          chArray = CredentialStore.GetPasswordInternal(this.LoadCredentialsDocument((Stream) fileStream), host, username, this._isUsernameCaseSensitive);
        }
        finally
        {
          if (fileStream != null)
            fileStream.Dispose();
        }
      }
      return chArray;
    }

    public void Close()
    {
      this.Dispose(true);
      GC.SuppressFinalize((object) this);
    }

    private static void SaveCredentialsDocument(XmlDocument credentialsXmlDocument, Stream credentialFile)
    {
      credentialFile.Position = 0L;
      credentialFile.SetLength(0L);
      credentialsXmlDocument.Save(credentialFile);
    }

    private static void FillCredentialNode(XmlNode element, string host, string username, char[] password)
    {
      XmlElement element1 = element.OwnerDocument.CreateElement("host");
      element1.InnerText = host;
      element.AppendChild((XmlNode) element1);
      XmlElement element2 = element.OwnerDocument.CreateElement("username");
      element2.InnerText = username;
      element.AppendChild((XmlNode) element2);
      XmlElement element3 = element.OwnerDocument.CreateElement("password");
      element3.InnerText = CredentialStore.EncryptPassword(password);
      element.AppendChild((XmlNode) element3);
    }

    private static string EncryptPassword(char[] password)
    {
      return Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(password), (byte[]) null, DataProtectionScope.CurrentUser));
    }

    private static void ValidateCredentialsDocument(XmlDocument credentialsXmlDocument)
    {
      bool flag = true;
      XmlDeclaration xmlDeclaration = (XmlDeclaration) credentialsXmlDocument.FirstChild;
      if (!(flag & xmlDeclaration.Version.StartsWith("1.") & xmlDeclaration.Encoding == "UTF-8" & credentialsXmlDocument.SelectSingleNode("/Credentials") != null & credentialsXmlDocument.SelectSingleNode("/Credentials/version") != null))
        throw new XmlSchemaValidationException("The credentials .xml file is not well formed.");
    }

    private static XmlNode GetCredentialNode(XmlDocument credentialsXmlDocument, string host, string username, bool isUsernameCaseSensitive)
    {
      XmlNode xmlNode = (XmlNode) null;
      foreach (XmlNode node in credentialsXmlDocument.SelectNodes("/Credentials/passwordEntry"))
      {
        if (CredentialStore.IsValidPasswordEntryNode(node) && node["host"].InnerText.Equals(host, StringComparison.OrdinalIgnoreCase))
        {
          string innerText = node["username"].InnerText;
          StringComparison comparisonType = isUsernameCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
          if (string.Equals(innerText, username, comparisonType))
          {
            xmlNode = node;
            break;
          }
        }
      }
      return xmlNode;
    }

    private static char[] GetPasswordInternal(XmlDocument credentialsXmlDocument, string host, string username, bool isUsernameCaseSensitive)
    {
      char[] chArray = (char[]) null;
      XmlNode credentialNode = CredentialStore.GetCredentialNode(credentialsXmlDocument, host, username, isUsernameCaseSensitive);
      if (credentialNode != null)
        chArray = CredentialStore.DecryptPassword(credentialNode["password"].InnerText);
      return chArray;
    }

    private static char[] DecryptPassword(string password)
    {
      return Encoding.UTF8.GetChars(ProtectedData.Unprotect(Convert.FromBase64String(password), (byte[]) null, DataProtectionScope.CurrentUser));
    }

    private static int GetHashCode(string s)
    {
      if (s == null)
        throw new ArgumentNullException("s");
      int num = 0;
      for (int index = 0; index < s.Length; ++index)
        num = 31 * num + (int) s[index];
      return num;
    }

    private static char[] UnobfuscatePassword(string password, string host, string username)
    {
      byte[] array = Convert.FromBase64String(password);
      byte num = (byte) (CredentialStore.GetHashCode(host.ToLower() + username) % 256);
      for (int index = 0; index < array.Length; ++index)
        array[index] ^= num;
      int length = Array.IndexOf<byte>(array, (byte) 0);
      if (length < 0)
        throw new FormatException("Invalid password format. " + string.Format("Host: {0}, Username: {1}", (object) host, (object) username));
      byte[] bytes = new byte[length];
      Array.Copy((Array) array, (Array) bytes, bytes.Length);
      return Encoding.UTF8.GetChars(bytes);
    }

    private static FileSecurity GetSecuritySettings()
    {
      FileSecurity fileSecurity = new FileSecurity();
      fileSecurity.SetAccessRuleProtection(true, false);
      fileSecurity.AddAccessRule((FileSystemAccessRule) fileSecurity.AccessRuleFactory((IdentityReference) new NTAccount(WindowsIdentity.GetCurrent().Name), -1, false, InheritanceFlags.None, PropagationFlags.None, AccessControlType.Allow));
      return fileSecurity;
    }

    private static void InitializeCredentialsDocument(Stream credentialsFile)
    {
      XmlDocument credentialsXmlDocument = new XmlDocument();
      credentialsXmlDocument.AppendChild((XmlNode) credentialsXmlDocument.CreateXmlDeclaration("1.0", "UTF-8", (string) null));
      XmlElement element1 = credentialsXmlDocument.CreateElement("Credentials");
      XmlElement element2 = credentialsXmlDocument.CreateElement("version");
      element2.InnerText = "2.0";
      element1.AppendChild((XmlNode) element2);
      credentialsXmlDocument.AppendChild((XmlNode) element1);
      CredentialStore.SaveCredentialsDocument(credentialsXmlDocument, credentialsFile);
    }

    private XmlDocument LoadCredentialsDocument(Stream credentialsFile)
    {
      XmlDocument xmlDocument = new XmlDocument();
      credentialsFile.Position = 0L;
      try
      {
        Stream input = credentialsFile;
        XmlReaderSettings settings = new XmlReaderSettings()
        {
          CloseInput = false,
          ProhibitDtd = true
        };
        using (XmlReader reader = XmlReader.Create(input, settings))
          xmlDocument.Load(reader);
      }
      catch (XmlException ex)
      {
        throw new XmlException(string.Format("Corrupted credential store file content: \"{0}\".", (object) ((FileStream) credentialsFile).Name));
      }
      CredentialStore.ValidateCredentialsDocument(xmlDocument);
      if (xmlDocument.SelectSingleNode("/Credentials/version").InnerText != "2.0")
        xmlDocument = this.UpdateDocumentVersion(xmlDocument, credentialsFile);
      return xmlDocument;
    }

    private XmlDocument UpdateDocumentVersion(XmlDocument document, Stream stream)
    {
      string innerText = document.SelectSingleNode("/Credentials/version").InnerText;
      if (innerText != "1.0")
        throw new ArgumentException(string.Format("Cannot convert document version {0} to version {1}", (object) innerText, (object) "2.0"));
      XmlDocument credentialsXmlDocument = document;
      try
      {
        XmlDocument xmlDocument = (XmlDocument) credentialsXmlDocument.CloneNode(true);
        xmlDocument.SelectSingleNode("/Credentials/version").InnerText = "2.0";
        XmlNodeList xmlNodeList = xmlDocument.SelectNodes("/Credentials/passwordEntry");
        if (xmlNodeList != null)
        {
          foreach (XmlNode xmlNode in xmlNodeList)
          {
            char[] password = CredentialStore.UnobfuscatePassword(xmlNode["password"].InnerText, xmlNode["host"].InnerText, xmlNode["username"].InnerText);
            xmlNode["password"].InnerText = CredentialStore.EncryptPassword(password);
          }
          credentialsXmlDocument = xmlDocument;
          CredentialStore.SaveCredentialsDocument(credentialsXmlDocument, stream);
        }
      }
      catch
      {
      }
      return credentialsXmlDocument;
    }

    private FileStream OpenFile(FileShare fileShare)
    {
      DateTime dateTime = DateTime.Now.AddSeconds(20.0);
      while (true)
      {
        try
        {
          if (DateTime.Now > dateTime)
          {
            WindowsIdentity current = WindowsIdentity.GetCurrent();
            throw new TimeoutException("Could not aquire access to credential file: " + this._credentialFilePath + ". Either the current user account (" + (current != null ? current.Name : "<no name>") + ") does not have permissions to read the file or another process/thread has locked the file.");
          }
          return File.Open(this._credentialFilePath, FileMode.Open, FileAccess.ReadWrite, fileShare);
        }
        catch (UnauthorizedAccessException ex)
        {
          Thread.Sleep(500);
        }
      }
    }

    private static bool IsValidPasswordEntryNode(XmlNode node)
    {
      return true & node.Name == "passwordEntry" & node["host"] != null & node["username"] != null & node["password"] != null;
    }

    private void Dispose(bool disposing)
    {
      int num = disposing ? 1 : 0;
      this._objectAlreadyDisposed = true;
    }
  }
}
