// Decompiled with JetBrains decompiler
// Type: VMware.Security.CredentialStore.CredentialStoreFactory
// Assembly: VMware.Security.CredentialStore, Version=5.8.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B602E82B-CA33-42BA-BBE5-BCAA5F312917
// Assembly location: C:\Program Files (x86)\VMware\Infrastructure\vSphere PowerCLI\VMware.Security.CredentialStore.dll

using System.IO;

namespace POSHCredStore
{
  public class CredentialStoreFactory
  {
    public static ICredentialStore CreateCredentialStore()
    {
      return (ICredentialStore) new CredentialStore();
    }

    public static ICredentialStore CreateCredentialStore(bool isUsernameCaseSensitive)
    {
      return (ICredentialStore) new CredentialStore(isUsernameCaseSensitive);
    }

    public static ICredentialStore CreateCredentialStore(FileInfo file)
    {
      return (ICredentialStore) new CredentialStore(file);
    }

    public static ICredentialStore CreateCredentialStore(FileInfo file, bool isUsernameCaseSensitive)
    {
      if (file == null || file.Directory.Exists)
        return (ICredentialStore) new CredentialStore(file, isUsernameCaseSensitive);
      throw new DirectoryNotFoundException(file.Directory.FullName);
    }
  }
}
