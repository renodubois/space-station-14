using Content.Server.GameTicking;
using Content.Server.Interfaces.GameTicking;
using Content.Shared.Sandbox;
using Robust.Server.Interfaces.Player;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;

namespace Content.Server.Sandbox
{
    internal sealed class SandboxManager : SharedSandboxManager, ISandboxManager
    {
#pragma warning disable 649
        [Dependency] private readonly IPlayerManager _playerManager;
        [Dependency] private readonly IServerNetManager _netManager;
        [Dependency] private readonly IGameTicker _gameTicker;
#pragma warning restore 649

        private bool _isSandboxEnabled;

        public bool IsSandboxEnabled
        {
            get => _isSandboxEnabled;
            set
            {
                _isSandboxEnabled = value;
                UpdateSandboxStatusForAll();
            }
        }

        public void Initialize()
        {
            _netManager.RegisterNetMessage<MsgSandboxStatus>(nameof(MsgSandboxStatus));
            _netManager.RegisterNetMessage<MsgSandboxRespawn>(nameof(MsgSandboxRespawn), SandboxRespawnReceived);

            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
            _gameTicker.OnRunLevelChanged += GameTickerOnOnRunLevelChanged;
        }

        private void GameTickerOnOnRunLevelChanged(GameRunLevelChangedEventArgs obj)
        {
            // Automatically clear sandbox state when round resets.
            if (obj.NewRunLevel == GameRunLevel.PreRoundLobby)
            {
                IsSandboxEnabled = false;
            }
        }

        private void OnPlayerStatusChanged(object sender, SessionStatusEventArgs e)
        {
            if (e.NewStatus != SessionStatus.Connected || e.OldStatus != SessionStatus.Connecting)
            {
                return;
            }

            var msg = _netManager.CreateNetMessage<MsgSandboxStatus>();
            msg.SandboxAllowed = IsSandboxEnabled;
            _netManager.ServerSendMessage(msg, e.Session.ConnectedClient);
        }

        private void SandboxRespawnReceived(MsgSandboxRespawn message)
        {
            if (!IsSandboxEnabled)
            {
                return;
            }

            var player = _playerManager.GetSessionByChannel(message.MsgChannel);
            _gameTicker.Respawn(player);
        }

        private void UpdateSandboxStatusForAll()
        {
            var msg = _netManager.CreateNetMessage<MsgSandboxStatus>();
            msg.SandboxAllowed = IsSandboxEnabled;
            _netManager.ServerSendToAll(msg);
        }
    }
}
