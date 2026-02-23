using System;
using System.Runtime.InteropServices;
using System.Security.Principal;

public class ImpersonationContext : IDisposable
{
    private WindowsImpersonationContext impersonationContext;

    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern bool LogonUser(
        string lpszUsername,
        string lpszDomain,
        string lpszPassword,
        int dwLogonType,
        int dwLogonProvider,
        out IntPtr phToken
    );

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    public static extern bool CloseHandle(IntPtr handle);

    public ImpersonationContext(string username, string domain, string password)
    {
        IntPtr tokenHandle = IntPtr.Zero;
        try
        {
            const int LOGON32_PROVIDER_DEFAULT = 0;
            const int LOGON32_LOGON_NEW_CREDENTIALS = 9;

            if (LogonUser(username, domain, password, LOGON32_LOGON_NEW_CREDENTIALS, LOGON32_PROVIDER_DEFAULT, out tokenHandle))
            {
                using (WindowsIdentity newId = new WindowsIdentity(tokenHandle))
                {
                    impersonationContext = newId.Impersonate();
                }
            }
            else
            {
                int error = Marshal.GetLastWin32Error();
                throw new ApplicationException($"Login failed with error code: {error}");
            }
        }
        finally
        {
            if (tokenHandle != IntPtr.Zero)
            {
                CloseHandle(tokenHandle);
            }
        }
    }

    public void Dispose()
    {
        impersonationContext.Undo();
        impersonationContext.Dispose();
    }
}