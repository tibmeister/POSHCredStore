// Decompiled with JetBrains decompiler
// Type: VMware.Security.CredentialStore.ICredentialStore
// Assembly: VMware.Security.CredentialStore, Version=5.8.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B602E82B-CA33-42BA-BBE5-BCAA5F312917
// Assembly location: C:\Program Files (x86)\VMware\Infrastructure\vSphere PowerCLI\VMware.Security.CredentialStore.dll

using System.Collections.Generic;

namespace POSHCredStore
{
  public interface ICredentialStore
  {
    char[] GetPassword(string host, string username);

    bool AddPassword(string host, string username, char[] password);

    bool RemovePassword(string host, string username);

    void ClearPasswords();

    IEnumerable<string> GetHosts();

    IEnumerable<string> GetUsernames(string host);

    void Close();
  }
}
