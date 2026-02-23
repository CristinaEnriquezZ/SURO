using System.Security.Principal;
using System.Runtime.InteropServices;
using System;

public class NetworkImpersonation : IDisposable
{
    private WindowsImpersonationContext impersonationContext;

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool LogonUser(
        string lpszUsername,
        string lpszDomain,
        string lpszPassword,
        int dwLogonType,
        int dwLogonProvider,
        out IntPtr phToken
    );

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr handle);

    public const int LOGON32_PROVIDER_DEFAULT = 0;
    public const int LOGON32_LOGON_NETWORK = 3;
    public const int LOGON32_LOGON_NEW_CREDENTIALS = 9;

    public NetworkImpersonation(string username, string domain, string password, int logonType)
    {
        IntPtr token = IntPtr.Zero;
        try
        {
            if (LogonUser(username, domain, password, logonType, LOGON32_PROVIDER_DEFAULT, out token))
            {
                WindowsIdentity newId = new WindowsIdentity(token);
                impersonationContext = newId.Impersonate();
            }
            else
            {
                int error = Marshal.GetLastWin32Error();
                throw new ApplicationException($"Login failed with error code: {error}");
            }
        }
        finally
        {
            if (token != IntPtr.Zero)
            {
                CloseHandle(token);
            }
        }
    }

    public void Dispose()
    {
        if (impersonationContext != null)
        {
            impersonationContext.Undo();
            impersonationContext.Dispose();
        }
    }
}