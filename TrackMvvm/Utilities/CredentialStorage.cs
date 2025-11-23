using CredentialManagement;

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
            using var cred = new Credential
            {
                Target = CredentialTarget,
                Username = email,
                Password = password,
                Type = CredentialType.Generic,
                PersistanceType = PersistanceType.LocalComputer
            };
            return cred.Save();
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
            using var cred = new Credential { Target = CredentialTarget };
            if (cred.Load())
            {
                return (cred.Username, cred.Password);
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
            using var cred = new Credential { Target = CredentialTarget };
            return cred.Delete();
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
            using var cred = new Credential { Target = CredentialTarget };
            return cred.Exists();
        }
        catch
        {
            return false;
        }
    }
}
