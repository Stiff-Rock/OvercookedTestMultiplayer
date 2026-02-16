using System;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField hostIpIF;
    [SerializeField] private TMP_InputField hostPortIF;

    [SerializeField] private TMP_InputField clientIpIF;
    [SerializeField] private TMP_InputField clientPortIF;

    private UnityTransport transport;

    private void Start()
    {
        transport = NetworkManager.Singleton.gameObject.GetComponent<UnityTransport>();

        hostIpIF.text = transport.ConnectionData.Address;
        clientIpIF.text = transport.ConnectionData.Address;

        hostPortIF.text = $"{transport.ConnectionData.Port}";
        clientPortIF.text = $"{transport.ConnectionData.Port}";
    }

    public void SetClientData()
    {
        SetConnectionData(clientIpIF, clientPortIF);
    }

    public void SetHostData()
    {
        SetConnectionData(hostIpIF, hostPortIF);
    }

    private void SetConnectionData(TMP_InputField IpIF, TMP_InputField PortIF)
    {
        try
        {
            transport.SetConnectionData(IpIF.text, ushort.Parse(PortIF.text));
        }
        catch (Exception e)
        {
            Debug.LogError($"Could not validate connection data: {e.Message}");
        }
    }
}
