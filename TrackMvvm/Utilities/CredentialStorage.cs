using Meziantou.Framework.Win32;

namespace TrackMvvm.Utilities;

/// <summary>
/// Secure storage for credentials using Windows Credential Manager
/// </summary>
public static class CredentialStorage
{
    private const string CredentialTarget = "TrackMvvm:Supabase";

    /// <summary>
    /// Store email and password securely in Windows Credential Manager
    /// </summary>
    public static bool StoreCredentials(string email, string password)
    {
        try
        {
            CredentialManager.WriteCredential(
                applicationName: CredentialTarget,
                userName: email,
                secret: password,
                comment: "TrackMvvm Supabase Credentials",
                persistence: CredentialPersistence.LocalMachine);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Retrieve stored credentials from Windows Credential Manager
    /// </summary>
    /// <returns>Tuple of (email, password) or null if not found</returns>
    public static (string Email, string Password)? RetrieveCredentials()
    {
        try
        {
            var credential = CredentialManager.ReadCredential(CredentialTarget);
            if (credential != null)
            {
                return (credential.UserName, credential.Password);
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Delete stored credentials from Windows Credential Manager
    /// </summary>
    public static bool DeleteCredentials()
    {
        try
        {
            CredentialManager.DeleteCredential(CredentialTarget);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Check if credentials are stored
    /// </summary>
    public static bool HasStoredCredentials()
    {
        try
        {
            var credential = CredentialManager.ReadCredential(CredentialTarget);
            return credential != null;
        }
        catch
        {
            return false;
        }
    }
}
