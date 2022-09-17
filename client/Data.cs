using Microsoft.Management.Infrastructure;
using Microsoft.Management.Infrastructure.Generic;

namespace specify_client;

public class Data
{
    private CimSession session;
    private const string DefaultNs = @"root\cimv2";

    public Data()
    {
        session = CimSession.Create(null);
    }

    public CimKeyedCollection<CimProperty> GetWmi()
    {
        return session.GetInstance(DefaultNs, new CimInstance("Win32_OperatingSystem")).CimInstanceProperties;
    }
}
