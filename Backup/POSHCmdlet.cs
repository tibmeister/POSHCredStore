using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;

namespace POSHCredStore
{
  [Cmdlet("Get", "CredentialStoreItem", ConfirmImpact = ConfirmImpact.None, SupportsShouldProcess = false)]
  [OutputType(new Type[] {typeof (CredentialStoreItem)})]
  public class GetCredentialStoreItem : PSCmdlet
  {
    private string _file;
    private string _host;
    private string _user;

    [Parameter(Position = 0)]
    [ValidateNotNullOrEmpty]
    public string Host
    {
      [DebuggerStepThrough] get
      {
        return this._host;
      }
      set
      {
        this._host = value;
      }
    }

    [ValidateNotNullOrEmpty]
    [Parameter(Position = 1)]
    public string User
    {
      [DebuggerStepThrough] get
      {
        return this._user;
      }
      set
      {
        this._user = value;
      }
    }

    [Parameter(Position = 2)]
    [ValidateNotNullOrEmpty]
    public string File
    {
      get
      {
        if (string.IsNullOrEmpty(this._file) || Path.IsPathRooted(this._file))
          return this._file;
        return Path.Combine(this.SessionState.Path.CurrentFileSystemLocation.ProviderPath, this._file);
      }
      set
      {
        ProviderInfo provider;
        PSDriveInfo drive;
        this._file = this.SessionState.Path.GetUnresolvedProviderPathFromPSPath(value, out provider, out drive);
        if (!System.IO.File.Exists(this._file))
          throw new FileNotFoundException("Credentials file doesn't exist.", this._file);
      }
    }

    protected override void ProcessRecord()
    {
      FileInfo file = string.IsNullOrEmpty(this.File) ? (FileInfo) null : new FileInfo(this.File);
      string str1 = string.IsNullOrEmpty(this.Host) ? "*" : this.Host;
      WildcardPattern hostPattern = new WildcardPattern(str1, WildcardOptions.Compiled | WildcardOptions.IgnoreCase);
      string str2 = string.IsNullOrEmpty(this.User) ? "*" : this.User;
      WildcardPattern usernamePattern = new WildcardPattern(str2, WildcardOptions.Compiled | WildcardOptions.IgnoreCase);
      List<CredentialStoreItem> list = new List<CredentialStoreItem>();
      try
      {
        ICredentialStore credentialStore = (ICredentialStore) null;
        try
        {
          credentialStore = CredentialStoreFactory.CreateCredentialStore(file);
          foreach (string str3 in credentialStore.GetHosts())
          {
            if (hostPattern.IsMatch(str3))
            {
              foreach (string str4 in credentialStore.GetUsernames(str3))
              {
                if (usernamePattern.IsMatch(str4))
                {
                  string password = new string(credentialStore.GetPassword(str3, str4));
                  list.Add((CredentialStoreItem) new CredentialStoreItemImpl(str3, str4, password, this.File));
                }
              }
            }
          }
        }
        finally
        {
          if (credentialStore != null)
            credentialStore.Close();
        }
      }
      catch (Exception ex)
      {
        this.ThrowTerminatingError(new ErrorRecord(ex, "Core_GetCredentialStoreItem_ProcessRecordget", System.Management.Automation.ErrorCategory.NotSpecified, (object) null));
      }
      this.WriteObject((object) list, true);
      this.ReportErrorIfNotFound(str1, str2, hostPattern, usernamePattern, (IEnumerable<CredentialStoreItem>) list);
    }

    private void ReportErrorIfNotFound(string hostPatternString, string userPatternString, WildcardPattern hostPattern, WildcardPattern usernamePattern, IEnumerable<CredentialStoreItem> result)
    {
      if (!WildcardPattern.ContainsWildcardCharacters(hostPatternString))
      {
        bool flag = false;
        foreach (CredentialStoreItem credentialStoreItem in result)
        {
          if (hostPattern.IsMatch(credentialStoreItem.Host))
          {
            flag = true;
            break;
          }
        }
        if (!flag)
        {
          VimException vimException = new VimException("Core_GetCredentialStoreItem_ProcessRecord_NotFoundByHost", VMware.VimAutomation.Sdk.Types.V1.ErrorHandling.VimException.ErrorCategory.ObjectNotFound, string.Format(Strings.Core_GetCredentialStoreItem_ProcessRecord_NotFoundByHost, (object) hostPatternString), VimExceptionSeverity.Error, (object) null, (Exception) null, (string) null, (string) null);
          this.WriteError(new ErrorRecord((Exception) vimException, vimException.ErrorId, (System.Management.Automation.ErrorCategory) vimException.ErrorCategory, (object) null));
        }
      }
      if (WildcardPattern.ContainsWildcardCharacters(userPatternString))
        return;
      bool flag1 = false;
      foreach (CredentialStoreItem credentialStoreItem in result)
      {
        if (usernamePattern.IsMatch(credentialStoreItem.User))
        {
          flag1 = true;
          break;
        }
      }
      if (flag1)
        return;
      VimException vimException1 = new VimException("Core_GetCredentialStoreItem_ProcessRecord_NotFoundByUser", VMware.VimAutomation.Sdk.Types.V1.ErrorHandling.VimException.ErrorCategory.ObjectNotFound, string.Format(Strings.Core_GetCredentialStoreItem_ProcessRecord_NotFoundByUser, (object) userPatternString), VimExceptionSeverity.Error, (object) null, (Exception) null, (string) null, (string) null);
      this.WriteError(new ErrorRecord((Exception) vimException1, vimException1.ErrorId, (System.Management.Automation.ErrorCategory) vimException1.ErrorCategory, (object) null));
    }
  }

  [Cmdlet("New", "CredentialStoreItem", ConfirmImpact = ConfirmImpact.Medium, SupportsShouldProcess = false)]
  [OutputType(new Type[] {typeof (CredentialStoreItem)})]
  public class NewCredentialStoreItem : PSCmdlet
  {
    private string _file;
    private string _host;
    private string _password;
    private string _user;

    [Parameter(Mandatory = true, Position = 0)]
    [ValidateNotNullOrEmpty]
    public string Host
    {
      [DebuggerStepThrough] get
      {
        return this._host;
      }
      set
      {
        this._host = value;
      }
    }

    [Parameter(Mandatory = true, Position = 1)]
    [ValidateNotNullOrEmpty]
    public string User
    {
      [DebuggerStepThrough] get
      {
        return this._user;
      }
      set
      {
        this._user = value;
      }
    }

    [Parameter(Mandatory = false, Position = 2)]
    public string Password
    {
      [DebuggerStepThrough] get
      {
        return this._password;
      }
      set
      {
        this._password = value;
      }
    }

    [ValidateNotNullOrEmpty]
    [Parameter(Position = 3)]
    public string File
    {
      get
      {
        if (string.IsNullOrEmpty(this._file) || Path.IsPathRooted(this._file))
          return this._file;
        return Path.Combine(this.SessionState.Path.CurrentFileSystemLocation.ProviderPath, this._file);
      }
      set
      {
        ProviderInfo provider;
        PSDriveInfo drive;
        this._file = this.SessionState.Path.GetUnresolvedProviderPathFromPSPath(value, out provider, out drive);
      }
    }

    protected override void ProcessRecord()
    {
      FileInfo file = string.IsNullOrEmpty(this.File) ? (FileInfo) null : new FileInfo(this.File);
      List<CredentialStoreItemImpl> list = new List<CredentialStoreItemImpl>();
      ICredentialStore credentialStore = (ICredentialStore) null;
      try
      {
        credentialStore = CredentialStoreFactory.CreateCredentialStore(file);
        if (this.Password == null)
          this.Password = "";
        credentialStore.AddPassword(this.Host, this.User, this.Password.ToCharArray());
        list.Add(new CredentialStoreItemImpl(this.Host, this.User, new string(credentialStore.GetPassword(this.Host, this.User)), this.File));
      }
      finally
      {
        if (credentialStore != null)
          credentialStore.Close();
      }
      this.WriteObject((object) list, true);
    }
  }
  
  [OutputType(new Type[] {typeof (void)})]
  [Cmdlet("Remove", "CredentialStoreItem", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true)]
  public class RemoveCredentialStoreItem : PSCmdlet
  {
    private const string ParameterSetByFilters = "ByFilters";
    private const string ParameterSetByItemObject = "ByCredentialItemObject";
    private string _file;
    private string _host;
    private CredentialStoreItem[] _items;
    private string _user;

    [ValidateNotNull]
    [Parameter(Mandatory = true, ParameterSetName = "ByCredentialItemObject", Position = 0, ValueFromPipeline = true)]
    public CredentialStoreItem[] CredentialStoreItem
    {
      [DebuggerStepThrough] get
      {
        return this._items;
      }
      set
      {
        this._items = value;
      }
    }

    [Parameter(ParameterSetName = "ByFilters", Position = 0)]
    [ValidateNotNullOrEmpty]
    public string Host
    {
      [DebuggerStepThrough] get
      {
        return this._host;
      }
      set
      {
        this._host = value;
      }
    }

    [Parameter(ParameterSetName = "ByFilters", Position = 1)]
    [ValidateNotNullOrEmpty]
    public string User
    {
      [DebuggerStepThrough] get
      {
        return this._user;
      }
      set
      {
        this._user = value;
      }
    }

    [ValidateNotNullOrEmpty]
    [Parameter(ParameterSetName = "ByFilters", Position = 2)]
    public string File
    {
      get
      {
        if (string.IsNullOrEmpty(this._file) || Path.IsPathRooted(this._file))
          return this._file;
        return Path.Combine(this.SessionState.Path.CurrentFileSystemLocation.ProviderPath, this._file);
      }
      set
      {
        ProviderInfo provider;
        PSDriveInfo drive;
        this._file = this.SessionState.Path.GetUnresolvedProviderPathFromPSPath(value, out provider, out drive);
        if (!System.IO.File.Exists(this._file))
          throw new FileNotFoundException("Credentials file doesn't exist.", this._file);
      }
    }

    protected override void ProcessRecord()
    {
      if (this.ParameterSetName == "ByFilters" && string.IsNullOrEmpty(this.Host) && string.IsNullOrEmpty(this.User))
        this.ThrowTerminatingError(VMware.VimAutomation.Sdk.Util10Ps.ErrorHandling.ExceptionHelper.VimExceptionToErrorRecord(VMware.VimAutomation.Sdk.Util10.ErrorHandling.ExceptionHelper.CreateClientSideException("Core_RemoveCredentialStoreItem_ProcessRecord_InvalidArguemnt", VMware.VimAutomation.Sdk.Types.V1.ErrorHandling.VimException.ErrorCategory.InvalidArgument, (object) null, (Exception) null, typeof (ViError), VMware.VimAutomation.ViCore.Cmdlets.ResourceHelper.ResourceService, (string) null)));
      CredentialStoreItem[] credentialStoreItemArray;
      if (this.ParameterSetName == "ByCredentialItemObject")
        credentialStoreItemArray = this._items;
      else if (this.ParameterSetName == "ByFilters")
      {
        List<CredentialStoreItemImpl> list = new List<CredentialStoreItemImpl>();
        FileInfo file = string.IsNullOrEmpty(this.File) ? (FileInfo) null : new FileInfo(this.File);
        ICredentialStore credentialStore = (ICredentialStore) null;
        try
        {
          credentialStore = CredentialStoreFactory.CreateCredentialStore(file);
          WildcardPattern wildcardPattern1 = new WildcardPattern("*", WildcardOptions.Compiled | WildcardOptions.IgnoreCase);
          if (!string.IsNullOrEmpty(this.Host))
            wildcardPattern1 = new WildcardPattern(this.Host, WildcardOptions.Compiled | WildcardOptions.IgnoreCase);
          WildcardPattern wildcardPattern2 = new WildcardPattern("*", WildcardOptions.Compiled);
          if (!string.IsNullOrEmpty(this.User))
            wildcardPattern2 = new WildcardPattern(this.User, WildcardOptions.Compiled);
          foreach (string str1 in credentialStore.GetHosts())
          {
            if (wildcardPattern1.IsMatch(str1))
            {
              foreach (string str2 in credentialStore.GetUsernames(str1))
              {
                if (wildcardPattern2.IsMatch(str2))
                {
                  string password = new string(credentialStore.GetPassword(str1, str2));
                  list.Add(new CredentialStoreItemImpl(str1, str2, password, this.File));
                }
              }
            }
          }
        }
        finally
        {
          if (credentialStore != null)
            credentialStore.Close();
        }
        credentialStoreItemArray = (CredentialStoreItem[]) list.ToArray();
      }
      else
        credentialStoreItemArray = new CredentialStoreItem[0];
      foreach (CredentialStoreItem credentialStoreItem in credentialStoreItemArray)
      {
        if (this.ShouldProcess(string.Format(VMware.VimAutomation.ViCore.Cmdlets.ResourceHelper.ResourceService.GetString("Core_RemoveCredentialStoreItem_ProcessRecord_Action"), (object) credentialStoreItem.Host, (object) credentialStoreItem.User)))
        {
          FileInfo file = string.IsNullOrEmpty(credentialStoreItem.File) ? (FileInfo) null : new FileInfo(credentialStoreItem.File);
          ICredentialStore credentialStore = (ICredentialStore) null;
          try
          {
            credentialStore = CredentialStoreFactory.CreateCredentialStore(file);
            credentialStore.RemovePassword(credentialStoreItem.Host, credentialStoreItem.User);
          }
          finally
          {
            if (credentialStore != null)
              credentialStore.Close();
          }
        }
      }
    }
  } 
  
}