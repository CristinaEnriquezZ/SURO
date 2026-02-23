using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

public sealed class NetworkConnection : IDisposable
{
    private readonly string _networkName;

    public NetworkConnection(string networkName, string username, string password)
    {
        _networkName = networkName;

        var netResource = new NETRESOURCE
        {
            dwType = ResourceType.RESOURCETYPE_DISK,
            lpRemoteName = _networkName
        };

        // Nota: username puede ser "DOMINIO\\usuario" o "SERVIDOR\\usuario" si es local del file server
        var result = WNetAddConnection2(
            netResource,
            password,
            username,
            0);

        if (result != 0 && result != 1219) // 1219 = ya hay una sesión con diferentes credenciales
        {
            throw new Win32Exception(result, $"Error conectando a recurso de red {_networkName}. Código: {result}");
        }
    }

    public void Dispose()
    {
        // Desconecta la sesión (forzar = true)
        WNetCancelConnection2(_networkName, 0, true);
    }

    [DllImport("Mpr.dll")]
    private static extern int WNetAddConnection2(NETRESOURCE netResource, string password, string username, int flags);

    [DllImport("Mpr.dll")]
    private static extern int WNetCancelConnection2(string name, int flags, bool force);

    [StructLayout(LayoutKind.Sequential)]
    private class NETRESOURCE
    {
        public ResourceScope dwScope = ResourceScope.RESOURCE_GLOBALNET;
        public ResourceType dwType = ResourceType.RESOURCETYPE_DISK;
        public ResourceDisplayType dwDisplayType = ResourceDisplayType.RESOURCEDISPLAYTYPE_SHARE;
        public ResourceUsage dwUsage = ResourceUsage.RESOURCEUSAGE_CONNECTABLE;
        public string lpLocalName = null;
        public string lpRemoteName = null;
        public string lpComment = null;
        public string lpProvider = null;
    }

    private enum ResourceScope : int { RESOURCE_CONNECTED = 1, RESOURCE_GLOBALNET = 2, RESOURCE_REMEMBERED = 3, RESOURCE_RECENT = 4, RESOURCE_CONTEXT = 5 }
    private enum ResourceType : int { RESOURCETYPE_ANY = 0, RESOURCETYPE_DISK = 1, RESOURCETYPE_PRINT = 2 }
    private enum ResourceDisplayType : int { RESOURCEDISPLAYTYPE_GENERIC = 0x0, RESOURCEDISPLAYTYPE_DOMAIN = 0x01, RESOURCEDISPLAYTYPE_SERVER = 0x02, RESOURCEDISPLAYTYPE_SHARE = 0x03 }
    [Flags] private enum ResourceUsage : int { RESOURCEUSAGE_CONNECTABLE = 0x00000001, RESOURCEUSAGE_CONTAINER = 0x00000002 }
}