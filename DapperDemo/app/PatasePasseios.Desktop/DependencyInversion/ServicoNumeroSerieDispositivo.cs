using System;
using System.Linq;
using System.Net.NetworkInformation;

namespace PatasePasseios.Desktop.DependencyInversion;

internal sealed class ServicoNumeroSerieDispositivo
{
    public string RecuperarNumeroSerieDispositivo()
    {
        var address = string.Empty;

        var bytes = (from nic in NetworkInterface.GetAllNetworkInterfaces()
                     where nic.OperationalStatus == OperationalStatus.Up &&
                     nic.GetPhysicalAddress().GetAddressBytes().Length > 0
                     select nic.GetPhysicalAddress().GetAddressBytes()).FirstOrDefault();

        if (bytes != null)
        {
            address = BitConverter.ToString(bytes);
        }

        return address;
    }
}