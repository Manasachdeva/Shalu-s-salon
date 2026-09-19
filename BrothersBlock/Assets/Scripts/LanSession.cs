using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace BrothersBlock
{
    public sealed class LanSession : MonoBehaviour
    {
        public static LanSession Instance { get; private set; }
        public event Action Changed;
        public NetworkManager Manager { get; private set; }
        public bool Busy { get; private set; }
        public string Message { get; private set; } = "Same Wi-Fi. Your own little world.";
        public string HostAddress { get; private set; }
        public bool Playing { get { return Manager != null && Manager.IsConnectedClient; } }
        public int Players { get { return Manager == null ? 0 : Manager.ConnectedClientsIds.Count; } }
        private float deadline;
        private bool disconnectPending;
        private string disconnectMessage;
        private readonly HashSet<ulong> reservedSlots = new HashSet<ulong>();

        private void Awake()
        {
            Instance = this;
            if (GetComponent<AdventureDirector>() == null) gameObject.AddComponent<AdventureDirector>();
            Manager = GetComponent<NetworkManager>();
            Manager.ConnectionApprovalCallback = Approve;
            Manager.OnClientConnectedCallback += Connected;
            Manager.OnClientDisconnectCallback += Disconnected;
            Manager.OnTransportFailure += TransportFailed;
            HostAddress = string.Join("  /  ", LanRules.LocalAddresses());
            Application.targetFrameRate = 30;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        public void Host()
        {
            if (Busy || Manager.IsListening || Manager.ShutdownInProgress) return;
            Prepare();
            HostAddress = string.Join("  /  ", LanRules.LocalAddresses());
            Manager.GetComponent<UnityTransport>().SetConnectionData("127.0.0.1", LanRules.Port, "0.0.0.0");
            Message = "Opening your neighbourhood…";
            try { if (!Manager.StartHost()) Fail("Could not host. Leave the room and try again."); }
            catch (Exception exception) { Debug.LogException(exception); Fail("Could not host. Check your Wi-Fi connection."); }
            Notify();
        }

        public void Join(string rawAddress)
        {
            if (Busy || Manager.IsListening || Manager.ShutdownInProgress) return;
            string address;
            if (!LanRules.TryAddress(rawAddress, out address)) { Message = "Enter the host's Wi-Fi address, for example 192.168.1.20."; Notify(); return; }
            Prepare();
            Manager.GetComponent<UnityTransport>().SetConnectionData(address, LanRules.Port);
            Message = "Connecting to " + address + "…";
            try { if (!Manager.StartClient()) Fail("Could not start the connection. Please try again."); }
            catch (Exception exception) { Debug.LogException(exception); Fail("Could not join. Check the host address and Wi-Fi."); }
            Notify();
        }

        private void Prepare()
        {
            Busy = true;
            disconnectPending = false;
            reservedSlots.Clear();
            deadline = Time.unscaledTime + 15f;
            Manager.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(LanRules.Protocol);
        }

        private void Approve(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            // Reserve immediately so two simultaneous joins cannot both take the last slot.
            string refusal = LanRules.Refusal(Manager.ConnectedClientsIds.Count + reservedSlots.Count, request.Payload);
            response.Approved = refusal.Length == 0;
            if (response.Approved) reservedSlots.Add(request.ClientNetworkId);
            response.CreatePlayerObject = response.Approved;
            response.Position = new Vector3(Manager.ConnectedClientsIds.Count == 0 ? -3f : 3f, 1f, -10f);
            response.Rotation = Quaternion.identity;
            response.Reason = refusal;
            response.Pending = false;
        }

        private void Connected(ulong id)
        {
            reservedSlots.Remove(id);
            if (id == Manager.LocalClientId) Busy = false;
            Message = Manager.IsHost ? "Your room is open. Share the address with your brother." : "You're in. Go explore together.";
            Notify();
        }

        private void Disconnected(ulong id)
        {
            reservedSlots.Remove(id);
            if (Manager.IsServer)
            {
                Message = "Your brother left. The room is still open.";
                Notify();
                return;
            }
            disconnectPending = true;
            disconnectMessage = string.IsNullOrEmpty(Manager.DisconnectReason) ? "Connection ended. Check that the host is still in the game." : Manager.DisconnectReason;
        }

        private void TransportFailed()
        {
            disconnectPending = true;
            disconnectMessage = "Network connection failed. Reconnect to the same Wi-Fi and try again.";
        }

        private void Update()
        {
            if (disconnectPending) { disconnectPending = false; Fail(disconnectMessage); }
            if (Busy && Time.unscaledTime > deadline) Fail("Couldn't reach the host. Check the address and that both phones are on the same Wi-Fi.");
        }

        public void Leave() { Fail("Room closed on this phone. Ready for another adventure?"); }

        private void Fail(string message)
        {
            Busy = false;
            disconnectPending = false;
            reservedSlots.Clear();
            Message = message;
            if (Manager.IsListening || Manager.IsClient || Manager.IsServer) Manager.Shutdown();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Notify();
        }

        private void Notify() { if (Changed != null) Changed(); }
        private void OnDestroy()
        {
            if (Manager != null)
            {
                Manager.OnClientConnectedCallback -= Connected;
                Manager.OnClientDisconnectCallback -= Disconnected;
                Manager.OnTransportFailure -= TransportFailed;
                Manager.ConnectionApprovalCallback = null;
            }
            if (Instance == this) Instance = null;
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
        }
    }
}
