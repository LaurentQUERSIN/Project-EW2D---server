using Stormancer;
using Stormancer.Core;
using Stormancer.Plugins;
using Stormancer.Server.Components;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project_EW2D___server
{
    static class GameSceneExtensions
    {
        public static void AddGameScene(this IAppBuilder builder)
        {
            builder.SceneTemplate("server", scene => new server(scene));
        }
    }

    class server
    {
        private ISceneHost _scene;
        private IEnvironment _env;
        private bool _isRunning = false;
        private uint _ids = 0;
        private Dictionary<uint, Player> _players = new Dictionary<uint, Player>();
        private Dictionary<uint, Bullet> _bullets = new Dictionary<uint, Bullet>(); 

        public  server(ISceneHost scene)
        {
            _scene = scene;
            _env = _scene.GetComponent<IEnvironment>();
            _scene.Connecting.Add(onConnecting);
            _scene.Connected.Add(onConnected);
            _scene.Disconnected.Add(onDisconnected);
            _scene.AddRoute("update_position", onUpdatePosition);
            _scene.AddRoute("chat", onReceivingMessage);
 //         _scene.AddRoute("firing_weapon", onFiringWeapon);
 //         _scene.AddRoute("colliding", onCollising);
 //         _scene.AddRoute("upadte_status", onUpdateStatus);
            _scene.Starting.Add(onStarting);
            _scene.Shuttingdown.Add(onShutdown);
        }

        private Task onStarting(dynamic arg)
        {
            if (_isRunning == false)
            {
                _isRunning = true;
                runGame();
            }
            return Task.FromResult(true);
        }

        private Task onShutdown(ShutdownArgs arg)
        {
            return Task.FromResult(true);
        }

        private Task onConnecting(IScenePeerClient client)
        {
            if (_isRunning == false)
                throw new ClientException("le serveur est vérouillé.");
            else if (_players.Count >= 100)
                throw new ClientException("le serveur est complet.");
            return Task.FromResult(true);
        }

        private Task onConnected(IScenePeerClient client)
        {
            PlayerInfo playerinfo = client.GetUserData<PlayerInfo>();
            _scene.Broadcast("chat", playerinfo.name + " a rejoint le combat !");
            playerinfo.setId(_ids);
            client.Send("getID", s => 
            {
                using (var writer = new BinaryWriter(s, Encoding.UTF8, true))
                    writer.Write(_ids);
            }, PacketPriority.MEDIUM_PRIORITY, PacketReliability.RELIABLE);
            _players.Add(_ids, new Player(playerinfo, _env.Clock));
            _ids++;
            return Task.FromResult(true);
        }

        private Task onDisconnected(DisconnectedArgs arg)
        {
            PlayerInfo player = arg.Peer.GetUserData<PlayerInfo>();
            _scene.Broadcast("chat", player.name + " a quitté le combat ! (" + arg.Reason +")");
            _players.Remove(player.id);
            return Task.FromResult(true);
        }

        private void onReceivingMessage(Packet<IScenePeerClient> packet)
        {
            _scene.Broadcast("chat", packet.Stream);
        }

        private void onUpdatePosition(Packet<IScenePeerClient> packet)
        {
            using (var reader = new BinaryReader(packet.Stream))
            {
                var id = reader.ReadUInt32();
                var x = reader.ReadSingle();
                var y = reader.ReadSingle();
                var rot = reader.ReadChar();
                var vx = reader.ReadSingle();
                var vy = reader.ReadSingle();

                if (_players.ContainsKey(id))
                    _players[id].updatePosition(x, y, rot, vx, vy, _env.Clock);
            }
        }

        private void runGame()
        {
            long lastUpdate = _env.Clock;
            while (_isRunning)
            {
                if (_env.Clock - lastUpdate > 100)
                {
                    lastUpdate = _env.Clock;
                    _scene.Broadcast("update_position", s =>
                    {
                        using (var writer = new BinaryWriter(s, Encoding.UTF8, true))
                        {
                            foreach (Player p in _players.Values)
                            {
                                if (p.lastUpdate < lastUpdate)
                                {
                                    writer.Write(p.id);
                                    writer.Write(p.pos_x);
                                    writer.Write(p.pos_y);
                                    writer.Write(p.rot);
                                    writer.Write(p.vect_x);
                                    writer.Write(p.vect_y);
                                    writer.Write(p.lastUpdate);
                                }
                            }
                        }
                    }, PacketPriority.MEDIUM_PRIORITY, PacketReliability.UNRELIABLE_SEQUENCED);
                }
            }
        }
    }

    
}
